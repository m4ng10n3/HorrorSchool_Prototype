using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public List<ItemData> items = new List<ItemData>();
    public int selectedIndex = 0;

    public event Action OnInventoryChanged;

    public void AddItem(ItemData item)
    {
        if (item == null) return;

        items.Add(item);

        if (items.Count == 1)
        {
            selectedIndex = 0;
        }

        Debug.Log("Raccolto: " + item.itemName);
        OnInventoryChanged?.Invoke();
    }

    public void RemoveSelectedItem()
    {
        if (items.Count == 0) return;

        Debug.Log("Rimosso: " + items[selectedIndex].itemName);
        items.RemoveAt(selectedIndex);

        if (items.Count == 0)
        {
            selectedIndex = 0;
        }
        else if (selectedIndex >= items.Count)
        {
            selectedIndex = items.Count - 1;
        }

        OnInventoryChanged?.Invoke();
    }

    public ItemData GetSelectedItem()
    {
        if (items.Count == 0) return null;
        return items[selectedIndex];
    }

    public void SelectNextItem()
    {
        if (items.Count == 0) return;

        selectedIndex++;
        if (selectedIndex >= items.Count)
            selectedIndex = 0;

        Debug.Log("Oggetto selezionato: " + items[selectedIndex].itemName);
        OnInventoryChanged?.Invoke();
    }

    public void SelectPreviousItem()
    {
        if (items.Count == 0) return;

        selectedIndex--;
        if (selectedIndex < 0)
            selectedIndex = items.Count - 1;

        Debug.Log("Oggetto selezionato: " + items[selectedIndex].itemName);
        OnInventoryChanged?.Invoke();
    }
}
