// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

﻿using UnityEngine;
using System.Collections.Generic;

namespace Fungus
{
    /// <summary>
    /// Type of audio effect to play.
    /// </summary>

    // 播放的语音的模式
    public enum AudioMode
    {
        /// <summary> Use short beep sound effects. </summary>
        Beeps,
        /// <summary> Use long looping sound effect. </summary>
        SoundEffect,
    }

    /// <summary>
    /// Manages audio effects for Dialogs.
    /// </summary>

    // WriterAudio
    // 和SayDialog, Writer，挂载在一起
    public class WriterAudio : MonoBehaviour, IWriterListener
    {
        // 音量
        [Tooltip("Volume level of writing sound effects")]
        [Range(0,1)]
        [SerializeField] protected float volume = 1f;

        // 循环
        [Tooltip("Loop the audio when in Sound Effect mode. Has no effect in Beeps mode.")]
        [SerializeField] protected bool loop = true;

        // 音源
        // If none is specifed then we use any AudioSource on the gameobject, and if that doesn't exist we create one.
        [Tooltip("AudioSource to use for playing sound effects. If none is selected then one will be created.")]
        [SerializeField] protected AudioSource targetAudioSource;

        [Tooltip("Type of sound effect to play when writing text")]
        [SerializeField] protected AudioMode audioMode = AudioMode.Beeps;

        // 3类声音

        // 文本一个字符一个字符出现
        // 的beep声音
        [Tooltip("List of beeps to randomly select when playing beep sound effects. Will play maximum of one beep per character, with only one beep playing at a time.")]
        [SerializeField] protected List<AudioClip> beepSounds = new List<AudioClip>();

        // 哪个声音？
        // AudioMode是SoundEffect时候的声音
        [Tooltip("Long playing sound effect to play when writing text")]
        [SerializeField] protected AudioClip soundEffect;

        // 输入声音
        // 点击继续的声音？
        [Tooltip("Sound effect to play on user input (e.g. a click)")]
        [SerializeField] protected AudioClip inputSound;

        protected float targetVolume = 0f;

        // 单个文字出现的时候
        // 是否播放Beep
        // When true, a beep will be played on every written character glyph
        protected bool playBeeps;

        // 标记，
        // 是否在播放语音
        // True when a voiceover clip is playing
        protected bool playingVoiceover = false;

        public bool IsPlayingVoiceOver { get { return playingVoiceover; } }

        // Q: ???
        // Time when current beep will have finished playing
        protected float nextBeepTime;


        // 音源还剩多长时间
        public float GetSecondsRemaining()
        {
            if (IsPlayingVoiceOver)
            {
                return targetAudioSource.clip.length - targetAudioSource.time;
            }
            else
            {
                return 0F;
            }
        }

        // 设置声音模式
        protected virtual void SetAudioMode(AudioMode mode)
        {
            audioMode = mode;
        }

        protected virtual void Awake()
        {
            // 获取/添加AudioSource控件
            // Need to do this in Awake rather than Start due to init order issues
            if (targetAudioSource == null)
            {
                targetAudioSource = GetComponent<AudioSource>();
                if (targetAudioSource == null)
                {
                    targetAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            targetAudioSource.volume = 0f;
        }

        // 播放音源文件
        protected virtual void Play(AudioClip audioClip)
        {
            // 检查
            // 是否有音源，可播放的声音
            if (targetAudioSource == null ||
                (audioMode == AudioMode.SoundEffect && soundEffect == null && audioClip == null) ||
                (audioMode == AudioMode.Beeps && beepSounds.Count == 0))
            {
                return;
            }

            // 标记
            // 不是播放voiceover
            playingVoiceover = false;

            // 设置声音0
            // Q: 这里声音设置0是？
            targetAudioSource.volume = 0f;
            targetVolume = volume;

            // 有传入audioClip
            // 则播放
            if (audioClip != null)
            {
                // Voice over clip provided
                targetAudioSource.clip = audioClip;
                targetAudioSource.loop = loop;
                targetAudioSource.Play();
            }

            // 设置的是soundEffect
            // 则播放
            else if (audioMode == AudioMode.SoundEffect &&
                     soundEffect != null)
            {
                // Use sound effects defined in WriterAudio
                targetAudioSource.clip = soundEffect;
                targetAudioSource.loop = loop;
                targetAudioSource.Play();
            }

            // 设置的是beep
            // 则播放
            else if (audioMode == AudioMode.Beeps)
            {
                // Use beeps defined in WriterAudio
                targetAudioSource.clip = null;
                targetAudioSource.loop = false;
                playBeeps = true;
            }
        }

        // Q: 暂停是音量成0?
        protected virtual void Pause()
        {
            if (targetAudioSource == null)
            {
                return;
            }

            // There's an audible click if you call audioSource.Pause() so instead just drop the volume to 0.
            targetVolume = 0f;
        }

        // 停止播放
        // 音量变0
        protected virtual void Stop()
        {
            if (targetAudioSource == null)
            {
                return;
            }

            // There's an audible click if you call audioSource.Stop() so instead we just switch off
            // looping and let the audio stop automatically at the end of the clip
            targetVolume = 0f;
            targetAudioSource.loop = false;
            playBeeps = false;
            playingVoiceover = false;
        }


        // 开始播放
        // Q: 是音量重新开启！
        protected virtual void Resume()
        {
            if (targetAudioSource == null)
            {
                return;
            }

            targetVolume = volume;
        }

        // 音量是渐变的
        protected virtual void Update()
        {
            targetAudioSource.volume = Mathf.MoveTowards(targetAudioSource.volume, targetVolume, Time.deltaTime * 5f);
        }

        #region IWriterListener implementation

        public virtual void OnInput()
        {
            if (inputSound != null)
            {
                // Assumes we're playing a 2D sound
                AudioSource.PlayClipAtPoint(inputSound, Vector3.zero);
            }
        }

        public virtual void OnStart(AudioClip audioClip)
        {
            if (playingVoiceover)
            {
                return;
            }
            Play(audioClip);
        }
        
        public virtual void OnPause()
        {
            if (playingVoiceover)
            {
                return;
            }
            Pause();
        }
        
        public virtual void OnResume()
        {
            if (playingVoiceover)
            {
                return;
            }
            Resume();
        }
        
        public virtual void OnEnd(bool stopAudio)
        {
            if (stopAudio)
            {
                Stop();
            }
        }

        // 播放beep音效
        public virtual void OnGlyph()
        {
            // 如果在播放文本语音
            // 则退出
            // 不播放beep
            if (playingVoiceover)
            {
                return;
            }

            // 有设置Beep
            if (playBeeps && beepSounds.Count > 0)
            {
                // 音源没有在播放
                if (!targetAudioSource.isPlaying)
                {
                    // 达到，下一次beep时间
                    if (nextBeepTime < Time.realtimeSinceStartup)
                    {
                        // 随机选择个beep声音
                        targetAudioSource.clip = beepSounds[Random.Range(0, beepSounds.Count)];

                        if (targetAudioSource.clip != null)
                        {
                            targetAudioSource.loop = false;
                            targetVolume = volume;
                            targetAudioSource.Play();

                            // 设置下一次beep时间
                            float extend = targetAudioSource.clip.length;
                            nextBeepTime = Time.realtimeSinceStartup + extend;
                        }
                    }
                }
            }
        }

        // 开始播放
        // voiceOverClip
        public virtual void OnVoiceover(AudioClip voiceOverClip)
        {
            if (targetAudioSource == null)
            {
                return;
            }

            // 标记，播放中
            playingVoiceover = true;

            // 音量
            targetAudioSource.volume = volume;
            targetVolume = volume;

            // 不循环
            targetAudioSource.loop = false;

            // 播放
            targetAudioSource.clip = voiceOverClip;
            targetAudioSource.Play();
        }
            
        #endregion
    }
}
