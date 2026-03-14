using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Displays the player inventory as a horizontal row of item slots at the bottom of the screen.
/// Attach to a UI manager GameObject inside the Canvas.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public PlayerInventory playerInventory;

    [Header("Slot Settings")]
    public Transform slotsContainer;       // Parent with HorizontalLayoutGroup
    public GameObject slotPrefab;          // Prefab: Panel + TextMeshProUGUI child

    [Header("Colors")]
    public Color normalColor = new Color(0.1f, 0.1f, 0.1f, 0.7f);
    public Color selectedColor = new Color(0.8f, 0.2f, 0.1f, 0.9f);
    public Color normalTextColor = Color.white;
    public Color selectedTextColor = Color.white;

    private List<GameObject> slotInstances = new List<GameObject>();

    private void Start()
    {
        if (playerInventory == null)
        {
            Debug.LogError("[InventoryUI] PlayerInventory non assegnato!");
            return;
        }

        playerInventory.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
            playerInventory.OnInventoryChanged -= Refresh;
    }

    private void Refresh()
    {
        // Remove old slots
        foreach (GameObject slot in slotInstances)
            Destroy(slot);

        slotInstances.Clear();

        var items = playerInventory.items;
        int selectedIndex = playerInventory.selectedIndex;

        if (items.Count == 0)
        {
            // Show an empty placeholder slot
            CreateSlot("[ vuoto ]", false);
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            bool isSelected = (i == selectedIndex);
            string label = isSelected ? $"[ {items[i].itemName} ]" : items[i].itemName;
            CreateSlot(label, isSelected);
        }
    }

    private void CreateSlot(string label, bool isSelected)
    {
        GameObject slot = Instantiate(slotPrefab, slotsContainer);
        slotInstances.Add(slot);

        // Background color
        UnityEngine.UI.Image bg = slot.GetComponent<UnityEngine.UI.Image>();
        if (bg != null)
            bg.color = isSelected ? selectedColor : normalColor;

        // Label text
        TextMeshProUGUI text = slot.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = label;
            text.color = isSelected ? selectedTextColor : normalTextColor;
            text.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
        }
    }
}
