using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace Zaprillator.Patches;

[HarmonyPatch(typeof(PatcherTool))]
internal class PatcherToolPatch
{
    [HarmonyPatch("ScanGun", MethodType.Enumerator)]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ScanGunTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        // Patch the call to TryGetComponent to emulate a call to TryGetComponentInChildren
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(
                    OpCodes.Callvirt,
                    AccessTools.FirstMethod(
                        typeof(GameObject), 
                        m => m.Name == nameof(GameObject.TryGetComponent)
                             && m.GetParameters().Length == 1).MakeGenericMethod(typeof(IShockableWithGun))
                )
            )
            .ThrowIfInvalid("Call to GameObject.TryGetComponent not found!")
            .Set(OpCodes.Call, AccessTools.Method(typeof(PatcherToolPatch), nameof(TryGetShockableWithGun)))
            .ThrowIfInvalid("Could not patch PatcherTool.ScanGun!")
            .InstructionEnumeration();
    }

    private static bool TryGetShockableWithGun(GameObject obj, out Component component)
    {
        component = (Component)obj.GetComponentInChildren<IShockableWithGun>();
        return component != null;
    }

    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    public static void Start(ref PatcherTool __instance)
    {
        __instance.anomalyMask |= 1 << 20; // Layer 20 = Player Ragdoll
    }
}