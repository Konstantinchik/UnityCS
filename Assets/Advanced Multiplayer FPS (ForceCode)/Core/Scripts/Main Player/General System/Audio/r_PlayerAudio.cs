using UnityEngine;
using Unity.Netcode;

public class r_PlayerAudio : NetworkBehaviour
{
    [Header("Audio Sources")]
    public AudioSource footstepsSource;
    public AudioSource slideSource;
    public AudioSource landSource;

    [Header("Footstep Clips")]
    public AudioClip[] concreteFootsteps;
    public AudioClip[] metalFootsteps;

    [Header("Slide Clips")]
    public AudioClip slideStart;
    public AudioClip slideLoop;
    public AudioClip slideEnd;

    [Header("Land Clips")]
    public AudioClip[] concreteLand;
    public AudioClip[] metalLand;

    public enum SurfaceType { Concrete, Metal }
    public enum MoveState { Walk, Run, Sprint }

    public void PlayFootstepSound(Vector3 position, SurfaceType surface, MoveState moveState)
    {
        if (IsOwner)
            PlayFootstepServerRpc(position, (int)surface, moveState.ToString());
    }

    public void PlayLandSound(Vector3 position, SurfaceType surface)
    {
        if (IsOwner)
            PlayLandServerRpc(position, (int)surface);
    }

    public void UpdateSlideSound(bool isSliding)
    {
        if (IsOwner)
            PlaySlideServerRpc(transform.position, isSliding);
    }

    [ServerRpc]
    private void PlayFootstepServerRpc(Vector3 position, int surfaceIndex, string moveState)
    {
        PlayFootstepClientRpc(position, surfaceIndex, moveState);
    }

    [ClientRpc]
    private void PlayFootstepClientRpc(Vector3 position, int surfaceIndex, string moveState)
    {
        footstepsSource.transform.position = position;

        AudioClip[] clips = surfaceIndex == 0 ? concreteFootsteps : metalFootsteps;
        if (clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        footstepsSource.pitch = moveState switch
        {
            "Walk" => 0.9f,
            "Run" => 1.1f,
            "Sprint" => 1.3f,
            _ => 1f
        };

        footstepsSource.clip = clip;
        footstepsSource.Play();
    }

    [ServerRpc]
    private void PlaySlideServerRpc(Vector3 position, bool isSliding)
    {
        PlaySlideClientRpc(position, isSliding);
    }

    [ClientRpc]
    private void PlaySlideClientRpc(Vector3 position, bool isSliding)
    {
        slideSource.transform.position = position;

        if (isSliding)
        {
            slideSource.clip = slideStart;
            slideSource.loop = false;
            slideSource.Play();

            Invoke(nameof(PlaySlideLoop), slideStart.length);
        }
        else
        {
            CancelInvoke(nameof(PlaySlideLoop));
            slideSource.clip = slideEnd;
            slideSource.loop = false;
            slideSource.Play();
        }
    }

    private void PlaySlideLoop()
    {
        slideSource.clip = slideLoop;
        slideSource.loop = true;
        slideSource.Play();
    }

    [ServerRpc]
    private void PlayLandServerRpc(Vector3 position, int surfaceIndex)
    {
        PlayLandClientRpc(position, surfaceIndex);
    }

    [ClientRpc]
    private void PlayLandClientRpc(Vector3 position, int surfaceIndex)
    {
        landSource.transform.position = position;

        AudioClip[] clips = surfaceIndex == 0 ? concreteLand : metalLand;
        if (clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        landSource.clip = clip;
        landSource.Play();
    }
}
