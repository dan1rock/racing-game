using UnityEngine;

public class AutoReset : MonoBehaviour
{
    [SerializeField] private bool onExit = false;
    
    private void OnTriggerEnter(Collider other)
    {
        if (onExit) return;
        
        Car car = other.transform.parent.GetComponent<Car>();
        
        if (!car.playerControlled) return;
        
        car.InvokeReset();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!onExit) return;
        
        Car car = other.transform.parent.GetComponent<Car>();
        
        if (!car.playerControlled) return;
        
        car.InvokeReset();
    }
}
