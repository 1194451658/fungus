// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEngine;

namespace Fungus
{
    /// <summary>
    /// Animator variable type.
    /// </summary>
    [VariableInfo("Other", "Animator")]
    [AddComponentMenu("")]
    [System.Serializable]

    // Animator类型，对应的变量
    public class AnimatorVariable : VariableBase<Animator>
    {
        // 标记此变量支持的
        //  * 比较操作符
        //  * 运算操作符
        // 在VariableCondition.cs中有使用
        public static readonly CompareOperator[] compareOperators = { CompareOperator.Equals, CompareOperator.NotEquals };
        public static readonly SetOperator[] setOperators = { SetOperator.Assign };

        // 支持的运算
        //  * 只支持相同
        //  * 不同
        public virtual bool Evaluate(CompareOperator compareOperator, Animator value)
        {
            bool condition = false;

            switch (compareOperator)
            {
                case CompareOperator.Equals:
                    condition = Value == value;
                    break;
                case CompareOperator.NotEquals:
                    condition = Value != value;
                    break;
                default:
                    Debug.LogError("The " + compareOperator.ToString() + " comparison operator is not valid.");
                    break;
            }

            return condition;
        }

        // 支持的操作
        //  * 只支持赋值
        public override void Apply(SetOperator setOperator, Animator value)
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
    /// Container for an Animator variable reference or constant value.
    /// </summary>
    [System.Serializable]

    // 封装AnimatorVariable和直接的Animator值
    // 可以直接赋值给Aniamtor
    public struct AnimatorData
    {
        [SerializeField]

        // defaultText: "<Value>"
        // variableTypes: typeof(AnimatorVariable)
        [VariableProperty("<Value>", typeof(AnimatorVariable))]

        // 变量
        public AnimatorVariable animatorRef;
        
        // 不使用AnimatorVariable的时候
        // 直接使用此值
        [SerializeField]
        public Animator animatorVal;

        // 可以直接，转换到Animator
        public static implicit operator Animator(AnimatorData animatorData)
        {
            return animatorData.Value;
        }

        public AnimatorData(Animator v)
        {
            animatorVal = v;
            animatorRef = null;
        }
            
        // 获取值
        //  * 获取变量AnimatorVariable的值
        //  * 直接获取Animator的值
        public Animator Value
        {
            get {
                return (animatorRef == null) ? animatorVal : animatorRef.Value; 
            }
            set {
                if (animatorRef == null) {
                    animatorVal = value; 
                } 
                else {
                    animatorRef.Value = value; 
                } 
            }
        }

        public string GetDescription()
        {
            if (animatorRef == null)
            {
                return animatorVal.ToString();
            }
            else
            {
                return animatorRef.Key;
            }
        }
    }
}
