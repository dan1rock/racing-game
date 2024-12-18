using Unity.VisualScripting;
using UnityEngine;

public class AutoReset : MonoBehaviour
{
    [SerializeField] private bool onExit = false;
    
    private void OnTriggerEnter(Collider other)
    {
        if (onExit) return;
        
        Car car = other.transform.parent.GetComponent<Car>();
        
        if (!car) return;
        if (!car.GetComponent<CarPlayer>()) return;
        
        CarBot bot = car.GetComponent<CarBot>();
        if (bot)
        {
            bot.Reset();
        }
        else
        {
            car.InvokeReset(true);   
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!onExit) return;
        
        Car car = other.transform.parent.GetComponent<Car>();

        if (!car) return;
        if (!car.GetComponent<CarPlayer>()) return;
        
        CarBot bot = car.GetComponent<CarBot>();
        if (bot)
        {
            bot.Reset();
        }
        else
        {
            car.InvokeReset(true);   
        }
    }
}
