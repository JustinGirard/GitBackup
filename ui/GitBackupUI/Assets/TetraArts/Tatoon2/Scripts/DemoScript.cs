using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DemoScript : MonoBehaviour
{
    public MeshRenderer shaderball;
    public Light mainLight;
    public GameObject addlight;
    private bool activAddlight;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (activAddlight == true)
        {
            addlight.SetActive(true);
        }
        else addlight.SetActive(false);
       
    }

    private void OnGUI()
    {
        

        if (GUI.Button(new Rect(10, 30, 150, 50), "DirectionalLight"))
        {
            if (mainLight.intensity == 0.2f)
            {
                mainLight.intensity = 1f;
            }
            else mainLight.intensity = 0.2f;
        }

        if (GUI.Button(new Rect(10, 85, 150, 50), "Point Light"))
        {
            activAddlight = !activAddlight;
        }
        
        
    }
   

}
