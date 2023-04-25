using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool pause = false;
    public GameObject pm;

    private void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            if (pause)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pm.SetActive(false);
        Time.timeScale = 1f;
        pause = false;
    }

    public void Pause()
    {
        pm.SetActive(true);
        Time.timeScale = 0f;
        pause = true;
    }

    public void MainMenu()
    {
        pause = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Title_Screen");
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }
}
