// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

namespace Fungus
{
    /// <summary>
    /// Displays a button in a multiple choice menu.
    /// </summary>
    [CommandInfo("Narrative", 
                 "Menu", 
                 "Displays a button in a multiple choice menu")]
    [AddComponentMenu("")]
    public class Menu : Command, ILocalizable
    {
        [Tooltip("Text to display on the menu button")]
        [TextArea()]

        // 菜单文本
        [SerializeField] protected string text = "Option Text";

        // 注释描述
        [Tooltip("Notes about the option text for other authors, localization, etc.")]
        [SerializeField] protected string description = "";

        // 菜单的
        // 下一个目标Block
        [FormerlySerializedAs("targetSequence")]
        [Tooltip("Block to execute when this option is selected")]
        [SerializeField] protected Block targetBlock;

        [Tooltip("Hide this option if the target block has been executed previously")]
        [SerializeField] protected bool hideIfVisited;

        [Tooltip("If false, the menu option will be displayed but will not be selectable")]
        [SerializeField] protected BooleanData interactable = new BooleanData(true);

        // 自定义的
        // 菜单界面
        [Tooltip("A custom Menu Dialog to use to display this menu. All subsequent Menu commands will use this dialog.")]
        [SerializeField] protected MenuDialog setMenuDialog;

        [Tooltip("If true, this option will be passed to the Menu Dialogue but marked as hidden, this can be used to hide options while maintaining a Menu Shuffle.")]
        [SerializeField] protected BooleanData hideThisOption = new BooleanData(false);

        #region Public members

        public MenuDialog SetMenuDialog  { get { return setMenuDialog; } set { setMenuDialog = value; } }

        public override void OnEnter()
        {
            // 使用自定义的
            // 菜单界面
            if (setMenuDialog != null)
            {
                // Override the active menu dialog
                MenuDialog.ActiveMenuDialog = setMenuDialog;
            }

            bool hideOption = (hideIfVisited && targetBlock != null && targetBlock.GetExecutionCount() > 0) ||
                hideThisOption.Value;

            // 获取菜单窗口
            var menuDialog = MenuDialog.GetMenuDialog();
            if (menuDialog != null)
            {
                menuDialog.SetActive(true);

                var flowchart = GetFlowchart();

                // 替换显示文本中的变量
                string displayText = flowchart.SubstituteVariables(text);

                // 添加菜单
                // 传入菜单参数
                menuDialog.AddOption(displayText, interactable, hideOption, targetBlock);
            }
            
            Continue();
        }

        // 获取连接的Block
        public override void GetConnectedBlocks(ref List<Block> connectedBlocks)
        {
            if (targetBlock != null)
            {
                connectedBlocks.Add(targetBlock);
            }       
        }

        public override string GetSummary()
        {
            if (targetBlock == null)
            {
                return "Error: No target block selected";
            }

            if (text == "")
            {
                return "Error: No button text selected";
            }

            return text + " : " + targetBlock.BlockName;
        }

        // 命令颜色
        public override Color GetButtonColor()
        {
            return new Color32(184, 210, 235, 255);
        }

        public override bool HasReference(Variable variable)
        {
            return interactable.booleanRef == variable ||
                hideThisOption.booleanRef == variable ||
                base.HasReference(variable);
        }

        #endregion

        #region ILocalizable implementation
        // 多语言相关

        public virtual string GetStandardText()
        {
            return text;
        }

        public virtual void SetStandardText(string standardText)
        {
            text = standardText;
        }
        
        public virtual string GetDescription()
        {
            return description;
        }
        
        public virtual string GetStringId()
        {
            // String id for Menu commands is MENU.<Localization Id>.<Command id>
            return "MENU." + GetFlowchartLocalizationId() + "." + itemId;
        }

        #endregion

        #region Editor caches
#if UNITY_EDITOR

        // Q: ???
        // 这是要做什么？
        protected override void RefreshVariableCache()
        {
            base.RefreshVariableCache();

            var f = GetFlowchart();

            // Q: ???
            f.DetermineSubstituteVariables(text, referencedVariables);
        }
#endif
        #endregion Editor caches
    }
}
