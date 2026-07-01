using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    public VisualEffect mainEffects { get; private set; }

    public enum VFXType
    {
        Explosion
    }

    private Dictionary<VFXType, string> VFXValuePairs = new Dictionary<VFXType, string>
    {
        [VFXType.Explosion] = "OnDeath"
    };

    // Reused per-event payload. Many impacts can fire in a single frame, so each must carry its own
    // position/color as event attributes — a shared graph property would be overwritten before the
    // graph processes the batch, collapsing every impact onto the last position.
    private VFXEventAttribute impactAttr;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        mainEffects = GetComponentInChildren<VisualEffect>();
        if (mainEffects != null)
            impactAttr = mainEffects.CreateVFXEventAttribute();
    }

    public void PlayVFX(VFXType type, Vector3 position)
    {
        if (!VFXValuePairs.ContainsKey(type))
        {
            print("NO VFX OF TYPE " + type + " FOUND IN VFXVALUEPAIRS");
        }

        mainEffects.SetVector3("Position", position);
        mainEffects.SendEvent(VFXValuePairs[type]);
    }

    // Impact spark routed through the single persistent graph instead of a per-projectile VisualEffect.
    // position + velocity travel as per-event attributes so concurrent impacts spawn at their own
    // location and the spark can inherit a fraction of the projectile's momentum (realistic spall).
    public void PlayImpact(Vector3 position, Vector3 velocity, string eventName)
    {
        if (mainEffects == null || impactAttr == null) return;
        impactAttr.SetVector3("position", position);
        impactAttr.SetVector3("velocity", velocity);
        mainEffects.SendEvent(eventName, impactAttr);
    }
}
