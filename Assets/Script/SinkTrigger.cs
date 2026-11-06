using UnityEngine;

public class SinkTrigger : MonoBehaviour
{
    public GameLogic GL; // reference game logic Scripts

    void OnTriggerExit(Collider other)
    {
        string dishName = other.gameObject.name;
        if (GL.GameState == "Step5Wait" && (dishName == "plate" || dishName == "bowl" || dishName == "mug" || dishName == "knife1" || dishName == "knife2")){
            GL.currentDish = dishName;
            GL.GameState = "Step6";
            // this script is for step 6 to get the dirty dish lifted out of the sink
            // show next UI which is to scrub the dish
        }
    }
}
