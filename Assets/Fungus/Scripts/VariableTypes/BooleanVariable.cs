// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEngine;

namespace Fungus
{
    /// <summary>
    /// Boolean variable type.
    /// </summary>
    [VariableInfo("", "Boolean")]
    [AddComponentMenu("")]
    [System.Serializable]
    public class BooleanVariable : VariableBase<bool>
    {
        public static readonly CompareOperator[] compareOperators = { CompareOperator.Equals, CompareOperator.NotEquals };
        public static readonly SetOperator[] setOperators = { SetOperator.Assign, SetOperator.Negate };

        // 支持的比较操作
        public virtual bool Evaluate(CompareOperator compareOperator, bool booleanValue)
        {
            bool condition = false;
            
            bool lhs = Value;
            bool rhs = booleanValue;
            
            switch (compareOperator)
            {
                // 相等
                case CompareOperator.Equals:
                    condition = lhs == rhs;
                    break;
                // 不相等
                case CompareOperator.NotEquals:
                    condition = lhs != rhs;
                    break;
                default:
                    Debug.LogError("The " + compareOperator.ToString() + " comparison operator is not valid.");
                    break;
            }
            
            return condition;
        }

        // 支持的操作
        //  * 赋值
        //  * 取反
        public override void Apply(SetOperator setOperator, bool value)
        {
            switch (setOperator)
            {
                // 赋值
                case SetOperator.Assign:
                    Value = value;
                    break;

                // 赋值，取反
                case SetOperator.Negate:
                    Value = !value;
                    break;
                default:
                    Debug.LogError("The " + setOperator.ToString() + " set operator is not valid.");
                    break;
            }
        }
    }

    /// <summary>
    /// Container for a Boolean variable reference or constant value.
    /// </summary>
    [System.Serializable]

    //  * bool变量和常量的封装
    //      * 封装BooleanVariable
    //      * 封装bool值
    public struct BooleanData
    {
        [SerializeField]
        [VariableProperty("<Value>", typeof(BooleanVariable))]
        public BooleanVariable booleanRef;

        [SerializeField]
        public bool booleanVal;

        public BooleanData(bool v)
        {
            booleanVal = v;
            booleanRef = null;
        }
        
        // 转换成bool类型
        public static implicit operator bool(BooleanData booleanData)
        {
            return booleanData.Value;
        }

        public bool Value
        {
            get { return (booleanRef == null) ? booleanVal : booleanRef.Value; }
            set { if (booleanRef == null) { booleanVal = value; } else { booleanRef.Value = value; } }
        }

        public string GetDescription()
        {
            if (booleanRef == null)
            {
                return booleanVal.ToString();
            }
            else
            {
                return booleanRef.Key;
            }
        }
    }
}
