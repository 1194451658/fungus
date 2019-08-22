// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using System;
using UnityEngine;

namespace Fungus
{
    public class ControlWithDisplay<TDisplayEnum> : Command
    {
        // 显示操作枚举
        [Tooltip("Display type")]
        [SerializeField] protected TDisplayEnum display;

        // 判断显示操作枚举
        // 是不是None
        protected virtual bool IsDisplayNone<TEnum>(TEnum enumValue)
        {
            string displayTypeStr = Enum.GetName(typeof (TEnum), enumValue);
            return displayTypeStr == "None";
        }

        #region Public members

        // 获取当前显示操作枚举
        public virtual TDisplayEnum Display { get { return display; } }

        #endregion
    }
}
