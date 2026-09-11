using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;

public class ResourceDropZone : NetworkBehaviour, IInteractable
{
    [SerializeField] private SelectionOutline m_selectionOutline;

    [SerializeField] private List<ComponentController> m_componentControllers;
    [SerializeField] private ObjectType m_acceptedObjectType;
    private NetworkVariable<int> m_stackedRessources = new(0);
    public int StackedRessources => m_stackedRessources.Value;
    public event Action OnDropZoneFilled;

    [SerializeField] private ItemsAudio m_itemsAudio;

    public bool Interact(ObjectType objectType)
    {
        if(IsServer == false)
            return false;
        
        if(objectType != m_acceptedObjectType)
            return false;

        if(m_stackedRessources.Value >= m_componentControllers.Count)
            return false;
        
        PlayerAudioClientRpc();
        m_componentControllers[m_stackedRessources.Value].SetEnabled(true);

        m_stackedRessources.Value ++;
        if(m_stackedRessources.Value >= m_componentControllers.Count)
        {
            OnDropZoneFilled?.Invoke();
        }
        return true;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayerAudioClientRpc()
    {
        if(m_itemsAudio != null)
            m_itemsAudio.PlaySound();

    }
    
    
    public void ToggleSelection(bool isSelected)
    {
        if(m_selectionOutline != null)
        {
            m_selectionOutline.ToggleOutline(isSelected);
        }
    }
}
