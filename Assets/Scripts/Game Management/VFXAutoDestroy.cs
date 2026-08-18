using UnityEngine;

// Destroys a detached one-shot VisualEffect once its EFFECT time — not wall time — reaches the
// given lifetime. VFX Graph clamps each simulation step to VFXManager.maxDeltaTime (default 50 ms),
// so in a heavy frame the effect advances less than the frame took; a plain Destroy(obj, t) then
// kills the object while the effect is still mid-life. Accumulating the same clamped dt here
// tracks the effect's clock instead.
public class VFXAutoDestroy : MonoBehaviour
{
    private float lifetime;
    private float elapsed;

    public static void Attach(GameObject target, float lifetime)
    {
        VFXAutoDestroy d = target.AddComponent<VFXAutoDestroy>();
        d.lifetime = lifetime;
    }

    private void Update()
    {
        elapsed += Mathf.Min(Time.deltaTime, UnityEngine.VFX.VFXManager.maxDeltaTime);
        if (elapsed >= lifetime)
            Destroy(gameObject);
    }
}
