using CP_SDK.Unity;
using CP_SDK_BS.Game;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BeatSaberPlus_MultiplayerLateJoinFix
{
    internal static class PracticeSongController
    {
        private const string SelectingSongState = "SelectingSong";
        private const string WarmingUpState = "WarmingUp";
        private const string PlayingState = "Playing";
        private const string ResultsState = "Results";

        private static readonly Type s_NetworkManagerType = Type.GetType("BeatSaberPlus_Multiplayer.Network.NetworkManager, BeatSaberPlus_Multiplayer");
        private static readonly PropertyInfo s_RoomDataProperty = s_NetworkManagerType?.GetProperty("RoomData", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly PropertyInfo s_SelfPlayerProperty = s_NetworkManagerType?.GetProperty("SelfPlayer", BindingFlags.Static | BindingFlags.NonPublic);

        private static object s_LastFlowCoordinator;
        private static bool s_PracticeRunning;
        private static bool s_PracticeExitRequested;

        internal static bool IsPracticeRunning => s_PracticeRunning;

        internal static bool CanUsePracticeMode()
        {
            object l_RoomData = s_RoomDataProperty?.GetValue(null);
            object l_SelfPlayer = s_SelfPlayerProperty?.GetValue(null);
            if (l_RoomData == null || l_SelfPlayer == null)
                return false;

            string l_RoomState = GetFieldValue(l_RoomData, "State")?.ToString();
            if (l_RoomState != WarmingUpState && l_RoomState != PlayingState && l_RoomState != ResultsState)
                return false;

            uint l_HostLUID = GetUIntField(l_RoomData, "HostLUID");
            uint l_PlayerLUID = GetUIntField(l_SelfPlayer, "LUID");
            return l_HostLUID != l_PlayerLUID;
        }

        internal static void RefreshPracticeButton(object p_RoomMainView)
        {
            object l_Button = AccessTools.Field(p_RoomMainView.GetType(), "m_SelectSongButton")?.GetValue(p_RoomMainView);
            if (l_Button == null)
                return;

            if (CanUsePracticeMode())
            {
                Invoke(l_Button, "SetText", "Play\nSolo");
                Invoke(l_Button, "SetInteractable", true);
            }
            else
            {
                Invoke(l_Button, "SetText", "Select\nLevel");
            }
        }

        internal static bool OpenPracticeSelection(object p_RoomMainView)
        {
            object l_FlowCoordinator = GetFlowCoordinatorFromRoomView(p_RoomMainView);
            if (l_FlowCoordinator == null)
                return true;

            s_LastFlowCoordinator = l_FlowCoordinator;
            LateJoinSpectateController.EnsureSpectatingForPractice();
            AccessTools.Method(l_FlowCoordinator.GetType(), "SwitchToRoomLevelSelection")?.Invoke(l_FlowCoordinator, null);
            return false;
        }

        internal static bool TryStartSelectedPracticeSong(object p_FlowCoordinator)
        {
            if (!CanUsePracticeMode())
                return true;

            object l_LevelSelectionNavigationController = AccessTools.Field(p_FlowCoordinator.GetType(), "m_LevelSelectionNavigationController")?.GetValue(p_FlowCoordinator);
            if (l_LevelSelectionNavigationController == null)
                return false;

            BeatmapLevel l_Level = GetPropertyValue(l_LevelSelectionNavigationController, "beatmapLevel") as BeatmapLevel;
            if (l_Level == null)
            {
                Plugin.Log?.Warn("No practice song selected.");
                return false;
            }

            BeatmapKey l_Key = (BeatmapKey)GetPropertyValue(l_LevelSelectionNavigationController, "beatmapKey");
            BeatmapCharacteristicSO l_Characteristic = l_Key.beatmapCharacteristic;
            BeatmapDifficulty l_Difficulty = l_Key.difficulty;

            if (!Levels.BeatmapLevel_HasDifficulty(l_Level, l_Characteristic, l_Difficulty))
            {
                Plugin.Log?.Warn("Selected practice song does not have that characteristic/difficulty.");
                return false;
            }

            s_LastFlowCoordinator = p_FlowCoordinator;
            s_PracticeRunning = true;
            s_PracticeExitRequested = false;
            LateJoinSpectateController.EnsureSpectatingForPractice();
            AccessTools.Method(p_FlowCoordinator.GetType(), "SwitchToRoomView")?.Invoke(p_FlowCoordinator, null);

            Levels.LoadBeatmapLevelDataByLevelID(l_Level.levelID, delegate(BeatmapLevel p_LoadedLevel, IBeatmapLevelData p_LoadedData)
            {
                MTMainThreadInvoker.Enqueue(delegate
                {
                    if (p_LoadedLevel == null || p_LoadedData == null)
                    {
                        Plugin.Log?.Warn("Practice song failed to load.");
                        s_PracticeRunning = false;
                        return;
                    }

                    Plugin.Log?.Info("Starting waiting practice song: " + p_LoadedLevel.songName);
                    Levels.StartBeatmapLevel(p_LoadedLevel, l_Characteristic, l_Difficulty, p_LoadedData, null, null, null, null, OnPracticeSongFinished, "Room");
                });
            });

            return false;
        }

        internal static void Sync()
        {
            if (!s_PracticeRunning || s_PracticeExitRequested)
                return;

            object l_RoomData = s_RoomDataProperty?.GetValue(null);
            string l_RoomState = GetFieldValue(l_RoomData, "State")?.ToString();
            if (l_RoomState == ResultsState || l_RoomState == SelectingSongState)
                RequestPracticeExit();
        }

        private static void RequestPracticeExit()
        {
            s_PracticeExitRequested = true;
            MTMainThreadInvoker.Enqueue(delegate
            {
                try
                {
                    PauseController l_PauseController = Resources.FindObjectsOfTypeAll<PauseController>().FirstOrDefault();
                    if (l_PauseController != null)
                    {
                        Plugin.Log?.Info("Room finished multiplayer song; ending waiting practice song.");
                        AccessTools.Method(typeof(PauseController), "HandlePauseMenuManagerDidPressMenuButton")?.Invoke(l_PauseController, null);
                    }
                    else
                    {
                        OnPracticeSongFinished(default(StandardLevelScenesTransitionSetupDataSO), default(LevelCompletionResults));
                    }
                }
                catch (Exception l_Exception)
                {
                    Plugin.Log?.Error("[PracticeSongController.RequestPracticeExit] Error:");
                    Plugin.Log?.Error(l_Exception);
                    OnPracticeSongFinished(default(StandardLevelScenesTransitionSetupDataSO), default(LevelCompletionResults));
                }
            });
        }

        private static void OnPracticeSongFinished(StandardLevelScenesTransitionSetupDataSO _, LevelCompletionResults __)
        {
            s_PracticeRunning = false;
            s_PracticeExitRequested = false;
            MTMainThreadInvoker.Enqueue(ReturnToRoomView);
        }

        private static void ReturnToRoomView()
        {
            try
            {
                if (s_LastFlowCoordinator != null && s_RoomDataProperty?.GetValue(null) != null)
                    AccessTools.Method(s_LastFlowCoordinator.GetType(), "SwitchToRoomView")?.Invoke(s_LastFlowCoordinator, null);
            }
            catch (Exception l_Exception)
            {
                Plugin.Log?.Error("[PracticeSongController.ReturnToRoomView] Error:");
                Plugin.Log?.Error(l_Exception);
            }
        }

        private static object GetFlowCoordinatorFromRoomView(object p_RoomMainView)
        {
            Type l_FlowCoordinatorType = Type.GetType("BeatSaberPlus_Multiplayer.UI.MultiplayerPViewFlowCoordinator, BeatSaberPlus_Multiplayer");
            if (l_FlowCoordinatorType == null)
                return null;

            foreach (object l_FlowCoordinator in Resources.FindObjectsOfTypeAll(l_FlowCoordinatorType))
            {
                if (l_FlowCoordinator != null)
                    return l_FlowCoordinator;
            }

            return AccessTools.Property(p_RoomMainView.GetType(), "parentFlowCoordinator")?.GetValue(p_RoomMainView);
        }

        private static object GetFieldValue(object p_Target, string p_FieldName)
        {
            return p_Target?.GetType().GetField(p_FieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(p_Target);
        }

        private static uint GetUIntField(object p_Target, string p_FieldName)
        {
            object l_Value = GetFieldValue(p_Target, p_FieldName);
            return l_Value is uint l_UInt ? l_UInt : 0u;
        }

        private static object GetPropertyValue(object p_Target, string p_PropertyName)
        {
            return p_Target.GetType().GetProperty(p_PropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(p_Target);
        }

        private static void Invoke(object p_Target, string p_MethodName, params object[] p_Parameters)
        {
            AccessTools.Method(p_Target.GetType(), p_MethodName)?.Invoke(p_Target, p_Parameters);
        }
    }
}
