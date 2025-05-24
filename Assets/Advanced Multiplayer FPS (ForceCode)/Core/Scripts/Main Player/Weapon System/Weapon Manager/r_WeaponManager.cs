// New implementation using Unity Netcode for GameObjects
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace ForceCodeFPS
{
    [System.Serializable] public enum r_WeaponItemType { PRIMARY, SECONDARY, LETHAL, TACTICAL }

    public class r_WeaponManagerData
    {
        [Header("Weapon Data")]
        public r_WeaponPickupBase m_WeaponData;

        [Header("Runtime Data")]
        [HideInInspector] public r_WeaponController m_WeaponObject_FP;
        [HideInInspector] public r_ThirdPersonWeapon m_WeaponObject_TP;

        [Header("Unique runtime ID")]
        [HideInInspector] public string m_UniqueID = System.Guid.NewGuid().ToString();
    }

    public class r_WeaponManager : NetworkBehaviour
    {
        public r_PlayerController m_PlayerController;

        [Header("Weapon Holder")]
        [SerializeField] private Transform m_WeaponParent;

        [Header("Camera Holder")]
        [SerializeField] private Camera m_Camera;

        [Space(10)]
        public List<r_WeaponManagerData> m_AllWeapons = new();
        public List<r_WeaponManagerData> m_LocalWeapons = new();
        private NetworkList<int> m_NetworkWeaponIDs;

        [Header("Pickup Settings")]
        [SerializeField] private int m_WeaponSlots;
        [SerializeField] private float m_PickupDistance;

        [Header("Drop Settings")]
        [SerializeField] private float m_WeaponDropForce;

        [Header("Ammunation Settings")]
        public int m_TotalAmmunation;

        [HideInInspector] public int m_CurrentWeaponIndex;
        [HideInInspector] public bool m_ChangingWeapon;
        [HideInInspector] public bool m_ReloadingWeapon;
        [HideInInspector] public r_WeaponManagerData m_CurrentWeapon = null;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                m_NetworkWeaponIDs = new NetworkList<int>();
            }
            else
            {
                m_NetworkWeaponIDs = new NetworkList<int>();
            }

            m_NetworkWeaponIDs.OnListChanged += OnNetworkWeaponsChanged;
        }

        private void OnNetworkWeaponsChanged(NetworkListEvent<int> change)
        {
            if (!IsOwner && change.Type == NetworkListEvent<int>.EventType.Add)
            {
                int index = change.Index;
                int weaponID = change.Value;
                CreateWeapon(index, weaponID);
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            HandleInputs();
            HandleStates();
        }

        private void HandleInputs()
        {
            if (Physics.Raycast(m_Camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0)), out RaycastHit hit, m_PickupDistance))
            {
                var weaponPickup = hit.transform.GetComponent<r_WeaponPickup>();
                if (weaponPickup != null)
                {
                    var ui = m_PlayerController.m_PlayerUI;
                    if (!ui.m_WeaponPickupImage.gameObject.activeSelf) ui.m_WeaponPickupImage.gameObject.SetActive(true);

                    ui.m_WeaponPickupText.text = $"Hold [F] to pickup [{weaponPickup.m_WeaponPickupData.m_WeaponName}]";
                    ui.m_WeaponPickupImage.texture = weaponPickup.m_WeaponPickupData.m_WeaponTexture;

                    if (m_PlayerController.m_InputManager.WeaponPickKey())
                    {
                        OnValidatePickup(weaponPickup.m_WeaponPickupData.m_WeaponID);
                        weaponPickup.OnPickup();
                    }
                }
            }

            if (m_PlayerController.m_InputManager.WeaponDropKey() && m_LocalWeapons.Count > 0 && !m_ChangingWeapon)
            {
                DropWeaponServerRpc(m_CurrentWeaponIndex);
            }

            if (m_LocalWeapons.Count > 0 && !m_ChangingWeapon)
            {
                for (int i = 1; i <= m_LocalWeapons.Count && i <= 5; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha0 + i)) ChangeWeaponServerRpc(i - 1);
                }
            }
        }

        private void HandleStates()
        {
            m_ReloadingWeapon = m_LocalWeapons.Any(w => w.m_WeaponObject_FP != null && w.m_WeaponObject_FP.IsReloading);
        }

        private void OnValidatePickup(int weaponID)
        {
            if (m_LocalWeapons.Count >= m_WeaponSlots)
                SwapWeaponServerRpc(weaponID);
            else
                PickupWeaponServerRpc(weaponID);
        }

        [ServerRpc]
        private void PickupWeaponServerRpc(int weaponID)
        {
            var weaponData = m_AllWeapons.FirstOrDefault(w => w.m_WeaponData.m_WeaponID == weaponID)?.m_WeaponData;
            if (weaponData == null) return;

            r_WeaponManagerData newWeapon = new() { m_WeaponData = weaponData };
            m_LocalWeapons.Add(newWeapon);
            m_NetworkWeaponIDs.Add(weaponID);

            int newIndex = m_LocalWeapons.Count - 1;
            CreateWeapon(newIndex, weaponID);
            ChangeWeaponClientRpc(newIndex);
        }

        [ServerRpc]
        private void SwapWeaponServerRpc(int weaponID)
        {
            if (m_CurrentWeaponIndex < 0 || m_CurrentWeaponIndex >= m_LocalWeapons.Count) return;
            var weaponData = m_AllWeapons.FirstOrDefault(w => w.m_WeaponData.m_WeaponID == weaponID)?.m_WeaponData;
            if (weaponData == null) return;

            m_LocalWeapons[m_CurrentWeaponIndex] = new r_WeaponManagerData { m_WeaponData = weaponData };
            m_NetworkWeaponIDs[m_CurrentWeaponIndex] = weaponID;
            CreateWeapon(m_CurrentWeaponIndex, weaponID);
            ChangeWeaponClientRpc(m_CurrentWeaponIndex);
        }

        [ServerRpc]
        private void DropWeaponServerRpc(int index)
        {
            if (index < 0 || index >= m_LocalWeapons.Count) return;

            var weaponPrefab = m_LocalWeapons[index].m_WeaponData.m_Weapon_Pickup_Prefab;
            GameObject drop = Instantiate(weaponPrefab, m_Camera.transform.position, m_Camera.transform.rotation);
            if (drop.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false;
                rb.AddForce(m_Camera.transform.forward * m_WeaponDropForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 100f);
            }
            NetworkObject netObj = drop.GetComponent<NetworkObject>();
            if (netObj) netObj.Spawn();

            m_LocalWeapons.RemoveAt(index);
            m_NetworkWeaponIDs.RemoveAt(index);
        }

        [ServerRpc]
        private void ChangeWeaponServerRpc(int index)
        {
            if (index < 0 || index >= m_LocalWeapons.Count) return;
            ChangeWeaponClientRpc(index);
        }

        [ClientRpc]
        private void ChangeWeaponClientRpc(int index)
        {
            if (index < 0 || index >= m_LocalWeapons.Count) return;

            StartCoroutine(ChangeWeaponCoroutine(m_CurrentWeaponIndex, index));
        }

        private void CreateWeapon(int index, int weaponID)
        {
            var weaponData = m_AllWeapons.FirstOrDefault(w => w.m_WeaponData.m_WeaponID == weaponID)?.m_WeaponData;
            if (weaponData == null) return;

            var data = new r_WeaponManagerData { m_WeaponData = weaponData };
            m_LocalWeapons.Insert(index, data);

            data.m_WeaponObject_FP = Instantiate(weaponData.m_Weapon_FP_Prefab, m_WeaponParent);
            data.m_WeaponObject_TP = Instantiate(weaponData.m_Weapon_TP_Prefab, m_PlayerController.m_ThirdPersonManager.m_WeaponParent);
        }

        private IEnumerator ChangeWeaponCoroutine(int fromIndex, int toIndex)
        {
            m_ChangingWeapon = true;

            if (fromIndex >= 0 && fromIndex < m_LocalWeapons.Count)
            {
                m_LocalWeapons[fromIndex].m_WeaponObject_FP.UnEquipWeapon();
                m_PlayerController.m_ThirdPersonManager.OnUnequipWeapon(m_LocalWeapons[fromIndex].m_UniqueID, 0);
                yield return new WaitForSeconds(0.2f);
            }

            if (toIndex >= 0 && toIndex < m_LocalWeapons.Count)
            {
                m_LocalWeapons[toIndex].m_WeaponObject_FP.EquipWeapon();
                m_PlayerController.m_ThirdPersonManager.OnEquipWeapon(m_LocalWeapons[toIndex].m_UniqueID, 0);
                m_CurrentWeaponIndex = toIndex;
            }

            m_ChangingWeapon = false;
        }

        // ÇÀÃËÓØÊÀ, ÏÅÐÅÄÅËÛÂÀÒÜ
        public r_WeaponManagerData FindWeaponByName(string weaponName)
        {
            return m_LocalWeapons.FirstOrDefault(w => w.m_WeaponData.m_WeaponName == weaponName);
        }
    }
}
