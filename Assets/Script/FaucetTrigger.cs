using UnityEngine;

public class FaucetTrigger : MonoBehaviour
{
    public GameLogic GL; // reference GameLogic Scripts

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == GL.currentDish && GL.GameState == "Step7Wait"){
            GL.GameState = "Step8";
            // Step 8 : Dish rinsed under the Faucet
            // Show Next UI - Place it in the Drying Rack
            // Change GameState to Step 8 Wait
        }
    }
}
