using Entities;
using UnityEngine;

// [RequireComponent(typeof(EnvironmentManager))]
// [RequireComponent(typeof(StatusEffectHandler))]
// [RequireComponent(typeof(ResistanceHandler))]
// [RequireComponent(typeof(AttributeManager))]
// [RequireComponent(typeof(EquipmentManager))]

public class EntityContext : MonoBehaviour
{
    // public EnvironmentManager EnvironmentManager { get; protected set; }
    // public StatusEffectHandler StatusEffectHandler { get; protected set; }
    // public ResistanceHandler ResistanceHandler { get; protected set; }
    // public AttributeManager AttributeManager { get; protected set; }
    // public EquipmentManager EquipmentManager { get; protected set; }

    public float Health { get; set; }

    protected virtual void Awake()
    {
        // EnvironmentManager = GetComponent<EnvironmentManager>();
        // StatusEffectHandler = GetComponent<StatusEffectHandler>();
        // ResistanceHandler = GetComponent<ResistanceHandler>();
        // AttributeManager = GetComponent<AttributeManager>();
        // EquipmentManager = GetComponent<EquipmentManager>();
    }
}
