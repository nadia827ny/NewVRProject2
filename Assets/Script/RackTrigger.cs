using UnityEngine;

public class RackTrigger : MonoBehaviour
{
    public GameLogic GL; // reference game logic Scripts

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == GL.currentDish && GL.GameState == "Step8Wait"){
            GL.GameState = "Step8Done";
            // Dish is placed in the drying rack
            // Repeat Steps 5-8 for the next Dish, or go to Step 9
        }
    }
}
