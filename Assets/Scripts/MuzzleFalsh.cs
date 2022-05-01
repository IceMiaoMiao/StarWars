using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 
/// </summary>
public class MuzzleFalsh : MonoBehaviour
{
    public GameObject flashHold;
    public float flashTime;
    public Sprite[] flashSprites;
    public SpriteRenderer[] spriteRenders;
    
    private void Start()
    {
        Deactivate();
        
    }

    public void Activate()
    {
        flashHold.SetActive(true);
        int flashSpriteIndex = Random.Range(0, flashSprites.Length);
        for (int i = 0; i < spriteRenders.Length; i++)
        {
            spriteRenders[i].sprite = flashSprites[flashSpriteIndex];
        }
        
        Invoke("Deactivate",flashTime);
    }

    void Deactivate()
    {
        flashHold.SetActive(false);
    }
    
    
}
