using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;

namespace RadarLabelsFix
{
    // The game re-sorts the whole radar target list (players included) whenever a radar
    // booster is added, but some code paths later write player names by player slot index.
    // After that, a monitor entry can show one player's name while tracking another player.
    // This plugin re-derives every player entry's label from the player it actually tracks.
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "Gezang.RadarLabelsFix";
        public const string Name = "RadarLabelsFix";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            var harmony = new Harmony(Guid);

            var relabelMap = new HarmonyMethod(typeof(Plugin), nameof(RelabelInstance));
            var relabelMain = new HarmonyMethod(typeof(Plugin), nameof(RelabelMainMap));

            // List was reordered or changed: fix labels afterwards.
            Patch(harmony, typeof(ManualCameraRenderer), "SyncOrderOfRadarBoostersInList", postfix: relabelMap);
            Patch(harmony, typeof(ManualCameraRenderer), "AddTransformAsTargetToRadar", postfix: relabelMap);
            Patch(harmony, typeof(ManualCameraRenderer), "RemoveTargetFromRadar", postfix: relabelMap);
            // Names were (re)assigned by slot index: fix labels afterwards.
            Patch(harmony, typeof(PlayerControllerB), "SendNewPlayerValuesClientRpc", postfix: relabelMain);
            Patch(harmony, typeof(StartOfRound), "OnPlayerConnectedClientRpc", postfix: relabelMain);
            Patch(harmony, typeof(StartOfRound), "OnPlayerDC", postfix: relabelMain);
            // Labels are about to be read: make sure they're right first.
            Patch(harmony, typeof(ManualCameraRenderer), "updateMapTarget", prefix: relabelMap);
            Patch(harmony, typeof(Terminal), "ParsePlayerSentence", prefix: relabelMain);

            Log.LogInfo($"{Name} {Version} loaded");
        }

        private static void Patch(Harmony harmony, Type type, string method, HarmonyMethod prefix = null, HarmonyMethod postfix = null)
        {
            try
            {
                MethodInfo target = AccessTools.Method(type, method);
                if (target == null)
                {
                    Log.LogWarning($"Could not find {type.Name}.{method}; skipping that hook");
                    return;
                }
                harmony.Patch(target, prefix: prefix, postfix: postfix);
            }
            catch (Exception e)
            {
                Log.LogWarning($"Failed to hook {type.Name}.{method}: {e}");
            }
        }

        private static void RelabelInstance(ManualCameraRenderer __instance) => Relabel(__instance);

        private static void RelabelMainMap() => Relabel(StartOfRound.Instance != null ? StartOfRound.Instance.mapScreen : null);

        internal static void Relabel(ManualCameraRenderer map)
        {
            try
            {
                if (map == null || map.radarTargets == null)
                    return;

                List<TransformAndName> targets = map.radarTargets;
                bool changed = false;
                for (int i = 0; i < targets.Count; i++)
                {
                    TransformAndName target = targets[i];
                    if (target == null || target.isNonPlayer || target.transform == null)
                        continue;

                    PlayerControllerB player = target.transform.GetComponent<PlayerControllerB>();
                    if (player == null || string.IsNullOrEmpty(player.playerUsername))
                        continue;

                    if (target.name != player.playerUsername)
                    {
                        Log.LogInfo($"Radar target #{i}: label '{target.name}' -> '{player.playerUsername}' (tracks {player.gameObject.name})");
                        target.name = player.playerUsername;
                        changed = true;
                    }
                }

                if (changed)
                    RefreshMonitorName(map);
            }
            catch (Exception e)
            {
                Log.LogWarning($"Relabel failed: {e}");
            }
        }

        // The monitor's name text is only set when the target switches, so refresh it if the
        // currently shown entry was relabelled.
        private static void RefreshMonitorName(ManualCameraRenderer map)
        {
            StartOfRound round = StartOfRound.Instance;
            if (round == null || map != round.mapScreen || round.mapScreenPlayerName == null)
                return;

            int index = map.targetTransformIndex;
            if (index < 0 || index >= map.radarTargets.Count || map.radarTargets[index] == null)
                return;

            round.mapScreenPlayerName.text = map.radarTargets[index].name ?? "";
        }
    }
}
