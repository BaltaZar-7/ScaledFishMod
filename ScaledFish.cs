#nullable disable
using HarmonyLib;
using Il2Cpp;
using Il2CppNodeCanvas.Tasks.Actions;
using Il2CppTLD.IntBackedUnit;
using MelonLoader;
using MelonLoader.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
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
            if (__instance == null)
                return;

            MelonCoroutines.Start(
                FishScaler.ScaleAfterSpawn(__instance, "[Awake]")
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
                FishScaler.ScaleAfterSpawn(__instance, "[Drop]")
            );
        }
    }

    // ---------------- SCALER ----------------

    internal static class FishScaler
    {
        // vanilla base scale cache
        private static readonly Dictionary<IntPtr, Vector3> _baseScales = new();

        public static IEnumerator ScaleAfterSpawn(GearItem gi, string sourceTag)
        {
            // exit / save / invalid safety
            if (gi == null || !gi || gi.gameObject == null)
                yield break;

            if (gi.m_InPlayerInventory)
                yield break;

            Transform tr = gi.transform;
            if (tr == null)
                yield break;

            GameObject go = gi.gameObject;
            if (go == null || !IsFish(go.name))
                yield break;

            yield return null;

            if (gi == null || !gi || tr == null)
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

            IntPtr key = tr.Pointer;

            if (!_baseScales.TryGetValue(key, out Vector3 baseScale))
            {
                baseScale = tr.localScale;
                _baseScales[key] = baseScale;
            }

            float scaleFactor = CalculateScaleFactor(
                kg,
                go.name,
                go.GetComponent<FoodWeight>()
            );

            tr.localScale = baseScale * scaleFactor;

            DebugUtil.DebugLog(
                $"[ScaledFishMod] {go.name} scaled by {scaleFactor:F2}x (≈{kg:F2} kg) {sourceTag}"
            );
        }

        // ---------------- SCALE LOGIC ----------------

        private static float CalculateScaleFactor(
            float kg,
            string name,
            FoodWeight fw)
        {
            float minW = 0.5f;
            float maxW = 7.0f;

            if (fw != null)
            {
                minW = fw.m_MinWeight.ToQuantity(1f);
                maxW = fw.m_MaxWeight.ToQuantity(1f);
            }

            float minScale = 0.8f;
            float maxScale = 1.4f;

            string lname = name.ToLowerInvariant();
            if (lname.Contains("whitefish")) { minScale = 0.8f; maxScale = 1.16f; }
            else if (lname.Contains("burbot")) { minScale = 1.0f; maxScale = 1.72f; }
            else if (lname.Contains("rainbowtrout")) { minScale = 0.52f; maxScale = 0.88f; }
            else if (lname.Contains("cohosalmon")) { minScale = 0.87f; maxScale = 1.28f; }
            else if (lname.Contains("goldeye")) { minScale = 0.73f; maxScale = 1.42f; }
            else if (lname.Contains("redirishlord")) { minScale = 0.95f; maxScale = 1.25f; }
            else if (lname.Contains("rockfish")) { minScale = 0.9f; maxScale = 1.45f; }

            float t = Mathf.InverseLerp(minW, maxW, kg);
            return Mathf.Lerp(minScale, maxScale, t);
        }

        private static bool IsFish(string name)
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