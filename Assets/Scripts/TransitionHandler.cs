using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionHandler : MonoBehaviour
{
    public Animator an;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            CallTransition();
        }
    }

    public void CallTransition()
    {
        an.SetTrigger("Start");
        an.SetTrigger("Transition");
    }
    
}
