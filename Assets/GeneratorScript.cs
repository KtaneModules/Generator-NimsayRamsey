using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using KModkit;

public class GeneratorScript : MonoBehaviour {

	/*
		TP is fixed, however, I would like the dual solve function to only trigger when 2 or more VALID modules are solved. This means I will need to check each new solved module every solve
	*/

	public KMBombInfo Bomb;
	public KMAudio Audio;
	public KMBombModule Module;
	public KMBossModule BossInfo;

	public Material[] LightColors;//Black, White, unlitRed, Red, unlitYellow, Yellow, unlitGreen, Green
	public KMSelectable[] ButtonList;
	public Renderer[] ButtonLights;
	public TextMesh SegmentDisplay;

	public KMSelectable submitLever;
	public Transform submitLeverObject;
	public Renderer[] statusLights;
	//public KMSelectable submitButton;
	//public KMSelectable[] toggleButtonList;
	public bool moduleDebug;
	//-----------------------------------------------------//
	//READONLY LIBRARIES

	private bool[] buttonONS = {false, false, false, false, false, false};

	private bool submitBOOL = false;
	private bool submitLAG = false;
	private int lagFrames = 0;

	private List<string> IGNORELIST = new List<string> {"The Generator", "Zxample Module 1"};
	private int stageLength;
	private List<bool[]> SolveStages = new List<bool[]> {};
	private int currentStage = 0;
	private bool[] currentSet = {false, false, false, false, false, false};
	private int submitStage = 0;

	private int SOLVES;
	private int solveCount;
	private string MostRecent;
    private List<string> SolveList = new List<string> { };

	private int[] logSET = {0, 0, 0, 0, 0, 0};
	private bool primeOVERIDE = false;
	private int primeNumber = 0;
	private int ledCount = 0;
	
	private string[] alphabet = new string[] {//The Alphabet
		"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"
	  // 1    2    3    4    5    6    7    8    9    10   11   12   13   14   15   16   17   18   19   20   21   22   23   24   25   26
	  // N    P    P    N    P    N    P    N    N    N    P    N    P    N    N    N    P    N    P    N    N    N    P    N    N    N
	};

	//Prime letters are B/2, C/3, E/5, G/7, K/11, M/13, Q/17, S/19, W/23

	private bool dualSolve = false; //Failsafe in case 2 or more mods are solved on the same frame
	
	//-----------------------------------------------------//

	//Logging (Goes at the bottom of global variables)
	static int moduleIdCounter = 1;
	int moduleId;

	private bool TPAutoActive = false;
	private bool moduleSolved = false;

	private void Awake() {
		moduleId = moduleIdCounter++;
		foreach (KMSelectable NAME in ButtonList) {
			KMSelectable pressedObject = NAME;
			NAME.OnInteract += delegate () { pressButton(pressedObject); return false; };
		}
		submitLever.OnInteract += delegate () { Submit(); return false; };
	}

	void Start() {
		Debug.LogFormat("[The Generator #{0}] Generator Initialized. Stages shown as TL-ML-BL // TR-MR-BR", moduleId);
		InitSolution();
		StartCoroutine(UpdateSolves());
		statusLights[2].material = LightColors[3];
	}

	void InitSolution() {
		foreach (string ignored in BossInfo.GetIgnoredModules("The Generator")){
			IGNORELIST.Add(ignored);
		}
		foreach (string module in Bomb.GetSolvableModuleNames()){
			if(!IGNORELIST.Contains(module)){
				stageLength += 1;
			}
		}
		//Debug.Log(stageLength);
		//Debug.Log(stageLength / 2);
		if (stageLength > 4 && !moduleDebug) { stageLength = UnityEngine.Random.Range((stageLength / 2) + (stageLength % 2), stageLength+1); }
		Debug.LogFormat("[The Generator #{0}] Number of stages: {1}", moduleId, stageLength-1);

		NewShuffleSet(false);
		StageToLog(0);
		renderDisplay(false);
		Debug.LogFormat("[The Generator #{0}] Starting Set: {1}-{2}-{3} // {4}-{5}-{6}", moduleId, logSET[0], logSET[1], logSET[2], logSET[3], logSET[4], logSET[5]);
	}

	void pressButton(KMSelectable buttonObject) {//KMSelectable button
		int buttonNum = Array.IndexOf(ButtonList, buttonObject);
		buttonObject.AddInteractionPunch();
		if (currentStage == stageLength && !moduleSolved){
			if (!buttonONS[buttonNum]){
				ButtonLights[buttonNum].material = LightColors[1];
				Audio.PlaySoundAtTransform("click57", transform);
			} else {
				ButtonLights[buttonNum].material = LightColors[0];
				Audio.PlaySoundAtTransform("click34", transform);
			}
			buttonONS[buttonNum] = !buttonONS[buttonNum];
		} else {
			Audio.PlaySoundAtTransform("click34", transform);
		}
	}

	void Submit() {
		if (currentStage == stageLength && !moduleSolved){
			if (submitBOOL == submitLAG){
				if (!submitBOOL){
					Audio.PlaySoundAtTransform("click36", transform);
					//Audio.PlaySoundAtTransform("click8", transform);
					Audio.PlaySoundAtTransform("click35", transform);
					Audio.PlaySoundAtTransform("click43", transform);
				} else {
					Audio.PlaySoundAtTransform("click34", transform);
					//switchLights[4].material = LightColors[0];
				}
				submitBOOL = !submitBOOL;
			}
		} else {
			Audio.PlaySoundAtTransform("click57", transform);
		}
	}

	IEnumerator UpdateSolves() {
		while (!TPAutoActive) {
			if (SOLVES != Bomb.GetSolvedModuleNames().Count() && currentStage != stageLength) {
				yield return new WaitForSeconds(0.1f);
				dualSolve = false;
				//Debug.Log(solveCount);
				GrabTrippedName();
				if (dualSolve) {// && currentStage != solveCount && currentStage != stageLength
					Debug.LogFormat("[The Generator #{0}] Multiple modules solved on the same frame. Reducing the stage count by {1} max", moduleId, Bomb.GetSolvedModuleNames().Count() - solveCount);
					stageLength -= Bomb.GetSolvedModuleNames().Count() - solveCount;
					solveCount = Bomb.GetSolvedModuleNames().Count();
					if (currentStage >= stageLength) {
						currentStage++;
						stageLength = currentStage;
						//currentStage++;
						Debug.LogFormat("[The Generator #{0}] Stage number reached. Awating power sequence...", moduleId);
						buttonONS = new bool[] {false, false, false, false, false, false};
						SegmentDisplay.text = "READY";
						Audio.PlaySoundAtTransform("metal_close_01", transform);
						Audio.PlaySoundAtTransform("lock_open_01", transform);
						for(int i = 0; i < 6; i++){
							ButtonLights[i].material = LightColors[0];
						}
						statusLights[2].material = LightColors[2];
						statusLights[1].material = LightColors[5];
						TPAutoActive = true;
					}
				} else if (!IGNORELIST.Contains(MostRecent)){
					solveCount = Bomb.GetSolvedModuleNames().Count();
					if (currentStage != solveCount && currentStage != stageLength){
						currentStage += 1;
						if (currentStage != stageLength){
							renderDisplay(false);
							NewShuffleSet(true);
							StageToLog(1);
							Debug.LogFormat("[The Generator #{0}] Submit Set: {1}-{2}-{3} // {4}-{5}-{6}", moduleId, logSET[0], logSET[1], logSET[2], logSET[3], logSET[4], logSET[5]);
						} else {
							Debug.LogFormat("[The Generator #{0}] Stage number reached. Awating power sequence...", moduleId);
							buttonONS = new bool[] {false, false, false, false, false, false};
							SegmentDisplay.text = "READY";
							Audio.PlaySoundAtTransform("metal_close_01", transform);
							Audio.PlaySoundAtTransform("lock_open_01", transform);
							for(int i = 0; i < 6; i++){
								ButtonLights[i].material = LightColors[0];
							}
							statusLights[2].material = LightColors[2];
							statusLights[1].material = LightColors[5];
							TPAutoActive = true;
						}
					} /*else if (currentStage == stageLength && !moduleSolved){
						Debug.LogFormat("[The Generator #{0}] <<POWER SURGE>> extra module solved before generator was powered. Striking...", moduleId);
						GetComponent<KMBombModule>().HandleStrike();
					}*/ //Maybe add a small visual flair when you solve additional modules so the log can still say power surge
				}
			} else { yield return new WaitForSeconds(0.01f); }
		}
	}

	void Update() {
		if (submitBOOL != submitLAG){
			if (lagFrames != 10){
				if (lagFrames < 6){
					if (submitBOOL){submitLeverObject.Rotate(15.0f, 0.0f, 0.0f);} else {submitLeverObject.Rotate(-15.0f, 0.0f, 0.0f);}
				} else if (lagFrames == 7){
					if (submitBOOL){submitLeverObject.Rotate(10.0f, 0.0f, 0.0f);} else {submitLeverObject.Rotate(-10.0f, 0.0f, 0.0f);}
				} else if (lagFrames == 9){
					if (submitBOOL){submitLeverObject.Rotate(-10.0f, 0.0f, 0.0f);} else {submitLeverObject.Rotate(10.0f, 0.0f, 0.0f);}
				}
				lagFrames += 1;
			} else {
				lagFrames = 0;
				submitLAG = submitBOOL;
				if(submitBOOL){
					SubmitTest();
				}
			}
		}
		
	}

	void GrabTrippedName () { //Borrowed from Validation
		Debug.Log(Bomb.GetSolvedModuleNames().Count() + " // " + SOLVES);
		//List<string> tempSolved = Bomb.GetSolvedModuleNames();
		Debug.Log(Bomb.GetSolvedModuleNames().Count() + " // " + SOLVES);
		if (Bomb.GetSolvedModuleNames().Count() - SOLVES >= 2) {
			dualSolve = true;
			primeOVERIDE = false;
			for(int i = SOLVES; i < Bomb.GetSolvedModuleNames().Count(); i++) { SolveList.Add(Bomb.GetSolvedModuleNames()[i]); }
			SOLVES = Bomb.GetSolvedModuleNames().Count();
			return;
		}
		//Debug.Log("I'm here");
		MostRecent = GetLatestSolve(Bomb.GetSolvedModuleNames(), SolveList);
        SolveList.Add(MostRecent);
        MostRecent = SolveList[SOLVES];
		Debug.Log(MostRecent);
		SOLVES = Bomb.GetSolvedModuleNames().Count();
		
		var module = MostRecent;
   	    if (module.StartsWith("The ")) {
  	    	module = module.Substring(4);
        }
		if (!Regex.IsMatch(module.Substring(0, 1), "[a-zA-Z0-9]")){
			primeOVERIDE = true;
			return;
		} else {primeOVERIDE = false;}
		string primeLetter = module.Substring(0, 1).ToUpper();
		//Debug.Log(primeLetter);
		if(Regex.IsMatch(primeLetter, "[0-9]")){
			primeNumber = Int32.Parse(primeLetter);
		} else {
			primeNumber = Array.IndexOf(alphabet, primeLetter)+1;
			
		}
	}

	//Borrowed from Validation
	private string GetLatestSolve(List<string> a, List<string> b) {
        string z = "";
        for (int i = 0; i < b.Count; i++)
        {
            a.Remove(b.ElementAt(i));
        }

        z = a.ElementAt(0);
        return z;
    }
	//END*/

	void renderDisplay (bool mode){
		string A = "";
		string B = "";
		int C = 0;
		if (!mode){
			A = "--";
			if (currentStage < 10){
				if(Bomb.GetSolvableModuleNames().Count() < 10){ B = "--"; } else if (Bomb.GetSolvableModuleNames().Count() < 100) { B = "-0"; } else { B = "00"; }
			} else if (currentStage < 100){
				if (Bomb.GetSolvableModuleNames().Count() < 100) { B = "-"; } else { B = "0"; }
			} else {
				B = "--";
			}
			C = currentStage;
		} else {
			A = "S-";
			if (submitStage < 10){
				if(Bomb.GetSolvableModuleNames().Count() < 10){ B = "--"; } else if (Bomb.GetSolvableModuleNames().Count() < 100) { B = "-0"; } else { B = "00"; }
			} else if (submitStage < 100){
				if (Bomb.GetSolvableModuleNames().Count() < 100) { B = "-"; } else { B = "0"; }
			} else {
				B = "--";
			}
			C = submitStage;
		}
		//if (moduleDebug) { SegmentDisplay.text = "WUMBO"; return; }
		SegmentDisplay.text = A + B + C;
	}

	void StageToLog(int mode) {
		int k = 0;
		for (int i = 0; i < 2; i++){
			for (int j = 0; j < 6; j+=2){
				if(mode == 0){
					if (currentSet[i+j]){logSET[k] = 1;} else {logSET[k] = 0;}
				} else if(mode == 1){
					if (SolveStages[currentStage][i+j]){logSET[k] = 1;} else {logSET[k] = 0;}
				} else {
					if (buttonONS[i+j]){logSET[k] = 1;} else {logSET[k] = 0;}
				}
				k += 1;
			}
		}
	}

	void NewShuffleSet(bool LOG){
		currentSet = new bool[] {false, false, false, false, false, false};
		ledCount = 0;
		for (int i = 0; i < 6; i++){
			if (UnityEngine.Random.Range(0, 2) == 1){
				currentSet[i] = true;
				ledCount += 1;
			}
		}
		//if (moduleDebug) { currentSet = new bool[] {true, true, false, true, true, false}; }
		RenderButtonSets();
		if (LOG){
			StageToLog(0);
			Debug.LogFormat("[The Generator #{0}] Showing stage {1}: {2}-{3}-{4} // {5}-{6}-{7}", moduleId, currentStage, logSET[0], logSET[1], logSET[2], logSET[3], logSET[4], logSET[5]);
		}
		if (currentStage == 0){//
			SolveStages.Add(currentSet);
		} else {
			for (int i = 0; i < 6; i++){buttonONS[i] = SolveStages[currentStage-1][i];}
			if (currentStage % 2 == 0){
				Debug.LogFormat("[The Generator #{0}] INVERT check passes", moduleId);
				for (int i = 0; i < 6; i++){buttonONS[i] = !buttonONS[i];}
			}
			
			bool conditionCheck = true;

			int PRIME = 0;
			//Debug.Log(primeNumber);
			if (!primeOVERIDE){
				for (int j = 1; j < 14; j++){
					if(primeNumber % j == 0){
						PRIME += 1;
						//Debug.Log(j);
					}
					if(PRIME >= 3 || primeNumber == 1){conditionCheck = false; break;}
					if(j == primeNumber){break;}
				}
			} else {
				conditionCheck = false;
			}
			//NAND
			if(conditionCheck){
				Debug.LogFormat("[The Generator #{0}] NAND check passes", moduleId);
				for (int i = 0; i < 6; i++){if (!currentSet[i] || !buttonONS[i]) {buttonONS[i] = true;} else {buttonONS[i] = false;}}
			} else {
				//AND
				if (ledCount >= 4){conditionCheck = true;} else {conditionCheck = false;}
				if(conditionCheck){
					Debug.LogFormat("[The Generator #{0}] AND check passes", moduleId);
					for (int i = 0; i < 6; i++){if (currentSet[i] && buttonONS[i]) {currentSet[i] = true;} else {currentSet[i] = false;}}
				} else {
					//OR
					Debug.LogFormat("[The Generator #{0}] OR check passes", moduleId);
					for (int i = 0; i < 6; i++){if (currentSet[i] || buttonONS[i]) {currentSet[i] = true;} else {currentSet[i] = false;}}
				}
			}

			for (int i = 0; i < 6; i++){buttonONS[i] = currentSet[i] ^ buttonONS[i];}

			//for (int i = 0; i < 6; i++){buttonONS[i] = false;}//DEBUG

			//OR
			int flatCheck = 0;
			for(int i = 0; i < 6; i++){if (buttonONS[i]) {flatCheck += 1;}}
			if(flatCheck == 0){
				Debug.LogFormat("[The Generator #{0}] SOLVE NOR check passes", moduleId);
				for (int i = 0; i < 6; i++){if (!currentSet[i] && !buttonONS[i]) {buttonONS[i] = true;} else {buttonONS[i] = false;}}
			} else if(flatCheck == 6){
				Debug.LogFormat("[The Generator #{0}] SOLVE AND check passes", moduleId);
				for (int i = 0; i < 6; i++){if (currentSet[i] && buttonONS[i]) {buttonONS[i] = true;} else {buttonONS[i] = false;}}
			}

			/*NOR
			flatCheck = 0;
			for(int i = 0; i < 6; i++){if (buttonONS[i]) {flatCheck += 1;}}
			if (flatCheck == 0 || flatCheck == 6){conditionCheck = true;} else {conditionCheck = false;}
			if (ledCount >= 3){conditionCheck = true;} else {conditionCheck = false;}
			if(conditionCheck){
				Debug.LogFormat("[The Generator #{0}] NOR check passes", moduleId);
				for (int i = 0; i < 6; i++){if (!currentSet[i] || !buttonONS[i]) {currentSet[i] = true;} else {currentSet[i] = false;}}
			}*/

			bool[] finalSEND = new bool[] {false, false, false, false, false, false};
			for(int i = 0; i < 6; i++){finalSEND[i] = buttonONS[i];}
			SolveStages.Add(finalSEND);
		}
	}

	void RenderButtonSets(){
		int light;
		for (int i = 0; i < 6; i++){
			light = 0;
			if (currentSet[i]){light = 1;}
			ButtonLights[i].material = LightColors[light];
		}
	}

	void SubmitTest(){
		if(moduleSolved) {return;}
		StageToLog(2);
		Debug.LogFormat("[The Generator #{0}] Submitting {1}-{2}-{3} // {4}-{5}-{6}...", moduleId, logSET[0], logSET[1], logSET[2], logSET[3], logSET[4], logSET[5]);
		for (int i = 0; i < 6; i++){
			if (buttonONS[i] != SolveStages[submitStage][i]){
				Debug.LogFormat("[The Generator #{0}] Submission for Stage {1} Incorrect. Showing correct sequence...", moduleId, submitStage);
				GetComponent<KMBombModule>().HandleStrike();
				for (int j = 0; j < 6; j++){
					if (buttonONS[j] && !SolveStages[submitStage][j]){
						ButtonLights[j].material = LightColors[3];
					} else if (!buttonONS[j] && SolveStages[submitStage][j]){
						ButtonLights[j].material = LightColors[5];
					}
				}
				submitBOOL = !submitBOOL;
				return;
			}
		}

		Debug.LogFormat("[The Generator #{0}] Submission for Stage {1} Correct", moduleId, submitStage);

		submitStage += 1;
		if (submitStage == stageLength){
			Debug.LogFormat("[The Generator #{0}] Generator successfully powered on", moduleId);
			Audio.PlaySoundAtTransform("steam hisses - Marker #1", transform);
			Audio.PlaySoundAtTransform("lock_open_01", transform);
			Audio.PlaySoundAtTransform("metal_open_01", transform);
			moduleSolved = true;
			statusLights[1].material = LightColors[4];
			statusLights[0].material = LightColors[7];
			SegmentDisplay.text = "POWER";
			for (int i = 0; i < 6; i++){
				ButtonLights[i].material = LightColors[1];
			}
			GetComponent<KMBombModule>().HandlePass();
			return;
		} else {
			Audio.PlaySoundAtTransform("metal_close_01", transform);
			submitBOOL = !submitBOOL;
			renderDisplay(true);
		}
	}

	// Twitch Plays Support by Kilo Bites // Modified by Nimsay Ramsey

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} Press Left/Right 1-3 to push a button on the left or right side || !{0} Submit to submit a stage || Read/Submit order for TP is L1-L2-L3-R1-R2-R3";
#pragma warning restore 414

	bool isValidPos(string n)
	{
		string[] valids = { "1", "2", "3" };
		if (!valids.Contains(n))
		{
			return false;
		}
		return true;
	}

	bool isValidSide(string n)
	{
		string[] valids = { "LEFT", "RIGHT" };
		if (!valids.Contains(n))
		{
			return false;
		}
		return true;
	}

	IEnumerator ProcessTwitchCommand (string command)
	{
		yield return null;

		string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

		if (split[0].EqualsIgnoreCase("PRESS"))
		{
			//int numberClicks = 0;
			int pos = 0;
			if (split.Length != 3)
			{
				yield return "sendtochaterror Please specify which button to press!";
				yield break;
			}
			else if (!isValidSide(split[1]))
			{
				yield return "sendtochaterror " + split[1] + " is not a valid side!";
				yield break;
			}
			else if (!isValidPos(split[2]))
			{
				yield return "sendtochaterror " + split[2] + " is not a valid number!";
				yield break;
			}
			int.TryParse(split[2], out pos);
			//int.TryParse(split[2], out numberClicks);
			pos = pos - 1;
			if (split[1].EqualsIgnoreCase("LEFT")){ ButtonList[pos*2].OnInteract(); } else { ButtonList[(pos*2)+1].OnInteract(); }

			yield break;
		}
		
		if (split[0].EqualsIgnoreCase("SUBMIT"))
		{
			submitLever.OnInteract();
			yield break;
		}
	}

	IEnumerator TwitchHandleForcedSolve() //Autosolver
	{
		Debug.Log("Autosolver Engaged");
		while (!TPAutoActive) yield return true;
		yield return null;
		Debug.Log("Autosolver started");
		for (int stage = 0; stage < stageLength; stage++){
			//Debug.Log("Stage " + stage);
			for (int i = 0; i < 6; i++){
				//Debug.Log(i);
				if(buttonONS[i] != SolveStages[submitStage][i]){ ButtonList[i].OnInteract(); }
				yield return new WaitForSeconds(0.1f);
			}
			submitLever.OnInteract();
			yield return new WaitForSeconds(0.3f);
		}
	}
}
