using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BeatSaberPlus_MultiplayerLateJoinFix
{
    [HarmonyPatch]
    internal static class GameplayManagerPracticeGuardPatches
    {
        private static readonly string[] s_TargetMethodNames =
        {
            "Logic_OnLevelStarted",
            "Logic_OnLevelEnded"
        };

        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type l_GameplayManagerType = Type.GetType("BeatSaberPlus_Multiplayer.Managers.GameplayManager, BeatSaberPlus_Multiplayer");
            if (l_GameplayManagerType == null)
                yield break;

            for (int l_I = 0; l_I < s_TargetMethodNames.Length; l_I++)
            {
                MethodInfo l_Method = AccessTools.Method(l_GameplayManagerType, s_TargetMethodNames[l_I]);
                if (l_Method != null)
                    yield return l_Method;
            }
        }

        private static bool Prefix()
        {
            return !PracticeSongController.IsPracticeRunning;
        }
    }

    [HarmonyPatch]
    internal static class NetworkManagerPracticeScoreGuardPatches
    {
        private static readonly string[] s_TargetMethodNames =
        {
            "Logic_LocalPlayerScore_Update",
            "Logic_LocalPlayerScore_Set",
            "RoomPlayerScore"
        };

        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type l_NetworkManagerType = Type.GetType("BeatSaberPlus_Multiplayer.Network.NetworkManager, BeatSaberPlus_Multiplayer");
            if (l_NetworkManagerType == null)
                yield break;

            for (int l_I = 0; l_I < s_TargetMethodNames.Length; l_I++)
            {
                MethodInfo l_Method = AccessTools.Method(l_NetworkManagerType, s_TargetMethodNames[l_I]);
                if (l_Method != null)
                    yield return l_Method;
            }
        }

        private static bool Prefix()
        {
            PracticeSongController.Sync();
            return !PracticeSongController.IsPracticeRunning;
        }
    }
}
