using System;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

#if UNITY_EDITOR
[CreateAssetMenu(menuName = "Behavior/Event Channels/On Damaged Event Channel")]
#endif
[Serializable, GeneratePropertyBag]
[EventChannelDescription(name: "On Damaged Event Channel", message: "[Self] is damaged by [Enemy]", category: "Events", id: "efcb6841d24ed38b62d207d9006c1429")]
public sealed partial class OnDamagedEventChannel : EventChannel<GameObject, GameObject> 
{
    public OnDamagedEventChannel onDamaged;
}

