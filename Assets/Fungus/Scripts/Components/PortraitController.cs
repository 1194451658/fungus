// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using MoonSharp.Interpreter;

namespace Fungus
{
    /// <summary>
    /// Types of display operations supported by portraits.
    /// </summary>
    public enum DisplayType
    {
        /// <summary> Do nothing. </summary>
        None,
        /// <summary> Show the portrait. </summary>
        Show,
        /// <summary> Hide the portrait. </summary>
        Hide,
        /// <summary> Replace the existing portrait. </summary>
        Replace,
        /// <summary> Move portrait to the front. </summary>
        MoveToFront
    }

    /// <summary>
    /// Directions that character portraits can face.
    /// </summary>

    // 朝向
    public enum FacingDirection
    {
        /// <summary> Unknown direction </summary>
        None,
        /// <summary> Facing left. </summary>
        Left,
        /// <summary> Facing right. </summary>
        Right
    }

    /// <summary>
    /// Offset direction for position.
    /// </summary>

    // 偏移方向
    public enum PositionOffset
    {
        /// <summary> Unknown offset direction. </summary>
        None,
        /// <summary> Offset applies to the left. </summary>
        OffsetLeft,
        /// <summary> Offset applies to the right. </summary>
        OffsetRight
    }

    /// <summary>
    /// Controls the Portrait sprites on stage
    /// </summary>
    public class PortraitController : MonoBehaviour
    {
        // Timer for waitUntilFinished functionality
        protected float waitTimer;

        protected Stage stage;


        // Q: Stage继承自PortraitController
        // 这里还获取Stage !!!
        // Stage上，
        //  * 只保存了一些变量设置
        //  * 没有写什么函数
        protected virtual void Awake()
        {
            stage = GetComponentInParent<Stage>();
        }

        // 立即执行onComplete
        // 还是fade结束后，调用
        // PortraitOptions: 各种参数
        protected virtual void FinishCommand(PortraitOptions options)
        {
            if (options.onComplete != null)
            {
                if (!options.waitUntilFinished)
                {
                    options.onComplete();
                }
                else
                {
                    // 是，waitUntilFinished
                    // 就是延迟FadeDuration调用回调
                    StartCoroutine(WaitUntilFinished(options.fadeDuration, options.onComplete));
                }
            }
            else
            {
                // onComplete == null !
                StartCoroutine(WaitUntilFinished(options.fadeDuration));
            }
        }

        /// <summary>
        /// Makes sure all options are set correctly so it won't break whatever command it's sent to
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>

        // 清理和检查参数
        // PortraitOptions
        protected virtual PortraitOptions CleanPortraitOptions(PortraitOptions options)
        {
            //
            // 使用默认参数
            //  * 就是使用Stage上的参数
            // 

            // Use default stage settings
            if (options.useDefaultSettings)
            {
                options.fadeDuration = stage.FadeDuration;
                options.moveDuration = stage.MoveDuration;
                options.shiftOffset = stage.ShiftOffset;
            }

            // if no previous portrait, use default portrait

            // 检查
            // 前一个角色，显示状态
            // State.portrait: 角色当前的Portrait?  之前的Portrait?
            // character.ProfileSprite: 当前显示的Portrait
            if (options.character.State.portrait == null)
            {
                options.character.State.portrait = options.character.ProfileSprite;
            }

            // Selected "use previous portrait"
            if (options.portrait == null)
            {
                options.portrait = options.character.State.portrait;
            }

            // 检查
            // 前一个位置的记录
            // if no previous position, use default position
            if (options.character.State.position == null)
            {
                options.character.State.position = stage.DefaultPosition.rectTransform;
            }

            // Selected "use previous position"
            if (options.toPosition == null)
            {
                options.toPosition = options.character.State.position;
            }

            if (options.replacedCharacter != null)
            {
                // if no previous position, use default position
                if (options.replacedCharacter.State.position == null)
                {
                    options.replacedCharacter.State.position = stage.DefaultPosition.rectTransform;
                }
            }

            // If swapping, use replaced character's position
            if (options.display == DisplayType.Replace)
            {
                options.toPosition = options.replacedCharacter.State.position;
            }

            // Selected "use previous position"
            if (options.fromPosition == null)
            {
                options.fromPosition = options.character.State.position;
            }

            // if portrait not moving, use from position is same as to position
            if (!options.move)
            {
                options.fromPosition = options.toPosition;
            }

            if (options.display == DisplayType.Hide)
            {
                options.fromPosition = options.character.State.position;
            }

            // if no previous facing direction, use default facing direction
            if (options.character.State.facing == FacingDirection.None)
            {
                options.character.State.facing = options.character.PortraitsFace;
            }

            // Selected "use previous facing direction"
            if (options.facing == FacingDirection.None)
            {
                options.facing = options.character.State.facing;
            }

            if (options.character.State.portraitImage == null)
            {
                CreatePortraitObject(options.character, options.fadeDuration);
            }

            return options;
        }

        /// <summary>
        /// Creates and sets the portrait image for a character
        /// </summary>
        /// <param name="character"></param>
        /// <param name="fadeDuration"></param>

        // 创建
        // 显示的Portrait实例
        protected virtual void CreatePortraitObject(Character character, float fadeDuration)
        {
            // 创建新Go
            // Create a new portrait object
            GameObject portraitObj = new GameObject(character.name,
                                                    typeof(RectTransform),
                                                    typeof(CanvasRenderer),
                                                    typeof(Image));

        
            // 放置到Canvas
            // Set it to be a child of the stage
            portraitObj.transform.SetParent(stage.PortraitCanvas.transform, true);

            // 添加Image控件
            // 并设置Portrait
            // Configure the portrait image
            Image portraitImage = portraitObj.GetComponent<Image>();
            portraitImage.preserveAspect = true;
            portraitImage.overrideSprite = character.ProfileSprite;
            portraitImage.color = new Color(1f, 1f, 1f, 0f);

            // LeanTween doesn't handle 0 duration properly
            float duration = (fadeDuration > 0f) ? fadeDuration : float.Epsilon;

            // 逐渐显示
            // Fade in character image (first time)
            LeanTween.alpha(portraitImage.transform as RectTransform, 1f, duration).setEase(stage.FadeEaseType).setRecursive(false);

            // 记录到Character
            // 当前显示状态
            // Tell character about portrait image
            character.State.portraitImage = portraitImage;
        }

        // 延迟duration, 
        // 调用onComplete
        protected virtual IEnumerator WaitUntilFinished(float duration, Action onComplete = null)
        {
            // Wait until the timer has expired
            // Any method can modify this timer variable to delay continuing.

            waitTimer = duration;
            while (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                yield return null;
            }

            // Wait until next frame just to be safe
            yield return new WaitForEndOfFrame();

            if (onComplete != null)
            {
                onComplete();
            }
        }

        // 设置角色Portrait的朝向
        protected virtual void SetupPortrait(PortraitOptions options)
        {
            // 设置当前Portrait位置
            SetRectTransform(options.character.State.portraitImage.rectTransform, options.fromPosition);

            // 先把图片
            // 转到正方向
            // PortraitsFace: 角色图片制作时候的方向
            // State.facing: 上一次，或，当前头像朝向
            if (options.character.State.facing != options.character.PortraitsFace)
            {
                options.character.State.portraitImage.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }
            else
            {
                options.character.State.portraitImage.rectTransform.localScale = new Vector3(1f, 1f, 1f);
            }

            // 转换成新的方向
            // options:facing: 新的朝向
            if (options.facing != options.character.PortraitsFace)
            {
                options.character.State.portraitImage.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }
            else
            {
                options.character.State.portraitImage.rectTransform.localScale = new Vector3(1f, 1f, 1f);
            }
        }

        protected virtual void DoMoveTween(Character character,
            RectTransform fromPosition,
            RectTransform toPosition,
            float moveDuration,
            Boolean waitUntilFinished)
        {
            PortraitOptions options = new PortraitOptions(true);
            options.character = character;
            options.fromPosition = fromPosition;
            options.toPosition = toPosition;
            options.moveDuration = moveDuration;
            options.waitUntilFinished = waitUntilFinished;

            DoMoveTween(options);
        }

        // 移动角色形象
        // 到目标点
        protected virtual void DoMoveTween(PortraitOptions options)
        {
            // 清理和检查参数
            CleanPortraitOptions(options);

            // 防止0
            // LeanTween doesn't handle 0 duration properly
            float duration = (options.moveDuration > 0f) ? options.moveDuration : float.Epsilon;

            // 向目标点移动
            // LeanTween.move uses the anchoredPosition, so all position images must have the same anchor position
            LeanTween.move(
                options.character.State.portraitImage.gameObject,
                options.toPosition.position,
                duration)
            .setEase(stage.FadeEaseType);

            if (options.waitUntilFinished)
            {
                waitTimer = duration;
            }
        }

        #region Public members

        /// <summary>
        /// Performs a deep copy of all values from one RectTransform to another.
        /// </summary>

        // 拷贝RectTransform数值
        public static void SetRectTransform(RectTransform oldRectTransform, RectTransform newRectTransform)
        {
            oldRectTransform.eulerAngles = newRectTransform.eulerAngles;
            oldRectTransform.position = newRectTransform.position;
            oldRectTransform.rotation = newRectTransform.rotation;
            oldRectTransform.anchoredPosition = newRectTransform.anchoredPosition;
            oldRectTransform.sizeDelta = newRectTransform.sizeDelta;
            oldRectTransform.anchorMax = newRectTransform.anchorMax;
            oldRectTransform.anchorMin = newRectTransform.anchorMin;
            oldRectTransform.pivot = newRectTransform.pivot;
            oldRectTransform.localScale = newRectTransform.localScale;
        }

        /// <summary>
        /// Using all portrait options available, run any portrait command.
        /// </summary>
        /// <param name="options">Portrait Options</param>
        /// <param name="onComplete">The function that will run once the portrait command finishes</param>

        // 执行Portrait命令
        public virtual void RunPortraitCommand(PortraitOptions options, Action onComplete)
        {
            waitTimer = 0f;

            // 判断提前结束
            // 情况1
            // If no character specified, do nothing
            if (options.character == null)
            {
                onComplete();
                return;
            }

            // 判断提前结束
            // 情况2
            // If Replace and no replaced character specified, do nothing
            if (options.display == DisplayType.Replace && options.replacedCharacter == null)
            {
                onComplete();
                return;
            }

            // 判断提前结束
            // 情况3
            // Early out if hiding a character that's already hidden
            if (options.display == DisplayType.Hide &&
                !options.character.State.onScreen)
            {
                onComplete();
                return;
            }

            // 清理参数
            options = CleanPortraitOptions(options);
            options.onComplete = onComplete;

            switch (options.display)
            {
                case (DisplayType.Show):
                    Show(options);
                    break;

                case (DisplayType.Hide):
                    Hide(options);
                    break;

                case (DisplayType.Replace):
                    Show(options);
                    Hide(options.replacedCharacter, options.replacedCharacter.State.position.name);
                    break;

                case (DisplayType.MoveToFront):
                    MoveToFront(options);
                    break;
            }
        }

        /// <summary>
        /// Moves Character in front of other characters on stage
        /// </summary>
        public virtual void MoveToFront(Character character)
        {
            PortraitOptions options = new PortraitOptions(true);
            options.character = character;

            MoveToFront(CleanPortraitOptions(options));
        }

        /// <summary>
        /// Moves Character in front of other characters on stage
        /// </summary>
        public virtual void MoveToFront(PortraitOptions options)
        {
            options.character.State.portraitImage.transform.SetSiblingIndex(options.character.State.portraitImage.transform.parent.childCount);
            options.character.State.display = DisplayType.MoveToFront;
            FinishCommand(options);
        }

        /// <summary>
        /// Shows character at a named position in the stage
        /// </summary>
        /// <param name="character"></param>
        /// <param name="position">Named position on stage</param>
        public virtual void Show(Character character, string position)
        {
            PortraitOptions options = new PortraitOptions(true);
            options.character = character;
            options.fromPosition = options.toPosition = stage.GetPosition(position);

            Show(options);
        }

        /// <summary>
        /// Shows character moving from a position to a position
        /// </summary>
        /// <param name="character"></param>
        /// <param name="portrait"></param>
        /// <param name="fromPosition">Where the character will appear</param>
        /// <param name="toPosition">Where the character will move to</param>
        public virtual void Show(Character character, string portrait, string fromPosition, string toPosition)
        {
            PortraitOptions options = new PortraitOptions(true);
            options.character = character;
            options.portrait = character.GetPortrait(portrait);
            options.fromPosition = stage.GetPosition(fromPosition);
            options.toPosition = stage.GetPosition(toPosition);
            options.move = true;

            Show(options);
        }

        /// <summary>
        /// From lua, you can pass an options table with named arguments
        /// example:
        ///     stage.show{character=jill, portrait="happy", fromPosition="right", toPosition="left"}
        /// Any option available in the PortraitOptions is available from Lua
        /// </summary>
        /// <param name="optionsTable">Moonsharp Table</param>
        public virtual void Show(Table optionsTable)
        {
            Show(PortraitUtil.ConvertTableToPortraitOptions(optionsTable, stage));
        }

        /// <summary>
        /// Show portrait with the supplied portrait options
        /// </summary>
        /// <param name="options"></param>

        // 显示角色命令
        public virtual void Show(PortraitOptions options)
        {
            // 清理和检查参数
            options = CleanPortraitOptions(options);

            // 使用Shift功能
            // 起点是相对位置
            if (options.shiftIntoPlace)
            {
                // 复制出来的是GameObject
                options.fromPosition = Instantiate(options.toPosition) as RectTransform;

                // 起点在左侧
                if (options.offset == PositionOffset.OffsetLeft)
                {
                    options.fromPosition.anchoredPosition =
                        new Vector2(options.fromPosition.anchoredPosition.x - Mathf.Abs(options.shiftOffset.x),
                            options.fromPosition.anchoredPosition.y - Mathf.Abs(options.shiftOffset.y));
                }

                // 起点在右侧
                else if (options.offset == PositionOffset.OffsetRight)
                {
                    options.fromPosition.anchoredPosition =
                        new Vector2(options.fromPosition.anchoredPosition.x + Mathf.Abs(options.shiftOffset.x),
                            options.fromPosition.anchoredPosition.y + Mathf.Abs(options.shiftOffset.y));
                }
                else
                {
                    // 直接使用Offset
                    // 进行偏移
                    options.fromPosition.anchoredPosition = new Vector2(options.fromPosition.anchoredPosition.x, options.fromPosition.anchoredPosition.y);
                }
            }

            // 设置角色Portrait的朝向
            SetupPortrait(options);

            // 防止0
            // LeanTween doesn't handle 0 duration properly
            float duration = (options.fadeDuration > 0f) ? options.fadeDuration : float.Epsilon;

            // 旧形象
            // 消失效果
            // Fade out a duplicate of the existing portrait image
            if (options.character.State.portraitImage != null && options.character.State.portraitImage.overrideSprite != null)
            {
                // 复制出当前Portrait
                // 播放消失效果
                // portraitImage是Image类型
                GameObject tempGO = GameObject.Instantiate(options.character.State.portraitImage.gameObject);

                // 放置在当前Portrait前
                tempGO.transform.SetParent(options.character.State.portraitImage.transform, false);
                tempGO.transform.localPosition = Vector3.zero;
                tempGO.transform.localScale = options.character.State.position.localScale;
                // 设置形象图片
                Image tempImage = tempGO.GetComponent<Image>();
                tempImage.overrideSprite = options.character.State.portraitImage.overrideSprite;
                tempImage.preserveAspect = true;
                tempImage.color = options.character.State.portraitImage.color;

                // alpha消失效果
                LeanTween.
                    alpha(tempImage.rectTransform, 0f, duration).
                    setEase(stage.FadeEaseType).
                    setOnComplete(
                        () => {
                            Destroy(tempGO);
                        }
                    ).
                    setRecursive(false);
            }

            // 渐显新的形象
            // Fade in the new sprite image
            if (options.character.State.portraitImage.overrideSprite != options.portrait ||
                options.character.State.portraitImage.color.a < 1f)
            {
                options.character.State.portraitImage.overrideSprite = options.portrait;
                options.character.State.portraitImage.color = new Color(1f, 1f, 1f, 0f);
                LeanTween.
                    alpha(options.character.State.portraitImage.rectTransform, 1f, duration).
                    setEase(stage.FadeEaseType).
                    setRecursive(false);
            }

            // 移动角色形象
            // 到目标点
            DoMoveTween(options);

            // 立即执行onComplete
            // 还是fade结束后，调用
            // PortraitOptions: 各种参数
            FinishCommand(options);

            // 添加角色到
            // CharactersOnStage列表
            if (!stage.CharactersOnStage.Contains(options.character))
            {
                stage.CharactersOnStage.Add(options.character);
            }

            // 更新角色状态
            // Update character state after showing
            options.character.State.onScreen = true;
            options.character.State.display = DisplayType.Show;
            options.character.State.portrait = options.portrait;
            options.character.State.facing = options.facing;
            options.character.State.position = options.toPosition;
        }

        /// <summary>
        /// Simple show command that shows the character with an available named portrait
        /// </summary>
        /// <param name="character">Character to show</param>
        /// <param name="portrait">Named portrait to show for the character, i.e. "angry", "happy", etc</param>
        public virtual void ShowPortrait(Character character, string portrait)
        {
            PortraitOptions options = new PortraitOptions(true);
            options.character = character;
            options.portrait = character.GetPortrait(portrait);

            if (character.State.position == null)
            {
                options.toPosition = options.fromPosition = stage.GetPosition("middle");
            }
            else
            {
                options.fromPosition = options.toPosition = character.State.position;
            }

            Show(options);
        }

        /// <summary>
        /// Simple character hide command
        /// </summary>
        /// <param name="character">Character to hide</param>
        public virtual void Hide(Character character)
        {
            PortraitOptions options = new PortraitOptions(true);
            options.character = character;

            Hide(options);
        }

        /// <summary>
        /// Move the character to a position then hide it
        /// </summary>
        /// <param name="character">Character to hide</param>
        /// <param name="toPosition">Where the character will disapear to</param>
        public virtual void Hide(Character character, string toPosition)
        {
            PortraitOptions options = new PortraitOptions(true);
            options.character = character;
            options.toPosition = stage.GetPosition(toPosition);
            options.move = true;

            Hide(options);
        }

        /// <summary>
        /// From lua, you can pass an options table with named arguments
        /// example:
        ///     stage.hide{character=jill, toPosition="left"}
        /// Any option available in the PortraitOptions is available from Lua
        /// </summary>
        /// <param name="optionsTable">Moonsharp Table</param>
        public virtual void Hide(Table optionsTable)
        {
            Hide(PortraitUtil.ConvertTableToPortraitOptions(optionsTable, stage));
        }

        /// <summary>
        /// Hide portrait with provided options
        /// </summary>
        public virtual void Hide(PortraitOptions options)
        {
            // 清理和检查参数
            CleanPortraitOptions(options);

            if (options.character.State.display == DisplayType.None)
            {
                return;
            }

            // 设置角色Portrait的朝向
            SetupPortrait(options);
                
            // 防止0
            // LeanTween doesn't handle 0 duration properly
            float duration = (options.fadeDuration > 0f) ? options.fadeDuration : float.Epsilon;

            LeanTween.
                alpha(options.character.State.portraitImage.rectTransform, 0f, duration).
                setEase(stage.FadeEaseType).
                setRecursive(false);

            // 移动角色形象
            // 到目标点
            DoMoveTween(options);

            // 从列表中移除
            stage.CharactersOnStage.Remove(options.character);

            // 更新角色的状态
            //update character state after hiding
            options.character.State.onScreen = false;
            options.character.State.portrait = options.portrait;
            options.character.State.facing = options.facing;
            options.character.State.position = options.toPosition;
            options.character.State.display = DisplayType.Hide;

            // 立即执行onComplete
            // 还是fade结束后，调用
            FinishCommand(options);
        }

        /// <summary>
        /// Sets the dimmed state of a character on the stage.
        /// </summary>
        public virtual void SetDimmed(Character character, bool dimmedState)
        {
            if (character.State.dimmed == dimmedState)
            {
                return;
            }

            character.State.dimmed = dimmedState;

            Color targetColor = dimmedState ? stage.DimColor : Color.white;

            // LeanTween doesn't handle 0 duration properly
            float duration = (stage.FadeDuration > 0f) ? stage.FadeDuration : float.Epsilon;

            LeanTween.color(character.State.portraitImage.rectTransform, targetColor, duration).setEase(stage.FadeEaseType).setRecursive(false);
        }

        #endregion
    }
}
