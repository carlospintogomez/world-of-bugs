using UnityEngine;

public class CollisionChecker : MonoBehaviour
{
    public bool Colliding;

    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Cube2" || other.tag == "Cube1")
        {
            var overlay = GameObject.FindGameObjectWithTag("GameWatcherOverlay").GetComponent<UnityEngine.UI.Text>();
            overlay.text = "Object clipped!";
            Colliding = true;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.tag == "Cube2" || other.tag == "Cube1")
        {
            var overlay = GameObject.FindGameObjectWithTag("GameWatcherOverlay").GetComponent<UnityEngine.UI.Text>();
            overlay.text = "Object being clipped...";
            Colliding = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Cube2" || other.tag == "Cube1")
        {
            var overlay = GameObject.FindGameObjectWithTag("GameWatcherOverlay").GetComponent<UnityEngine.UI.Text>();
            overlay.text = "No object is clipped.";
            Colliding = false;
        }
    }
}
