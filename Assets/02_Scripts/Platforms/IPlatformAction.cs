using UnityEngine;

public interface IPlatformAction
{
    // Called when the player first enters the platform
    void OnPlayerEnter(GameObject player);
    
    // Called while the player remains on the platform (useful for continuous collection/upgrading)
    void OnPlayerStay(GameObject player);
    
    // Called when the player leaves the platform
    void OnPlayerExit(GameObject player);
}
