using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BeatSaberPlus_MultiplayerLateJoinFix
{
    [HarmonyPatch]
    internal static class NetworkManagerPatches
    {
        private static readonly string[] s_TargetMethodNames =
        {
            "Handle_SMsgRoomJoinResult",
            "Handle_SMsgRoomState",
            "Handle_SMsgRoomUpdated",
            "Handle_SMsgRoomPlayerState",
            "Handle_SMsgRoomStopLevel"
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

        private static void Postfix()
        {
            LateJoinSpectateController.Sync();
            PracticeSongController.Sync();
        }
    }
}
