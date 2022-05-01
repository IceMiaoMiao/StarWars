using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public class Crosshairs : MonoBehaviour
{
    public LayerMask targetMask;
    public Color dotHeightColor;
    private Color originalDotColor;
    public SpriteRenderer dot;

    private void Start()
    {
        Cursor.visible = false;
        originalDotColor = dot.color;
    }

    private void Update()
    {
        transform.Rotate(Vector3.forward * -40 * Time.deltaTime);
    }

    public void DetectTargets(Ray ray)
    {
        dot.color = Physics.Raycast(ray,100,targetMask) ? dotHeightColor : originalDotColor;
    }
}
