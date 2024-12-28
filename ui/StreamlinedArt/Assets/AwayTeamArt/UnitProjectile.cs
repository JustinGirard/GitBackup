using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitProjectile : MonoBehaviour
{
    bool isEnabled = true;
    public bool IsEnabled()
    {
        return isEnabled;
    }
    public void Disable()
    {
        isEnabled = false;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
