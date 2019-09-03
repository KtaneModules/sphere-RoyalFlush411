using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using theSphere;

public class theSphereScript : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMAudio Audio;

    public int[] selectedColourIndices;
    private int colourStage = 0;
    private int lastSelected = 9;
    public Animator colourCycle;
    public Animator[] stage5Lights;
    public KMSelectable sphere;
    public string[] colourNames;
    public string[] colourNamesCaps;
    public Light illuminator;
    public Animator rotation;
    public AudioSource hum;

    public GameObject[] stageLights;
    public GameObject interactionLights;
    public Color[] stageLightColours;

    public List<int> serialNumberInts = new List<int>();

    public int[] tapTimes;
    public int[] holdTimes;
    public int orderDeterminer = 0;
    public int[] pressOrder;
    public bool[] requiresTap;
    public bool[] correctInput;

    private float timeOfPress = 0f;
    private float timeOfRelease = 0f;
    private float heldTime = 0f;

    private int AABatts = 0;

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    int stage = 0;
    bool moduleSolved;
    bool pressed;
    bool checking;
    bool answerCheck;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        sphere.OnInteract += delegate () { PressSphere(); return false; };
        sphere.OnInteractEnded += delegate () { ReleaseSphere();};
    }

    void Start()
    {
        AABatts = Bomb.GetBatteryCount(Battery.AA) + Bomb.GetBatteryCount(Battery.AAx3) + Bomb.GetBatteryCount(Battery.AAx4);
        interactionLights.SetActive(false);
        foreach(GameObject stageLight in stageLights)
        {
            stageLight.SetActive(false);
        }
        GetSerialCharacters();
        SelectColours();
    }

    void GetSerialCharacters()
    {
        foreach (char c in Bomb.GetSerialNumber())
        {
            serialNumberInts.Add(c >= '0' && c <= '9' ? (c - '0') : ((c - 'A' + 1) % 10));
        }
        for(int i = 0; i <= 5; i++)
        {
            tapTimes[i] = serialNumberInts[i];
        }
    }

    void SelectColours()
    {
        for(int i = 0; i <= 4; i++)
        {
            int index = UnityEngine.Random.Range(0,8);
            while(index == lastSelected)
            {
                index = UnityEngine.Random.Range(0,8);
            }
            lastSelected = index;
            selectedColourIndices[i] = index;
        }
        Debug.LogFormat("[The Sphere #{0}] The cycling colours are {1}, {2}, {3}, {4} & {5}.", moduleId, colourNames[selectedColourIndices[0]], colourNames[selectedColourIndices[1]], colourNames[selectedColourIndices[2]], colourNames[selectedColourIndices[3]], colourNames[selectedColourIndices[4]]);
        Debug.LogFormat("[The Sphere #{0}] The converted serial number digits are {1}{2}{3}{4}{5}{6}.", moduleId, serialNumberInts[0], serialNumberInts[1], serialNumberInts[2], serialNumberInts[3], serialNumberInts[4], serialNumberInts[5]);
        StartCoroutine(colourCycleRoutine());
        CalculateHolds();
        CalculateOrder();
    }

    void CalculateHolds()
    {
        for(int i = 0; i<= 4; i++)
        {
            if(selectedColourIndices[i] == 0)
            {
                int redToSquare = Bomb.GetPortCount(Port.DVI) + Bomb.GetOffIndicators().Count();
                holdTimes[i] = ((redToSquare * redToSquare) % 10) + 1;
            }
            else if(selectedColourIndices[i] == 1)
            {
                int blueToCube = Bomb.GetBatteryCount() + Bomb.GetPortCount(Port.Parallel) + Bomb.GetOnIndicators().Count();
                int blueCubed = blueToCube * blueToCube * blueToCube;
                if(blueCubed > 9)
                {
                    holdTimes[i] = ((blueCubed / 10) % 10);
                    if(holdTimes[i] == 0)
                    {
                        holdTimes[i] = 5;
                    }
                }
                else
                {
                    holdTimes[i] = 5;
                }
            }
            else if(selectedColourIndices[i] == 2)
            {
                int greenToRoot = serialNumberInts.Sum();
                holdTimes[i] = ((greenToRoot - 1) % 9 )+ 1;
                if(holdTimes[i] == 0)
                {
                    holdTimes[i] = 4;
                }
            }
            else if(selectedColourIndices[i] == 3)
            {
                int orangeToMod = (Bomb.GetBatteryHolderCount() + Bomb.GetPortPlates().Count() + 7) * (Bomb.GetPortCount(Port.RJ45) + Bomb.GetPortCount(Port.Parallel) + Bomb.GetOffIndicators().Count() + 3);
                List<int> orangeInts = new List<int>();
                foreach (char c in orangeToMod.ToString())
                {
                    orangeInts.Add(c - '0');
                }
                holdTimes[i] = ((orangeInts.Sum()) % 10) + 1;
            }
            else if(selectedColourIndices[i] == 4)
            {
                int difference = (Bomb.GetOnIndicators().Count() * Bomb.GetOnIndicators().Count()) - (Bomb.GetBatteryCount() * Bomb.GetBatteryCount());
                if(difference < 0)
                {
                    difference = difference * -1;
                }
                holdTimes[i] = (difference % 10) + 1;
            }
            else if(selectedColourIndices[i] == 5)
            {
                int purpleToCube = Bomb.GetPortCount() + Bomb.GetPortPlates().Count() + Bomb.GetOffIndicators().Count() + Bomb.GetBatteryHolderCount();
                int purpleCubed = purpleToCube * purpleToCube * purpleToCube;
                if(purpleCubed > 99)
                {
                    holdTimes[i] = ((purpleCubed / 100) % 10);
                    if(holdTimes[i] == 0)
                    {
                        holdTimes[i] = 7;
                    }
                }
                else
                {
                    holdTimes[i] = 7;
                }
            }
            else if(selectedColourIndices[i] == 6)
            {
                int platesToSquare = Bomb.GetPortPlates().Count() * Bomb.GetPortPlates().Count();
                int battsToCube = Bomb.GetBatteryCount() * Bomb.GetBatteryCount() * Bomb.GetBatteryCount();
                int platesBattsSum = battsToCube + platesToSquare;
                holdTimes[i] = ((platesBattsSum - 1) % 9 )+ 1;
                if(holdTimes[i] == 0)
                {
                    holdTimes[i] = 4;
                }
            }
            else if(selectedColourIndices[i] == 7)
            {
                int whiteToRoot = (Bomb.GetBatteryCount() + Bomb.GetOnIndicators().Count() + 13) * (Bomb.GetPortCount() + Bomb.GetOnIndicators().Count() + Bomb.GetOffIndicators().Count() + Bomb.GetPortPlates().Count() + 9);
                holdTimes[i] = ((whiteToRoot - 1) % 9 )+ 1;
            }
        }
        for(int i = 0; i <= 4; i++)
        {
            Debug.LogFormat("[The Sphere #{0}] {1} yields {2}.", moduleId, colourNamesCaps[selectedColourIndices[i]], holdTimes[i]);
        }
    }

    private void CalculateOrder()
    {
        if(AABatts == 2 && Bomb.GetPortCount(Port.Serial) == 1 && Bomb.IsIndicatorOff("FRQ") && Bomb.GetPortPlateCount() == 3)
        {
            pressOrder[0] = tapTimes[0];
            pressOrder[1] = holdTimes[0];
            pressOrder[2] = tapTimes[1];
            pressOrder[3] = holdTimes[1];
            pressOrder[4] = tapTimes[2];
            pressOrder[5] = holdTimes[2];
            pressOrder[6] = tapTimes[3];
            pressOrder[7] = holdTimes[3];
            pressOrder[8] = tapTimes[4];
            pressOrder[9] = holdTimes[4];
            pressOrder[10] = tapTimes[5];
            orderDeterminer = 10;
        }
        else
        {
            int finalVar1 = Bomb.GetOnIndicators().Count() + Bomb.GetBatteryHolderCount() + Bomb.GetPortCount(Port.Serial) + Bomb.GetPortCount(Port.RJ45);
            int finalVar2 = Bomb.GetOffIndicators().Count() + Bomb.GetPortPlateCount() + Bomb.GetBatteryCount(Battery.D) + Bomb.GetPortCount(Port.StereoRCA);
            orderDeterminer = (finalVar1 * finalVar2) % 10;

            if(orderDeterminer == 0)
            {
                //T4, T1, H5, T2, H3, H1, T6, T3, H2, H4, T5
                pressOrder[0] = tapTimes[3];
                requiresTap[0] = true;
                pressOrder[1] = tapTimes[0];
                requiresTap[1] = true;
                pressOrder[2] = holdTimes[4];
                pressOrder[3] = tapTimes[1];
                requiresTap[3] = true;
                pressOrder[4] = holdTimes[2];
                pressOrder[5] = holdTimes[0];
                pressOrder[6] = tapTimes[5];
                requiresTap[6] = true;
                pressOrder[7] = tapTimes[2];
                requiresTap[7] = true;
                pressOrder[8] = holdTimes[1];
                pressOrder[9] = holdTimes[3];
                pressOrder[10] = tapTimes[4];
                requiresTap[10] = true;
            }
            else if(orderDeterminer == 1)
            {
                //H3, T2, T6, T1, H2, H5, T3, T4, T5, H1, H4
                pressOrder[0] = holdTimes[2];
                pressOrder[1] = tapTimes[1];
                requiresTap[1] = true;
                pressOrder[2] = tapTimes[5];
                requiresTap[2] = true;
                pressOrder[3] = tapTimes[0];
                requiresTap[3] = true;
                pressOrder[4] = holdTimes[1];
                pressOrder[5] = holdTimes[4];
                pressOrder[6] = tapTimes[2];
                requiresTap[6] = true;
                pressOrder[7] = tapTimes[3];
                requiresTap[7] = true;
                pressOrder[8] = tapTimes[4];
                requiresTap[8] = true;
                pressOrder[9] = holdTimes[0];
                pressOrder[10] = holdTimes[3];
            }
            else if(orderDeterminer == 2)
            {
                //H5, H1, T3, T4, H3, T6, T1, H2, H4, T5, T2
                pressOrder[0] = holdTimes[4];
                pressOrder[1] = holdTimes[0];
                pressOrder[2] = tapTimes[2];
                requiresTap[2] = true;
                pressOrder[3] = tapTimes[3];
                requiresTap[3] = true;
                pressOrder[4] = holdTimes[2];
                pressOrder[5] = tapTimes[5];
                requiresTap[5] = true;
                pressOrder[6] = tapTimes[0];
                requiresTap[6] = true;
                pressOrder[7] = holdTimes[1];
                pressOrder[8] = holdTimes[3];
                pressOrder[9] = tapTimes[4];
                requiresTap[9] = true;
                pressOrder[10] = tapTimes[1];
                requiresTap[10] = true;
            }
            else if(orderDeterminer == 3)
            {
                //T1, H2, T3, H5, T6, H4, H1, T2, T4, T5, H3
                pressOrder[0] = tapTimes[0];
                requiresTap[0] = true;
                pressOrder[1] = holdTimes[1];
                pressOrder[2] = tapTimes[2];
                requiresTap[2] = true;
                pressOrder[3] = holdTimes[4];
                pressOrder[4] = tapTimes[5];
                requiresTap[4] = true;
                pressOrder[5] = holdTimes[3];
                pressOrder[6] = holdTimes[0];
                pressOrder[7] = tapTimes[1];
                requiresTap[7] = true;
                pressOrder[8] = tapTimes[3];
                requiresTap[8] = true;
                pressOrder[9] = tapTimes[4];
                requiresTap[9] = true;
                pressOrder[10] = holdTimes[2];
            }
            else if(orderDeterminer == 4)
            {
                //H1, T5, T3, H4, H2, T6, T1, T2, T4, H3, H5
                pressOrder[0] = holdTimes[0];
                pressOrder[1] = tapTimes[4];
                requiresTap[1] = true;
                pressOrder[2] = tapTimes[2];
                requiresTap[2] = true;
                pressOrder[3] = holdTimes[3];
                pressOrder[4] = holdTimes[1];
                pressOrder[5] = tapTimes[5];
                requiresTap[5] = true;
                pressOrder[6] = tapTimes[0];
                requiresTap[6] = true;
                pressOrder[7] = tapTimes[1];
                requiresTap[7] = true;
                pressOrder[8] = tapTimes[3];
                requiresTap[8] = true;
                pressOrder[9] = holdTimes[2];
                pressOrder[10] = holdTimes[4];
            }
            else if(orderDeterminer == 5)
            {
                //T2, T4, H5, H1, T3, T1, H2, H3, H4, T5, T6
                pressOrder[0] = tapTimes[1];
                requiresTap[0] = true;
                pressOrder[1] = tapTimes[3];
                requiresTap[1] = true;
                pressOrder[2] = holdTimes[4];
                pressOrder[3] = holdTimes[0];
                pressOrder[4] = tapTimes[2];
                requiresTap[4] = true;
                pressOrder[5] = tapTimes[0];
                requiresTap[5] = true;
                pressOrder[6] = holdTimes[1];
                pressOrder[7] = holdTimes[2];
                pressOrder[8] = holdTimes[3];
                pressOrder[9] = tapTimes[4];
                requiresTap[9] = true;
                pressOrder[10] = tapTimes[5];
                requiresTap[10] = true;
            }
            else if(orderDeterminer == 6)
            {
                //T6, H3, T2, H1, T5, T4, H4, H2, T3, T1, H5
                pressOrder[0] = tapTimes[5];
                requiresTap[0] = true;
                pressOrder[1] = holdTimes[2];
                pressOrder[2] = tapTimes[1];
                requiresTap[2] = true;
                pressOrder[3] = holdTimes[0];
                pressOrder[4] = tapTimes[4];
                requiresTap[4] = true;
                pressOrder[5] = tapTimes[3];
                requiresTap[5] = true;
                pressOrder[6] = holdTimes[3];
                pressOrder[7] = holdTimes[1];
                pressOrder[8] = tapTimes[2];
                requiresTap[8] = true;
                pressOrder[9] = tapTimes[0];
                requiresTap[9] = true;
                pressOrder[10] = holdTimes[4];

            }
            else if(orderDeterminer == 7)
            {
                //H4, H1, H3, T2, T6, H5, H2, T4, T3, T5, T1
                pressOrder[0] = holdTimes[3];
                pressOrder[1] = holdTimes[0];
                pressOrder[2] = holdTimes[2];
                pressOrder[3] = tapTimes[1];
                requiresTap[3] = true;
                pressOrder[4] = tapTimes[5];
                requiresTap[4] = true;
                pressOrder[5] = holdTimes[4];
                pressOrder[6] = holdTimes[1];
                pressOrder[7] = tapTimes[3];
                requiresTap[7] = true;
                pressOrder[8] = tapTimes[2];
                requiresTap[8] = true;
                pressOrder[9] = tapTimes[4];
                requiresTap[9] = true;
                pressOrder[10] = tapTimes[0];
                requiresTap[10] = true;
            }
            else if(orderDeterminer == 8)
            {
                //T4, T6, H3, T1, T2, H5, H1, T3, H2, T5, H4
                pressOrder[0] = tapTimes[3];
                requiresTap[0] = true;
                pressOrder[1] = tapTimes[5];
                requiresTap[1] = true;
                pressOrder[2] = holdTimes[2];
                pressOrder[3] = tapTimes[0];
                requiresTap[3] = true;
                pressOrder[4] = tapTimes[1];
                requiresTap[4] = true;
                pressOrder[5] = holdTimes[4];
                pressOrder[6] = holdTimes[0];
                pressOrder[7] = tapTimes[2];
                requiresTap[7] = true;
                pressOrder[8] = holdTimes[1];
                pressOrder[9] = tapTimes[4];
                requiresTap[9] = true;
                pressOrder[10] = holdTimes[3];
            }
            else if(orderDeterminer == 9)
            {
                //H2, T2, H3, T6, H1, T5, T4, H4, H5, T1, T3
                pressOrder[0] = holdTimes[1];
                pressOrder[1] = tapTimes[1];
                requiresTap[1] = true;
                pressOrder[2] = holdTimes[2];
                pressOrder[3] = tapTimes[5];
                requiresTap[3] = true;
                pressOrder[4] = holdTimes[0];
                pressOrder[5] = tapTimes[4];
                requiresTap[5] = true;
                pressOrder[6] = tapTimes[3];
                requiresTap[6] = true;
                pressOrder[7] = holdTimes[3];
                pressOrder[8] = holdTimes[4];
                pressOrder[9] = tapTimes[0];
                requiresTap[9] = true;
                pressOrder[10] = tapTimes[2];
                requiresTap[10] = true;
            }
            Debug.LogFormat("[The Sphere #{0}] Use order {1} when submitting your answer.", moduleId, orderDeterminer);
            for(int i = 0; i <= 10; i++)
            {
                if(requiresTap[i])
                {
                    Debug.LogFormat("[The Sphere #{0}] Stage {1}: tap the sphere when the last digit of the second timer is {2}.", moduleId, i+1, pressOrder[i]);
                }
                else
                {
                    Debug.LogFormat("[The Sphere #{0}] Stage {1}: hold the sphere for {2} seconds.", moduleId, i+1, pressOrder[i]);
                }
            }
        }
    }

    IEnumerator colourCycleRoutine()
    {
        while(!moduleSolved)
        {
            if(colourStage == 4)
            {
                Audio.PlaySoundAtTransform("colour5Beep", transform);
                stage5Lights[0].SetBool("stage5", true);
                stage5Lights[1].SetBool("stage5", true);
            }
            if(selectedColourIndices[colourStage] == 0)
            {
                colourCycle.SetBool("red", true);
                yield return new WaitForSeconds(1f);
                colourCycle.SetBool("red", false);
            }
            else if (selectedColourIndices[colourStage] == 1)
            {
                colourCycle.SetBool("blue", true);
                yield return new WaitForSeconds(1f);
                colourCycle.SetBool("blue", false);
            }
            else if (selectedColourIndices[colourStage] == 2)
            {
                colourCycle.SetBool("green", true);
                yield return new WaitForSeconds(1f);
                colourCycle.SetBool("green", false);
            }
            else if (selectedColourIndices[colourStage] == 3)
            {
                colourCycle.SetBool("orange", true);
                yield return new WaitForSeconds(1f);
                colourCycle.SetBool("orange", false);
            }
            else if (selectedColourIndices[colourStage] == 4)
            {
                colourCycle.SetBool("pink", true);
                yield return new WaitForSeconds(1f);
                colourCycle.SetBool("pink", false);
            }
            else if (selectedColourIndices[colourStage] == 5)
            {
                colourCycle.SetBool("purple", true);
                yield return new WaitForSeconds(1f);
                colourCycle.SetBool("purple", false);
            }
            else if (selectedColourIndices[colourStage] == 6)
            {
                colourCycle.SetBool("grey", true);
                yield return new WaitForSeconds(1f);
                colourCycle.SetBool("grey", false);
            }
            else if (selectedColourIndices[colourStage] == 7)
            {
                colourCycle.SetBool("white", true);
                yield return new WaitForSeconds(1f);
                colourCycle.SetBool("white", false);
            }
            stage5Lights[0].SetBool("stage5", false);
            stage5Lights[1].SetBool("stage5", false);
            colourStage++;
            colourStage = colourStage % 5;
            yield return new WaitForSeconds(4.5f);
        }
    }

    public void PressSphere()
    {
        if(moduleSolved || checking)
        {
            return;
        }
        if(answerCheck)
        {
            for (int i = stage; i < 11; i++)
                stageLights[i].SetActive(false);
            answerCheck = false;
        }
        sphere.AddInteractionPunch(0.5f);
        timeOfPress = Bomb.GetTime();
        pressed = true;
        interactionLights.SetActive(true);
        StartCoroutine(checkForHold());
    }

    IEnumerator checkForHold()
    {
        Audio.PlaySoundAtTransform("singleClick", transform);
        yield return new WaitForSeconds(0.9f);
        if(pressed)
        {
            StartCoroutine(constantFlicker());
        }
    }

    public void ReleaseSphere()
    {
        if(!pressed || moduleSolved || checking)
        {
            return;
        }
        timeOfRelease = Bomb.GetTime();
        heldTime = Mathf.RoundToInt(Mathf.Abs(timeOfPress - timeOfRelease) % 60);
        if(heldTime == 0f)
        {
            TapCheck();
        }
        else
        {
            HoldCheck();
        }
        pressed = false;
        interactionLights.SetActive(false);
        do
        {
            stageLights[stage].SetActive(true);
            stage++;
        } while (stage < 11 && correctInput[stage]);
        if (stage == 11)
        {
            checking = true;
            answerCheck = true;
            StartCoroutine(SolveCheckRoutine());
        }
    }

    IEnumerator SolveCheckRoutine()
    {
        yield return new WaitForSeconds(2f);
        if(correctInput[0] && correctInput[1] && correctInput[2] && correctInput[3] && correctInput[4] && correctInput[5] && correctInput[6] && correctInput[7] && correctInput[8] && correctInput[9] && correctInput[10])
        {
            hum.Play();
            for(int i = 0; i <= 10; i++)
            {
                stageLights[i].GetComponentInChildren<Light>().color = stageLightColours[3];
            }
            moduleSolved = true;
            illuminator.gameObject.SetActive(false);
            rotation.enabled = false;
            int swoosh = 0;
            while(swoosh < 11)
            {
                yield return new WaitForSeconds(0.5f);
                Audio.PlaySoundAtTransform("singleClick", transform);
                yield return new WaitForSeconds(0.2f);
                stageLights[swoosh].SetActive(false);
                swoosh++;
            }
            yield return new WaitForSeconds(0.5f);
            Audio.PlaySoundAtTransform("colour5Beep", transform);
            GetComponent<KMBombModule>().HandlePass();
            Debug.LogFormat("[The Sphere #{0}] Inputs correct. Module disarmed.", moduleId);
            while(hum.volume != 0)
            {
                hum.volume -= 0.01f;
                yield return new WaitForSeconds(0.05f);
            }
            hum.Stop();
        }
        else
        {
            GetComponent<KMBombModule>().HandleStrike();
            Debug.LogFormat("[The Sphere #{0}] Strike! Not all stages were correct.", moduleId);
            for(int i = 0; i <= 10; i++)
            {
                if(!correctInput[i])
                {
                    Debug.LogFormat("[The Sphere #{0}] Stage {1} was incorrect.", moduleId, i+1);
                }
            }
            for(int i = 0; i <= 10; i++)
            {
                if(!correctInput[i])
                {
                    stageLights[i].GetComponentInChildren<Light>().color = stageLightColours[2];
                }
                else
                {
                    stageLights[i].GetComponentInChildren<Light>().color = stageLightColours[3];
                }
            }
            yield return new WaitForSeconds(3f);
        }
        stage = 0;
        while (stage < 11 && correctInput[stage]) stage++;
        checking = false;
    }

    private void TapCheck()
    {
        stageLights[stage].GetComponentInChildren<Light>().color = stageLightColours[0];
        int lastSecondsDigit = Mathf.FloorToInt(timeOfPress) % 10;
        if(requiresTap[stage] && lastSecondsDigit == pressOrder[stage])
        {
            correctInput[stage] = true;
            Debug.LogFormat("[The Sphere #{0}] At stage {1}, you tapped the sphere when the last digit of the second timer was {2}. That is correct.", moduleId, stage + 1, lastSecondsDigit);
        }
        else
        {
            Debug.LogFormat("[The Sphere #{0}] At stage {1}, you tapped the sphere when the last digit of the second timer was {2}. That is incorrect.", moduleId, stage + 1, lastSecondsDigit);
        }
    }

    private void HoldCheck()
    {
        stageLights[stage].GetComponentInChildren<Light>().color = stageLightColours[1];
        if(!requiresTap[stage] && heldTime == pressOrder[stage])
        {
            correctInput[stage] = true;
            Debug.LogFormat("[The Sphere #{0}] At stage {1}, you held the sphere for {2} seconds. That is correct.", moduleId, stage + 1, heldTime);
        }
        else
        {
            Debug.LogFormat("[The Sphere #{0}] At stage {1}, you held the sphere for {2} seconds. That is incorrect.", moduleId, stage + 1, heldTime);
        }
    }

    IEnumerator constantFlicker()
    {
        hum.Play();
        while(pressed)
        {
            interactionLights.SetActive(true);
            yield return new WaitForSeconds(0.05f);
            interactionLights.SetActive(false);
            yield return new WaitForSeconds(0.05f);
        }
        hum.Stop();
        Audio.PlaySoundAtTransform("singleClick", transform);
    }

	public readonly string TwitchHelpMessage = "Tap the sphere using !{0} tap <digit>. Hold the sphere using !{0} hold <length>. Commands can be chained together using semicolons and abbreviated.";

	public IEnumerator ProcessTwitchCommand(string command)
	{
		string[] chainedCommands = command.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
		if (chainedCommands.Length > 1)
		{
			var commandRoutines = chainedCommands.Select(ProcessTwitchCommand).ToArray();
			var invalidCommand = Array.Find(commandRoutines, routine => !routine.MoveNext());
			if (invalidCommand != null)
			{
				yield return "sendtochaterror The command \"" + chainedCommands[Array.IndexOf(commandRoutines, invalidCommand)] + "\" is invalid.";
				yield break;
			}

			yield return null;
			foreach (IEnumerator routine in commandRoutines)
			{
				yield return routine;
				yield return "trycancel The chained command was not continued due to a request to cancel.";
			}

			yield break;
		}

		string[] split = command.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (split.Length == 2)
		{
			int tapDigit;
			float holdTime;
			if ((split[0] == "tap" || split[0] == "t") && int.TryParse(split[1], out tapDigit))
			{
				yield return null;
				while (Mathf.FloorToInt(Bomb.GetTime()) % 10 != tapDigit)
					yield return "trycancel The sphere was not tapped due to a request to cancel.";

				sphere.OnInteract();
				sphere.OnInteractEnded();
				yield return new WaitForSeconds(0.1f);
			}
			else if ((split[0] == "hold" || split[0] == "h") && float.TryParse(split[1], out holdTime))
			{
				yield return null;

				sphere.OnInteract();
				while (Mathf.RoundToInt(Mathf.Abs(timeOfPress - Bomb.GetTime()) % 60) != holdTime)
					yield return true; // Return true which will get returned by the force solve.
				sphere.OnInteractEnded();
				yield return new WaitForSeconds(0.1f);
			}
		}
	}

    public IEnumerator TwitchHandleForcedSolve()
	{
		// If someone has failed any of the stages before the current stage we would have to take a strike, so just force a pass.
		if (stage > 0 && Enumerable.Range(0, stage - 1).Any(stageNumber => !correctInput[stageNumber]))
		{
			GetComponent<KMBombModule>().HandlePass();
			yield break;
		}

		// Execute the rest of the stages that were incorrect
		var commands = Enumerable.Range(stage, 11 - stage).Where(stageNumber => !correctInput[stageNumber]).Select(stageNumber => (requiresTap[stageNumber] ? "tap" : "hold") + " " + pressOrder[stageNumber]);
		foreach (string command in commands)
		{
			IEnumerator enumerator = ProcessTwitchCommand(command);
			while (enumerator.MoveNext())
			{
				object value = enumerator.Current;
				if (value is string && value.ToString().StartsWith("trycancel "))
				{
					yield return true;
				}
				else
				{
					yield return value;
				}
			}
		}
	}
}
