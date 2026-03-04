using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class VFXManager : MonoBehaviour
{
    public VisualEffect mainEffects { get; private set; }

    public enum VFXType
    {
        Explosion
    }

    private Dictionary<VFXType, string> VFXValuePairs = new Dictionary<VFXType, string>
    {
        [VFXType.Explosion] = "OnDeath"
    };

    void Start()
    {
        mainEffects = GetComponentInChildren<VisualEffect>();
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
}
