using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GalleryEventsExample : MonoBehaviour
{
    [SerializeField]
    private Gallery[] galleries;

    [SerializeField]
    private TextMeshProUGUI debugger;

    private void Awake()
    {
        for (int i = 0; i < galleries.Length; i++)
        {
            if (galleries[i] != null)
            {
                galleries[i].OnRelease += OnRelease;
                galleries[i].OnClick += OnClick;
                galleries[i].OnValueChanged += OnValueChanged;
            }
        }
    }

    public void OnRelease(int i)
    {
        if (debugger != null)
        {
            debugger.text = "On Release [Current index " + i.ToString() + "]";
        }
    }

    public void OnValueChanged(int i)
    {
        if (debugger != null)
        {
            debugger.text = "On Update [Current index " + i.ToString() + "]";
        }
    }
    
    public void OnClick(int i)
    {
        if (debugger != null)
        {
            debugger.text = "On Click [Current index " + i.ToString() + "]";
        }
    }
}
