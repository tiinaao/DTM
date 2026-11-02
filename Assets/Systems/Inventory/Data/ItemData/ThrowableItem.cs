using UnityEngine;

public enum ThrowImpactMode
{
    OnContact,   
    AfterFuse    
}

[CreateAssetMenu(fileName = "NewThrowable", menuName = "Inventory/Throwable")]
public class ThrowableItem : ItemData
{
    public GameObject projectilePrefab;
    public float throwForce = 12f;
    public float upwardForce = 2f;

    public ThrowImpactMode impactMode = ThrowImpactMode.OnContact;
    public float fuseSeconds = 2f;
    public float damage = 3f;
    public float impactRadius = 0f;

    public GameObject impactEffectPrefab;
    public AudioClip impactSound;
    public float effectLifetime = 3f;
}