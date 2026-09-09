using UnityEngine;

public class PickableResource : PickableBase
{
    protected override void ApplyAvailabilityState(bool newValue)
    {
        //No code
    }

    protected override void OnPickedUp()
    {
        if(IsServer == false)
            return;

        NetworkObject.Despawn();
    }
}
