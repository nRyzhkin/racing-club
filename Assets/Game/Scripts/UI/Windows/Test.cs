using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] private GameObject[] windows;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (windows[0].activeSelf)
            {
                windows[0].GetComponent<UIWindow>().CloseWindow();
            }
            else
            {
                windows[0].SetActive(true);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (windows[1].activeSelf)
            {
                windows[1].GetComponent<UIWindow>().CloseWindow();
            }
            else
            {
                windows[1].SetActive(true);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (windows[2].activeSelf)
            {
                windows[2].GetComponent<UIWindow>().CloseWindow();
            }
            else
            {
                windows[2].SetActive(true);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (windows[3].activeSelf)
            {
                windows[3].GetComponent<UIWindow>().CloseWindow();
            }
            else
            {
                windows[3].SetActive(true);
            }
        }
    }
}
