using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Camera")]
    public Camera playerCamera;

    public float mouseSensitivity = 2f;

    [Range(1f, 90f)] public float verticalClamp = 90f;
    [Range(1f, 90f)] public float horizontalClamp = 90f;

    private CharacterController _controller;
    private float _verticalRotation = 0f;
    private float _horizontalRotation = 0f;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        StartCoroutine(WaitForRoundManagerAndSubscribe());
    }

    private IEnumerator WaitForRoundManagerAndSubscribe()
    {
        while (RoundManager.Instance == null)
        {
            yield return null;
        }

        RoundManager.Instance.CurrentPhase.OnValueChanged += OnPhaseChanged;
        RefreshFreezeState();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.CurrentPhase.OnValueChanged -= OnPhaseChanged;
        }
    }

    private void OnPhaseChanged(GamePhase oldPhase, GamePhase newPhase)
    {
        RefreshFreezeState();
    }

    // Called by GameUIManager too, whenever pause state changes
    public void RefreshFreezeState()
    {
        bool questionActive = RoundManager.Instance != null && RoundManager.Instance.CurrentPhase.Value == GamePhase.Question;
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
            GetComponent<Renderer>().material.color = Color.green;
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

        _verticalRotation   -= mouseDelta.y;
        _horizontalRotation += mouseDelta.x;

        _verticalRotation   = Mathf.Clamp(_verticalRotation,   -verticalClamp,   verticalClamp);
        _horizontalRotation = Mathf.Clamp(_horizontalRotation, -horizontalClamp, horizontalClamp);

        playerCamera.transform.localRotation = Quaternion.Euler(_verticalRotation, _horizontalRotation, 0f);
    }
}