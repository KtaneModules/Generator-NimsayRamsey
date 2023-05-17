//using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using KModkit;

public class ExampleModuleTP : MonoBehaviour
{
    public KMSelectable[] buttons;

    int correctIndex;
    bool isActivated = false;

    static int moduleIdCounter = 1;
	int moduleId;

    void Start() {
        moduleId = moduleIdCounter++;
        Init();

        GetComponent<KMBombModule>().OnActivate += ActivateModule;
    }

    void Init()
    {
        correctIndex = Random.Range(0, 4);

        for(int i = 0; i < buttons.Length; i++)
        {
            string label = i == correctIndex ? "O" : "X";

            TextMesh buttonText = buttons[i].GetComponentInChildren<TextMesh>();
            buttonText.text = label;
            int j = i;
            buttons[i].OnInteract += delegate () { OnPress(j == correctIndex); return false; };
        }
    }

    void ActivateModule()
    {
        isActivated = true;
    }

    void OnPress(bool correctButton)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        if (!isActivated)
        {
            Debug.Log("Pressed button before module has been activated!");
            GetComponent<KMBombModule>().HandleStrike();
        }
        else
        {
            Debug.Log("Pressed " + correctButton + " button");
            if (correctButton)
            {
                GetComponent<KMBombModule>().HandlePass();
            }
            else
            {
                GetComponent<KMBombModule>().HandleStrike();
            }
        }
    }
    	// Twitch Plays Support by Kilo Bites // Modified by Nimsay Ramsey

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} Press Left/Right 1-3 to push a button on the left or right side || !{0} Submit to submit a stage || Read/Submit order for TP is L1-L2-L3-R1-R2-R3";
#pragma warning restore 414

	IEnumerator TwitchHandleForcedSolve() //Autosolver
	{
		yield return new WaitForSeconds(1.0f);
        Debug.Log("Batch " + moduleId);
        GetComponent<KMBombModule>().HandlePass();
	}
}