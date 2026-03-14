using UnityEngine;

public class ItemSpawnPoint : MonoBehaviour
{
    public ItemData itemData;

    private GameObject spawnedItem;

    public GameObject SpawnItem()
    {
        if (spawnedItem != null)
            Destroy(spawnedItem);

        if (itemData == null)
        {
            Debug.LogWarning($"[ItemSpawnPoint] Nessun ItemData assegnato su {gameObject.name}.");
            return null;
        }

        if (itemData.worldPrefab != null)
        {
            spawnedItem = Instantiate(itemData.worldPrefab, transform.position, transform.rotation);
        }
        else
        {
            spawnedItem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawnedItem.transform.position = transform.position;
            spawnedItem.transform.localScale = Vector3.one * 0.5f;
        }

        spawnedItem.name = itemData.itemName;

        if (spawnedItem.GetComponent<Rigidbody>() == null)
            spawnedItem.AddComponent<Rigidbody>();

        WorldItem worldItem = spawnedItem.GetComponent<WorldItem>();
        if (worldItem == null)
            worldItem = spawnedItem.AddComponent<WorldItem>();

        worldItem.itemData = itemData;

        spawnedItem.layer = LayerMask.NameToLayer("Interactable");

        return spawnedItem;
    }

    public void ClearSpawnedItem()
    {
        if (spawnedItem != null)
        {
            Destroy(spawnedItem);
            spawnedItem = null;
        }
    }
}
