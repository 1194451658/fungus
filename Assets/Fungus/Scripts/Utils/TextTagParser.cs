// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Fungus
{
    /// <summary>
    /// Parses a string for special Fungus text tags.
    /// </summary>

    // Fungus自己的Narrative Text Tag
    // 富文本解析器
    public static class TextTagParser
    {
        // Q: 后面有个问号是
        // lazy匹配
        const string TextTokenRegexString = @"\{.*?\}";

        // 添加成Word Token
        // paramList: 只有一个参数，就是要显示的文本
        private static void AddWordsToken(List<TextTagToken> tokenList, string words)
        {
            TextTagToken token = new TextTagToken();
            token.type = TokenType.Words;
            token.paramList = new List<string>(); 
            token.paramList.Add(words);
            tokenList.Add(token);
        }
        
        // 添加{} Token
        private static void AddTagToken(List<TextTagToken> tokenList, string tagText)
        {
            // 检查
            // 最基本格式
            if (tagText.Length < 3 ||
                tagText.Substring(0,1) != "{" ||
                tagText.Substring(tagText.Length - 1,1) != "}")
            {
                return;
            }
            
            string tag = tagText.Substring(1, tagText.Length - 2);
            
            var type = TokenType.Invalid;

            // 根据等号、逗号，
            // 分割到参数
            List<string> parameters = ExtractParameters(tag);
            
            // 粗体
            if (tag == "b")
            {
                type = TokenType.BoldStart;
            }
            else if (tag == "/b")
            {
                type = TokenType.BoldEnd;
            }

            // 斜体
            else if (tag == "i")
            {
                type = TokenType.ItalicStart;
            }
            else if (tag == "/i")
            {
                type = TokenType.ItalicEnd;
            }

            // 颜色
            else if (tag.StartsWith("color="))
            {
                type = TokenType.ColorStart;
            }
            else if (tag == "/color")
            {
                type = TokenType.ColorEnd;
            }

            // 大小
            else if (tag.StartsWith("size="))
            {
                type = TokenType.SizeStart;
            }
            else if (tag == "/size")
            {
                type = TokenType.SizeEnd;
            }

            // 等待输入
            else if (tag == "wi")
            {
                type = TokenType.WaitForInputNoClear;
            }
            else if (tag == "wc")
            {
                type = TokenType.WaitForInputAndClear;
            }

            // 等待语音结束
            else if (tag == "wvo")
            {
                type = TokenType.WaitForVoiceOver;
            }

            // 标点符号上
            // 等待多少时间
            else if (tag.StartsWith("wp="))
            {
                type = TokenType.WaitOnPunctuationStart;
            }
            else if (tag == "wp")
            {
                type = TokenType.WaitOnPunctuationStart;
            }
            else if (tag == "/wp")
            {
                type = TokenType.WaitOnPunctuationEnd;
            }

            // 等待
            else if (tag.StartsWith("w="))
            {
                type = TokenType.Wait;
            }
            else if (tag == "w")
            {
                type = TokenType.Wait;
            }

            // 清除
            else if (tag == "c")
            {
                type = TokenType.Clear;
            }

            // 播放速度
            else if (tag.StartsWith("s="))
            {
                type = TokenType.SpeedStart;
            }
            else if (tag == "s")
            {
                type = TokenType.SpeedStart;
            }
            else if (tag == "/s")
            {
                type = TokenType.SpeedEnd;
            }

            // 退出
            else if (tag == "x")
            {
                type = TokenType.Exit;
            }

            // 发送消息
            else if (tag.StartsWith("m="))
            {
                type = TokenType.Message;
            }

            // 震屏
            else if (tag.StartsWith("vpunch") ||
                     tag.StartsWith("vpunch="))
            {
                type = TokenType.VerticalPunch;
            }
            else if (tag.StartsWith("hpunch") ||
                     tag.StartsWith("hpunch="))
            {
                type = TokenType.HorizontalPunch;
            }
            else if (tag.StartsWith("punch") ||
                     tag.StartsWith("punch="))
            {
                type = TokenType.Punch;
            }

            // 闪屏
            else if (tag.StartsWith("flash") ||
                     tag.StartsWith("flash="))
            {
                type = TokenType.Flash;
            }

            // 音频
            else if (tag.StartsWith("audio="))
            {
                type = TokenType.Audio;
            }
            else if (tag.StartsWith("audioloop="))
            {
                type = TokenType.AudioLoop;
            }
            else if (tag.StartsWith("audiopause="))
            {
                type = TokenType.AudioPause;
            }
            else if (tag.StartsWith("audiostop="))
            {
                type = TokenType.AudioStop;
            }
            
            if (type != TokenType.Invalid)
            {
                TextTagToken token = new TextTagToken();
                token.type = type;
                token.paramList = parameters;           
                tokenList.Add(token);
            }
            else
            {
                Debug.LogWarning("Invalid text tag " + tag);
            }
        }

        //
        // 使用等号，逗号，进行分割后
        //
        private static List<string> ExtractParameters(string input)
        {
            // 使用"="进行分隔
            List<string> paramsList = new List<string>();
            int index = input.IndexOf('=');
            if (index == -1)
            {
                return paramsList;
            }

            // =号后面
            // 再用逗号"," 分隔
            string paramsStr = input.Substring(index + 1);
            var splits = paramsStr.Split(',');
            for (int i = 0; i < splits.Length; i++)
            {
                // 分隔出参数
                var p = splits[i];
                paramsList.Add(p.Trim());
            }
            return paramsList;
        }

        #region Public members

        /// <summary>
        /// Returns a description of the supported tags.
        /// </summary>
        public static string GetTagHelp()
        {
            return "" +
                "\t{b} Bold Text {/b}\n" + 
                "\t{i} Italic Text {/i}\n" +
                "\t{color=red} Color Text (color){/color}\n" +
                "\t{size=30} Text size {/size}\n" +
                "\n" +
                "\t{s}, {s=60} Writing speed (chars per sec){/s}\n" +
                "\t{w}, {w=0.5} Wait (seconds)\n" +
                "\t{wi} Wait for input\n" +
                "\t{wc} Wait for input and clear\n" +
                "\t{wvo} Wait for voice over line to complete\n" +
                "\t{wp}, {wp=0.5} Wait on punctuation (seconds){/wp}\n" +
                "\t{c} Clear\n" +
                "\t{x} Exit, advance to the next command without waiting for input\n" +
                "\n" +
                "\t{vpunch=10,0.5} Vertically punch screen (intensity,time)\n" +
                "\t{hpunch=10,0.5} Horizontally punch screen (intensity,time)\n" +
                "\t{punch=10,0.5} Punch screen (intensity,time)\n" +
                "\t{flash=0.5} Flash screen (duration)\n" +
                "\n" +
                "\t{audio=AudioObjectName} Play Audio Once\n" +
                "\t{audioloop=AudioObjectName} Play Audio Loop\n" +
                "\t{audiopause=AudioObjectName} Pause Audio\n" +
                "\t{audiostop=AudioObjectName} Stop Audio\n" +
                "\n" +
                "\t{m=MessageName} Broadcast message\n" +
                "\t{$VarName} Substitute variable";
        }

        /// <summary>
        /// Processes a block of story text and converts it to a list of tokens.
        /// </summary>
        public static List<TextTagToken> Tokenize(string storyText)
        {
            List<TextTagToken> tokens = new List<TextTagToken>();

            Regex myRegex = new Regex(TextTokenRegexString);

            Match m = myRegex.Match(storyText);   // m is the first match

            int position = 0;
            while (m.Success)
            {
                // Get bit leading up to tag
                string preText = storyText.Substring(position, m.Index - position);
                string tagText = m.Value;

                // {}Token之前的文本
                if (preText != "")
                {
                    AddWordsToken(tokens, preText);
                }

                AddTagToken(tokens, tagText);

                position = m.Index + tagText.Length;
                m = m.NextMatch();
            }

            // 添加最后
            // 剩余的文本
            if (position < storyText.Length)
            {
                string postText = storyText.Substring(position, storyText.Length - position);
                if (postText.Length > 0)
                {
                    AddWordsToken(tokens, postText);
                }
            }

            // Remove all leading whitespace & newlines after a {c} or {wc} tag
            // These characters are usually added for legibility when editing, but are not 
            // desireable when viewing the text in game.
            bool trimLeading = false;
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (trimLeading && token.type == TokenType.Words)
                {
                    token.paramList[0] = token.paramList[0].TrimStart(' ', '\t', '\r', '\n');
                }
                if (token.type == TokenType.Clear || token.type == TokenType.WaitForInputAndClear)
                {
                    trimLeading = true;
                }
                else
                {
                    trimLeading = false;
                }
            }

            return tokens;
        }

        #endregion
    }    
}
