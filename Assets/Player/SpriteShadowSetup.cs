using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class SpriteShadowSetup : MonoBehaviour
{
    public ShadowCastingMode castShadows = ShadowCastingMode.On;
    public bool receiveShadows = true;
    public bool applyToChildren = false;

    void OnEnable() { Apply(); }
    void OnValidate() { Apply(); }

    void Apply()
    {
        if (applyToChildren)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                Set(r);
        }
        else
        {
            Set(GetComponent<Renderer>());
        }
    }

    void Set(Renderer r)
    {
        if (r == null) return;
        r.shadowCastingMode = castShadows;
        r.receiveShadows = receiveShadows;
    }
}