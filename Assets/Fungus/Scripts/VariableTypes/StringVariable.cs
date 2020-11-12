// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEngine;

namespace Fungus
{
    /// <summary>
    /// String variable type.
    /// </summary>
    [VariableInfo("", "String")]
    [AddComponentMenu("")]
    [System.Serializable]
    public class StringVariable : VariableBase<string>
    {
        public static readonly CompareOperator[] compareOperators = {
            CompareOperator.Equals,
            CompareOperator.NotEquals 
        };

        public static readonly SetOperator[] setOperators = {
            SetOperator.Assign 
        };

        // 执行，比较操作
        public virtual bool Evaluate(CompareOperator compareOperator, string stringValue)
        {
            string lhs = Value;
            string rhs = stringValue;

            bool condition = false;

            switch (compareOperator)
            {
                case CompareOperator.Equals:
                    condition = lhs == rhs;
                    break;
                case CompareOperator.NotEquals:
                    condition = lhs != rhs;
                    break;
                default:
                    Debug.LogError("The " + compareOperator.ToString() + " comparison operator is not valid.");
                    break;
            }

            return condition;
        }

        // 执行，赋值操作
        public override void Apply(SetOperator setOperator, string value)
        {
            switch (setOperator)
            {
                case SetOperator.Assign:
                    Value = value;
                    break;
                default:
                    Debug.LogError("The " + setOperator.ToString() + " set operator is not valid.");
                    break;
            }
        }
    }

    /// <summary>
    /// Container for a string variable reference or constant value.
    /// Appears as a single line property in the inspector.
    /// For a multi-line property, use StringDataMulti.
    /// </summary>

    // 用来
    //  * 引用一个StringVariable变量
    //  * 或者，直接引用一个字符串常量
    [System.Serializable]
    public struct StringData
    {
        [SerializeField]

        // VariableProperty标记：
        //  * 类型
        //  * 默认值
        [VariableProperty("<Value>", typeof(StringVariable))]
        public StringVariable stringRef;

        // 值
        [SerializeField]
        public string stringVal;

        public StringData(string v)
        {
            stringVal = v;
            stringRef = null;
        }
        
        // 转换成字符串
        public static implicit operator string(StringData spriteData)
        {
            return spriteData.Value;
        }

        public string Value
        {
            get 
            { 
                if (stringVal == null) stringVal = "";

                // 优先返回，字符串变量的值
                //  返回，常量的值
                return (stringRef == null) ? stringVal : stringRef.Value; 
            }
            set { if (stringRef == null) { stringVal = value; } else { stringRef.Value = value; } }
        }

        public string GetDescription()
        {
            if (stringRef == null)
            {
                return stringVal;
            }
            else
            {
                return stringRef.Key;
            }
        }
    }

    /// <summary>
    /// Container for a string variable reference or constant value.
    /// Appears as a multi-line property in the inspector.
    /// For a single-line property, use StringData.
    /// </summary>
    [System.Serializable]
    public struct StringDataMulti
    {
        [SerializeField]
        [VariableProperty("<Value>", typeof(StringVariable))]
        public StringVariable stringRef;

        // 区别是，使用的是TextArea
        [TextArea(1,15)]
        [SerializeField]
        public string stringVal;

        public StringDataMulti(string v)
        {
            stringVal = v;
            stringRef = null;
        }

        public static implicit operator string(StringDataMulti spriteData)
        {
            return spriteData.Value;
        }

        public string Value
        {
            get 
            {
                if (stringVal == null) stringVal = "";
                return (stringRef == null) ? stringVal : stringRef.Value; 
            }
            set { if (stringRef == null) { stringVal = value; } else { stringRef.Value = value; } }
        }

        public string GetDescription()
        {
            if (stringRef == null)
            {
                return stringVal;
            }
            else
            {
                return stringRef.Key;
            }
        }
    }
        
}
