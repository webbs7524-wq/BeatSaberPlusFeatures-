using System;
using System.Reflection;

namespace BeatSaberPlus_MultiplayerLateJoinFix
{
    internal static class LateJoinSpectateController
    {
        private const string SelectingSongState = "SelectingSong";
        private const string WarmingUpState = "WarmingUp";
        private const string PlayingState = "Playing";

        private static readonly Type s_NetworkManagerType = Type.GetType("BeatSaberPlus_Multiplayer.Network.NetworkManager, BeatSaberPlus_Multiplayer");
        private static readonly PropertyInfo s_RoomDataProperty = s_NetworkManagerType?.GetProperty("RoomData", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly PropertyInfo s_SelfPlayerProperty = s_NetworkManagerType?.GetProperty("SelfPlayer", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo s_SetSpectateMethod = s_NetworkManagerType?.GetMethod("RoomPlayerSetSpectate", BindingFlags.Static | BindingFlags.NonPublic);

        private static bool s_AutoSpectateEnabled;
        private static bool s_WaitingForServerAck;
        private static DateTime s_LastSpectateRequestTime = DateTime.MinValue;

        internal static void Sync()
        {
            try
            {
                if (s_NetworkManagerType == null || s_RoomDataProperty == null || s_SelfPlayerProperty == null || s_SetSpectateMethod == null)
                    return;

                object l_RoomData = s_RoomDataProperty.GetValue(null);
                object l_SelfPlayer = s_SelfPlayerProperty.GetValue(null);
                if (l_RoomData == null || l_SelfPlayer == null)
                    return;

                string l_RoomState = GetFieldValue(l_RoomData, "State")?.ToString();
                bool l_IsSpectating = GetBoolField(l_SelfPlayer, "Spectate");

                if (l_IsSpectating == s_AutoSpectateEnabled)
                    s_WaitingForServerAck = false;

                if ((l_RoomState == WarmingUpState || l_RoomState == PlayingState) && !l_IsSpectating)
                {
                    RequestSpectate(true, "Auto-enabled lurk mode while joining an active Multiplayer+ song.");
                }
                else if (l_RoomState == SelectingSongState && s_AutoSpectateEnabled && l_IsSpectating && !PracticeSongController.IsPracticeRunning)
                {
                    RequestSpectate(false, "Auto-disabled lurk mode now that Multiplayer+ is back to song select.");
                }
                else if (l_RoomState == SelectingSongState && !l_IsSpectating && !PracticeSongController.IsPracticeRunning)
                {
                    s_AutoSpectateEnabled = false;
                    s_WaitingForServerAck = false;
                }
            }
            catch (Exception l_Exception)
            {
                Plugin.Log?.Error("[LateJoinSpectateController.Sync] Error:");
                Plugin.Log?.Error(l_Exception);
            }
        }

        private static void RequestSpectate(bool p_Value, string p_LogMessage)
        {
            if (s_WaitingForServerAck && (DateTime.UtcNow - s_LastSpectateRequestTime).TotalSeconds < 2)
                return;

            s_SetSpectateMethod.Invoke(null, new object[] { p_Value });
            s_AutoSpectateEnabled = p_Value;
            s_WaitingForServerAck = true;
            s_LastSpectateRequestTime = DateTime.UtcNow;
            Plugin.Log?.Info(p_LogMessage);
        }

        private static object GetFieldValue(object p_Target, string p_FieldName)
        {
            return p_Target.GetType().GetField(p_FieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(p_Target);
        }

        private static bool GetBoolField(object p_Target, string p_FieldName)
        {
            object l_Value = GetFieldValue(p_Target, p_FieldName);
            return l_Value is bool l_Bool && l_Bool;
        }

        internal static void EnsureSpectatingForPractice()
        {
            try
            {
                object l_SelfPlayer = s_SelfPlayerProperty?.GetValue(null);
                if (l_SelfPlayer != null && !GetBoolField(l_SelfPlayer, "Spectate"))
                    RequestSpectate(true, "Auto-enabled spectate while starting a waiting practice song.");
            }
            catch (Exception l_Exception)
            {
                Plugin.Log?.Error("[LateJoinSpectateController.EnsureSpectatingForPractice] Error:");
                Plugin.Log?.Error(l_Exception);
            }
        }
    }
}
