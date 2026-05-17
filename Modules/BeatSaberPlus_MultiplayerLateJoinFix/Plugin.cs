using HarmonyLib;
using IPA;
using System;
using System.Reflection;

namespace BeatSaberPlus_MultiplayerLateJoinFix
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal const string HarmonyID = "local.webbs.beatsaberplus_multiplayer_latejoinfix";

        internal static IPA.Logging.Logger Log;

        private static Harmony m_Harmony;

        [Init]
        public Plugin(IPA.Logging.Logger p_Logger)
        {
            Log = p_Logger;
        }

        [OnStart]
        public void OnApplicationStart()
        {
            try
            {
                m_Harmony = new Harmony(HarmonyID);
                m_Harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Info("Late join spectate fix loaded.");
            }
            catch (Exception l_Exception)
            {
                Log.Error("[LateJoinFix.OnApplicationStart] Error:");
                Log.Error(l_Exception);
            }
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            try
            {
                m_Harmony?.UnpatchSelf();
            }
            catch (Exception l_Exception)
            {
                Log.Error("[LateJoinFix.OnApplicationQuit] Error:");
                Log.Error(l_Exception);
            }
        }
    }
}
