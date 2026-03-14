using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    public Camera playerCamera;
    public float interactDistance = 3f;
    public LayerMask interactLayer;

    private PlayerInputActions inputActions;
    private PlayerInventory inventory;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        inventory = GetComponent<PlayerInventory>();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Interact.performed += OnInteractPerformed;
        inputActions.Player.Drop.performed += OnDropPerformed;
        inputActions.Player.NextItem.performed += OnNextItemPerformed;
        inputActions.Player.PreviousItem.performed += OnPreviousItemPerformed;
    }

    private void OnDisable()
    {
        inputActions.Player.Interact.performed -= OnInteractPerformed;
        inputActions.Player.Drop.performed -= OnDropPerformed;
        inputActions.Player.NextItem.performed -= OnNextItemPerformed;
        inputActions.Player.PreviousItem.performed -= OnPreviousItemPerformed;
        inputActions.Disable();
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        TryInteract();
    }

    private void OnDropPerformed(InputAction.CallbackContext context)
    {
        TryDropSelectedItem();
    }

    private void OnNextItemPerformed(InputAction.CallbackContext context)
    {
        inventory.SelectNextItem();
    }

    private void OnPreviousItemPerformed(InputAction.CallbackContext context)
    {
        inventory.SelectPreviousItem();
    }

    private void TryInteract()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
        {
            WorldItem worldItem = hit.collider.GetComponent<WorldItem>();

            if (worldItem != null)
            {
                inventory.AddItem(worldItem.itemData);
                Destroy(worldItem.gameObject);
                return;
            }

        }
    }

    private void TryDropSelectedItem()
    {
        ItemData selectedItem = inventory.GetSelectedItem();

        if (selectedItem == null)
        {
            Debug.Log("Nessun oggetto da droppare.");
            return;
        }

        GameObject droppedObject;

        if (selectedItem.worldPrefab != null)
        {
            droppedObject = Instantiate(
                selectedItem.worldPrefab,
                transform.position + transform.forward * 1.5f + Vector3.up * 0.5f,
                Quaternion.identity
            );
        }
        else
        {
            droppedObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            droppedObject.transform.position = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
            droppedObject.transform.localScale = Vector3.one * 0.5f;
        }

        droppedObject.name = "Dropped_" + selectedItem.itemName;

        if (droppedObject.GetComponent<Rigidbody>() == null)
            droppedObject.AddComponent<Rigidbody>();

        WorldItem worldItem = droppedObject.GetComponent<WorldItem>();
        if (worldItem == null)
            worldItem = droppedObject.AddComponent<WorldItem>();

        worldItem.itemData = selectedItem;

        // Make the dropped object detectable by the interact raycast
        droppedObject.layer = LayerMask.NameToLayer("Interactable");

        inventory.RemoveSelectedItem();

        Debug.Log("Droppato: " + selectedItem.itemName);
    }
}