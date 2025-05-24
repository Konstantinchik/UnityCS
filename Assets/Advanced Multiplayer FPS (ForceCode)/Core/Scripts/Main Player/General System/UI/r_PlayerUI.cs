using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace ForceCodeFPS
{
    public class r_PlayerUI : NetworkBehaviour
    {
        #region References
        public r_PlayerController m_PlayerController;
        #endregion

        #region Public variables
        [Header("UI Base Configuration")]
        public r_PlayerUIBase m_UIBase;

        [Header("Player HUD")]
        public GameObject m_PlayerHUD;

        [Header("Killmessage UI")]
        public Transform m_KillMessageContent;
        public GameObject m_KillMessagePrefab;

        [Header("Crosshair UI")]
        public RectTransform m_CrosshairRectTransform;
        public RawImage m_CrosshairCenterPointImage;

        [Header("Hitmarker UI")]
        public Image m_HitmarkerImage;

        [Header("Vitals UI")]
        public Text m_StaminaText;

        [Header("Bloody UI")]
        public Image m_BloodyScreenImage;
        public Image m_DamageIndicatorImage;

        [Space(10)]
        public Text m_HealthText;
        public Image m_HealthImage;

        [Header("Weapon UI")]
        public RawImage m_LocalWeaponPreview;
        public Text m_LocalWeaponName;
        public Text m_LocalWeaponAmmo;

        [Space(10)]
        public Text m_WeaponPickupText;
        public RawImage m_WeaponPickupImage;
        #endregion

        #region Private variables
        [HideInInspector] public float m_CurrentBloodyAlpha;
        [HideInInspector] public float m_CurrentIndicatorAlpha;
        [HideInInspector] public float m_CurrentHitmarkerTime;
        [HideInInspector] public float m_CurrentHitmarkerPosition;
        [HideInInspector] public float m_CurrentHitmarkerRotation;
        [HideInInspector] public float m_CurrentCrosshairSize;
        [HideInInspector] public float m_CurrentCrosshairRotation;
        [HideInInspector] public float m_CurrentCrosshairShootMultiplier;
        [HideInInspector] public Vector3 m_LastEnemyPosition;
        #endregion

        #region Unity Methods
        private void Start() => SetDefaults();

        private void Update()
        {
            HandleHitmarker();
            HandleWeaponUI();
            HandleStaminaUI();
            HandleCrosshairState();

            if (this.m_UIBase.m_BloodyScreenFeature) HandleBloodyScreen();
            if (this.m_UIBase.m_DamageIndicatorFeature) HandleDamageIndicator();
        }
        #endregion

        #region Handling Methods
        private void HandleBloodyScreen()
        {
            if (this.m_CurrentBloodyAlpha > 0.0f)
            {
                this.m_CurrentBloodyAlpha -= Time.deltaTime;
                SetBloodyScreenAlpha(this.m_CurrentBloodyAlpha);
            }
        }

        private void HandleDamageIndicator()
        {
            if (this.m_CurrentIndicatorAlpha > 0.0f)
            {
                this.m_CurrentIndicatorAlpha -= Time.deltaTime;
                SetDamageIndicatorAlpha(this.m_CurrentIndicatorAlpha);

                Vector3 _direction = new Vector3((m_LastEnemyPosition - this.m_PlayerController.transform.position).x, 0.0f, (m_LastEnemyPosition - this.m_PlayerController.transform.position).z).normalized;
                Vector3 _cross = Vector3.Cross(this.m_PlayerController.transform.forward, _direction);
                float _angle = Vector3.Angle(this.m_PlayerController.transform.forward, _direction) * Mathf.Sign(_cross.y);
                _angle += transform.rotation.eulerAngles.y;

                this.m_DamageIndicatorImage.transform.eulerAngles = new Vector3(0, 0, -_angle);
            }
        }

        private void HandleHitmarker()
        {
            if (this.m_CurrentHitmarkerTime > 0.0f)
            {
                this.m_CurrentHitmarkerTime -= Time.deltaTime;

                if (this.m_CurrentHitmarkerRotation != 0)
                {
                    this.m_CurrentHitmarkerRotation = Mathf.Lerp(this.m_CurrentHitmarkerRotation, 0, this.m_UIBase.m_HitmarkerRotationReturnSpeed * Time.deltaTime);
                    this.m_HitmarkerImage.transform.rotation = Quaternion.Euler(0, 0, this.m_CurrentHitmarkerRotation);
                }
            }

            this.m_HitmarkerImage.gameObject.SetActive(this.m_CurrentHitmarkerTime > 0);
        }

        private void HandleCrosshairState()
        {
            foreach (r_CrosshairState _State in this.m_UIBase.m_CrosshairStates)
            {
                if (_State.m_MoveState == this.m_PlayerController.m_MoveState)
                {
                    if (this.m_CurrentCrosshairSize != _State.m_CrosshairSize)
                    {
                        HandleCrosshair(_State.m_CrosshairSize, _State.m_CrosshairAdjustSpeed, _State.m_CrosshairRotation, _State.m_CrosshairRotationAdjustSpeed);
                    }
                }
            }

            this.m_CurrentCrosshairShootMultiplier = Mathf.Lerp(this.m_CurrentCrosshairShootMultiplier, 0, this.m_UIBase.m_CrosshairShootReturnSpeed);

            bool _crosshair_State = this.m_PlayerController.m_WeaponManager.m_CurrentWeapon == null ? false :
                (this.m_PlayerController.m_MoveState != r_MoveState.SPRINTING && this.m_PlayerController.m_PlayerCamera.m_CameraState != r_CameraState.AIMING);

            if (this.m_CrosshairRectTransform.gameObject.activeSelf != _crosshair_State)
                SetCrosshairState(_crosshair_State);
        }

        private void HandleCrosshair(float _CrosshairSize, float _CrosshairAdjustSpeed, float _CrosshairRotation, float _CrosshairRotationAdjustSpeed)
        {
            this.m_CurrentCrosshairSize = Mathf.Lerp(this.m_CurrentCrosshairSize, _CrosshairSize, Time.deltaTime * _CrosshairAdjustSpeed);
            this.m_CurrentCrosshairSize += this.m_CurrentCrosshairShootMultiplier;
            this.m_CrosshairRectTransform.sizeDelta = new Vector2(this.m_CurrentCrosshairSize, this.m_CurrentCrosshairSize);

            if (this.m_CurrentCrosshairRotation != _CrosshairRotation)
            {
                this.m_CurrentCrosshairRotation = Mathf.Lerp(this.m_CurrentCrosshairRotation, _CrosshairRotation, Time.deltaTime * _CrosshairRotationAdjustSpeed);
                this.m_CrosshairRectTransform.localEulerAngles = new Vector3(0, 0, this.m_CurrentCrosshairRotation);
            }
        }

        private void HandleWeaponUI()
        {
            r_WeaponManagerData _current_weapon = this.m_PlayerController.m_WeaponManager.m_CurrentWeapon;

            if (_current_weapon != null && this.m_PlayerController.m_WeaponManager.m_LocalWeapons.Count > 0)
            {
                if (!this.m_LocalWeaponPreview.enabled) this.m_LocalWeaponPreview.enabled = true;

                r_WeaponController _weapon = _current_weapon.m_WeaponObject_FP;

                if (_current_weapon.m_WeaponData != null)
                {
                    this.m_LocalWeaponName.text = _current_weapon.m_WeaponData.m_WeaponName;
                    this.m_LocalWeaponPreview.texture = _current_weapon.m_WeaponData.m_WeaponTexture;
                }

                if (_weapon != null)
                {
                    if (this.m_CrosshairCenterPointImage != null)
                        this.m_CrosshairCenterPointImage.gameObject.SetActive(_weapon.m_WeaponAimState != r_WeaponAimState.AIMING);

                    Color _mag_color = _weapon.m_Ammunation.m_Ammo == 0 ? m_UIBase.m_EmptyAmmunationColor :
                        (_weapon.m_Ammunation.m_Ammo <= _weapon.m_WeaponConfig.m_ReloadSettings.m_AmmoWarningOnBulletsLeft ?
                        m_UIBase.m_WarningAmmunationColor : m_UIBase.m_DefaultAmmunationColor);

                    Color _total_color = this.m_PlayerController.m_WeaponManager.m_TotalAmmunation == 0 ? m_UIBase.m_EmptyAmmunationColor :
                        (this.m_PlayerController.m_WeaponManager.m_TotalAmmunation <= _weapon.m_WeaponConfig.m_ReloadSettings.m_AmmoWarningOnBulletsLeft ?
                        m_UIBase.m_WarningAmmunationColor : m_UIBase.m_DefaultAmmunationColor);

                    string _mag_hex = ColorUtility.ToHtmlStringRGBA(_mag_color);
                    string _total_hex = ColorUtility.ToHtmlStringRGBA(_total_color);

                    this.m_LocalWeaponAmmo.text = $"<color=#{_mag_hex}>{_weapon.m_Ammunation.m_Ammo:000}</color> / <color=#{_total_hex}>{this.m_PlayerController.m_WeaponManager.m_TotalAmmunation:000}</color>";
                }
            }
            else
            {
                this.m_LocalWeaponName.text = "-";
                this.m_LocalWeaponAmmo.text = "- / -";
                this.m_LocalWeaponPreview.enabled = false;
            }
        }

        private void HandleStaminaUI() =>
            this.m_StaminaText.text = Mathf.RoundToInt(this.m_PlayerController.m_stamina).ToString();
        #endregion

        #region Actions
        public void OnCrosshairShoot(float _crosshair_increase_size) =>
            this.m_CurrentCrosshairShootMultiplier += _crosshair_increase_size;
        #endregion

        #region Setters
        private void SetDefaults()
        {
            this.m_HitmarkerImage.gameObject.SetActive(false);
            SetBloodyScreenAlpha(0);
            SetDamageIndicatorAlpha(0);
            SetHealthText(this.m_PlayerController.m_PlayerHealth.m_Health.Value);
        }

        public void SetBloodyScreen()
        {
            if (!this.m_UIBase.m_BloodyScreenFeature) return;
            this.m_CurrentBloodyAlpha = this.m_UIBase.m_BloodyScreenTime;
        }

        public void SetDamageIndicator(ulong senderClientId, Vector3 enemyPosition)
        {
            if (!this.m_UIBase.m_DamageIndicatorFeature) return;
            if (senderClientId == NetworkManager.Singleton.LocalClientId) return;

            this.m_LastEnemyPosition = enemyPosition;
            this.m_CurrentIndicatorAlpha = this.m_UIBase.m_DamageIndicatorTime;
        }

        public void SetHitmarker(bool enemyKilled)
        {
            this.m_HitmarkerImage.gameObject.SetActive(false);
            this.m_CurrentHitmarkerTime = this.m_UIBase.m_HitmarkerTime;
            this.m_HitmarkerImage.color = enemyKilled ? this.m_UIBase.m_HitmarkerKilledEnemyColor : this.m_UIBase.m_HitmarkerDefaultColor;
            this.m_CurrentHitmarkerRotation = Random.Range(-this.m_UIBase.m_HitmarkerRandomRotation, this.m_UIBase.m_HitmarkerRandomRotation);
        }

        public void SetKillmessage(string eliminatedName)
        {
            GameObject killmessage = Instantiate(this.m_KillMessagePrefab, this.m_KillMessageContent);
            string text = $"ELIMINATED <color=#ff0000ff>{eliminatedName}</color>".ToUpper();
            killmessage.GetComponent<Text>().text = text;
            Destroy(killmessage, 2f);
        }

        public void SetHealthText(float health)
        {
            Color color = health <= this.m_PlayerController.m_PlayerHealth.m_HealthBase.m_HealthWarning ?
                this.m_UIBase.m_WarningHealthColor : this.m_UIBase.m_DefaultHealthColor;

            this.m_HealthImage.color = color;
            this.m_HealthText.color = color;
            this.m_HealthText.text = Mathf.RoundToInt(health).ToString("000");
        }

        public void SetCrosshairState(bool state)
        {
            if (this.m_CrosshairRectTransform.gameObject.activeSelf != state)
                this.m_CrosshairRectTransform.gameObject.SetActive(state);
        }

        private void SetBloodyScreenAlpha(float alpha) =>
            this.m_BloodyScreenImage.color = new Color(this.m_BloodyScreenImage.color.r, this.m_BloodyScreenImage.color.g, this.m_BloodyScreenImage.color.b, alpha);

        private void SetDamageIndicatorAlpha(float alpha) =>
            this.m_DamageIndicatorImage.color = new Color(this.m_DamageIndicatorImage.color.r, this.m_DamageIndicatorImage.color.g, this.m_DamageIndicatorImage.color.b, alpha);
        #endregion
    }
}
