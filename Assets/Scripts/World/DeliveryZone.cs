using UnityEngine;

public class DeliveryZone : MonoBehaviour
{
    public void TryDeliver(PlayerInventory inventory)
    {
        ItemData selectedItem = inventory.GetSelectedItem();

        if (selectedItem == null)
        {
            Debug.Log("Non hai nessun oggetto da consegnare.");
            return;
        }

        Debug.Log($"[DeliveryZone] Oggetto consegnato: {selectedItem.itemName} — rimosso dall'inventario e distrutto.");

        inventory.RemoveSelectedItem();

        // Qui in futuro chiameremo:
        // TeacherRequestSystem.CheckDeliveredItem(selectedItem);
    }
}