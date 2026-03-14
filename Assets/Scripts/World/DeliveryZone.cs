using UnityEngine;

public class DeliveryZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        WorldItem worldItem = other.GetComponent<WorldItem>();

        if (worldItem == null) return;

        Debug.Log($"[DeliveryZone] Oggetto consegnato: {worldItem.itemData.itemName} — distrutto.");

        // Qui in futuro chiameremo:
        // TeacherRequestSystem.CheckDeliveredItem(worldItem.itemData);

        Destroy(other.gameObject);
    }
}