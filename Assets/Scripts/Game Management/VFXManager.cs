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

    public void PlayImpact(Vector3 position, Vector3 velocity, string eventName)
    {
        if (mainEffects == null || impactAttr == null) return;

        if (ProjectileManager.Instance != null && !ProjectileManager.Instance.IsInView(position)) return;
        impactAttr.SetVector3("position", position);
        impactAttr.SetVector3("velocity", velocity);
        mainEffects.SendEvent(eventName, impactAttr);
    }
}
