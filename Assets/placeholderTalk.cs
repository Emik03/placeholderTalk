﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;
using KModkit;

public class placeholderTalk : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombModule Module;
    public KMBombInfo Info;
    public TextMesh Screen;
    public KMSelectable[] btn;
    public Transform[] anchor;

    private bool _isSolved = false, _lightsOn = false, _isRandomising = false, formatText = true, _debug = false;
    private byte _answerId, _questionId, _questionOffsetId, _randomised = 0, frames = 0;
    private sbyte _previousModuleCarry = 0;
    private short _answerOffsetId;
    private int _moduleId = 0;
    private string _screenText1 = "", _screenText2 = "";

    private static bool _playSound;
    private static int _solvedTimes = 0, _moduleIdCounter = 1;

    /// <summary>
    /// Code that runs when bomb is loading.
    /// </summary>
    private void Start()
    {
        Module.OnActivate += Activate;
        _moduleId = _moduleIdCounter++;
    }

    /// <summary>
    /// Initalising buttons.
    /// </summary>
    private void Awake()
    {
        _previousModuleCarry = 0;
        _playSound = true;

        for (int i = 0; i < 4; i++)
        {
            int j = i;
            btn[i].OnInteract += delegate ()
            {
                HandlePress(j);
                return false;
            };
        }
    }

    /// <summary>
    /// Lights get turned on.
    /// </summary>
    void Activate()
    {
        Init();
        _lightsOn = true;
    }

    /// <summary>
    /// Runs the text flicker effect.
    /// </summary>
    private void FixedUpdate()
    {
        //makes the z coordinate based on sine waves for each button
        for (int i = 0; i < btn.Length; i++)
        {
            //amplification here
            float amplified = 0.0025f;

            float x = anchor[i].transform.position.x;
            float y = anchor[i].transform.position.y;
            //ensures that it stays snapped to the module, it can easily go offscreen
            float z = anchor[i].transform.position.z - (Mathf.Sin(Time.time + i * Mathf.PI / 2) * amplified);

            btn[i].transform.position = new Vector3(x, y, z);
        }
        

        if (_isRandomising && !_debug)
        {
            //frame counter, a cycle is 3 frames
            frames++;
            frames %= 3;

            //play sound effect once
            if (_randomised == 0 && _playSound)
            {
                Audio.PlaySoundAtTransform("shuffle", Module.transform);
                _playSound = false;
            }

            //if cycle is prepped
            if (frames == 0)
            {
                //shuffle the text 20 times
                if (_randomised < 20)
                    UpdateText(true);

                //after shuffling it 20 times, display the phrases
                else
                {
                    UpdateText(false);

                    frames = 0;
                    _randomised = 0;
                    _isRandomising = false;
                }
            }
        }

        //debug code
        else if (_debug)
        {
            //frame counter, a cycle is however many frames it modulates
            frames++;
            frames %= 255;

            if (frames == 0)
                UpdateText(false);
        }
    }

    /// <summary>
    /// Generate new phrases and calculate the answer of the module.
    /// </summary>
    void Init()
    {
        if (!_debug)
        {
            //determine the prompts given
            _questionOffsetId = (byte)Random.Range(0, _firstPhrase.Length);
            _questionId = (byte)Random.Range(0, _secondPhrase.Length);
        }

        else
        {
            //determine the prompts given
            _questionOffsetId = 15;
            _questionId = 158;
        }

        Debug.LogFormat("");
        Debug.LogFormat("[Placeholder Talk #{0}] First Phrase ({2}): \"{1}\"", _moduleId, _firstPhrase[_questionOffsetId], _questionOffsetId);
        Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase ({2}): \"{1}\"", _moduleId, _secondPhrase[_questionId].Replace("\n\n", " "), _questionId);

        //start displaying message
        _isRandomising = true;

        //generate an answer
        Debug.LogFormat("[Placeholder Talk #{0}] (First Phrase + Second Phrase) mod 4 = {1}. Push the button labeled {1}.", _moduleId, GetAnswer() + 1);
        Debug.LogFormat("");
    }

    /// <summary>
    /// Renders the text on screen.
    /// </summary>
    /// <param name="random">Determine whether or not the text rendered should be random.</param>
    void UpdateText(bool random)
    {
        //if not in a debugging state
        if (!_debug)
        {
            //if the text should be random
            if (!random)
            {
                //render the real text
                _screenText1 = "THE ANSWER ";
                _screenText1 += _firstPhrase[_questionOffsetId] + "\n\n";
                _screenText1 += _ordinals[Random.Range(0, _ordinals.Length)] + "\n\n";
                _screenText1 += "\n\n";
                _screenText2 = _secondPhrase[_questionId];

                //meme text for thumbnail
                //_screenText1 = "THE ANSWER ";
                //_screenText1 += _firstPhrase[18] + "\n\n";
                //_screenText1 += _ordinals[9] + "\n\n";
                //_screenText1 += "\n\n";
                //_screenText2 = _secondPhrase[143];

                //render the text
                RenderText();
            }

            else
            {
                //empty all text
                _screenText1 = "";
                _screenText2 = "";

                //render text randomly
                _randomised++;

                //render the text
                RandomText();
            }
        }

        else
        {
            _questionId++;
            _questionId %= 164;


            //debug
            _screenText1 = "THE ANSWER ";
            _screenText1 += _firstPhrase[15] + "\n\n";
            _screenText1 += _ordinals[7] + "\n\n";
            _screenText1 += "\n\n";
            _screenText2 = _secondPhrase[_questionId];
            //_screenText2 += "§";
            //_screenText2 += temp;

            //render the text
            RenderText();
        }
    }

    /// <summary>
    /// Renders the Screen using Screen.text
    /// </summary>
    void RenderText()
    {
        Screen.text = "";

        byte searchRange;

        //proper formatting
        switch (_questionId)
        {
            //error messages should display one line
            case 68:
            case 69:
            case 148:
                formatText = false;
                searchRange = 18;
                Screen.fontSize = 100;
                break;

            //ultra large messages display smaller font size
            case 66:
            case 67:
            case 162:
            case 163:
                formatText = true;
                searchRange = 34;
                Screen.fontSize = 70;
                break;

            //normal display
            default:
                formatText = true;
                searchRange = 18;
                Screen.fontSize = 110;
                break;
        }

        //render first phrase
        char[] renderedText = new char[_screenText1.Length];
        renderedText = _screenText1.ToCharArray();

        for (int i = 0; i < renderedText.Length; i++)
        {
            //render the character as normal
            Screen.text += renderedText[i];
        }

        //format it into screen.Text
        ushort startPos = searchRange;

        //render second phrase
        renderedText = new char[_screenText2.Length];
        renderedText = _screenText2.ToCharArray();

        //while it isn't outside of the array
        while (startPos < renderedText.Length && formatText)
        {
            if (startPos == 0)
                break;

            //change it to placeholder line break
            if (renderedText[startPos] == ' ')
            {
                renderedText[startPos] = '§';
                startPos += searchRange;
            }

            else
                startPos--;
        }

        for (int i = 0; i < renderedText.Length; i++)
        {
            //converting placeholder line breaks to actual line breaks
            if (renderedText[i] == '§' && formatText)
                Screen.text += "\n\n";

            //render the character as normal
            else
                Screen.text += renderedText[i];
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void RandomText()
    {
        Screen.text = "";

        char[] renderedText = new char[13];

        Screen.fontSize = 170;

        for (int i = 0; i < renderedText.Length; i++)
        {
            if (Random.Range(0, 2) == 0)
                renderedText[i] = _generation1[i];

            else
                renderedText[i] = _generation2[i];

            Screen.text += renderedText[i];
        }
    }

    /// <summary>
    /// Handle button presses and determine whether the answer is correct or not.
    /// </summary>
    /// <param name="num">The button that has been pushed, with the index being used as a comparsion against the answer of the module.</param>
    void HandlePress(int num)
    {
        //play sound
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn[num].transform);

        //if the lights are off or it's solved or it's randomising, do nothing
        if (!_lightsOn || _isSolved || _isRandomising)
            return;

        //include the amount of times solved if you got the special phrases
        _answerOffsetId += (short)(_solvedTimes * _previousModuleCarry);
        _answerOffsetId %= 4;
        _answerOffsetId += 4;
        _answerOffsetId %= 4;

        //if the button pushed is correct, initiate solved module status
        if (num == _answerOffsetId)
        {
            //increment the amount of times the module has been solved with one
            _solvedTimes++;

            Debug.LogFormat("[Placeholder Talk #{0}] Module Passed! The amount of times you solved is now {1}.", _moduleId, _solvedTimes);

            //1 in 100 chance of getting a funny message
            if (Random.Range(0, 100) == 0)
                Screen.text = "talk time :)";

            else
                Screen.text = "";

            Module.HandlePass();
            Audio.PlaySoundAtTransform("disarm", Module.transform);
            _isSolved = true;
        }

        //strike condition
        else
        {
            Debug.LogFormat("[Placeholder Talk #{0}] Answer incorrect! Strike and reset! Your answer: {1}, The correct answer: {2}", _moduleId, num + 1, _answerOffsetId + 1);
            Audio.PlaySoundAtTransform("strike", Module.transform);
            Audio.PlaySoundAtTransform("shuffle", Module.transform);
            Module.HandleStrike();

            //generate new phrases & answers
            Init();
        }
    }

    /// <summary>
    /// Calculates the answer of the module and stores it in AnswerOffsetId.
    /// </summary>
    private short GetAnswer()
    {
        //step 1 for calculating the first variable is starting with 1
        _answerOffsetId = 1;
        Debug.LogFormat("[Placeholder Talk #{0}] First Phrase: Start with N = {1}.", _moduleId, _answerOffsetId);

        //step 2 for calculating the first variable is adding 1 for every strike
        _answerOffsetId += (short)(Info.GetStrikes());
        Debug.LogFormat("[Placeholder Talk #{0}] First Phrase: Previous Answer + {1} Strikes = {2}.", _moduleId, Info.GetStrikes(), _answerOffsetId);

        //step 3 for calculating the first variable is multiplying by battery count
        _answerOffsetId *= (short)(Info.GetBatteryCount());
        Debug.LogFormat("[Placeholder Talk #{0}] First Phrase: Previous Answer * {1} Batteries = {2}.", _moduleId, Info.GetBatteryCount(), _answerOffsetId);

        //step 4 for calculating the first variable is adding or subtracting based on the first phrase given
        switch (_questionOffsetId)
        {
            case 0:
                _answerOffsetId++;
                Debug.LogFormat("[Placeholder Talk #{0}] First Phrase: Subtract N by -1.", _moduleId);
                break;

            case 1:
            case 2:
            case 3:
            case 16:
                Debug.LogFormat("[Placeholder Talk #{0}] First Phrase: Subtract N by 0.", _moduleId);
                break;

            case 4:
            case 5:
            case 6:
            case 17:
                _answerOffsetId--;
                Debug.LogFormat("[Placeholder Talk #{0}] First Phrase: Subtract N by 1.", _moduleId);
                break;

            case 7:
            case 8:
            case 9:
            case 18:
                _answerOffsetId -= 2;
                Debug.LogFormat("[Placeholder Talk #{0}] First Phrase: Subtract N by 2.", _moduleId);
                break;

            case 10:
            case 11:
            case 12:
            case 19:
                _answerOffsetId -= 3;
                Debug.LogFormat("[Placeholder Talk #{0}] First Phrase: Subtract N by 3.", _moduleId);
                break;

            case 13:
                _answerOffsetId -= 27;
                Debug.LogFormat("[Placeholder Talk #{0}] First Phrase: Subtract N by 27.", _moduleId);
                break;

            case 14:
                _answerOffsetId -= 30;
                Debug.LogFormat("[Placeholder Talk #{0}] First Phrase: Subtract N by 30.", _moduleId);
                break;

            case 15:
                _answerOffsetId += 2;
                Debug.LogFormat("[Placeholder Talk #{0}] First Phrase: Subtract N by -2.", _moduleId);
                break;
        }

        Debug.LogFormat("");

        //calculate answerId (second section of manual, second variable)
        _answerId = (byte)(_questionId % 4);
        Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Within the paragraph it's line number {1}, therefore the second phrase equals {1}.", _moduleId, _answerId + 1);

        //there's an exception where you add n for every n backslashes with phrases containing odd slashes
        //this also includes whether or not previous placeholder talks should be counted
        switch (_questionId)
        {
            //one backslash
            case 6:
            case 10:
            case 13:
            case 15:
            case 18:
            case 68:
            case 69:
            case 98:
            case 99:
            case 110:
            case 133:
            case 134:
                _answerId++;
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Odd number of slashes on second phrase, message contains 1 backslash. Add 1.", _moduleId);
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Does not contain the variable N, continue without changes.", _moduleId);
                break;

            //two backslashes
            case 0:
            case 4:
            case 11:
            case 20:
            case 23:
            case 28:
            case 33:
            case 35:
                _answerId += 2;
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Odd number of slashes on second phrase, message contains 2 backslashes. Add 2.", _moduleId);
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Does not contain the variable N, continue without changes.", _moduleId);
                break;

            //three backslashes
            case 148:
                _answerId += 3;
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Odd number of slashes on second phrase, message contains 3 backslashes. Add 3.", _moduleId);
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Does not contain the variable N, continue without changes.", _moduleId);
                break;

            //four backslashes
            case 71:
                _answerId += 4;
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Odd number of slashes on second phrase, message contains 4 backslashes. Add 4.", _moduleId);
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Does not contain the variable N, continue without changes.", _moduleId);
                break;

            //thirteen backslashes
            case 70:
                _answerId += 13;
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Odd number of slashes on second phrase, message contains 13 backslashes. Add 13.", _moduleId);
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Does not contain the variable N, continue without changes.", _moduleId);
                break;

            //n statements (negative placeholder)
            case 66:
                _previousModuleCarry = -1;
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Even number of slashes on second phrase, continue without changes.", _moduleId);
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Does contain the variable N, add -1 for every solved Placeholder Talk to second phrase.", _moduleId);
                break;

            //n statements (positive placeholder)
            case 67:
                _previousModuleCarry = 1;
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Even number of slashes on second phrase, continue without changes.", _moduleId);
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Does contain the variable N, add 1 for every solved Placeholder Talk to second phrase.", _moduleId);
                break;

            //n statements (n + 0)
            case 122:
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Even number of slashes on second phrase, continue without changes.", _moduleId);
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Does contain the variable N, add 0 to second phrase.", _moduleId);
                break;

            //n statements (n + 2)
            case 156:
                _answerId += 2;
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Even number of slashes on second phrase, continue without changes.", _moduleId);
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Does contain the variable N, add 2 to second phrase.", _moduleId);
                break;

            //everything else
            default:
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Even number of slashes on second phrase, continue without changes.", _moduleId);
                Debug.LogFormat("[Placeholder Talk #{0}] Second Phrase: Does not contain the variable N, continue without changes.", _moduleId);
                break;
        }

        Debug.LogFormat("");

        //combine answers, remodulate them twice since it can give negatives for some reason
        _answerOffsetId += _answerId;
        _answerOffsetId %= 4;
        _answerOffsetId += 4;
        _answerOffsetId %= 4;

        return _answerOffsetId;
    }

    //first phrase
    private readonly string[] _firstPhrase = new string[20]
    {
            "", "IS IN THE", "IS THE", "IS IN UH", "IS", "IS AT", "IS INN", "IS THE IN", "IN IS", "IS IN.", "IS IN", "THE", "FIRST-", "IN", "UH IS IN", "AT", "LAST-", "UH", "KEYBORD", "A"
    };

    //random ordinals
    private readonly string[] _ordinals = new string[10]
    {
            "", "FIRST POSITION", "SECOND POSITION", "THIRD POSITION", "FOURTH POSITION", "FIFTH POSITION", "MILLIONTH POSITION", "BILLIONTH POSITION", "LAST POSITION", "AN ANSWER"
    };

    //second phrase generation
    private readonly char[] _generation1 = new char[13]
    {
            'G', 'E', 'N', 'E', 'R', 'A', 'T', 'I', 'N', 'G', '.', '.', '.'
    };

    private readonly char[] _generation2 = new char[13]
    {
            'g', 'e', 'n', 'e', 'r', 'a', 't', 'i', 'n', 'g', '.', '.', '.'
    };

    //second phrase
    private readonly string[] _secondPhrase = new string[164]
    {
            //0
            "\\ / \\",
            "BACKSLASH\n\nSLASH BACKSLASH",
            "\\ SLASH \\",
            "BACKSLASH / BACKSLASH",

            //4
            "BACKSLASH BACK / \\ \\",
            "\\ \\ \\ \\",
            "BACKSLASH BACKSLASH BACK / \\",
            "\\ \\ \\ BACKSLASH",

            //8
            "BACK \\ SLASH \\",
            "BACK / \\ BACK /",
            "BACK BACKSLASH / \\",
            "BACK \\ / \\",

            //12
            "BACK SLASH / BACK SLASH",
            "BACK SLASH BACK SLASH BACK / \\",
            "BACK SLASH SLASH BACK SLASH",
            "BACK BACK SLASH / \\",

            //16
            "BLACKSASH",
            "\\ \\ \\ BACK SLASH",
            "LITERALLY JUST A / AND THEN A \\",
            "LITERALLY JUST A SLASH AND THEN A \\",

            //20
            "ALL OF THESE ARE WORDS: \\ / \\",
            "ALL OF THESE ARE SYMBOLS: SLASH SLASH BACKSLASH",
            "BACKSLASH SLASH SLASH, THE FIRST AND THIRD ARE SYMBOLS",
            "FIRST, SECOND AND THIRD ARE SYMBOLS, READY? \\ \\ / BACKSLASH",

            //24
            "WAIT, IS THIS A BACKSLASH?",
            "WAIT IS THIS A BACKSLASH?",
            "BACKSLASH BACK AND SLASH",
            "BACK SLASH BACK AND SLASH",

            //28
            "\\ SLASH / SLASH / SLASH / SLASH \\",
            "WAIT HOW MANY BATTERIES DO WE HAVE",
            "QUOTE BACKSLASH SLASH BACKSLASH END QUOTE SYMBOLS",
            "BACKSASH",

            //32
            "/ * / = /",
            "/ * \\ = \\",
            "\\ * \\ = \\",
            "\\ * / = \\",

            //36
            "NOTHING",
            "",
            "LITERALLY NOTHING",
            "NULL",

            //40
            "EMPTY",
            "IT'S EMPTY",
            "I CAN'T SEE ANYTHING",
            "THE LIGHTS WENT OUT, HOLD ON",

            //44
            "READY?",
            "THE LIGHTS",
            "A VERY LONG LIST OF SLASHES",
            "A VERY LONG LIST OF SLASH",

            //48
            "/ / / / / / / / / / / / / / / / / / / / / / / /",
            "LAST DIGIT OF THE SERIAL NUMBER",
            "QUOTE SLASH END QUOTE",
            "BACK- I MEAN SLASH NOT BACKSLASH THEN A BACKSLASH",

            //52
            "BLACKHASH",
            "BACKHASH",
            "THERE ARE TWENTY OR SOMETHING SLASHES",
            "THERE ARE 20 OR SOMETHING SLASHES",

            //56
            "TWO BACKSLASHES",
            "2 BACKSLASHES",
            "TO BACKSLASHES",
            "TOO BACKSLASHES",

            //60
            "TWO \\",
            "TWO \\ES",
            "THERE ARE TWO BACKSLASHES",
            "THERE'RE TWO BACKSLASHES",

            //64
            "THEIR ARE TWO BACKSLASHES",
            "THEY'RE ARE TWO BACKSLASHES",
            "ADD -N IN SECOND PHRASE WHERE N = AMOUNT OF TIMES THIS MODULE HAS BEEN SOLVED IN YOUR CURRENT BOMB",
            "ADD N IN SECOND PHRASE WHERE N = AMOUNT OF TIMES THIS MODULE HAS BEEN SOLVED IN YOUR CURRENT BOMB",

            //68
            "Parse error: syntax error, unexpected ''\\'' in /placeholderTalk/Assets/placeholderTalk.cs on line 684",
            "Parse error: syntax error, unexpected ''\\'' in /placeholderTalk/Manual/placeholderTalk.html on line 373",
            "/give @a command_block {Name:\"\\ \\ \\ \\ \\ \\ \\ \\ \\ \\ \\ \\ \\\"} 1",
            "/ u r a u r a \" \\ Parse Error \" u r a \" \\ Parse u r a / \" \\ Parse Error \" Error \" \\ Parse Error / \"",

            //72
            "WAIT THE ALARM WENT OFF",
            "MY GAME CRASHED",
            "BE RIGHT BACK",
            "I THOUGHT I DISABLED VANILLA MODULES",

            //76
            "WE HAVE WIRE SEQUENCES BLACK TO CHARLIE",
            "WE HAVE WIRE SEQUENCES BLACK TO C",
            "WE HAVE WIRE SEQUENCES BLACK TO SEA",
            "WE HAVE WIRE SEQUENCES BLACK TO SEE",

            //80
            "SINCE WHEN DID WE HAVE A NEEDY?",
            "ZEE ROW",
            "Z ROW",
            "THE ENTIRE ALPHABET",

            //84
            "WE HAVE TEN SECONDS",
            "ALPHA BRAVO CHARLIE AND SO ON",
            "ABCDEFGHIJKLL NOPQRSTUVWXYZ",
            "ABCDEFGHIJKLM NOPQRSTUVWXYZ",

            //88
            "THE ENTIRE ALPHABET BUT LETTERS",
            "LITERALLY THE ENTIRE ALPHABET",
            "ARE BEADY CUE DJANGO EYE FIJI",
            "THE ENTIRE ALFABET BUT LETTERS",

            //92
            "ALFA BRAVO CHARLIE DELTA ECHO FOXTROT",
            "AISLE BDELLIUM CUE DJEMBE EYE PHONEIC",
            "A B C D E F",
            "AYY BEE CEE DEE EEE EFF",

            //96
            "ABORT, WE'RE STARTING OVER",
            "I AM GONNA RESTART",
            "\\ / \\ WE HAVE ONE STRIKE",
            "BACKSLASH BACKSLASH BACK / \\ WITH ONE STRIKE",

            //100
            "YOU ARE CUTTING OUT",
            "I CANNOT HEAR YOU",
            "WAIT IT CHANGED",
            "NEVERMIND ANOTHER MODULE",

            //104
            "SLAAAAAASH",
            "SLAAAAAAASH",
            "SLAAAAAAAASH",
            "SLAAAAAAAAASH",

            //108
            "OKAY I GUESSED AND IT WAS CORRECT",
            "I THINK THE MOD IS BROKEN",
            "THERE ARE 3 BATTERIES. LITERALLY JUST A / AND THEN A \\",
            "DOES THE MANUAL SAY ANYTHING ABOUT A SECOND STAGE ?",
            
            //112
            "WHAT IS YOUR LEAST FAVORITE MODULE?",
            "THIS MASSAGE IS REALLY HARD TO COMMUNICATE",
            "THIS MESSAGE IS REALLY HARD TO COMMUNICATE",
            "THE ANSWER IS IN UHHH SECOND POSITION",

            //116
            "ALL WORDS THE NUMBER ZERO",
            "THE NUMBER ZERO",
            "THE NUMBER 0",
            "THE NUMBER 0 AS IN DIGIT",

            //120
            "0",
            "ZERO",
            "N + 0",
            "ZEEROW",

            //124
            "0 BATTERIES",
            "TIME RAN OUT",
            "AND KABOOM",
            "HUH?",

            //128
            "SOME CHINESE CHARACTERS",
            "頁 - 設 - 是 - 煵",
            "THE TEXT DOESN'T FIT",
            "AAAAAAAAAAAAAAAAAAAAAAAAAA",

            //132
            "FORWARD SLASH",
            "/(o w o)\\",
            "/(u w u)\\",
            "BACKWARD SLASH",

            //136
            "I HAVE TEN SECONDS",
            "THE SECOND PHRASE IS QUOTE BACKSLASH SLASH BACKSLASH END QUOTE",
            "IT SAYS ALL SYMBOLS BACK / \\ \\ BACKSLASH",
            "THE SECOND PHRASE IS QUOTE BACKSLASH SLASH BACKSLASH UNQUOTE",

            //140
            "WAIT COMMA IS THIS A BACKSLASH?",
            "ALPHA BRAVO CHARLIE DELTA ECHO FOXTROT",
            "WAIT COMMA IS THIS A BACKSLASH",
            "YOU JUST LOST THE GAME",

            //144
            "WAIT COMMA IS THIS A BACKSLASH QUESTION MARK",
            "ALL WORDS WAIT COMMA IS THIS A BACKSLASH QUESTION MARK",
            "배 - 탓 - 배 - 몸",
            "え - み - さ - ん",

            //148
            "Error: MissingComponentException (Could not find \"/screenFont\" in F:\\placeholderTalk\\Assets\\Materials)",
            "IT'S THE SAME AS BEFORE",
            "THIS MODULE HAS BEEN SPONSORED BY RAID SHADOW LEGENDS",
            "OH WE BLUE UP AS IN THE COLOR",

            //152
            "OH WE BLUE UP",
            "THIS MODULE HAS BEEN SPONSORED",
            "IT'S THE SAME ONE",
            "OH WE BLEW UP",

            //156
            "N + 2",
            "OH WE BLEW UP AS IN THE COLOR",
            "PRESS 1 IF >2 BATTERIES",
            "PARSE ERROR",

            //160
            "WAIT, IS THIS A BACKSLASH",
            "WAIT COMMA IS THIS A BACK SLASH",
            "o m g guys we found a creeper in the downstairs bathroom lemme get my diamond hoe from the inventory and shit i just died. thank you so much for watching and have a great rest of your day, make sure to like, comment and subscribe and eat that bell icon like its enderman bacon\n\nFOOL",
            "hello guys welcome back to another minecraft video and in todays video we will be talking about my brand new enderman holding a bacon statue, its furious, its hot and its powerful guys. its the definition of engineering at its finest, now lets enter from the rear of the building."
    };

    /// <summary>
    /// Determines whether the input from the TwitchPlays chat command is valid or not.
    /// </summary>
    /// <param name="par">The string from the user.</param>
    private bool IsValid(string par)
    {
        string[] validNumbers = { "1", "2", "3", "4" };

        if (validNumbers.Contains(par))
            return true;

        return false;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <#> (Presses the button labeled '#' | valid numbers are from 1-4)";
#pragma warning restore 414

    /// <summary>
    /// TwitchPlays Compatibility, detects every chat message and clicks buttons accordingly.
    /// </summary>
    /// <param name="command">The twitch command made by the user.</param>
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] buttonPressed = command.Split(' ');

        //if command is formatted correctly
        if (Regex.IsMatch(buttonPressed[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;

            //if command has no parameters
            if (buttonPressed.Length < 2)
                yield return "sendtochaterror Please specify the button you want to press! (Valid: 1-4)";

            //if command has too many parameters
            else if (buttonPressed.Length > 2)
                yield return "sendtochaterror Too many buttons pressed! Only one can be pressed at any time.";

            //if command has an invalid parameter
            else if(!IsValid(buttonPressed.ElementAt(1)))
                yield return "sendtochaterror Invalid number! Only buttons 1-4 can be pushed.";

            //if command is valid, push button accordingly
            else
            {
                int s = 0;
                int.TryParse(buttonPressed[1], out s);
                btn[s - 1].OnInteract();
            }
        }
    }

    /// <summary>
    /// Force the module to be solved in TwitchPlays
    /// </summary>
    IEnumerator TwitchHandleForcedSolve()
    {
        btn[_answerOffsetId].OnInteract();
        yield return null;
    }
}