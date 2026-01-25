#nullable disable
using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.IntBackedUnit;
using MelonLoader;
using MelonLoader.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ScaledFishMod
{
    public class Main : MelonMod
    {
        internal static bool DebugEnabled;

        public override void OnInitializeMelon()
        {
            DebugEnabled = File.Exists(
                Path.Combine(MelonEnvironment.UserDataDirectory, "scaledfish.debug")
            );

            MelonLogger.Msg(
                DebugEnabled
                    ? "[ScaledFishMod] Initialized (DEBUG ENABLED)"
                    : "[ScaledFishMod] Initialized"
            );
        }
    }

    // ---------------- PATCHES ----------------
    [HarmonyPatch(typeof(GearItem), nameof(GearItem.Awake))]
    internal static class GearItem_Awake_Patch
    {
        static void Postfix(GearItem __instance)
        {
            if (__instance == null || __instance.m_InPlayerInventory)
                return;

            MelonCoroutines.Start(
                FishScaler.ScaleAfterSceneLoad(__instance)
            );
        }
    }

    [HarmonyPatch(typeof(GearItem), nameof(GearItem.Drop))]
    internal static class GearItem_Drop_Patch
    {
        static void Postfix(GearItem __instance)
        {
            if (__instance == null)
                return;

            MelonCoroutines.Start(
                FishScaler.ScaleAfterDropDelayed(__instance)
            );
        }
    }

    // ---------------- SCALER ----------------
    internal static class FishScaler
    {
        // -------- BASE VISUAL SCALE --------
        private static readonly Dictionary<string, Vector3> _baseScales =
            new Dictionary<string, Vector3>()
        {
            { "whitefish",       new Vector3(0.35f, 0.35f, 0.35f) },
            { "burbot",          new Vector3(1.00f, 1.00f, 1.00f) },
            { "rainbowtrout",    new Vector3(0.35f, 0.35f, 0.35f) },
            { "cohosalmon",      new Vector3(0.35f, 0.35f, 0.35f) },
            { "goldeye",         new Vector3(1.00f, 1.00f, 1.00f) },
            { "redirishlord",    new Vector3(1.00f, 1.00f, 1.00f) },
            { "rockfish",        new Vector3(1.00f, 1.00f, 1.00f) },
            { "smallmouthbass",  new Vector3(0.60f, 0.60f, 0.60f) }
        };

        // -------- MANUAL WEIGHT RANGES --------
        private static readonly Dictionary<string, (float minW, float maxW)> _weightRanges =
            new Dictionary<string, (float, float)>()
        {
            { "whitefish",      (1.0f, 2.9f) },
            { "burbot",         (4.0f, 7.3f) },
            { "rainbowtrout",   (0.5f, 2.5f) },
            { "cohosalmon",     (2.6f, 6.5f) },
            { "goldeye",        (1.0f, 2.5f) },
            { "redirishlord",   (1.6f, 3.5f) },
            { "rockfish",       (1.5f, 4.5f) },
            { "smallmouthbass", (2.0f, 5.5f) }
        };

        // ---------------- COROUTINES ----------------
        public static IEnumerator ScaleAfterSceneLoad(GearItem gi)
        {
            yield return null;
            yield return null;
            yield return null;

            yield return ScaleNow(gi, "[SceneLoad]");
        }

        public static IEnumerator ScaleAfterDropDelayed(GearItem gi)
        {
            if (gi == null || !gi)
                yield break;

            int safetyCounter = 0;
            while (gi.m_InPlayerInventory)
            {
                yield return null;
                if (++safetyCounter > 30)
                    yield break;
            }

            yield return ScaleNow(gi, "[DropDelayed]");
        }

        // ---------------- CORE ----------------
        public static IEnumerator ScaleNow(GearItem gi, string sourceTag)
        {
            if (gi == null || !gi || gi.m_InPlayerInventory)
                yield break;

            Transform tr = gi.transform;
            if (tr == null)
                yield break;

            GameObject go = gi.gameObject;
            if (!IsFish(go.name))
                yield break;

            float kg;
            try
            {
                kg = gi.WeightKG.ToQuantity(1f);
            }
            catch
            {
                yield break;
            }

            if (kg <= 0.01f)
                yield break;

            float scaleFactor = CalculateScaleFactor(kg, go.name);

            tr.localScale *= scaleFactor;

            DebugUtil.DebugLog(
                $"{go.name} scaleFactor={scaleFactor:F2} kg={kg:F2} {sourceTag}"
            );
        }

        // ---------------- SCALE LOGIC ----------------
        private static float CalculateScaleFactor(float kg, string name)
        {
            string lname = name.ToLowerInvariant();

            float minScale = 0.8f;
            float maxScale = 1.4f;

            if (lname.Contains("whitefish")) { minScale = 0.8f; maxScale = 1.18f; }
            else if (lname.Contains("burbot")) { minScale = 1.1f; maxScale = 1.72f; }
            else if (lname.Contains("rainbowtrout")) { minScale = 0.52f; maxScale = 0.88f; }
            else if (lname.Contains("cohosalmon")) { minScale = 0.87f; maxScale = 1.28f; }
            else if (lname.Contains("goldeye")) { minScale = 0.73f; maxScale = 1.42f; }
            else if (lname.Contains("redirishlord")) { minScale = 0.95f; maxScale = 1.25f; }
            else if (lname.Contains("rockfish")) { minScale = 0.9f; maxScale = 1.45f; }

            float minW = 0.5f;
            float maxW = 7.0f;

            foreach (KeyValuePair<string, (float minW, float maxW)> kvp in _weightRanges)
            {
                if (lname.Contains(kvp.Key))
                {
                    minW = kvp.Value.minW;
                    maxW = kvp.Value.maxW;
                    break;
                }
            }

            float t = Mathf.InverseLerp(minW, maxW, kg);
            t = Mathf.Clamp01(t);

            return Mathf.Lerp(minScale, maxScale, t);
        }

        public static bool IsFish(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            name = name.ToLowerInvariant();
            return name.Contains("whitefish") ||
                   name.Contains("rainbowtrout") ||
                   name.Contains("redirishlord") ||
                   name.Contains("rockfish") ||
                   name.Contains("burbot") ||
                   name.Contains("cohosalmon") ||
                   name.Contains("goldeye") ||
                   name.Contains("smallmouthbass");
        }
    }

    internal static class DebugUtil
    {
        internal static void DebugLog(string msg)
        {
            if (Main.DebugEnabled)
                MelonLogger.Msg("[ScaledFish DEBUG] " + msg);
        }
    }
}