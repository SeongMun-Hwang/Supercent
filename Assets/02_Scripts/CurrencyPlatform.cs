using UnityEngine;

public class CurrencyPlatform : MonoBehaviour, IPlatformAction
{
    [SerializeField] private float collectInterval = 0.5f;
    private float _timer;

    public void OnPlayerEnter(GameObject player)
    {
        Debug.Log("Entered Currency Platform: Start collecting!");
        _timer = 0;
    }

    public void OnPlayerStay(GameObject player)
    {
        _timer += Time.deltaTime;
        if (_timer >= collectInterval)
        {
            _timer = 0;
            CollectCurrency();
        }
    }

    public void OnPlayerExit(GameObject player)
    {
        Debug.Log("Left Currency Platform.");
    }

    private void CollectCurrency()
    {
        // Add your currency logic here (e.g., Inventory.AddMoney(10))
        Debug.Log("Currency Collected!");
    }
}
