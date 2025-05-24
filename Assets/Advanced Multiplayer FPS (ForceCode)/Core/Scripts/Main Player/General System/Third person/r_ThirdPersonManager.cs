using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Netcode;

namespace ForceCodeFPS
{
    #region Serializable Classes
    [System.Serializable]
    public class r_ThirdPersonWeaponSlotItem
    {
        public GameObject m_ThirdPersonWeapon;
        public string m_UniqueID;
    }

    [System.Serializable]
    public class r_ThirdPersonWeaponSlot
    {
        public r_WeaponItemType m_WeaponType;
        public Transform m_WeaponSlot;
        public List<r_ThirdPersonWeaponSlotItem> m_SlotWeapons;
    }
    #endregion

    public class r_ThirdPersonManager : NetworkBehaviour
    {
        #region References
        public r_PlayerController m_PlayerController;
        #endregion

        #region Public Variables
        [Header("Character Animator")]
        public Animator m_ThirdPersonAnimator;

        [Header("Third Person Death Camera")]
        public r_ThirdPersonCamera m_ThirdPersonCamera;

        [Header("Spectate Holder Transform")]
        public Transform m_SpectateHolder;

        [Header("Weapon Manager")]
        public Transform m_WeaponParent;

        [Header("Animator Layer Settings")]
        public int m_WeaponAnimationLayer;

        public List<r_ThirdPersonWeaponSlot> m_WeaponSlots = new List<r_ThirdPersonWeaponSlot>();
        public List<r_ThirdPersonBodyPart> m_BodyParts = new List<r_ThirdPersonBodyPart>();
        public Renderer[] m_localRenderersOnlyShadows;
        #endregion

        #region Private Variables
        public r_WeaponManagerData m_CurrentWeapon;
        [Range(0, 1)] public float m_CurrentLeftHandWeight;
        public float m_AnimatorLeanAngle = 0;
        #endregion

        #region Network Variables
        private NetworkVariable<bool> m_IsFiring = new NetworkVariable<bool>();
        private NetworkVariable<bool> m_IsReloading = new NetworkVariable<bool>();
        private NetworkVariable<bool> m_ShellEjected = new NetworkVariable<bool>();
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (!IsOwner) return;
            Setup();
        }

        private void Update()
        {
            if (!IsOwner) return;
            if (m_PlayerController == null || m_ThirdPersonAnimator == null) return;

            HandleAnimator();
            HandleLeaning();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            m_IsFiring.OnValueChanged += OnWeaponFireEvent;
            m_IsReloading.OnValueChanged += OnWeaponReloadEvent;
            m_ShellEjected.OnValueChanged += OnShellEjectEvent;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            m_IsFiring.OnValueChanged -= OnWeaponFireEvent;
            m_IsReloading.OnValueChanged -= OnWeaponReloadEvent;
            m_ShellEjected.OnValueChanged -= OnShellEjectEvent;
        }
        #endregion

        #region Handling
        private void HandleAnimator()
        {
            //Set jumping boolean
            m_ThirdPersonAnimator.SetBool("Jumping", !m_PlayerController.m_CharacterController.isGrounded);

            //Horizontal and vertical movement with smoothness
            m_ThirdPersonAnimator.SetFloat("MoveX", m_PlayerController.m_InputManager.GetHorizontal(), 0.1f, Time.deltaTime);
            m_ThirdPersonAnimator.SetFloat("MoveZ", m_PlayerController.m_InputManager.GetVertical(), 0.1f, Time.deltaTime);

            //Set other movement booleans
            m_ThirdPersonAnimator.SetBool("Sprinting", m_PlayerController.m_MoveState == r_MoveState.SPRINTING);
            m_ThirdPersonAnimator.SetBool("Crouching", m_PlayerController.m_MoveState == r_MoveState.CROUCHING);
            m_ThirdPersonAnimator.SetBool("Sliding", m_PlayerController.m_MoveState == r_MoveState.SLIDING);

            //Set weapon ID
            m_ThirdPersonAnimator.SetInteger("weaponID", 
                m_PlayerController.m_WeaponManager.m_CurrentWeaponIndex == 0 && 
                m_PlayerController.m_WeaponManager.m_LocalWeapons.Count == 0 ? 
                -1 : m_CurrentWeapon.m_WeaponData.m_WeaponID);

            //Smooth hand transition
            m_CurrentLeftHandWeight = Mathf.Lerp(m_CurrentLeftHandWeight, 
                m_CurrentWeapon != null && 
                !m_PlayerController.m_WeaponManager.m_ChangingWeapon && 
                !m_PlayerController.m_WeaponManager.m_ReloadingWeapon ? 1 : 0, 
                Time.deltaTime * 8f);
        }

        private void HandleLeaning()
        {
            var _player_camera = m_PlayerController.m_PlayerCamera;
            float _animation_lean_angle = 0;
            float _animation_lean_speed = _player_camera.m_CameraBase.m_CameraLeanSettings.m_AnimatorLeanChangeSpeed;

            if (_player_camera.m_CameraRotationLeanAngle < 0)
            {
                _animation_lean_angle = -_player_camera.m_CameraBase.m_CameraLeanSettings.m_MaxLeanAngleAnimator;
            }
            else if (_player_camera.m_CameraRotationLeanAngle > 0)
            {
                _animation_lean_angle = _player_camera.m_CameraBase.m_CameraLeanSettings.m_MaxLeanAngleAnimator;
            }

            m_AnimatorLeanAngle = Mathf.Lerp(m_AnimatorLeanAngle, _animation_lean_angle, Time.deltaTime * _animation_lean_speed);
            m_ThirdPersonAnimator.SetFloat("Leaning", m_AnimatorLeanAngle);
        }
        #endregion

        #region Actions
        private void Setup()
        {
            SetLocalRendererShadows(ShadowCastingMode.ShadowsOnly);
            DeActivateRagdoll();
        }

        public void ActivateRagdoll()
        {
            if (m_BodyParts == null) return;

            foreach (r_ThirdPersonBodyPart _BodyPart in m_BodyParts)
            {
                if (_BodyPart.m_BodyParts.m_BodyPartRigidbody != null && 
                    _BodyPart.m_BodyParts.m_BodyPartCollider != null)
                {
                    _BodyPart.m_BodyParts.m_BodyPartRigidbody.drag = 0.5f;
                    _BodyPart.m_BodyParts.m_BodyPartRigidbody.isKinematic = false;
                    if (IsOwner) _BodyPart.m_BodyParts.m_BodyPartCollider.enabled = true;
                }
            }
        }

        public void DeActivateRagdoll()
        {
            if (m_BodyParts == null) return;

            foreach (r_ThirdPersonBodyPart _BodyPart in m_BodyParts)
            {
                if (_BodyPart.m_BodyParts.m_BodyPartRigidbody != null && 
                    _BodyPart.m_BodyParts.m_BodyPartCollider != null)
                {
                    _BodyPart.m_BodyParts.m_BodyPartRigidbody.isKinematic = true;
                    if (IsOwner) _BodyPart.m_BodyParts.m_BodyPartCollider.enabled = false;
                }
            }
        }

        public void ThirdPersonSuicide()
        {
            SetLocalRendererShadows(ShadowCastingMode.On);
            Component[] _Components = transform.GetComponents<Component>();

            foreach (Component _Component in _Components)
            {
                if (_Component is Transform) continue;
                Destroy(_Component);
            }

            ActivateRagdoll();
        }

        public void PlayAnimation(r_AnimationType _type, string _animationName, bool _setBool, int _layer)
        {
            switch (_type)
            {
                case r_AnimationType.PLAY: 
                    m_ThirdPersonAnimator.Play(_animationName, _layer, 0f); 
                    break;
                case r_AnimationType.SETTRIGGER: 
                    m_ThirdPersonAnimator.SetTrigger(_animationName); 
                    break;
                case r_AnimationType.SETBOOL: 
                    m_ThirdPersonAnimator.SetBool(_animationName, _setBool); 
                    break;
            }
        }

        public void OnWeaponFire()
        {
            if (IsServer)
            {
                m_IsFiring.Value = !m_IsFiring.Value;
            }
            else
            {
                OnWeaponFireServerRpc();
            }
        }

        public void OnWeaponReload()
        {
            if (IsServer)
            {
                m_IsReloading.Value = !m_IsReloading.Value;
            }
            else
            {
                OnWeaponReloadServerRpc();
            }
        }

        public void OnShellEject()
        {
            if (IsServer)
            {
                m_ShellEjected.Value = !m_ShellEjected.Value;
            }
            else
            {
                OnShellEjectServerRpc();
            }
        }
        #endregion

        #region Network Methods
        [ServerRpc]
        private void OnWeaponFireServerRpc()
        {
            m_IsFiring.Value = !m_IsFiring.Value;
        }

        [ServerRpc]
        private void OnWeaponReloadServerRpc()
        {
            m_IsReloading.Value = !m_IsReloading.Value;
        }

        [ServerRpc]
        private void OnShellEjectServerRpc()
        {
            m_ShellEjected.Value = !m_ShellEjected.Value;
        }

        private void OnWeaponFireEvent(bool previous, bool current)
        {
            if (m_CurrentWeapon == null) return;
            
            r_WeaponController _weapon_FP = m_CurrentWeapon.m_WeaponObject_FP;
            r_ThirdPersonWeapon _weapon_TP = m_CurrentWeapon.m_WeaponObject_TP;

            if (_weapon_FP != null && _weapon_TP != null)
            {
                if (!IsOwner)
                {
                    GameObject _muzzle = Instantiate(
                        _weapon_TP.m_MuzzleFlash, 
                        _weapon_TP.m_MuzzlePointTransform.position, 
                        _weapon_TP.m_MuzzlePointTransform.rotation);
                    
                    Destroy(_muzzle, _weapon_FP.m_WeaponConfig.m_weaponFXSettings.m_muzzleLifetime + 0.2f);
                }

                PlayAnimation(r_AnimationType.PLAY, 
                    _weapon_FP.m_WeaponConfig.m_AnimationSettings.m_FireAnimName, 
                    false, 
                    m_WeaponAnimationLayer);
            }
        }

        private void OnWeaponReloadEvent(bool previous, bool current)
        {
            PlayAnimation(r_AnimationType.SETTRIGGER, "Reload", false, m_WeaponAnimationLayer);
        }

        private void OnShellEjectEvent(bool previous, bool current)
        {
            if (m_CurrentWeapon == null) return;
            
            r_WeaponController _weapon_FP = m_CurrentWeapon.m_WeaponObject_FP;
            r_ThirdPersonWeapon _weapon_TP = m_CurrentWeapon.m_WeaponObject_TP;

            if (_weapon_FP != null && _weapon_TP != null)
            {
                Rigidbody _shell = Instantiate(
                    _weapon_TP.m_BulletShell, 
                    _weapon_TP.m_ShellPointTransform.position, 
                    _weapon_TP.m_ShellPointTransform.rotation);

                _shell.velocity = _shell.transform.forward * _weapon_FP.m_WeaponConfig.m_weaponFXSettings.m_shellEjectForce;

                Vector3 _shellRotation = new Vector3(
                    Random.Range(-_weapon_FP.m_WeaponConfig.m_weaponFXSettings.m_shellRandomRotation.x, 
                    _weapon_FP.m_WeaponConfig.m_weaponFXSettings.m_shellRandomRotation.x),
                    Random.Range(-_weapon_FP.m_WeaponConfig.m_weaponFXSettings.m_shellRandomRotation.y, 
                    _weapon_FP.m_WeaponConfig.m_weaponFXSettings.m_shellRandomRotation.y),
                    Random.Range(-_weapon_FP.m_WeaponConfig.m_weaponFXSettings.m_shellRandomRotation.z, 
                    _weapon_FP.m_WeaponConfig.m_weaponFXSettings.m_shellRandomRotation.z));

                _shell.transform.localRotation = Quaternion.Euler(
                    _weapon_FP.m_WeaponConfig.m_weaponFXSettings.m_shellStartRotation + _shellRotation);

                Destroy(_shell, _weapon_FP.m_WeaponConfig.m_weaponFXSettings.m_shellLifeTime);
            }
        }
        #endregion

        #region Weapon Management
        public void OnEquipWeapon(string _unique_weapon_id, float _equip_duration) => 
            StartCoroutine(OnEquipWeaponCorountine(_unique_weapon_id, _equip_duration));

        private IEnumerator OnEquipWeaponCorountine(string _unique_weapon_id, float _equip_duration)
        {
            yield return new WaitForSeconds(_equip_duration);

            var _local_weapons = m_PlayerController.m_WeaponManager.m_LocalWeapons;
            var _local_weapon = _local_weapons.Find(x => x.m_UniqueID == _unique_weapon_id);

            if (_local_weapon == null || _local_weapon.m_WeaponObject_FP == null || 
                _local_weapon.m_WeaponObject_TP == null) yield break;

            PlayAnimation(r_AnimationType.PLAY, 
                _local_weapon.m_WeaponData.m_Weapon_FP_Prefab.m_WeaponConfig.m_AnimationSettings.m_EquipAnimName, 
                false, 
                m_WeaponAnimationLayer);

            _local_weapon.m_WeaponObject_TP.gameObject.SetActive(true);
            _local_weapon.m_WeaponObject_TP.transform.parent = m_WeaponParent;
            _local_weapon.m_WeaponObject_TP.transform.localPosition = 
                _local_weapon.m_WeaponData.m_Weapon_TP_Prefab.m_DefaultPosition;
            _local_weapon.m_WeaponObject_TP.transform.localRotation = 
                Quaternion.Euler(_local_weapon.m_WeaponData.m_Weapon_TP_Prefab.m_DefaultRotation);

            r_ThirdPersonWeaponSlot _slot = FindWeaponSlotByType(_local_weapon.m_WeaponData.m_WeaponType);

            if (_slot != null)
            {
                r_ThirdPersonWeaponSlotItem _weapon_in_slot = 
                    _slot.m_SlotWeapons.Find(x => x.m_UniqueID == _unique_weapon_id);

                if (_weapon_in_slot != null)
                {
                    _slot.m_SlotWeapons.Remove(_weapon_in_slot);
                }
            }
        }

        public void OnUnequipWeapon(string _unique_weapon_id, float _unequip_duration) => 
            StartCoroutine(OnUnequipWeaponCorountine(_unique_weapon_id, _unequip_duration));

        private IEnumerator OnUnequipWeaponCorountine(string _unique_weapon_id, float _unequip_duration)
        {
            var _local_weapons = m_PlayerController.m_WeaponManager.m_LocalWeapons;
            var _local_weapon = _local_weapons.Find(x => x.m_UniqueID == _unique_weapon_id);

            if (_local_weapon == null || _local_weapon.m_WeaponObject_FP == null || 
                _local_weapon.m_WeaponObject_TP == null) yield break;

            PlayAnimation(r_AnimationType.PLAY, 
                _local_weapon.m_WeaponObject_FP.m_WeaponConfig.m_AnimationSettings.m_UnequipAnimName, 
                false, 
                m_WeaponAnimationLayer);

            yield return new WaitForSeconds(_unequip_duration);

            if (_local_weapon == null || _local_weapon.m_WeaponObject_FP == null || 
                _local_weapon.m_WeaponObject_TP == null) yield break;

            r_ThirdPersonWeaponSlot _slot = FindWeaponSlotByType(_local_weapon.m_WeaponData.m_WeaponType);

            if (_slot != null)
            {
                r_ThirdPersonWeaponSlotItem _slot_item = new r_ThirdPersonWeaponSlotItem 
                { 
                    m_ThirdPersonWeapon = _local_weapon.m_WeaponObject_TP.gameObject, 
                    m_UniqueID = _unique_weapon_id 
                };

                _slot.m_SlotWeapons.Add(_slot_item);
                _local_weapon.m_WeaponObject_TP.transform.parent = _slot.m_WeaponSlot;
                _local_weapon.m_WeaponObject_TP.transform.localPosition = Vector3.zero;
                _local_weapon.m_WeaponObject_TP.transform.localRotation = Quaternion.identity;
            }
        }
        #endregion

        #region Helper Methods
        private r_ThirdPersonWeaponSlot FindWeaponSlotByType(r_WeaponItemType _type) => 
            m_WeaponSlots.Find(x => x.m_WeaponType == _type);

        public void SetLocalRendererShadows(ShadowCastingMode _Mode)
        {
            if (m_localRenderersOnlyShadows.Length == 0) return;

            if (IsOwner)
            {
                foreach (var renderer in m_localRenderersOnlyShadows)
                {
                    if (renderer) renderer.shadowCastingMode = _Mode;
                }
            }
        }
        #endregion

        #region Animation IK
        private void OnAnimatorIK()
        {
            if (m_ThirdPersonAnimator == null) return;

            if (m_CurrentWeapon != null && m_CurrentWeapon.m_WeaponObject_TP != null)
            {
                r_ThirdPersonWeapon _weapon = m_CurrentWeapon.m_WeaponObject_TP;

                if (_weapon != null)
                {
                    float weight = _weapon.m_LeftHandIK ? m_CurrentLeftHandWeight : 0;
                    m_ThirdPersonAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
                    m_ThirdPersonAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, weight);
                    m_ThirdPersonAnimator.SetIKPosition(AvatarIKGoal.LeftHand, _weapon.m_LeftHandTransform.position);
                    m_ThirdPersonAnimator.SetIKRotation(AvatarIKGoal.LeftHand, _weapon.m_LeftHandTransform.rotation);
                }
            }
            else
            {
                m_ThirdPersonAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, m_CurrentLeftHandWeight);
                m_ThirdPersonAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, m_CurrentLeftHandWeight);
            }
        }
        #endregion
    }
}