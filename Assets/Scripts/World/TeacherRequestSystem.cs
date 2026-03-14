using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TeacherRequestSystem : MonoBehaviour
{
    [Header("Spawn Points")]
    public List<ItemSpawnPoint> spawnPoints = new List<ItemSpawnPoint>();

    [Header("Timer")]
    public float timeLimit = 30f;

    // Events
    public event Action<ItemData, float> OnNewRequest;    // item richiesto, tempo disponibile
    public event Action<ItemData> OnRequestCompleted;     // item consegnato correttamente
    public event Action<ItemData> OnRequestFailed;        // tempo scaduto
    public event Action OnAllDelivered;                   // tutti gli oggetti consegnati

    public ItemData RequestedItem { get; private set; }
    public float TimeRemaining { get; private set; }
    public bool RequestActive { get; private set; }

    /// <summary>Blocca il timer e le richieste (es. game over).</summary>
    public void Freeze()
    {
        RequestActive = false;
    }

    // Pool degli spawn point ancora da consegnare
    private List<ItemSpawnPoint> remainingSpawnPoints = new List<ItemSpawnPoint>();
    private ItemSpawnPoint activeSpawnPoint;

    private void Start()
    {
        SpawnAllItems();
        PickNextRequest();
    }

    private void Update()
    {
        if (!RequestActive) return;

        TimeRemaining -= Time.deltaTime;

        if (TimeRemaining <= 0f)
            FailRequest();
    }

    // Spawna tutti gli oggetti nei loro punti all'inizio (o al restart)
    private void SpawnAllItems()
    {
        remainingSpawnPoints.Clear();

        foreach (ItemSpawnPoint sp in spawnPoints)
        {
            if (sp == null || sp.itemData == null)
            {
                Debug.LogWarning("[TeacherRequestSystem] Spawn point nullo o senza ItemData, ignorato.");
                continue;
            }

            sp.SpawnItem();
            remainingSpawnPoints.Add(sp);
        }
    }

    // Sceglie casualmente il prossimo item richiesto tra quelli rimasti
    private void PickNextRequest()
    {
        if (remainingSpawnPoints.Count == 0)
        {
            AllDelivered();
            return;
        }

        int index = UnityEngine.Random.Range(0, remainingSpawnPoints.Count);
        activeSpawnPoint = remainingSpawnPoints[index];
        RequestedItem = activeSpawnPoint.itemData;

        TimeRemaining = timeLimit;
        RequestActive = true;

        Debug.Log($"[Teacher] Richiesta: porta '{RequestedItem.itemName}' entro {timeLimit}s. ({remainingSpawnPoints.Count} rimasti)");
        OnNewRequest?.Invoke(RequestedItem, timeLimit);
    }

    // Restituisce true se l'oggetto consegnato è quello richiesto
    public bool NotifyDelivery(ItemData deliveredItem)
    {
        if (!RequestActive) return false;

        if (deliveredItem == RequestedItem)
        {
            Debug.Log($"[Teacher] Consegna corretta: {deliveredItem.itemName}!");
            RequestActive = false;

            remainingSpawnPoints.Remove(activeSpawnPoint);
            activeSpawnPoint = null;

            OnRequestCompleted?.Invoke(deliveredItem);
            PickNextRequest();
            return true;
        }

        Debug.Log($"[Teacher] Oggetto sbagliato: {deliveredItem.itemName}. Serviva: {RequestedItem.itemName}.");
        return false;
    }

    private void FailRequest()
    {
        RequestActive = false;
        Debug.Log($"[Teacher] Tempo scaduto! Non hai consegnato '{RequestedItem.itemName}'.");
        OnRequestFailed?.Invoke(RequestedItem);

        // Il tempo scade: l'oggetto rimane in scena, si passa al prossimo
        remainingSpawnPoints.Remove(activeSpawnPoint);
        activeSpawnPoint = null;

        PickNextRequest();
    }

    private void AllDelivered()
    {
        Debug.Log("[Teacher] Tutti gli oggetti consegnati! Riavvio...");
        OnAllDelivered?.Invoke();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
