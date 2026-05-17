using HarmonyLib;
using System;
using System.Reflection;

namespace BeatSaberPlus_MultiplayerLateJoinFix
{
    [HarmonyPatch]
    internal static class RoomMainViewRefreshRoomPatch
    {
        private static MethodBase TargetMethod()
        {
            Type l_RoomMainViewType = Type.GetType("BeatSaberPlus_Multiplayer.UI.MultiplayerPRoomMainView, BeatSaberPlus_Multiplayer");
            return AccessTools.Method(l_RoomMainViewType, "RefreshRoom");
        }

        private static void Postfix(object __instance)
        {
            PracticeSongController.RefreshPracticeButton(__instance);
        }
    }

    [HarmonyPatch]
    internal static class RoomMainViewSelectSongPatch
    {
        private static MethodBase TargetMethod()
        {
            Type l_RoomMainViewType = Type.GetType("BeatSaberPlus_Multiplayer.UI.MultiplayerPRoomMainView, BeatSaberPlus_Multiplayer");
            return AccessTools.Method(l_RoomMainViewType, "OnSelectSongButton");
        }

        private static bool Prefix(object __instance)
        {
            return !PracticeSongController.CanUsePracticeMode() || PracticeSongController.OpenPracticeSelection(__instance);
        }
    }

    [HarmonyPatch]
    internal static class FlowCoordinatorLevelSelectionActionPatch
    {
        private static MethodBase TargetMethod()
        {
            Type l_FlowCoordinatorType = Type.GetType("BeatSaberPlus_Multiplayer.UI.MultiplayerPViewFlowCoordinator, BeatSaberPlus_Multiplayer");
            return AccessTools.Method(l_FlowCoordinatorType, "HandleLevelSelection_ActionButton");
        }

        private static bool Prefix(object __instance)
        {
            return PracticeSongController.TryStartSelectedPracticeSong(__instance);
        }
    }
}
