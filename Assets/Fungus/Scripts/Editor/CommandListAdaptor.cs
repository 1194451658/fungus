// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEngine;
using UnityEditor;
using System;
using UnityEditorInternal;

namespace Fungus.EditorUtils
{
    public class CommandListAdaptor
    {

        public void DrawCommandList()
        {
            if (block.CommandList.Count == 0)
            {
                EditorGUILayout.HelpBox("Press the + button below to add a command to the list.", MessageType.Info);
            }
            else
            {
                EditorGUI.indentLevel++;
                list.DoLayoutList();
                EditorGUI.indentLevel--;
            }
        }

        protected SerializedProperty _arrayProperty;

        protected ReorderableList list;

        protected Block block;

        public float fixedItemHeight;

        public SerializedProperty this[int index]
        {
            get { return _arrayProperty.GetArrayElementAtIndex(index); }
        }

        public SerializedProperty arrayProperty
        {
            get { return _arrayProperty; }
        }

        // arrayProperty: 
        //  * Block.commandList字段
        public CommandListAdaptor(Block _block, SerializedProperty arrayProperty)
        {
            if (arrayProperty == null)
                throw new ArgumentNullException("Array property was null.");
            if (!arrayProperty.isArray)
                throw new InvalidOperationException("Specified serialized propery is not an array.");

            this._arrayProperty = arrayProperty;
            this.block = _block;

            list = new ReorderableList(arrayProperty.serializedObject, arrayProperty, true, true, false, false);
            list.drawHeaderCallback = DrawHeader;
            list.drawElementCallback = DrawItem;
            //list.elementHeightCallback = GetElementHeight;
        }

        //private float GetElementHeight(int index)
        //{
        //    return EditorGUI.GetPropertyHeight(this[index], null, true);// + EditorGUIUtility.singleLineHeight;
        //}

        private void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, new GUIContent("Commands"));
        }

        public void DrawItem(Rect position, int index, bool selected, bool focused)
        {
            Command command = this[index].objectReferenceValue as Command;

            if (command == null)
            {
                return;
            }

            // 获取命令类，
            //  * 开头的CommandInfoAttribute
            CommandInfoAttribute commandInfoAttr = CommandEditor.GetCommandInfo(command.GetType());
            if (commandInfoAttr == null)
            {
                return;
            }

            var flowchart = (Flowchart)command.GetFlowchart();
            if (flowchart == null)
            {
                return;
            }

            // 判断命令类别
            bool isComment = command.GetType() == typeof(Comment);
            bool isLabel = (command.GetType() == typeof(Label));


            // 处理命令的Summary
            bool error = false;
            string summary = command.GetSummary();
            if (summary == null)
            {
                summary = "";
            }
            else
            {
                summary = summary.Replace("\n", "").Replace("\r", "");
            }

            if (summary.StartsWith("Error:"))
            {
                error = true;
            }

            if (isComment || isLabel)
            {
                summary = "<b> " + summary + "</b>";
            }
            else
            {
                summary = "<i>" + summary + "</i>";
            }

            bool commandIsSelected = false;
            foreach (Command selectedCommand in flowchart.SelectedCommands)
            {
                if (selectedCommand == command)
                {
                    commandIsSelected = true;
                    break;
                }
            }

            string commandName = commandInfoAttr.CommandName;

            // 一行命令的样式
            GUIStyle commandLabelStyle = new GUIStyle(GUI.skin.box);
            commandLabelStyle.normal.background = FungusEditorResources.CommandBackground;
            // 边框宽度
            int borderSize = 5;
            commandLabelStyle.border.top = borderSize;
            commandLabelStyle.border.bottom = borderSize;
            commandLabelStyle.border.left = borderSize;
            commandLabelStyle.border.right = borderSize;
            commandLabelStyle.alignment = TextAnchor.MiddleLeft;
            commandLabelStyle.richText = true;
            commandLabelStyle.fontSize = 11;
            commandLabelStyle.padding.top -= 1;

            // 缩进的宽度
            float indentSize = 20;

            // 处理命令的缩进
            for (int i = 0; i < command.IndentLevel; ++i)
            {
                Rect indentRect = position;
                indentRect.x += i * indentSize;// - 21;
                indentRect.width = indentSize + 1;
                indentRect.y -= 2;
                indentRect.height += 5;
                // 一个灰的颜色
                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                GUI.Box(indentRect, "", commandLabelStyle);
            }

            // 命令名称的宽度
            //  * 例如：EvoSay
            float commandNameWidth = Mathf.Max(commandLabelStyle.CalcSize(new GUIContent(commandName)).x, 90f);

            // 缩进宽度
            float indentWidth = command.IndentLevel * indentSize;

            // 
            Rect commandLabelRect = position;
            commandLabelRect.x += indentWidth;// - 21;
            commandLabelRect.y -= 2;

            // 宽度上，
            //  * 减去缩进之后的宽度
            commandLabelRect.width -= (indentSize * command.IndentLevel);// - 22);
            commandLabelRect.height += 5;

            // There's a weird incompatibility between the Reorderable list control used for the command list and 
            // the UnityEvent list control used in some commands. In play mode, if you click on the reordering grabber
            // for a command in the list it causes the UnityEvent list to spew null exception errors.
            // The workaround for now is to hide the reordering grabber from mouse clicks by extending the command
            // selection rectangle to cover it. We are planning to totally replace the command list display system.
            Rect clickRect = position;
            //clickRect.x -= 20;
            //clickRect.width += 20;


            //  可点击区域：
            //      * 是整个命令区域

            // 鼠标点击下了
            // Select command via left click
            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                clickRect.Contains(Event.current.mousePosition))
            {
                // flowchart.selectedCommands: 
                //  * 就是在flowchart中记录

                // 是选中的命令，并且
                // 点击的是，鼠标左键
                if (flowchart.SelectedCommands.Contains(command) &&
                    Event.current.button == 0)
                {
                    // Left click on already selected command
                    // Command key and shift key is not pressed

                    // 没有点击shift key
                    // 没有点击command key
                    if (!EditorGUI.actionKey && !Event.current.shift)
                    {
                        BlockEditor.actionList.Add(delegate
                        {
                            // 取消选中
                            // Q: 2条重复的命令？
                            flowchart.SelectedCommands.Remove(command);
                            flowchart.ClearSelectedCommands();
                        });
                    }

                    //  -----------------------------------------
                    //      使用ctrl键，控制多选中的，取消选择
                    //  ----------------------------------------

                    // Command key pressed
                    // actionKey是:
                    //  * windosw是ctrl
                    if (EditorGUI.actionKey)
                    {
                        BlockEditor.actionList.Add(delegate
                        {
                            flowchart.SelectedCommands.Remove(command);
                        });
                        Event.current.Use();
                    }
                }
                else
                {
                    //  点击的不是，选中的命令 ||
                    // 点击的，不是鼠标左键

                    bool shift = Event.current.shift;

                    // Left click and no command key
                    //  点击了鼠标左键 &&
                    //  点击的，不是选中的命令 &&
                    //  没有按下，其他按键
                    if (!shift &&
                        !EditorGUI.actionKey &&
                        Event.current.button == 0)
                    {
                        // 点击的是，鼠标左键
                        BlockEditor.actionList.Add(delegate
                        {
                            flowchart.ClearSelectedCommands();
                        });
                        Event.current.Use();
                    }

                    // 
                    //  点击的不是，选中的命令 &&
                    //  (点击的不是左键 ||
                    //  是选中的命令 ||
                    //  有按下其他按键)


                    // 或：
                    // 点击的，不是鼠标左键 &&
                    //  (点击的不是左键 ||
                    //  是选中的命令 ||
                    //  有按下其他按键)


                    BlockEditor.actionList.Add(delegate
                    {
                        // 选中命令
                        flowchart.AddSelectedCommand(command);
                    });

                    // Find first and last selected commands
                    int firstSelectedIndex = -1;
                    int lastSelectedIndex = -1;
                    if (flowchart.SelectedCommands.Count > 0)
                    {
                        if (flowchart.SelectedBlock != null)
                        {
                            // 遍历，选中的block中的命令
                            for (int i = 0; i < flowchart.SelectedBlock.CommandList.Count; i++)
                            {
                                Command commandInBlock = flowchart.SelectedBlock.CommandList[i];
                                foreach (Command selectedCommand in flowchart.SelectedCommands)
                                {
                                    if (commandInBlock == selectedCommand)
                                    {
                                        // 获取，第一个被选中的command的下标
                                        firstSelectedIndex = i;
                                        break;
                                    }
                                }
                            }
                            for (int i = flowchart.SelectedBlock.CommandList.Count - 1; i >= 0; i--)
                            {
                                Command commandInBlock = flowchart.SelectedBlock.CommandList[i];
                                foreach (Command selectedCommand in flowchart.SelectedCommands)
                                {
                                    if (commandInBlock == selectedCommand)
                                    {
                                        // 获取，最后一个被选中的command的下标
                                        lastSelectedIndex = i;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (shift)
                    {
                        int currentIndex = command.CommandIndex;
                        if (firstSelectedIndex == -1 ||
                            lastSelectedIndex == -1)
                        {
                            // No selected command found - select entire list
                            firstSelectedIndex = 0;
                            lastSelectedIndex = currentIndex;
                        }
                        else
                        {
                            if (currentIndex < firstSelectedIndex)
                            {
                                firstSelectedIndex = currentIndex;
                            }
                            if (currentIndex > lastSelectedIndex)
                            {
                                lastSelectedIndex = currentIndex;
                            }
                        }

                        for (int i = Math.Min(firstSelectedIndex, lastSelectedIndex); i < Math.Max(firstSelectedIndex, lastSelectedIndex); ++i)
                        {
                            var selectedCommand = flowchart.SelectedBlock.CommandList[i];
                            BlockEditor.actionList.Add(delegate
                            {
                                flowchart.AddSelectedCommand(selectedCommand);
                            });
                        }
                    }

                    Event.current.Use();
                }
                GUIUtility.keyboardControl = 0; // Fix for textarea not refeshing (change focus)
            }

            Color commandLabelColor = Color.white;
            if (flowchart.ColorCommands)
            {
                commandLabelColor = command.GetButtonColor();
            }

            if (commandIsSelected)
            {
                commandLabelColor = Color.green;
            }
            else if (!command.enabled)
            {
                commandLabelColor = Color.grey;
            }
            else if (error)
            {
                // TODO: Show warning icon
            }

            GUI.backgroundColor = commandLabelColor;

            // 注释命令
            //  * 不显示，命令名称
            if (isComment)
            {
                GUI.Label(commandLabelRect, "", commandLabelStyle);
            }
            else
            {
                // 显示，命令名称
                string commandNameLabel;
                if (flowchart.ShowLineNumbers)
                {
                    // 显示，命令前面的，序号
                    commandNameLabel = command.CommandIndex.ToString() + ": " + commandName;
                }
                else
                {
                    commandNameLabel = commandName;
                }

                // 可以扩展的点
                //  * 例如：多人对话进行自定义的命令行显示
                // 显示命令Label
                GUI.Label(commandLabelRect, commandNameLabel, commandLabelStyle);
            }

            if (command.ExecutingIconTimer > Time.realtimeSinceStartup)
            {
                Rect iconRect = new Rect(commandLabelRect);
                iconRect.x += iconRect.width - commandLabelRect.width - 20;
                iconRect.width = 20;
                iconRect.height = 20;

                Color storeColor = GUI.color;

                float alpha = (command.ExecutingIconTimer - Time.realtimeSinceStartup) / FungusConstants.ExecutingIconFadeTime;
                alpha = Mathf.Clamp01(alpha);

                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Label(iconRect, FungusEditorResources.PlaySmall, new GUIStyle());

                GUI.color = storeColor;
            }

            Rect summaryRect = new Rect(commandLabelRect);
            if (isComment)
            {
                summaryRect.x += 5;
            }
            else
            {
                summaryRect.x += commandNameWidth + 5;
                summaryRect.width -= commandNameWidth + 5;
            }

            GUIStyle summaryStyle = new GUIStyle();
            summaryStyle.fontSize = 10;
            summaryStyle.padding.top += 5;
            summaryStyle.richText = true;
            summaryStyle.wordWrap = false;
            summaryStyle.clipping = TextClipping.Clip;
            commandLabelStyle.alignment = TextAnchor.MiddleLeft;

            // 显示Summary
            GUI.Label(summaryRect, summary, summaryStyle);

            if (error)
            {
                GUISkin editorSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
                Rect errorRect = new Rect(summaryRect);
                errorRect.x += errorRect.width - 20;
                errorRect.y += 2;
                errorRect.width = 20;
                GUI.Label(errorRect, editorSkin.GetStyle("CN EntryError").normal.background);
                summaryRect.width -= 20;
            }

            GUI.backgroundColor = Color.white;
        }

    }
}
