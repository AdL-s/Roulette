using TMPro;
using UnityEngine;

public class ItemTooltip : MonoBehaviour
{
    private static ItemTooltip _instance;

    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Vector2 offset = new Vector2(18f, -18f);

    private Canvas _canvas;

    private void Awake()
    {
        _instance = this;

        if (panel == null || nameText == null || descriptionText == null)
        {
            Debug.LogError("[ItemTooltip] Panel, Name Text or Description Text is not assigned.", this);
            return;
        }

        panel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (panel == null || !panel.gameObject.activeSelf) return;

        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null) _canvas = _canvas.rootCanvas;
        }

        Vector2 screenPos = ItemSystem.PointerPosition + offset;

        if (_canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            panel.position = screenPos;
        }
        else
        {
            // Screen Space - Camera / World Space canvas
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                _canvas.transform as RectTransform, screenPos, _canvas.worldCamera, out Vector3 world);
            panel.position = world;
        }
    }

    private static ItemTooltip Get()
    {
        if (_instance == null)
            _instance = FindFirstObjectByType<ItemTooltip>(FindObjectsInactive.Include);
        return _instance;
    }

    public static void Show(string itemName, string description)
    {
        var tip = Get();
        if (tip == null)
        {
            Debug.LogWarning("[ItemTooltip] No ItemTooltip in the scene.");
            return;
        }

        if (tip.panel == null || tip.nameText == null || tip.descriptionText == null) return;

        if (!tip.gameObject.activeSelf) tip.gameObject.SetActive(true);

        tip.nameText.text = itemName;
        tip.descriptionText.text = description;
        tip.panel.gameObject.SetActive(true);
    }

    public static void Hide()
    {
        if (_instance == null || _instance.panel == null) return;
        _instance.panel.gameObject.SetActive(false);
    }
}