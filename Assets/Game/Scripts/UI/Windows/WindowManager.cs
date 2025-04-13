using System.Collections.Generic;
using UnityEngine;

public class WindowManager : MonoBehaviour
{
    public static WindowManager Instance { get; private set; }
    
    private Stack<UIWindow> windowStack = new Stack<UIWindow>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OpenWindow(UIWindow window)
    {
        // Если окно уже открыто, ничего не делаем
        if (window.gameObject.activeSelf) return;
        
        // Если это полноэкранное окно, добавляем в стек
        if (window.isFullscreen)
        {
            // Делаем предыдущее окно полупрозрачным
            if (windowStack.Count > 0)
            {
                var topWindow = windowStack.Peek();
                topWindow.canvasGroup.interactable = false;
                topWindow.canvasGroup.blocksRaycasts = false;
            }
            
            windowStack.Push(window);
        }
        
        window.gameObject.SetActive(true);
    }

    public void CloseTopWindow()
    {
        if (windowStack.Count == 0) return;
        
        var window = windowStack.Pop();
        window.CloseWindow();
        
        // Восстанавливаем предыдущее окно
        if (windowStack.Count > 0)
        {
            var topWindow = windowStack.Peek();
            topWindow.canvasGroup.interactable = true;
            topWindow.canvasGroup.blocksRaycasts = true;
        }
    }

    public bool IsWindowOpen(UIWindow window)
    {
        return window.gameObject.activeSelf;
    }
}