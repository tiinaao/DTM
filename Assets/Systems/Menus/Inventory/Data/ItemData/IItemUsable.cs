using UnityEngine;

public abstract class ItemUsable : MonoBehaviour
{
    public abstract string ItemId { get; }
    public abstract void Use();
}