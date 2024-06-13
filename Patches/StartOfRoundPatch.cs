using System.Linq;
using Zaprillator.Behaviors;
using HarmonyLib;
using UnityEngine;

namespace Zaprillator.Patches;

[HarmonyPatch(typeof(StartOfRound))]
public class StartOfRoundPatch
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    private static void Awake()
    {
        // Get all GameObjects, including hidden ones
        var gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        
        // Look for the RagdollGrabbable one
        var ragdoll = (from o in gameObjects
            where o.name == "RagdollGrabbableObject"
            select o).FirstOrDefault();
        
        // Patch it! But we should avoid doing this multiple times...
        if(ragdoll?.gameObject.GetComponent<RevivablePlayer>() is null)
            ragdoll?.gameObject.AddComponent<RevivablePlayer>();
    }
}