// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace Fungus
{
    /// <summary>
    /// Define a set of screen positions where character sprites can be displayed.
    /// </summary>
    [ExecuteInEditMode]

    // 只有变量设置
    // 没有特别多数
    // 函数功能，在PortraitController中实现
    public class Stage : PortraitController
    {
        [Tooltip("Canvas object containing the stage positions.")]
        [SerializeField] protected Canvas portraitCanvas;

        // 不说话的时
        // 将角色变暗
        [Tooltip("Dim portraits when a character is not speaking.")]
        [SerializeField] protected bool dimPortraits;

        // 变暗时的颜色
        [Tooltip("Choose a dimColor")]
        [SerializeField] protected Color dimColor =new Color(0.5f, 0.5f, 0.5f, 1f);

        // 角色，显示/隐藏时间
        [Tooltip("Duration for fading character portraits in / out.")]
        [SerializeField] protected float fadeDuration = 0.5f;

        // 角色移动时间
        [Tooltip("Duration for moving characters to a new position")]
        [SerializeField] protected float moveDuration = 1f;

        // 变暗的缓动
        [Tooltip("Ease type for the fade tween.")]
        [SerializeField] protected LeanTweenType fadeEaseType;

        // Q: shift操作
        [Tooltip("Constant offset to apply to portrait position.")]
        [SerializeField] protected Vector2 shiftOffset;

        // 角色默认出现位置
        [Tooltip("The position object where characters appear by default.")]
        [SerializeField] protected Image defaultPosition;

        // 定义的各个位置
        [Tooltip("List of stage position rect transforms in the stage.")]
        [SerializeField] protected List<RectTransform> positions;

        protected List<Character> charactersOnStage = new List<Character>();

        // 全局列表
        protected static List<Stage> activeStages = new List<Stage>();

        // 加入到全局列表
        protected virtual void OnEnable()
        {
            if (!activeStages.Contains(this))
            {
                activeStages.Add(this);
            }
        }

        // 从全局列表移除
        protected virtual void OnDisable()
        {
            activeStages.Remove(this);
        }

        protected virtual void Start()
        {
            // 开启Stage的Canvas
            if (Application.isPlaying &&
                portraitCanvas != null)
            {
                // Ensure the stage canvas is active
                portraitCanvas.gameObject.SetActive(true);
            }
        }

        #region Public members

        /// <summary>
        /// Gets the list of active stages.
        /// </summary>

        // 获取全局列表
        public static List<Stage> ActiveStages { get { return activeStages; } }

        /// <summary>
        /// Returns the currently active stage.
        /// </summary>

        // 获取第一个可用Stage
        public static Stage GetActiveStage()
        {
            if (Stage.activeStages == null ||
                Stage.activeStages.Count == 0)
            {
                return null;
            }

            return Stage.activeStages[0];
        }

        /// <summary>
        /// Canvas object containing the stage positions.
        /// </summary>
        public virtual Canvas PortraitCanvas { get { return portraitCanvas; } }

        /// <summary>
        /// Dim portraits when a character is not speaking.
        /// </summary>
        public virtual bool DimPortraits { get { return dimPortraits; } set { dimPortraits = value; } }

        /// <summary>
        /// Choose a dimColor.
        /// </summary>
        public virtual Color DimColor { get { return dimColor; } set { dimColor = value; } }

        /// <summary>
        /// Duration for fading character portraits in / out.
        /// </summary>
        public virtual float FadeDuration { get { return fadeDuration; } set { fadeDuration = value; } }

        /// <summary>
        /// Duration for moving characters to a new position.
        /// </summary>
        public virtual float MoveDuration { get { return moveDuration; } set { moveDuration = value; } }

        /// <summary>
        /// Ease type for the fade tween.
        /// </summary>
        public virtual LeanTweenType FadeEaseType { get { return fadeEaseType; } }

        /// <summary>
        /// Constant offset to apply to portrait position.
        /// </summary>
        public virtual Vector2 ShiftOffset { get { return shiftOffset; } }

        /// <summary>
        /// The position object where characters appear by default.
        /// </summary>
        public virtual Image DefaultPosition { get { return defaultPosition; } }

        /// <summary>
        /// List of stage position rect transforms in the stage.
        /// </summary>
        public virtual List<RectTransform> Positions { get { return positions; } }

        /// <summary>
        /// List of currently active characters on the stage.
        /// </summary>
        public virtual List<Character> CharactersOnStage { get { return charactersOnStage; } }

        /// <summary>
        /// Searches the stage's named positions
        /// If none matches the string provided, give a warning and return a new RectTransform
        /// </summary>
        public RectTransform GetPosition(string positionString)
        {
            if (string.IsNullOrEmpty(positionString))
            {
                return null;
            }

            for (int i = 0; i < positions.Count; i++)
            {
                if ( String.Compare(positions[i].name, positionString, true) == 0 )
                {
                    return positions[i];
                }
            }
            return null;
        }

        #endregion
    }
}

