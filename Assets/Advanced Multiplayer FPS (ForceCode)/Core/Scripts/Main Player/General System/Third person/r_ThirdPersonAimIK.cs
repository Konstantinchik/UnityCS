using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ForceCodeFPS
{
    #region Serializable classes
    [System.Serializable]
    public class r_AimPart
    {
        [Header("Aim Transform")]
        public Transform m_AimTransform;

        [Header("Weight")]
        [Range(0, 1)] public float m_Weight;
    }
    #endregion

    public class r_ThirdPersonAimIK : NetworkBehaviour
    {
        #region Public Variables
        [Header("Player Controller")]
        public r_PlayerController m_PlayerController;

        [Header("Related Transform")]
        public Transform m_TargetTransform;
        public Transform m_AimTransform;

        [Header("Aim Settings")]
        public float m_AimSmoothness = 5f;

        [Header("Aim Configuration")]
        public List<r_AimPart> m_AimParts = new List<r_AimPart>();
        #endregion

        #region Private Variables
        private float[] m_Weights;
        #endregion

        #region Unity Callbacks
        private void Start()
        {
            if (!IsOwner) return;
            SaveWeights();
        }

        private void Update()
        {
            if (!IsOwner) return;
            HandleWeights();
        }

        private void LateUpdate()
        {
            if (!IsOwner) return;

            if (m_AimParts.Count > 0 && m_TargetTransform != null && m_AimTransform != null)
                HandleAimIK();
        }
        #endregion

        #region Weight Handling
        private void HandleWeights()
        {
            for (int i = 0; i < m_Weights.Length; i++)
            {
                if (m_PlayerController.m_MoveState == r_MoveState.SPRINTING)
                {
                    if (m_AimParts[i].m_Weight != 0)
                        m_AimParts[i].m_Weight = Mathf.Lerp(m_AimParts[i].m_Weight, 0, Time.deltaTime * m_AimSmoothness);
                }
                else
                {
                    if (m_AimParts[i].m_Weight != m_Weights[i])
                        AddWeight(i, m_Weights[i]);
                }
            }
        }
        #endregion

        #region IK Handling
        private void HandleAimIK()
        {
            foreach (r_AimPart aimPart in m_AimParts)
            {
                if (aimPart.m_AimTransform)
                    FaceTarget(aimPart.m_AimTransform, aimPart.m_Weight);
            }
        }

        private void FaceTarget(Transform aimTransform, float weight)
        {
            Vector3 aimDirection = m_AimTransform.forward;
            Vector3 targetDirection = m_TargetTransform.position - aimTransform.position;

            Quaternion aimTowards = Quaternion.FromToRotation(aimDirection, targetDirection);
            Quaternion finalRotation = Quaternion.Slerp(Quaternion.identity, aimTowards, weight);

            aimTransform.rotation = finalRotation * aimTransform.rotation;
        }
        #endregion

        #region Helpers
        private void SaveWeights()
        {
            m_Weights = new float[m_AimParts.Count];
            for (int i = 0; i < m_AimParts.Count; i++)
                m_Weights[i] = m_AimParts[i].m_Weight;
        }

        private void AddWeight(int index, float targetWeight)
        {
            m_AimParts[index].m_Weight = Mathf.Lerp(m_AimParts[index].m_Weight, targetWeight, Time.deltaTime * m_AimSmoothness);
        }
        #endregion
    }
}
