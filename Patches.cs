using System;
using HarmonyLib;

namespace FishCues
{
    /// <summary>
    /// The bite. FishingFloat.RPC_Nibble only moves m_nibbleTime when it accepts a nibble offered the
    /// right bait, so a changed value on our own float means "hookable right now" - the half second the
    /// vanilla game gives you before the fish takes the bait and leaves. We only watch it happen;
    /// TryToHook itself is untouched, so hooking stays vanilla.
    /// </summary>
    [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.RPC_Nibble))]
    internal static class NibblePatch
    {
        private static void Prefix(FishingFloat __instance, out float __state)
        {
            __state = __instance.m_nibbleTime;
        }

        private static void Postfix(FishingFloat __instance, bool correctBait, float __state)
        {
            try
            {
                if (!correctBait || __instance.m_nibbleTime == __state)
                    return;

                Player player = Player.m_localPlayer;
                if (player != null && FishCuesPlugin.IsMine(__instance, player))
                    FishCuesPlugin.Plink();
            }
            catch (Exception e)
            {
                FishCuesPlugin.WarnOnce("NibblePatch", e);
            }
        }
    }
}
