using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Adds a smooth, gusting wind force to one or more Dynamic Bone components.
/// Rotate this GameObject to change the wind direction; its blue Z axis is the
/// direction the wind travels.
/// </summary>
[AddComponentMenu("Dynamic Bone/Dynamic Bone Wind")]
[DefaultExecutionOrder(-10000)]
[RequireComponent(typeof(PhotonView))]
public class DynamicBoneWind : MonoBehaviour, IPunObservable
{
    [Header("Affected Bones")]
    [Tooltip("Also affect every active Dynamic Bone in the scene, including ones on other prefabs.")]
    public bool m_AffectAllDynamicBones = true;

    [Min(0.1f)]
    [Tooltip("How often spawned Dynamic Bones are discovered.")]
    public float m_RefreshInterval = 0.5f;

    [Tooltip("Optional extra Dynamic Bone components. Use these when Affect All Dynamic Bones is off.")]
    public List<DynamicBone> m_DynamicBones = new List<DynamicBone>();

    [Header("Networking")]
    [Tooltip("Synchronize this wind's settings and sway phase through Photon PUN 2. Cosmetic bone positions are simulated locally.")]
    public bool m_SyncWithPhoton = true;

    [Header("Wind")]
    [Tooltip("The direction the wind travels. Local uses this object's forward direction.")]
    public bool m_UseLocalDirection = true;

    [Tooltip("Used when Local Direction is disabled. This is a world-space direction.")]
    public Vector3 m_WorldDirection = Vector3.forward;

    [Min(0)]
    [Tooltip("Maximum force used by the sway. Start low and increase gradually.")]
    public float m_Strength = 0.2f;

    [Range(0, 1)]
    [Tooltip("How much the wind continually leans in its main direction. Lower values produce a more natural side-to-side sway.")]
    public float m_SustainedPush = 0.15f;

    [Min(0)]
    [Tooltip("How quickly hair sways from side to side.")]
    public float m_SwayFrequency = 1.25f;

    [Range(0, 1.5f)]
    [Tooltip("How wide the side-to-side sway is.")]
    public float m_SwayAmount = 0.8f;

    [Min(0)]
    [Tooltip("Seconds used to soften changes in the wind force. Zero applies sway immediately.")]
    public float m_SwaySmoothing = 0.2f;

    [Range(0, 1)]
    [Tooltip("How much the wind strength swells and eases over time.")]
    public float m_Gustiness = 0.35f;

    [Min(0)]
    [Tooltip("How quickly gusts change. Lower values create broad, gentle gusts.")]
    public float m_GustFrequency = 0.35f;

    [Range(0, 45)]
    [Tooltip("Small side-to-side variation in the wind direction, in degrees.")]
    public float m_DirectionVariation = 8f;

    [Tooltip("Different seed values create a different gust pattern.")]
    public float m_Seed;

    [Header("Scene View")]
    public Color m_GizmoColor = new Color(0.2f, 0.75f, 1f, 0.9f);
    [Min(0.1f)] public float m_GizmoLength = 2f;

    readonly List<DynamicBone> m_AffectedBones = new List<DynamicBone>();
    PhotonView m_PhotonView;
    Vector3 m_SynchronizedDirection = Vector3.forward;
    Vector3 m_CurrentWindForce;
    Vector3 m_WindForceVelocity;
    bool m_HasSynchronizedDirection;
    float m_NextRefreshTime;

    void OnEnable()
    {
        m_PhotonView = GetComponent<PhotonView>();
        RegisterWithPhotonView();
        RefreshAffectedBones();
    }

    void OnDisable()
    {
        ClearWindForce();
    }

    void Update()
    {
        if (Time.time >= m_NextRefreshTime)
            RefreshAffectedBones();

        Vector3 direction = GetWindDirection();
        float time = GetWindTime();
        float gust = Mathf.PerlinNoise(time, m_Seed + 17.31f) * 2f - 1f;
        float strength = m_Strength * (1f + gust * m_Gustiness);

        Vector3 side = GetPerpendicular(direction);
        Vector3 up = Vector3.Cross(side, direction).normalized;
        float swayTime = time * m_SwayFrequency * Mathf.PI * 2f;
        float sideSway = (Mathf.Sin(swayTime + m_Seed) + Mathf.Sin(swayTime * 0.47f + m_Seed * 2.17f) * 0.35f) * m_SwayAmount;
        float verticalSway = Mathf.Sin(swayTime * 0.73f + m_Seed * 3.41f) * m_SwayAmount * 0.18f;
        float irregularity = (Mathf.PerlinNoise(time, m_Seed + 48.72f) * 2f - 1f) * m_DirectionVariation / 45f;
        Vector3 targetWindForce = (direction * m_SustainedPush + side * (sideSway + irregularity) + up * verticalSway) * strength;
        m_CurrentWindForce = m_SwaySmoothing > 0
            ? Vector3.SmoothDamp(m_CurrentWindForce, targetWindForce, ref m_WindForceVelocity, m_SwaySmoothing)
            : targetWindForce;

        foreach (DynamicBone bone in m_AffectedBones)
        {
            if (bone != null)
                bone.m_ExternalForce = m_CurrentWindForce;
        }
    }

    [ContextMenu("Refresh Dynamic Bones in Scene")]
    void RefreshAffectedBones()
    {
        m_AffectedBones.Clear();

        AddUniqueBones(m_DynamicBones);
        if (m_AffectAllDynamicBones)
            AddUniqueBones(FindObjectsByType<DynamicBone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));

        m_NextRefreshTime = Time.time + m_RefreshInterval;
    }

    void AddUniqueBones(IEnumerable<DynamicBone> bones)
    {
        foreach (DynamicBone bone in bones)
        {
            if (bone != null && !m_AffectedBones.Contains(bone))
                m_AffectedBones.Add(bone);
        }
    }

    void ClearWindForce()
    {
        foreach (DynamicBone bone in m_AffectedBones)
        {
            if (bone != null)
                bone.m_ExternalForce = Vector3.zero;
        }
    }

    Vector3 GetWindDirection()
    {
        if (ShouldUseSynchronizedWind())
            return m_SynchronizedDirection;

        Vector3 direction = m_UseLocalDirection ? transform.forward : m_WorldDirection;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
    }

    float GetWindTime()
    {
        return m_SyncWithPhoton && PhotonNetwork.InRoom ? (float)PhotonNetwork.Time : Time.time;
    }

    bool ShouldUseSynchronizedWind()
    {
        return m_SyncWithPhoton && PhotonNetwork.InRoom && m_PhotonView != null && !m_PhotonView.IsMine && m_HasSynchronizedDirection;
    }

    // Keeps the PUN setup on every copy of this object identical without requiring
    // Dynamic Bone components themselves to be networked.
    void RegisterWithPhotonView()
    {
        if (!m_SyncWithPhoton || m_PhotonView == null)
            return;

        if (m_PhotonView.ObservedComponents == null)
            m_PhotonView.ObservedComponents = new List<Component>();

        if (!m_PhotonView.ObservedComponents.Contains(this))
            m_PhotonView.ObservedComponents.Add(this);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (!m_SyncWithPhoton)
            return;

        if (stream.IsWriting)
        {
            stream.SendNext(m_UseLocalDirection ? transform.forward : m_WorldDirection);
            stream.SendNext(m_Strength);
            stream.SendNext(m_SustainedPush);
            stream.SendNext(m_SwayFrequency);
            stream.SendNext(m_SwayAmount);
            stream.SendNext(m_SwaySmoothing);
            stream.SendNext(m_Gustiness);
            stream.SendNext(m_GustFrequency);
            stream.SendNext(m_DirectionVariation);
            stream.SendNext(m_Seed);
        }
        else
        {
            m_SynchronizedDirection = ((Vector3)stream.ReceiveNext()).normalized;
            m_Strength = (float)stream.ReceiveNext();
            m_SustainedPush = (float)stream.ReceiveNext();
            m_SwayFrequency = (float)stream.ReceiveNext();
            m_SwayAmount = (float)stream.ReceiveNext();
            m_SwaySmoothing = (float)stream.ReceiveNext();
            m_Gustiness = (float)stream.ReceiveNext();
            m_GustFrequency = (float)stream.ReceiveNext();
            m_DirectionVariation = (float)stream.ReceiveNext();
            m_Seed = (float)stream.ReceiveNext();
            m_HasSynchronizedDirection = true;
        }
    }

    void OnDrawGizmos()
    {
        Vector3 origin = transform.position;
        Vector3 direction = GetWindDirection();
        float length = Mathf.Max(0.1f, m_GizmoLength);
        Vector3 side = GetPerpendicular(direction);

        Gizmos.color = new Color(m_GizmoColor.r, m_GizmoColor.g, m_GizmoColor.b, 0.18f);
        Gizmos.DrawSphere(origin, length * 0.12f);

        Gizmos.color = m_GizmoColor;
        DrawArrow(origin, direction, length);
        DrawArrow(origin + side * length * 0.28f, direction, length * 0.7f);
        DrawArrow(origin - side * length * 0.28f, direction, length * 0.7f);
    }

    void DrawArrow(Vector3 origin, Vector3 direction, float length)
    {
        Vector3 end = origin + direction * length;
        Vector3 side = GetPerpendicular(direction);
        Vector3 up = Vector3.Cross(side, direction).normalized;
        float headLength = Mathf.Min(length * 0.28f, 0.45f);

        Gizmos.DrawLine(origin, end);
        Gizmos.DrawLine(end, end - direction * headLength + side * headLength * 0.5f);
        Gizmos.DrawLine(end, end - direction * headLength - side * headLength * 0.5f);
        Gizmos.DrawLine(end, end - direction * headLength + up * headLength * 0.5f);
        Gizmos.DrawLine(end, end - direction * headLength - up * headLength * 0.5f);
    }

    Vector3 GetPerpendicular(Vector3 direction)
    {
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up);
        if (perpendicular.sqrMagnitude < 0.001f)
            perpendicular = Vector3.Cross(direction, Vector3.right);
        return perpendicular.normalized;
    }
}
