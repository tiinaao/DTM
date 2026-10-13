using System.Collections.Generic;
using UnityEngine;

public abstract class NPCBase : MonoBehaviour
{
    private static readonly Dictionary<string, NPCBase> registry = new Dictionary<string, NPCBase>();

    [SerializeField] private string displayName;
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    public string Id => string.IsNullOrEmpty(displayName) ? string.Empty : displayName.Trim().ToLower().Replace(" ", "_");
    public bool PlayerIsNear { get; private set; }

    public static NPCBase GetById(string id)
    {
        registry.TryGetValue(id, out NPCBase npc);
        return npc;
    }

    protected virtual void OnEnable()
    {
        if (!string.IsNullOrEmpty(Id))
        {
            registry[Id] = this;
        }
    }

    protected virtual void OnDisable()
    {
        if (!string.IsNullOrEmpty(Id) && registry.TryGetValue(Id, out NPCBase registered) && registered == this)
        {
            registry.Remove(Id);
        }
    }

    protected virtual void Update()
    {
        if (PlayerIsNear && Input.GetKeyDown(interactKey))
        {
            Interact();
        }
    }

    public void Interact()
    {
        if (!PlayerIsNear)
        {
            return;
        }

        OnInteract();
    }

    protected abstract void OnInteract();

    protected virtual void OnPlayerEnter()
    {
    }

    protected virtual void OnPlayerExit()
    {
    }

    private void OnTriggerEnter(Collider otherCollider)
    {
        if (otherCollider.CompareTag("Player"))
        {
            PlayerIsNear = true;
            OnPlayerEnter();
        }
    }

    private void OnTriggerExit(Collider otherCollider)
    {
        if (otherCollider.CompareTag("Player"))
        {
            PlayerIsNear = false;
            OnPlayerExit();
        }
    }
}