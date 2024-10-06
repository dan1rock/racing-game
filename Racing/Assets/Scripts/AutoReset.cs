using UnityEngine;

public class AutoReset : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Car car = other.transform.parent.GetComponent<Car>();
        
        if (!car.playerControlled) return;
        
        car.InvokeReset();
    }
}
