using UnityEngine;

public class SpongeTrigger : MonoBehaviour
{
    public GameLogic GL; // reference game logic scripts

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == GL.currentDish && GL.GameState == "Step6Wait"){
            GL.GameState = "Step7";
            // Ths is step 7  for the dish scrubbed with the Sponge
            // Show next UI, to  Rinse the Dish
        }
    }
}
