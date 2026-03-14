using UnityEngine;

public class DeliveryZone : MonoBehaviour
{
    [Header("Descrizione (tooltip)")]
    [SerializeField] private string zoneName = "Zona di consegna";
    [TextArea] [SerializeField] private string zoneDescription = "Consegna qui l'oggetto richiesto.";

    public string Description => $"{zoneName}\n{zoneDescription}";

    private TeacherRequestSystem teacher;

    private void Awake()
    {
        teacher = FindFirstObjectByType<TeacherRequestSystem>();

        if (teacher == null)
            Debug.LogWarning("[DeliveryZone] Nessun TeacherRequestSystem trovato in scena.");
    }

    private void OnTriggerEnter(Collider other)
    {
        WorldItem worldItem = other.GetComponent<WorldItem>();

        if (worldItem == null) return;

        Debug.Log($"[DeliveryZone] Oggetto ricevuto: {worldItem.itemData.itemName}.");

        bool accepted = teacher != null && teacher.NotifyDelivery(worldItem.itemData);

        if (accepted)
            Destroy(other.gameObject);
    }
}