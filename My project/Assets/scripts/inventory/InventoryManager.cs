using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public Transform inventoryPanel;
    public GameObject inventoryItemPrefab;
    [Header("Layout")]
    [SerializeField] private Vector2 cellSize = new Vector2(200f, 240f);
    [SerializeField] private Vector2 cellSpacing = new Vector2(12f, 12f);

    private void Awake()
    {
        InventoryDatabase.Load();
    }

    private void OnEnable()
    {
        UpdateInventoryUI();
        if (InventoryState.Instance != null)
            InventoryState.Instance.InventoryChanged += UpdateInventoryUI;
    }

    private void OnDisable()
    {
        if (InventoryState.Instance != null)
            InventoryState.Instance.InventoryChanged -= UpdateInventoryUI;
    }

    public void UpdateInventoryUI()
    {
        var state = InventoryState.Instance;
        if (state == null)
            return;

        EnsureGridLayout();

        foreach (Transform child in inventoryPanel)
            Destroy(child.gameObject);

        foreach (var entry in state.GetReceivedItemCounts())
        {
            var itemId = entry.Key;
            var amount = Mathf.Max(1, entry.Value);
            ItemData data = InventoryDatabase.GetItem(itemId);
            if (data == null) continue;

            data.quantity = amount;

            GameObject item = Instantiate(inventoryItemPrefab, inventoryPanel);
            item.GetComponent<InventoryItemUI>().Setup(data);
        }
    }

    private void EnsureGridLayout()
    {
        if (inventoryPanel == null) return;

        var grid = inventoryPanel.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = inventoryPanel.gameObject.AddComponent<GridLayoutGroup>();

        grid.cellSize = cellSize;
        grid.spacing = cellSpacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
    }
}
