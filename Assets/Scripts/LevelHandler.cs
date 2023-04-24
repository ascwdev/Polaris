using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * LevelHandler.cs
 * Author: Alex Williams
 * Date: 23/03/23
 *
 * This script manages the levels in the game. Essentially, collider objects are used as the
 * bounds for a level, and checks made on entering/exiting the collider handles the
 * activation/deactivation of objects in a given level.
 */

public class LevelHandler : MonoBehaviour
{
    public GameObject vc;
    public GameObject obs;
    public GameObject cp;
    

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !other.isTrigger)
        {
            vc.SetActive(true);
            obs.SetActive(true);
            cp.SetActive(true);
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !other.isTrigger)
        {
            vc.SetActive(false);
            obs.SetActive(false);
            cp.SetActive(false);
        }
    }
}
