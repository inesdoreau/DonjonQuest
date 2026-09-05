using Unity.Netcode.Components;
using UnityEngine;

public class PickableTool : PickableBase
{
    [SerializeField] private ComponentController m_componentController;

    protected override void ApplyAvailabilityState(bool newValue)
    {
        if(IsServer)
        {
            m_componentController.SetEnabled(newValue);
        }
    }

    protected override void OnPickedUp()
    {
        // No code
    }

    public void Drop(Vector3 position)
    {
        if(IsServer == false)
        {
            return;
        }
        transform.position = new Vector3(position.x, transform.position.y, position.z);
        m_isAvailable.Value = true;
    }
}
