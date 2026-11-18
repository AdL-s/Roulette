using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Camera")] public Camera playerCamera;

    [Tooltip(
        "Your own body renderers. Hidden from your camera but still cast shadows. Empty = renderers on this object only (not children, so your items and tags stay visible).")]
    [SerializeField]
    private Renderer[] hideFromOwnCamera;

    public float mouseSensitivity = 2f;

    [Range(1f, 90f)] public float verticalClamp = 90f;
    [Range(1f, 90f)] public float horizontalClamp = 90f;

    private CharacterController _controller;
    private float _verticalRotation = 0f;
    private float _horizontalRotation = 0f;

    private RoundManager _round;
    private bool _phaseSubscribed;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
                var listener = playerCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }

            enabled = false;
            return;
        }

        HideOwnBody();
        StartCoroutine(WaitForRoundManagerAndSubscribe());
    }

    private void HideOwnBody()
    {
        if (hideFromOwnCamera == null || hideFromOwnCamera.Length == 0)
            hideFromOwnCamera = GetComponents<Renderer>();

        foreach (var r in hideFromOwnCamera)
        {
            if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        Unsubscribe();
    }

    private IEnumerator WaitForRoundManagerAndSubscribe()
    {
        while (RoundManager.Instance == null)
        {
            yield return null;
        }

        _round = RoundManager.Instance;

        if (_round.IsReady) SubscribeAndRefresh();
        else _round.OnReady += SubscribeAndRefresh;
    }

    private void SubscribeAndRefresh()
    {
        if (_round == null) return;

        _round.OnReady -= SubscribeAndRefresh;

        if (!_phaseSubscribed)
        {
            _round.CurrentPhase.OnValueChanged += OnPhaseChanged;
            _phaseSubscribed = true;
        }

        RefreshFreezeState();
    }

    private void Unsubscribe()
    {
        if (_round != null)
        {
            _round.OnReady -= SubscribeAndRefresh;
            if (_phaseSubscribed) _round.CurrentPhase.OnValueChanged -= OnPhaseChanged;
        }

        _phaseSubscribed = false;
        _round = null;
    }

    private void OnPhaseChanged(GamePhase oldPhase, GamePhase newPhase)
    {
        RefreshFreezeState();
    }

    public void RefreshFreezeState()
    {
        bool questionActive = RoundManager.Instance != null &&
                              RoundManager.Instance.CurrentPhase.Value == GamePhase.Question;
        bool paused = GameUIManager.Instance != null && GameUIManager.Instance.IsPaused;
        bool freeze = questionActive || paused;

        enabled = !freeze;
        Cursor.lockState = freeze ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = freeze;
    }

    void Start()
    {
        if (IsOwner)
        {
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", Color.green); // URP Lit
                block.SetColor("_Color", Color.green); // Built-in
                rend.SetPropertyBlock(block);
            }
        }

        _controller = GetComponent<CharacterController>();

        if (playerCamera == null)
            Debug.LogWarning("[PlayerMovement] No camera assigned!");
    }

    void Update()
    {
        HandleCameraRotation();
    }

    private void HandleCameraRotation()
    {
        if (playerCamera == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * (mouseSensitivity * 0.1f);

        _verticalRotation -= mouseDelta.y;
        _horizontalRotation += mouseDelta.x;

        _verticalRotation = Mathf.Clamp(_verticalRotation, -verticalClamp, verticalClamp);
        _horizontalRotation = Mathf.Clamp(_horizontalRotation, -horizontalClamp, horizontalClamp);

        playerCamera.transform.localRotation = Quaternion.Euler(_verticalRotation, _horizontalRotation, 0f);
    }
}