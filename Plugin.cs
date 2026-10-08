using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace FishCues
{
    /// <summary>
    /// Two audio cues, nothing else: a plink when a fish bites, and a tone for as long as the hooked
    /// fish is struggling - silence means it is calm, so you never have to look at the float.
    /// Read-only: it patches nothing that touches stamina, line length or catches, so fishing stays
    /// vanilla and it is safe on vanilla servers.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class FishCuesPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "online.buddycloud.fishcues";
        public const string PluginName = "FishCues";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;
        internal static FishCuesPlugin Self;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<string> PlinkClip;
        internal static ConfigEntry<float> PlinkVolume;
        internal static ConfigEntry<string> StruggleClip;
        internal static ConfigEntry<float> StruggleVolume;
        internal static ConfigEntry<float> StrugglePitch;

        private AudioSource _plinkSource;
        private AudioSource _toneSource;
        private string _plinkName;
        private string _struggleName;

        // Used when a configured name does not exist, so a typo silences a cue rather than killing it.
        private static readonly string[] PlinkFallbacks =
            { "Splash_Water_Small3", "Splash_Water_Small1", "UI_Craft_Finish_01" };
        private static readonly string[] StruggleFallbacks =
            { "Tar_Bubble_Loop", "Boil_CauldronBubble_Loop", "Amb_Caves_WaterDrop_01" };
        private static readonly HashSet<string> Warned = new HashSet<string>();

        private void Awake()
        {
            Log = Logger;
            Self = this;

            Enabled = Config.Bind("General", "Enabled", true,
                "Play the fishing audio cues.");
            PlinkClip = Config.Bind("General", "PlinkClip", "Splash_Water_Small2",
                "Name of any vanilla game sound to play when a fish bites. Defaults to a small water splash - wet rather than alarming, and it belongs to fishing rather than to the crafting menu. Free text: the game holds thousands of sounds.");
            PlinkVolume = Config.Bind("General", "PlinkVolume", 0.15f,
                "Volume of the 'plink' played when a fish bites (0 disables just the plink). The splash peaks near 0 dBFS so it reads louder than the number: 0.3 is +6 dB, 0.1 -3.5 dB.");
            StruggleClip = Config.Bind("General", "StruggleClip", "Items_Bathtub_Bubbles_Loop",
                "Looped sound played while the hooked fish struggles. Defaults to a bathtub's bubbles: 21 s long, so no audible cycle, and the bathtub is the only thing in the game that plays it - you will never hear it by accident while fishing.");
            StruggleVolume = Config.Bind("General", "StruggleVolume", 0.25f,
                "Volume of the tone played while the hooked fish struggles (0 disables just the tone). Silence means the fish is calm.");
            StrugglePitch = Config.Bind("General", "StrugglePitch", 1.0f,
                "Pitch of the struggle tone, 0.5 to 2. Raise it if the tone hides under water and wind.");

            Harmony.CreateAndPatchAll(typeof(FishCuesPlugin).Assembly, PluginGuid);
            Log.LogInfo($"{PluginName} {PluginVersion}: plink on the bite, tone while the fish struggles.");
        }

        private void Update()
        {
            try
            {
                EnsureSources();

                if (!Enabled.Value || StruggleVolume.Value <= 0f)
                {
                    StopTone();
                    return;
                }

                Player player = Player.m_localPlayer;
                if (player == null)
                {
                    StopTone();
                    return;
                }

                Fish fish = MyCatch(player);
                if (fish != null && fish.IsEscaping())
                    StartTone();
                else
                    StopTone();
            }
            catch (Exception e)
            {
                WarnOnce("Update", e);
            }
        }

        private void OnDestroy()
        {
            StopTone();
        }

        private void EnsureSources()
        {
            if (_plinkSource == null)
            {
                _plinkSource = gameObject.AddComponent<AudioSource>();
                _plinkSource.playOnAwake = false;
                _plinkSource.loop = false;
                _plinkSource.spatialBlend = 0f;          // 2D: we are the only listener
            }
            if (_toneSource == null)
            {
                _toneSource = gameObject.AddComponent<AudioSource>();
                _toneSource.playOnAwake = false;
                _toneSource.loop = true;
                _toneSource.spatialBlend = 0f;
            }
            RefreshClips();
            // The mixer only exists after the main menu is up; keep trying until it is.
            if (AudioMan.instance != null)
            {
                if (_plinkSource.outputAudioMixerGroup == null)
                    _plinkSource.outputAudioMixerGroup = AudioMan.instance.m_guiMixer;
                if (_toneSource.outputAudioMixerGroup == null)
                    _toneSource.outputAudioMixerGroup = AudioMan.instance.m_guiMixer;
            }
        }

        /// <summary>
        /// Look the configured sounds up in the game's own clips, and redo it if a name is edited in the
        /// config while playing. Missing names leave that one cue silent, with one warning in the log.
        /// </summary>
        private void RefreshClips()
        {
            if (_plinkSource.clip == null ||
                !string.Equals(_plinkName, PlinkClip.Value, StringComparison.OrdinalIgnoreCase))
            {
                _plinkName = PlinkClip.Value;
                _plinkSource.clip = Clips.Find(_plinkName, PlinkFallbacks);
                if (_plinkSource.clip == null)
                {
                    Clips.WarnMissing("bite", "PlinkClip", _plinkName);
                }
            }
            if (_toneSource.clip == null ||
                !string.Equals(_struggleName, StruggleClip.Value, StringComparison.OrdinalIgnoreCase))
            {
                _struggleName = StruggleClip.Value;
                _toneSource.clip = Clips.Find(_struggleName, StruggleFallbacks);
                if (_toneSource.clip == null)
                {
                    Clips.WarnMissing("struggle", "StruggleClip", _struggleName);
                }
            }
        }

        private void StartTone()
        {
            if (_toneSource.clip == null)
            {
                return;
            }
            _toneSource.volume = Mathf.Clamp01(StruggleVolume.Value);
            _toneSource.pitch = Mathf.Clamp(StrugglePitch.Value, 0.5f, 2f);
            if (!_toneSource.isPlaying)
            {
                _toneSource.Play();
                Log.LogDebug("Struggle tone on.");
            }
        }

        private void StopTone()
        {
            if (_toneSource != null && _toneSource.isPlaying)
            {
                _toneSource.Stop();
                Log.LogDebug("Struggle tone off.");
            }
        }

        /// <summary>Our float's hooked fish, or null. Never another player's - see IsMine.</summary>
        private static Fish MyCatch(Player player)
        {
            foreach (FishingFloat ff in FishingFloat.GetAllInstances())
            {
                if (!IsMine(ff, player))
                    continue;
                Fish fish = ff.GetCatch();
                if (fish != null)
                    return fish;
            }
            return null;
        }

        /// <summary>
        /// The float's ZDO stores who cast it, which is how the game itself decides ownership. Without
        /// this we would also cue on fish biting someone else's float on a multiplayer server.
        /// </summary>
        internal static bool IsMine(FishingFloat ff, Player player)
        {
            if (ff == null || ff.m_nview == null || !ff.m_nview.IsValid())
                return false;
            return ff.m_nview.GetZDO().GetLong(ZDOVars.s_rodOwner, 0L) == player.GetZDOID().UserID;
        }

        internal static void Plink()
        {
            try
            {
                FishCuesPlugin self = Self;
                if (self == null || !Enabled.Value || PlinkVolume.Value <= 0f)
                    return;
                self.EnsureSources();
                if (self._plinkSource != null && self._plinkSource.clip != null)
                {
                    self._plinkSource.PlayOneShot(self._plinkSource.clip, Mathf.Clamp01(PlinkVolume.Value));
                    Log.LogDebug("Bite plink.");
                }
            }
            catch (Exception e)
            {
                WarnOnce("Plink", e);
            }
        }

        /// <summary>Log a given failure once, not sixty times a second.</summary>
        internal static void WarnOnce(string key, Exception e)
        {
            if (!Warned.Add(key))
                return;
            Log.LogWarning($"FishCues: {key} failed ({e.GetType().Name}: {e.Message}). That cue is disabled; the game is unaffected.");
            Log.LogDebug(e.ToString());
        }
    }
}
