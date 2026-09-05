using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public abstract class PickableBase : NetworkBehaviour, IInteractable
{
    protected NetworkVariable<bool> m_isAvailable = new (
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField] private SelectionOutline m_outline;
    [SerializeField] private ObjectType m_objectType;

    public bool CanBePickedUp => m_isAvailable.Value;
    public ObjectType ObjectType => m_objectType;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        m_isAvailable.OnValueChanged += OnAvailabilityChange;
        ApplyAvailabilityState(m_isAvailable.Value);
    }

    public override void OnNetworkDespawn()
    {
        m_isAvailable.OnValueChanged -= OnAvailabilityChange;
        base.OnNetworkDespawn();
    }

    private void OnAvailabilityChange(bool previousValue, bool newValue)
    {
        ApplyAvailabilityState(newValue);
    }

    protected abstract void ApplyAvailabilityState(bool newValue);

    public void PickUp()
    {
        if(IsServer == false)
        {
            return;
        }
        m_isAvailable.Value = false;
        OnPickedUp();
    }

    protected abstract void OnPickedUp();

    public void ToggleSelection(bool isSelected)
    {
        if(m_outline != null)
        {
            m_outline.ToggleOutline(isSelected);
        }
    }


}
