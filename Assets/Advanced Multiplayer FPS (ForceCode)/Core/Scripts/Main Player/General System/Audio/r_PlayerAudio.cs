using UnityEngine;
using Unity.Netcode;

public class r_PlayerAudio : NetworkBehaviour
{
    [Header("Audio Sources")]
    public AudioSource footstepsSource;
    public AudioSource slideSource;
    public AudioSource landSource;
    public AudioSource weaponSource; // Добавлен для звуков оружия

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

    public void OnWeaponAudioPlay(string weaponName, string audioClipName, float audioVolume, bool networked)
    {
        if (!IsOwner) return;

        if (networked)
        {
            // Отправляем запрос на сервер для воспроизведения звука
            PlayWeaponSoundServerRpc(weaponName, audioClipName, audioVolume);
        }
        else
        {
            // Локальное воспроизведение
            AudioClip clip = FindWeaponAudioClip(weaponName, audioClipName);
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, weaponSource.transform.position, audioVolume);
            }
        }
    }

    [ServerRpc]
    private void PlayWeaponSoundServerRpc(string weaponName, string audioClipName, float audioVolume)
    {
        // Рассылаем всем клиентам
        PlayWeaponSoundClientRpc(weaponName, audioClipName, audioVolume);
    }

    [ClientRpc]
    private void PlayWeaponSoundClientRpc(string weaponName, string audioClipName, float audioVolume)
    {
        if (IsOwner) return; // Владелец уже воспроизвел звук локально

        AudioClip clip = FindWeaponAudioClip(weaponName, audioClipName);
        if (clip != null)
        {
            weaponSource.transform.position = transform.position;
            weaponSource.PlayOneShot(clip, audioVolume);
        }
    }

    private AudioClip FindWeaponAudioClip(string weaponName, string clipName)
    {
        // Реализуйте логику поиска нужного аудиоклипа
        // Это может быть через Resources.Load или ссылки в инспекторе
        return Resources.Load<AudioClip>($"Weapons/{weaponName}/{clipName}");
    }

    public void PlayFootstepSound(Vector3 position, SurfaceType surface, MoveState moveState)
    {
        if (!IsOwner) return;

        PlayFootstepServerRpc(position, (int)surface, moveState.ToString());
    }

    [ServerRpc]
    private void PlayFootstepServerRpc(Vector3 position, int surfaceIndex, string moveState)
    {
        PlayFootstepClientRpc(position, surfaceIndex, moveState);
    }

    [ClientRpc]
    private void PlayFootstepClientRpc(Vector3 position, int surfaceIndex, string moveState)
    {
        if (IsOwner) return; // Владелец уже обработал звук локально

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

    public void PlayLandSound(Vector3 position, SurfaceType surface)
    {
        if (!IsOwner) return;

        PlayLandServerRpc(position, (int)surface);
    }

    [ServerRpc]
    private void PlayLandServerRpc(Vector3 position, int surfaceIndex)
    {
        PlayLandClientRpc(position, surfaceIndex);
    }

    [ClientRpc]
    private void PlayLandClientRpc(Vector3 position, int surfaceIndex)
    {
        if (IsOwner) return;

        landSource.transform.position = position;

        AudioClip[] clips = surfaceIndex == 0 ? concreteLand : metalLand;
        if (clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        landSource.clip = clip;
        landSource.Play();
    }

    public void UpdateSlideSound(bool isSliding)
    {
        if (!IsOwner) return;

        PlaySlideServerRpc(transform.position, isSliding);
    }

    [ServerRpc]
    private void PlaySlideServerRpc(Vector3 position, bool isSliding)
    {
        PlaySlideClientRpc(position, isSliding);
    }

    [ClientRpc]
    private void PlaySlideClientRpc(Vector3 position, bool isSliding)
    {
        if (IsOwner) return;

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

    #region OnPlayerHurtAudioPlay
    public void OnPlayerHurtAudioPlay(Vector3 position)
    {
        if (!IsOwner) return;
        PlayHurtSoundServerRpc(position);
    }

    [ServerRpc]
    private void PlayHurtSoundServerRpc(Vector3 position)
    {
        PlayHurtSoundClientRpc(position);
    }

    [ClientRpc]
    private void PlayHurtSoundClientRpc(Vector3 position)
    {
        if (IsOwner) return;

        // Воспроизведение звука удара
        // weaponSource используется условно — можешь завести отдельный AudioSource, если нужно
        if (weaponSource != null && weaponSource.clip != null)
        {
            weaponSource.transform.position = position;
            weaponSource.Play();
        }
    }
    #endregion
}