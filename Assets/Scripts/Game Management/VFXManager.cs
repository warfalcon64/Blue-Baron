using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class VFXManager : MonoBehaviour
{
    public VisualEffect mainEffects { get; private set; }

    public enum VFXType
    {
        Spark,
        Explosion
    }

    private Dictionary<VFXType, string> VFXValuePairs = new Dictionary<VFXType, string>
    { 
        [VFXType.Spark] = "LaserHit",
        [VFXType.Explosion] = "OnDeath"
    };

    // Start is called before the first frame update
    void Start()
    {
        mainEffects = GetComponentInChildren<VisualEffect>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayVFX(VFXType type, Vector3 position)
    {
        if (!VFXValuePairs.ContainsKey(type))
        {
            print("NO VFX OF TYPE " + type + " FOUND IN VFXVALUEPAIRS");
        }

        VFXEventAttribute eventAttribute = mainEffects.CreateVFXEventAttribute();
        int vfxPosition = Shader.PropertyToID("Position");

        mainEffects.SetVector3(vfxPosition, position);
        mainEffects.SendEvent(VFXValuePairs[type], eventAttribute);
    }
}
