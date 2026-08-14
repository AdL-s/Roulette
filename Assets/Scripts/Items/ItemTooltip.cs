using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Screen-space tooltip that follows the mouse. One per scene, on your UI canvas.
// Setup: Canvas > empty "ItemTooltip" object with this script,
// child panel (Image) containing two TMP texts (name + description).
public class ItemTooltip : MonoBehaviour
{
    private static ItemTooltip _instance;

    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Vector2 offset = new Vector2(18f, -18f);

    private void Awake()
    {
        _instance = this;
        panel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!panel.gameObject.activeSelf || Mouse.current == null) return;
        panel.position = Mouse.current.position.ReadValue() + offset;
    }

    public static void Show(string itemName, string description)
    {
        if (_instance == null) return;
        _instance.nameText.text = itemName;
        _instance.descriptionText.text = description;
        _instance.panel.gameObject.SetActive(true);
    }

    public static void Hide()
    {
        if (_instance == null) return;
        _instance.panel.gameObject.SetActive(false);
    }
}
