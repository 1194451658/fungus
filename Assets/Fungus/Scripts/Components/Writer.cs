// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Text;

namespace Fungus
{
    /// <summary>
    /// Current state of the writing process.
    /// </summary>
    public enum WriterState
    {
        /// <summary> Invalid state. </summary>
        Invalid,

        /// <summary> Writer has started writing. </summary>
        Start,

        /// <summary> Writing has been paused. </summary>
        Pause,

        /// <summary> Writing has resumed after a pause. </summary>
        Resume,

        /// <summary> Writing has ended. </summary>
        End
    }

    /// <summary>
    /// Writes text using a typewriter effect to a UI text object.
    /// </summary>
    public class Writer : MonoBehaviour, IDialogInputListener
    {
        [Tooltip("Gameobject containing a Text, Inout Field or Text Mesh object to write to")]
        [SerializeField] protected GameObject targetTextObject;

        [Tooltip("Gameobject to punch when the punch tags are displayed. If none is set, the main camera will shake instead.")]
        [SerializeField] protected GameObject punchObject;

        [Tooltip("Writing characters per second")]
        [SerializeField] protected float writingSpeed = 60;

        [Tooltip("Pause duration for punctuation characters")]
        [SerializeField] protected float punctuationPause = 0.25f;

        [Tooltip("Color of text that has not been revealed yet")]
        [SerializeField] protected Color hiddenTextColor = new Color(1,1,1,0);

        [Tooltip("Write one word at a time rather one character at a time")]
        [SerializeField] protected bool writeWholeWords = false;

        [Tooltip("Force the target text object to use Rich Text mode so text color and alpha appears correctly")]
        [SerializeField] protected bool forceRichText = true;

        [Tooltip("Click while text is writing to finish writing immediately")]
        [SerializeField] protected bool instantComplete = true;

        // This property is true when the writer is waiting for user input to continue
        protected bool isWaitingForInput;

        // This property is true when the writer is writing text or waiting (i.e. still processing tokens)
        protected bool isWriting;

        protected float currentWritingSpeed;
        protected float currentPunctuationPause;

        // 封装文本控件
        // Text, InputField, TextMesh, TMP_Text, IWriterTextDestination
        protected TextAdapter textAdapter = new TextAdapter();

        protected bool boldActive = false;
        protected bool italicActive = false;
        protected bool colorActive = false;
        protected string colorText = "";
        protected bool sizeActive = false;
        protected float sizeValue = 16f;
        protected bool inputFlag;
        protected bool exitFlag;

        protected List<IWriterListener> writerListeners = new List<IWriterListener>();

        protected StringBuilder openString = new StringBuilder(256);
        protected StringBuilder closeString = new StringBuilder(256);
        protected StringBuilder leftString = new StringBuilder(1024);
        protected StringBuilder rightString = new StringBuilder(1024);
        protected StringBuilder outputString = new StringBuilder(1024);
        protected StringBuilder readAheadString = new StringBuilder(1024);

        protected string hiddenColorOpen = "";
        protected string hiddenColorClose = "";

        protected int visibleCharacterCount = 0;

        // WriterAudio
        // 和SayDialog, Writer，挂载在一起
        public WriterAudio AttachedWriterAudio { get; set; }

        protected virtual void Awake()
        {
            GameObject go = targetTextObject;
            if (go == null)
            {
                go = gameObject;
            }

            // 初始化
            // TextAdapter
            textAdapter.InitFromGameObject(go);

            // 
            // 获取所有的
            // WriterListener
            // 

            // Cache the list of child writer listeners
            var allComponents = GetComponentsInChildren<Component>();
            for (int i = 0; i < allComponents.Length; i++)
            {
                var component = allComponents[i];
                IWriterListener writerListener = component as IWriterListener;
                if (writerListener != null)
                {
                    writerListeners.Add(writerListener);
                }
            }

            // 根据hiddenTextColor
            // 初始化hiddenColorOpen, hiddenColorClose
            CacheHiddenColorStrings();
        }

        //
        // 根据hiddenTextColor
        // 初始化hiddenColorOpen, hiddenColorClose
        //
        protected virtual void CacheHiddenColorStrings()
        {
            // Cache the hidden color string
            Color32 c = hiddenTextColor;
            hiddenColorOpen = String.Format("<color=#{0:X2}{1:X2}{2:X2}{3:X2}>", c.r, c.g, c.b, c.a);
            hiddenColorClose = "</color>";
        }

        protected virtual void Start()
        {
            // 是否开启
            // 富文本
            if (forceRichText)
            {
                textAdapter.ForceRichText();
            }
        }
        
        // 设置OpenString
        protected virtual void UpdateOpenMarkup()
        {
            openString.Length = 0;
            
            // 检查是否
            // 支持富文本
            if (textAdapter.SupportsRichText())
            {
                if (sizeActive)
                {
                    openString.Append("<size=");
                    openString.Append(sizeValue);
                    openString.Append(">"); 
                }

                if (colorActive)
                {
                    openString.Append("<color=");
                    openString.Append(colorText);
                    openString.Append(">"); 
                }

                if (boldActive)
                {
                    openString.Append("<b>"); 
                }

                if (italicActive)
                {
                    openString.Append("<i>"); 
                }           
            }
        }
        
        // 设置CloseString
        protected virtual void UpdateCloseMarkup()
        {
            closeString.Length = 0;
            
            if (textAdapter.SupportsRichText())
            {
                if (italicActive)
                {
                    closeString.Append("</i>"); 
                }           
                if (boldActive)
                {
                    closeString.Append("</b>"); 
                }
                if (colorActive)
                {
                    closeString.Append("</color>"); 
                }
                if (sizeActive)
                {
                    closeString.Append("</size>"); 
                }
            }
        }

        // 检查paramList的个数是count
        protected virtual bool CheckParamCount(List<string> paramList, int count) 
        {
            if (paramList == null)
            {
                Debug.LogError("paramList is null");
                return false;
            }
            if (paramList.Count != count)
            {
                Debug.LogError("There must be exactly " + paramList.Count + " parameters.");
                return false;
            }
            return true;
        }

        // 将paramList中index处参数
        // 当做single来解析
        protected virtual bool TryGetSingleParam(List<string> paramList, int index, float defaultValue, out float value) 
        {
            value = defaultValue;
            if (paramList.Count > index) 
            {
                Single.TryParse(paramList[index], out value);
                return true;
            }
            return false;
        }

        // 处理Token序列
        protected virtual IEnumerator ProcessTokens(List<TextTagToken> tokens, bool stopAudio, Action onComplete)
        {
            // Reset control members
            boldActive = false;
            italicActive = false;
            colorActive = false;
            sizeActive = false;
            colorText = "";
            sizeValue = 16f;
            currentPunctuationPause = punctuationPause;
            currentWritingSpeed = writingSpeed;

            exitFlag = false;
            isWriting = true;

            TokenType previousTokenType = TokenType.Invalid;

            // 开始，
            // 遍历所有Token
            for (int i = 0; i < tokens.Count; ++i)
            {
                // 持续保持暂停
                // Pause between tokens if Paused is set
                while (Paused)
                {
                    yield return null;
                }

                var token = tokens[i];

                // Notify listeners about new token
                WriterSignals.DoTextTagToken(this, token, i, tokens.Count);
               
                // Update the read ahead string buffer. This contains the text for any 
                // Word tags which are further ahead in the list. 
                readAheadString.Length = 0;

                // 向后查看Token
                // 获取到所有的Words Token
                for (int j = i + 1; j < tokens.Count; ++j)
                {
                    var readAheadToken = tokens[j];

                    if (readAheadToken.type == TokenType.Words &&
                        readAheadToken.paramList.Count == 1)
                    {
                        readAheadString.Append(readAheadToken.paramList[0]);
                    }
                    else if (readAheadToken.type == TokenType.WaitForInputAndClear)
                    {
                        break;
                    }
                }

                switch (token.type)
                {
                // 显示文本
                case TokenType.Words:
                    yield return StartCoroutine(DoWords(token.paramList, previousTokenType));
                    break;
                    
                // 处理<b></b>
                case TokenType.BoldStart:
                    boldActive = true;
                    break;
                case TokenType.BoldEnd:
                    boldActive = false;
                    break;
                    
                // 处理斜体
                case TokenType.ItalicStart:
                    italicActive = true;
                    break;
                case TokenType.ItalicEnd:
                    italicActive = false;
                    break;
                    
                // 处理颜色
                case TokenType.ColorStart:
                    if (CheckParamCount(token.paramList, 1)) 
                    {
                        colorActive = true;
                        // 取出颜色参数
                        colorText = token.paramList[0];
                    }
                    break;
                case TokenType.ColorEnd:
                    colorActive = false;
                    break;

                // 处理Size标签
                case TokenType.SizeStart:
                    if (TryGetSingleParam(token.paramList, 0, 16f, out sizeValue))
                    {
                        sizeActive = true;
                    }
                    break;
                case TokenType.SizeEnd:
                    sizeActive = false;
                    break;

                // 等待
                case TokenType.Wait:
                    yield return StartCoroutine(DoWait(token.paramList));
                    break;
                    
                // 等待输入
                case TokenType.WaitForInputNoClear:
                    yield return StartCoroutine(DoWaitForInput(false));
                    break;
                    
                case TokenType.WaitForInputAndClear:
                    yield return StartCoroutine(DoWaitForInput(true));
                    break;

                // 等待语音结束
                case TokenType.WaitForVoiceOver:
                    yield return StartCoroutine(DoWaitVO());
                    break;

                // 等待标点
                case TokenType.WaitOnPunctuationStart:
                    TryGetSingleParam(token.paramList, 0, punctuationPause, out currentPunctuationPause);
                    break;
                    
                case TokenType.WaitOnPunctuationEnd:
                    currentPunctuationPause = punctuationPause;
                    break;
                    
                // 清屏
                case TokenType.Clear:
                        textAdapter.Text = "";
                    break;
                    
                // 更改
                // 字符出现速度
                case TokenType.SpeedStart:
                    TryGetSingleParam(token.paramList, 0, writingSpeed, out currentWritingSpeed);
                    break;
                    
                case TokenType.SpeedEnd:
                    currentWritingSpeed = writingSpeed;
                    break;
                    
                // 结束
                case TokenType.Exit:
                    exitFlag = true;
                    break;

                // 发送消息
                case TokenType.Message:
                    if (CheckParamCount(token.paramList, 1)) 
                    {
                        Flowchart.BroadcastFungusMessage(token.paramList[0]);
                    }
                    break;
                    
                // 震屏
                case TokenType.VerticalPunch: 
                    {
                        float vintensity;
                        float time;
                        TryGetSingleParam(token.paramList, 0, 10.0f, out vintensity);
                        TryGetSingleParam(token.paramList, 1, 0.5f, out time);
                        Punch(new Vector3(0, vintensity, 0), time);
                    }
                    break;
                    
                case TokenType.HorizontalPunch: 
                    {
                        float hintensity;
                        float time;
                        TryGetSingleParam(token.paramList, 0, 10.0f, out hintensity);
                        TryGetSingleParam(token.paramList, 1, 0.5f, out time);
                        Punch(new Vector3(hintensity, 0, 0), time);
                    }
                    break;
                    
                case TokenType.Punch: 
                    {
                        float intensity;
                        float time;
                        TryGetSingleParam(token.paramList, 0, 10.0f, out intensity);
                        TryGetSingleParam(token.paramList, 1, 0.5f, out time);
                        Punch(new Vector3(intensity, intensity, 0), time);
                    }
                    break;
                    
                // 闪屏
                case TokenType.Flash:
                    float flashDuration;
                    TryGetSingleParam(token.paramList, 0, 0.2f, out flashDuration);
                    Flash(flashDuration);
                    break;

                // 播放音效
                case TokenType.Audio: 
                    {
                        AudioSource audioSource = null;
                        if (CheckParamCount(token.paramList, 1))
                        {
                            audioSource = FindAudio(token.paramList[0]);
                        }
                        if (audioSource != null)
                        {
                            audioSource.PlayOneShot(audioSource.clip);
                        }
                    }
                    break;
                    
                // 循环播放音效
                case TokenType.AudioLoop:
                    {
                        AudioSource audioSource = null;
                        if (CheckParamCount(token.paramList, 1)) 
                        {
                            audioSource = FindAudio(token.paramList[0]);
                        }
                        if (audioSource != null)
                        {
                            audioSource.Play();
                            audioSource.loop = true;
                        }
                    }
                    break;
                    
                // 暂停播放音效
                case TokenType.AudioPause:
                    {
                        AudioSource audioSource = null;
                        if (CheckParamCount(token.paramList, 1)) 
                        {
                            audioSource = FindAudio(token.paramList[0]);
                        }
                        if (audioSource != null)
                        {
                            audioSource.Pause();
                        }
                    }
                    break;
                    
                // 停止播放音效
                case TokenType.AudioStop:
                    {
                        AudioSource audioSource = null;
                        if (CheckParamCount(token.paramList, 1)) 
                        {
                            audioSource = FindAudio(token.paramList[0]);
                        }
                        if (audioSource != null)
                        {
                            audioSource.Stop();
                        }
                    }
                    break;
                }

                // 记录当前的
                // Token类型
                previousTokenType = token.type;

                if (exitFlag)
                {
                    break;
                }
            }

            inputFlag = false;
            exitFlag = false;
            isWaitingForInput = false;
            isWriting = false;

            // 通知结束
            NotifyEnd(stopAudio);

            if (onComplete != null)
            {
                onComplete();
            }
        }

        // 显示文本
        protected virtual IEnumerator DoWords(List<string> paramList, TokenType previousTokenType)
        {
            if (!CheckParamCount(paramList, 1))
            {
                yield break;
            }

            // 替换\n
            string param = paramList[0].Replace("\\n", "\n");

            //  ----------------------
            //  如果是从头显示
            //  清除掉开头的空白字符
            //  ----------------------
            // Trim whitespace after a {wc} or {c} tag
            if (previousTokenType == TokenType.WaitForInputAndClear ||
                previousTokenType == TokenType.Clear)
            {
                param = param.TrimStart(' ', '\t', '\r', '\n');
            }

            // Start with the visible portion of any existing displayed text.
            string startText = "";
            if (visibleCharacterCount > 0 &&
                visibleCharacterCount <= textAdapter.Text.Length)
            {
                startText = textAdapter.Text.Substring(0, visibleCharacterCount);
            }
                
            UpdateOpenMarkup();
            UpdateCloseMarkup();

            float timeAccumulator = Time.deltaTime;

            // 遍历文本的
            // 所有字符
            for (int i = 0; i < param.Length + 1; ++i)
            {
                // Exit immediately if the exit flag has been set
                if (exitFlag)
                {
                    break;
                }

                // Pause mid sentence if Paused is set
                while (Paused)
                {
                    yield return null;
                }

                // 分割文本
                // 一个字符一个字符显示，还是一个单词一个单词显示
                PartitionString(writeWholeWords, param, i);

                // 生成最后要显示的文本
                ConcatenateString(startText);
                textAdapter.Text = outputString.ToString();

                // 通知文本被更新
                NotifyGlyph();

                // No delay if user has clicked and Instant Complete is enabled
                if (instantComplete && inputFlag)
                {
                    continue;
                }

                // 标点符号时候的等待
                // Punctuation pause
                if (leftString.Length > 0 && 
                    rightString.Length > 0 &&
                    IsPunctuation(leftString.ToString(leftString.Length - 1, 1)[0]))
                {
                    yield return StartCoroutine(DoWait(currentPunctuationPause));
                }

                // Delay between characters
                if (currentWritingSpeed > 0f)
                {
                    if (timeAccumulator > 0f)
                    {
                        timeAccumulator -= 1f / currentWritingSpeed;
                    } 
                    else
                    {
                        yield return new WaitForSeconds(1f / currentWritingSpeed);
                    }
                }
            }
        }

        // 分割文本
        // 一个字符一个字符显示，还是一个单词一个单词显示
        // i: 处理inputString中的，第几字符
        protected virtual void PartitionString(bool wholeWords, string inputString, int i)
        {
            // 处理后，左侧的文本
            leftString.Length = 0;
            // 处理后，右侧的文本
            rightString.Length = 0;

            // Reached last character
            leftString.Append(inputString);
            if (i >= inputString.Length)
            {
                return;
            }

            rightString.Append(inputString);

            if (wholeWords)
            {
                // Look ahead to find next whitespace or end of string
                for (int j = i; j < inputString.Length + 1; ++j)
                {
                    if (j == inputString.Length || Char.IsWhiteSpace(inputString[j]))
                    {
                        leftString.Length = j;
                        rightString.Remove(0, j);
                        break;
                    }
                }
            }
            else
            {
                leftString.Remove(i, inputString.Length - i);
                rightString.Remove(0, i);
            }
        }

        // 生成最后要显示的文本
        protected virtual void ConcatenateString(string startText)
        {
            outputString.Length = 0;

            // string tempText = startText + openText + leftText + closeText;

            // 构造
            // 最后要显示的，字符串
            outputString.Append(startText);
            outputString.Append(openString);
            outputString.Append(leftString);
            outputString.Append(closeString);

            // Track how many visible characters are currently displayed so
            // we can easily extract the visible portion again later.
            visibleCharacterCount = outputString.Length;

            // 构造
            // 右侧，被隐藏的文本！
            // 这里隐藏的文本也一起显示
            // 应该是在文本逐次出现的时候，排版不会发生变化

            // Make right hand side text hidden
            if (textAdapter.SupportsRichText() &&
                rightString.Length + readAheadString.Length > 0)
            {
                // Ensure the hidden color strings are populated
                if (hiddenColorOpen.Length == 0)
                {
                    CacheHiddenColorStrings();
                }

                outputString.Append(hiddenColorOpen);
                outputString.Append(rightString);
                outputString.Append(readAheadString);
                outputString.Append(hiddenColorClose);
            }
        }

        // 等待时间
        // 只接受一个参数
        protected virtual IEnumerator DoWait(List<string> paramList)
        {
            var param = "";
            if (paramList.Count == 1)
            {
                param = paramList[0];
            }

            // 解析参数
            float duration = 1f;
            if (!Single.TryParse(param, out duration))
            {
                duration = 1f;
            }

            // 等待时间
            yield return StartCoroutine( DoWait(duration) );
        }

        // 等待
        // 说话语音的结束
        protected virtual IEnumerator DoWaitVO()
        {
            float duration = 0f;

            if (AttachedWriterAudio != null)
            {
                duration = AttachedWriterAudio.GetSecondsRemaining();
            }

            yield return StartCoroutine(DoWait(duration));
        }

        // 等待多长时间
        protected virtual IEnumerator DoWait(float duration)
        {
            // 通知暂停
            NotifyPause();

            float timeRemaining = duration;
            while (timeRemaining > 0f && !exitFlag)
            {
                // Q: 立刻显示出所有文本？
                if (instantComplete && inputFlag)
                {
                    break;
                }

                timeRemaining -= Time.deltaTime;
                yield return null;
            }

            // 通知重新开始
            NotifyResume();
        }

        // 等待输入
        protected virtual IEnumerator DoWaitForInput(bool clear)
        {
            // 通知暂停
            NotifyPause();

            inputFlag = false;
            isWaitingForInput = true;

            // 等待输入
            while (!inputFlag && !exitFlag)
            {
                yield return null;
            }
        
            isWaitingForInput = false;          
            inputFlag = false;

            if (clear)
            {
                textAdapter.Text = "";
            }

            // 通知
            // 重新运行
            NotifyResume();
        }
        
        // 是否是标点符号
        protected virtual bool IsPunctuation(char character)
        {
            return character == '.' || 
                character == '?' ||  
                    character == '!' || 
                    character == ',' ||
                    character == ':' ||
                    character == ';' ||
                    character == ')';
        }
        
        // 震屏
        protected virtual void Punch(Vector3 axis, float time)
        {
            GameObject go = punchObject;
            if (go == null)
            {
                go = Camera.main.gameObject;
            }

            if (go != null)
            {
                iTween.ShakePosition(go, axis, time);
            }
        }
        
        // 闪屏实现
        protected virtual void Flash(float duration)
        {
            var cameraManager = FungusManager.Instance.CameraManager;

            cameraManager.ScreenFadeTexture = CameraManager.CreateColorTexture(new Color(1f,1f,1f,1f), 32, 32);
            cameraManager.Fade(1f, duration, delegate {
                cameraManager.ScreenFadeTexture = CameraManager.CreateColorTexture(new Color(1f,1f,1f,1f), 32, 32);
                cameraManager.Fade(0f, duration, null);
            });
        }
        
        // 根据名称
        // 查找有AudioSource的GameObject
        protected virtual AudioSource FindAudio(string audioObjectName)
        {
            GameObject go = GameObject.Find(audioObjectName);
            if (go == null)
            {
                return null;
            }
            
            return go.GetComponent<AudioSource>();
        }

        // 通知输入
        protected virtual void NotifyInput()
        {
            WriterSignals.DoWriterInput(this);

            for (int i = 0; i < writerListeners.Count; i++)
            {
                var writerListener = writerListeners[i];
                writerListener.OnInput();
            }
        }

        // 通知Start
        protected virtual void NotifyStart(AudioClip audioClip)
        {
            WriterSignals.DoWriterState(this, WriterState.Start);

            for (int i = 0; i < writerListeners.Count; i++)
            {
                var writerListener = writerListeners[i];
                writerListener.OnStart(audioClip);
            }
        }

        // 通知暂停
        protected virtual void NotifyPause()
        {
            // 通知
            // 状态变化
            WriterSignals.DoWriterState(this, WriterState.Pause);

            // 通知
            // 所有的WriterListener
            for (int i = 0; i < writerListeners.Count; i++)
            {
                var writerListener = writerListeners[i];
                writerListener.OnPause();
            }
        }

        // 通知Resume
        protected virtual void NotifyResume()
        {
            // 通知
            // 状态变化
            WriterSignals.DoWriterState(this, WriterState.Resume);

            // 通知
            // 所有的WriterListener
            for (int i = 0; i < writerListeners.Count; i++)
            {
                var writerListener = writerListeners[i];
                writerListener.OnResume();
            }
        }

        // 通知结束
        protected virtual void NotifyEnd(bool stopAudio)
        {
            // 通知
            // 状态变化
            WriterSignals.DoWriterState(this, WriterState.End);

            // 通知
            // 所有的WriterListener
            for (int i = 0; i < writerListeners.Count; i++)
            {
                var writerListener = writerListeners[i];
                writerListener.OnEnd(stopAudio);
            }
        }

        protected virtual void NotifyGlyph()
        {
            WriterSignals.DoWriterGlyph(this); 

            for (int i = 0; i < writerListeners.Count; i++)
            {
                var writerListener = writerListeners[i];
                writerListener.OnGlyph();
            }
        }

        #region Public members

        /// <summary>
        /// This property is true when the writer is writing text or waiting (i.e. still processing tokens).
        /// </summary>
        public virtual bool IsWriting { get { return isWriting; } }

        /// <summary>
        /// This property is true when the writer is waiting for user input to continue.
        /// </summary>
        public virtual bool IsWaitingForInput { get { return isWaitingForInput; } }

        /// <summary>
        /// Pauses the writer.
        /// </summary>
        public virtual bool Paused { set; get; }

        /// <summary>
        /// Stop writing text.
        /// </summary>
        public virtual void Stop()
        {
            if (isWriting || isWaitingForInput)
            {
                exitFlag = true;
            }
        }

        /// <summary>
        /// Writes text using a typewriter effect to a UI text object.
        /// </summary>
        /// <param name="content">Text to be written</param>
        /// <param name="clear">If true clears the previous text.</param>
        /// <param name="waitForInput">Writes the text and then waits for player input before calling onComplete.</param>
        /// <param name="stopAudio">Stops any currently playing audioclip.</param>
        /// <param name="waitForVO">Wait for the Voice over to complete before proceeding</param>
        /// <param name="audioClip">Audio clip to play when text starts writing.</param>
        /// <param name="onComplete">Callback to call when writing is finished.</param>
        public virtual IEnumerator Write(string content, bool clear, bool waitForInput, bool stopAudio, bool waitForVO, AudioClip audioClip, Action onComplete)
        {
            if (clear)
            {
                textAdapter.Text = "";
                visibleCharacterCount = 0;
            }

            if (!textAdapter.HasTextObject())
            {
                yield break;
            }

            // If this clip is null then WriterAudio will play the default sound effect (if any)
            NotifyStart(audioClip);

            string tokenText = TextVariationHandler.SelectVariations(content);
            
            if (waitForInput)
            {
                tokenText += "{wi}";
            }

            if(waitForVO)
            {
                tokenText += "{wvo}";
            }

            // 解析文本
            // 成Token
            List<TextTagToken> tokens = TextTagParser.Tokenize(tokenText);

            gameObject.SetActive(true);

            // 开始依次执行Token
            yield return StartCoroutine(ProcessTokens(tokens, stopAudio, onComplete));
        }

        public void SetTextColor(Color textColor)
        {
            textAdapter.SetTextColor(textColor);
        }

        public void SetTextAlpha(float textAlpha)
        {
            textAdapter.SetTextAlpha(textAlpha);
        }

        #endregion

        #region IDialogInputListener implementation

        public virtual void OnNextLineEvent()
        {
            inputFlag = true;

            if (isWriting)
            {
                NotifyInput();
            }
        }

        #endregion
    }
}
