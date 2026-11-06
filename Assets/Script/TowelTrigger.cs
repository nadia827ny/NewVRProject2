using UnityEngine;

public class TowelTrigger : MonoBehaviour
{
    public GameLogic GL; // reference GameLogic Scripts

    void OnTriggerEnter(Collider other)
    {
        string dishName = other.gameObject.name;
        if (GL.GameState == "Step13Wait" && (dishName == "plate" || dishName == "bowl" || dishName == "mug" || dishName == "knife1" || dishName == "knife2")){
            // Only count each dish once
            if (!GL.driedDishes.Contains(dishName + ",")){
                GL.driedDishes = GL.driedDishes + dishName + ",";
                GL.dishesDried = GL.dishesDried + 1;
            }
            // All 5 dishes are dry - go to Step 14 Stack your dishes
            if (GL.dishesDried >= 5){
                GL.GameState = "Step14";
            }
        }
    }
}
