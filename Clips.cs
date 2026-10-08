using System;
using System.Collections.Generic;
using UnityEngine;

namespace FishCues
{
    /// <summary>
    /// Resolves sound names to the game's own AudioClips, so the mod ships no audio assets at all.
    ///
    /// Synthesising clips in code is not an option here: AudioClip.Create/SetData take Span&lt;T&gt;
    /// overloads in Unity 6, and a net472 compile against the .NET Framework targeting pack has no
    /// System.Span&lt;T&gt;, so it fails with CS0518 before the game is even involved. Using the game's
    /// own sounds is also leaner and blends with the mix instead of fighting it.
    /// </summary>
    internal static class Clips
    {
        private const float RescanSeconds = 15f;

        private static Dictionary<string, AudioClip> _byName;
        private static float _nextScan = -1f;
        private static readonly HashSet<string> Warned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Clip named <paramref name="preferred"/>, else the first fallback that exists, else null.
        /// Names are the game's asset names (the file name without extension), case-insensitive.
        /// </summary>
        public static AudioClip Find(string preferred, params string[] fallbacks)
        {
            Dictionary<string, AudioClip> map = Map();
            if (map == null)
            {
                return null;
            }
            if (!string.IsNullOrWhiteSpace(preferred) && map.TryGetValue(preferred.Trim(), out AudioClip hit))
            {
                return hit;
            }
            if (fallbacks != null)
            {
                foreach (string name in fallbacks)
                {
                    if (map.TryGetValue(name, out hit))
                    {
                        return hit;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Tell the user once per cue. An unknown name must not be silent, because the obvious guess
        /// would then be "the mod is broken" rather than "that sound does not exist".
        /// </summary>
        public static void WarnMissing(string role, string configKey, string preferred)
        {
            if (Warned.Add(role) && FishCuesPlugin.Log != null)
            {
                FishCuesPlugin.Log.LogWarning(
                    $"FishCues: no game sound called '{preferred}', so the {role} cue is silent. " +
                    $"Set {configKey} to any sound name from the game.");
            }
        }

        /// <summary>
        /// FindObjectsOfTypeAll is a full resource sweep, so build the index once and only rebuild it
        /// while it is still empty - early in the main menu the audio assets are not loaded yet.
        /// </summary>
        private static Dictionary<string, AudioClip> Map()
        {
            if (_byName != null && _byName.Count > 0)
            {
                return _byName;
            }
            if (Time.time < _nextScan)
            {
                return null;
            }
            _nextScan = Time.time + RescanSeconds;

            AudioClip[] all = Resources.FindObjectsOfTypeAll<AudioClip>();
            if (all == null || all.Length == 0)
            {
                return null;
            }
            var map = new Dictionary<string, AudioClip>(all.Length, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < all.Length; i++)
            {
                AudioClip clip = all[i];
                if (clip != null && !string.IsNullOrEmpty(clip.name) && !map.ContainsKey(clip.name))
                {
                    map[clip.name] = clip;
                }
            }
            _byName = map;
            FishCuesPlugin.Log?.LogDebug($"FishCues: indexed {map.Count} game sounds.");
            return map;
        }
    }
}
