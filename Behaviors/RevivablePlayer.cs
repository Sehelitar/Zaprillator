using System;
using GameNetcodeStuff;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Zaprillator.Behaviors;

[RequireComponent(typeof(RagdollGrabbableObject))]
internal class RevivablePlayer : NetworkBehaviour, IShockableWithGun
{
    private RagdollGrabbableObject _ragdoll;
    private bool _bodyShocked = false;
    private GrabbableObject _shockedBy = null;
    private float _batteryLevel = 0f;

    private void Start()
    {
        _ragdoll = gameObject.GetComponent<RagdollGrabbableObject>();
    }

    bool IShockableWithGun.CanBeShocked()
    {
        return _ragdoll != null;
    }

    float IShockableWithGun.GetDifficultyMultiplier()
    {
        return .1f;
    }

    NetworkObject IShockableWithGun.GetNetworkObject()
    {
        return NetworkObject;
    }

    Vector3 IShockableWithGun.GetShockablePosition()
    {
        return transform.position;
    }

    Transform IShockableWithGun.GetShockableTransform()
    {
        return transform;
    }

    void IShockableWithGun.ShockWithGun(PlayerControllerB shockedByPlayer)
    {
        _bodyShocked = true; 
        RoundManager.Instance.FlickerLights();

        if (!shockedByPlayer.IsOwner)
            return;
        
        _shockedBy = shockedByPlayer.currentlyHeldObjectServer;
        if (_shockedBy is not PatcherTool) return;
        _batteryLevel = _shockedBy.insertedBattery.charge;
        StartCoroutine(AutoStopShocking());
    }

    private IEnumerator AutoStopShocking()
    {
        yield return new WaitForSeconds(1f);
        ((PatcherTool)_shockedBy)?.StopShockingAnomalyOnClient();
    }

    void IShockableWithGun.StopShockingWithGun()
    {
        if (!_bodyShocked)
            return;
        
        _bodyShocked = false;
        RoundManager.Instance.FlickerLights();

        if (_shockedBy == null)
            return;

        var restoreHealth = Mathf.RoundToInt(_batteryLevel * 100);
        _shockedBy.UseUpBatteries();
        _shockedBy.SyncBatteryServerRpc(0);
        _shockedBy = null;
        
        RevivePlayerAtServerRpc(_ragdoll.ragdoll.playerScript.actualClientId, _ragdoll.ragdoll.transform.position, restoreHealth);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void RevivePlayerAtServerRpc(ulong clientId, Vector3 position, int health)
    {
        // If a full charge is required to revive but battery was not full => do nothing
        if (Plugin.GameConfig.RequiresFullCharge.Value && health < 100)
            return;
        
        // If restored health is not related to initial gun charge, set health to maximum
        if (!Plugin.GameConfig.RelativeHealth.Value)
            health = 100;
        
        RevivePlayerAtClientRpc(clientId, position, health);
    }

    [ClientRpc]
    private void RevivePlayerAtClientRpc(ulong clientId, Vector3 position, int health)
    {
        var player = (from playerScript in StartOfRound.Instance.allPlayerScripts where playerScript.actualClientId == clientId select playerScript).FirstOrDefault();
        if (player != null)
            StartOfRound.Instance.StartCoroutine(RevivePlayerCoroutine(player, position + new Vector3(0f, .5f, 0f), health));
    }
    
    // This Coroutine is a rewrite of StartOfRound.ReviveDeadPlayers
    private static IEnumerator RevivePlayerCoroutine(PlayerControllerB targetPlayer, Vector3? reviveAt = null, int health = 100)
    {
        if (!targetPlayer.isPlayerDead)
            yield break;
        
        var playerIndex = Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer);
        var localPlayer = GameNetworkManager.Instance.localPlayerController;
        var isTargetLocalPlayer = targetPlayer == localPlayer;

        targetPlayer.ResetPlayerBloodObjects(targetPlayer.isPlayerDead);

        targetPlayer.isClimbingLadder = false;
        targetPlayer.ResetZAndXRotation();
        targetPlayer.thisController.enabled = true;
        targetPlayer.health = health;
        targetPlayer.disableLookInput = false;

        targetPlayer.isPlayerDead = false;
        targetPlayer.isPlayerControlled = true;
        targetPlayer.isInElevator = true;
        targetPlayer.isInHangarShipRoom = true;
        targetPlayer.isInsideFactory = false;
        if(isTargetLocalPlayer)
            StartOfRound.Instance.SetPlayerObjectExtrapolate(false);
        targetPlayer.TeleportPlayer(reviveAt ?? StartOfRound.Instance.GetPlayerSpawnPosition(playerIndex));
        targetPlayer.setPositionOfDeadPlayer = false;
        targetPlayer.DisablePlayerModel(StartOfRound.Instance.allPlayerObjects[playerIndex], true, true);
        targetPlayer.helmetLight.enabled = false;
        
        targetPlayer.Crouch(false);
        targetPlayer.criticallyInjured = false;
        targetPlayer.playerBodyAnimator?.SetBool("Limp", false);
        targetPlayer.bleedingHeavily = false;
        targetPlayer.activatingItem = false;
        targetPlayer.twoHanded = false;
        targetPlayer.inSpecialInteractAnimation = false;
        targetPlayer.disableSyncInAnimation = false;
        targetPlayer.inAnimationWithEnemy = null;
        targetPlayer.holdingWalkieTalkie = false;
        targetPlayer.speakingToWalkieTalkie = false;
        
        targetPlayer.isSinking = false;
        targetPlayer.isUnderwater = false;
        targetPlayer.sinkingValue = 0.0f;
        targetPlayer.statusEffectAudio.Stop();
        targetPlayer.DisableJetpackControlsLocally();
        targetPlayer.health = health;
        
        targetPlayer.mapRadarDotAnimator.SetBool("dead", false);
        if (targetPlayer.IsOwner && isTargetLocalPlayer)
        {
            HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", false);
            targetPlayer.hasBegunSpectating = false;
            HUDManager.Instance.RemoveSpectateUI();
            HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
            targetPlayer.hinderedMultiplier = 1f;
            targetPlayer.isMovementHindered = 0;
            targetPlayer.sourcesCausingSinking = 0;
            
            targetPlayer.reverbPreset = StartOfRound.Instance.shipReverb;
        }
        
        if(isTargetLocalPlayer)
            SoundManager.Instance.earsRingingTimer = 0.0f;
        targetPlayer.voiceMuffledByEnemy = false;
        SoundManager.Instance.playerVoicePitchTargets[playerIndex] = 1f;
        SoundManager.Instance.SetPlayerPitch(1f, playerIndex);
        if (targetPlayer.currentVoiceChatIngameSettings == null)
            StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
        if (targetPlayer.currentVoiceChatIngameSettings != null)
        {
            if (targetPlayer.currentVoiceChatIngameSettings.voiceAudio == null)
                targetPlayer.currentVoiceChatIngameSettings.InitializeComponents();
            if (targetPlayer.currentVoiceChatIngameSettings.voiceAudio == null)
                yield break;
            targetPlayer.currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
        }

        if (isTargetLocalPlayer)
        {
            HUDManager.Instance.UpdateHealthUI(100, false);
            localPlayer.spectatedPlayerScript = null;
            HUDManager.Instance.audioListenerLowPass.enabled = false;
            StartOfRound.Instance.SetSpectateCameraToGameOverMode(false, localPlayer);
        }
        RagdollGrabbableObject[] objectsOfType = FindObjectsOfType<RagdollGrabbableObject>();
        for (int index = 0; index < objectsOfType.Length; ++index)
        {
            if (objectsOfType[index].ragdoll.playerScript == targetPlayer && objectsOfType[index].isHeld && objectsOfType[index].playerHeldBy != null)
                objectsOfType[index].playerHeldBy.DropAllHeldItems();
        }

        foreach (var component in FindObjectsOfType<DeadBodyInfo>())
        {
            if(component.playerScript == targetPlayer)
                Destroy(component.gameObject);
        }

        StartOfRound.Instance.livingPlayers += 1;
        StartOfRound.Instance.UpdatePlayerVoiceEffects();

        if (isTargetLocalPlayer)
        {
            yield return new WaitForSeconds(2f);
            HUDManager.Instance.HideHUD(false);
        }
    }
}
