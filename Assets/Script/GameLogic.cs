using UnityEngine;

public class GameLogic : MonoBehaviour
{
    // Game state 

    public string GameState = "Initialize";

    // Which dish is being washed right now and how many are done
    public string currentDish = "";
    public int dishesWashed = 0;

    // Which dishes are dried and how many are done
    public string driedDishes = "";
    public int dishesDried = 0;

    
    // Grabbables
    public GameObject spongeObj;
    public GameObject plateObj;
    public GameObject bowlObj;
    public GameObject mugObj;
    public GameObject knife1Obj;
    public GameObject knife2Obj;
    public GameObject towelObj;

    // Sensors
    public GameObject sinkSensorObj;
    public GameObject spongeSensorObj;
    public GameObject faucetSensorObj;
    public GameObject rackSensorObj;
    public GameObject towelSensorObj;

    // Next Button
    public GameObject nextButtonObj;

    // UI Screens
    public GameObject screen0Obj; // Intro
    public GameObject screen1Obj; // Walk to the kitchen
    public GameObject screen2Obj; // Walk to the sink
    public GameObject screen3Obj; // Turn the sink on
    public GameObject screen4Obj; // Grab the sponge
    public GameObject screen5Obj; // Grab a dirty dish
    public GameObject screen6Obj; // Scrub the dish
    public GameObject screen7Obj; // Rinse the dish
    public GameObject screen8Obj; // Place it in the drying rack
    public GameObject screen9Obj; // Put the sponge down
    public GameObject screen10Obj; // Turn off the sink
    public GameObject screen11Obj; // Find the dish towel
    public GameObject screen12Obj; // Go back to the drying rack
    public GameObject screen13Obj; // Dry each dish
    public GameObject screen14Obj; // Stack your dishes
    public GameObject screen15Obj; // Set the towel down
    public GameObject screen16Obj; // Complete

    void Start()
    {
        // Intro Screen
        // Disable the grabbles
        spongeObj.GetComponent<Oculus.Interaction.Grabbable>().enabled = false;
        plateObj.GetComponent<Oculus.Interaction.Grabbable>().enabled = false;
        bowlObj.GetComponent<Oculus.Interaction.Grabbable>().enabled = false;
        mugObj.GetComponent<Oculus.Interaction.Grabbable>().enabled = false;
        knife1Obj.GetComponent<Oculus.Interaction.Grabbable>().enabled = false;
        knife2Obj.GetComponent<Oculus.Interaction.Grabbable>().enabled = false;
        towelObj.GetComponent<Oculus.Interaction.Grabbable>().enabled = false;
        // Hide all UI Screens first
        HideAllUIScreens();
        // Show first UI screen with Next Button
        screen0Obj.SetActive(true);
        nextButtonObj.SetActive(true);
        GameState = "Step0Wait";
    }

    void HideAllUIScreens(){
        screen0Obj.SetActive(false);
        screen1Obj.SetActive(false);
        screen2Obj.SetActive(false);
        screen3Obj.SetActive(false);
        screen4Obj.SetActive(false);
        screen5Obj.SetActive(false);
        screen6Obj.SetActive(false);
        screen7Obj.SetActive(false);
        screen8Obj.SetActive(false);
        screen9Obj.SetActive(false);
        screen10Obj.SetActive(false);
        screen11Obj.SetActive(false);
        screen12Obj.SetActive(false);
        screen13Obj.SetActive(false);
        screen14Obj.SetActive(false);
        screen15Obj.SetActive(false);
        screen16Obj.SetActive(false);
    }

    // Find the dish object from its name
    GameObject GetDishObj(string dishName){
        if (dishName == "plate") return plateObj;
        if (dishName == "bowl") return bowlObj;
        if (dishName == "mug") return mugObj;
        if (dishName == "knife1") return knife1Obj;
        if (dishName == "knife2") return knife2Obj;
        return null;
    }

    // Update is called once per frame
    void Update()
    {
        switch(GameState){
            case "Step0Wait":
                // Waiting for the player to click next
                break;
            case "Step1":
                // Show Step 1 screen
                HideAllUIScreens();
                screen1Obj.SetActive(true);
                nextButtonObj.SetActive(true);
                GameState = "Step1Wait";
                break;
            case "Step1Wait":
               
                break;
            case "Step2":
                // Show Step 2 screen
                HideAllUIScreens();
                screen2Obj.SetActive(true); 
                nextButtonObj.SetActive(true);
                GameState = "Step2Wait";
                break;
            case "Step2Wait":
             
                break;
            case "Step3":
                // Show Step 3 screen
                HideAllUIScreens();
                screen3Obj.SetActive(true); 
                nextButtonObj.SetActive(true);
                GameState = "Step3Wait";
                break;
            case "Step3Wait":
                
                break;
            case "Step4":
                // Show Step 4 screen
                HideAllUIScreens();
                screen4Obj.SetActive(true); 
                nextButtonObj.SetActive(false);
                // make the sponge grabbable
                spongeObj.GetComponent<Oculus.Interaction.Grabbable>().enabled = true;
                GameState = "Step4Wait";
                break;
            case "Step4Wait":
                
                break;
            case "Step5":
                // Show Step 5 screen
                HideAllUIScreens();
                screen5Obj.SetActive(true); 
                nextButtonObj.SetActive(false);
                
                if (dishesWashed == 0){
                    plateObj.GetComponent<Oculus.Interaction.Grabbable>().enabled = true;
                    bowlObj.GetComponent<Oculus.Interaction.Grabbable>().enabled = true;
                    mugObj.GetComponent<Oculus.Interaction.Grabbable>().enabled = true;
                    knife1Obj.GetComponent<Oculus.Interaction.Grabbable>().enabled = true;
                    knife2Obj.GetComponent<Oculus.Interaction.Grabbable>().enabled = true;
                }
                GameState = "Step5Wait";
                break;
            case "Step5Wait":
                // Waiting for the dish lifted out of the sink
                break;
            case "Step6":
                // Show Step 6 screen
                HideAllUIScreens();
                screen6Obj.SetActive(true); // Scrub the dish screen
                nextButtonObj.SetActive(false);
                GameState = "Step6Wait";
                break;
            case "Step6Wait":
                // Waiting for the dish to bescrubbed
                break;
            case "Step7":
                // Show Step 7 screen
                HideAllUIScreens();
                screen7Obj.SetActive(true); // Rinse the dish screen
                nextButtonObj.SetActive(false);
                GameState = "Step7Wait";
                break;
            case "Step7Wait":
                
                break;
            case "Step8":
                // Show Step 8 screen
                HideAllUIScreens();
                screen8Obj.SetActive(true); // Place it in the drying rack screen
                nextButtonObj.SetActive(false);
                GameState = "Step8Wait";
                break;
            case "Step8Wait":
                
                break;
            case "Step8Done":
                // Dish is clean and in the rack make it not grabbable
                GetDishObj(currentDish).GetComponent<Oculus.Interaction.Grabbable>().enabled = false;
                dishesWashed = dishesWashed + 1;
                currentDish = "";
                // Repeat steps 5-8 until all 5 dishes are washed
                if (dishesWashed < 5){
                    GameState = "Step5";
                } else {
                    GameState = "Step9";
                }
                break;
            case "Step9":
                // Show Step 9 screen
                HideAllUIScreens();
                screen9Obj.SetActive(true); // Put the sponge down screen
                nextButtonObj.SetActive(true);
                GameState = "Step9Wait";
                break;
            case "Step9Wait":
                
                break;
            case "Step10":
                // Show Step 10 screen
                HideAllUIScreens();
                screen10Obj.SetActive(true); // Turn off the sink screen
                nextButtonObj.SetActive(true);
                // make the sponge not grabbable
                spongeObj.GetComponent<Oculus.Interaction.Grabbable>().enabled = false;
                GameState = "Step10Wait";
                break;
            case "Step10Wait":
                
                break;
            case "Step11":
                // Show Step 11 screen
                HideAllUIScreens();
                screen11Obj.SetActive(true); 
                nextButtonObj.SetActive(false);
                // make the towel grabbable
                towelObj.GetComponent<Oculus.Interaction.Grabbable>().enabled = true;
                GameState = "Step11Wait";
                break;
            case "Step11Wait":
                // Waiting for the towel to be grabbed
                break;
            case "Step12":
                // Show Step 12 screen
                HideAllUIScreens();
                screen12Obj.SetActive(true); 
                nextButtonObj.SetActive(true);
                GameState = "Step12Wait";
                break;
            case "Step12Wait":
               
                break;
            case "Step13":
                // Show Step 13 screen
                HideAllUIScreens();
                screen13Obj.SetActive(true); // Dry each dish screen
                nextButtonObj.SetActive(false);
                // make the dishes grabbable again to dry them
                plateObj.GetComponent<Oculus.Interaction.Grabbable>().enabled = true;
                bowlObj.GetComponent<Oculus.Interaction.Grabbable>().enabled = true;
                mugObj.GetComponent<Oculus.Interaction.Grabbable>().enabled = true;
                knife1Obj.GetComponent<Oculus.Interaction.Grabbable>().enabled = true;
                knife2Obj.GetComponent<Oculus.Interaction.Grabbable>().enabled = true;
                GameState = "Step13Wait";
                break;
            case "Step13Wait":
                // Waiting for the towel to touch all 5 dishes
                break;
            case "Step14":
                // Show Step 14 UI screen
                HideAllUIScreens();
                screen14Obj.SetActive(true); // Stack your dishes UI
                nextButtonObj.SetActive(true);
                GameState = "Step14Wait";
                break;
            case "Step14Wait":
                
                break;
            case "Step15":
                // Show Step 15 screen
                HideAllUIScreens();
                screen15Obj.SetActive(true); // Set the towel down screen
                nextButtonObj.SetActive(false);
                GameState = "Step15Wait";
                break;
            case "Step15Wait":
                // Waiting for the towel to be set down by the rack
                break;
            case "Step16":
                // Show Step 16 screen
                HideAllUIScreens();
                screen16Obj.SetActive(true); // last screen
                nextButtonObj.SetActive(false);
                // make the towel not grabbable
                towelObj.GetComponent<Oculus.Interaction.Grabbable>().enabled = false;
                GameState = "Step16Wait";
                break;
            case "Step16Wait":
                // done
                break;
        }
    }

    // Next Button clicked
    public void NextButton(){
        if (GameState == "Step0Wait"){ GameState = "Step1"; return; }
        if (GameState == "Step1Wait"){ GameState = "Step2"; return; }
        if (GameState == "Step2Wait"){ GameState = "Step3"; return; }
        if (GameState == "Step3Wait"){ GameState = "Step4"; return; }
        if (GameState == "Step9Wait"){ GameState = "Step10"; return; }
        if (GameState == "Step10Wait"){ GameState = "Step11"; return; }
        if (GameState == "Step12Wait"){ GameState = "Step13"; return; }
        if (GameState == "Step14Wait"){ GameState = "Step15"; return; }
    }

    // Sponge Grab 
    public void SpongeSelected(){
        if (GameState == "Step4Wait"){
            GameState = "Step5";
        }
    }

    // Towel Grab 
    public void TowelSelected(){
        if (GameState == "Step11Wait"){
            GameState = "Step12";
        }
    }

}
