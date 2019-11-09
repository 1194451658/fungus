// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEngine;
using System;

namespace Fungus
{
    /// <summary>
    /// Standard comparison operators.
    /// </summary>

    // 比较运算
    public enum CompareOperator
    {
        /// <summary> == mathematical operator.</summary>
        Equals,

        /// <summary> != mathematical operator.</summary>
        NotEquals,

        /// <summary> < mathematical operator.</summary>
        LessThan,

        /// <summary> > mathematical operator.</summary>
        GreaterThan,

        /// <summary> <= mathematical operator.</summary>
        LessThanOrEquals,

        /// <summary> >= mathematical operator.</summary>
        GreaterThanOrEquals
    }

    /// <summary>
    /// Mathematical operations that can be performed on variables.
    /// </summary>

    // 运算符号
    public enum SetOperator
    {
        // = 赋值运算
        /// <summary> = operator. </summary>
        Assign,

        //  ! 取反运算
        /// <summary> =! operator. </summary>
        Negate,

        // + 加运算
        /// <summary> += operator. </summary>
        Add,

        // - 减运算
        /// <summary> -= operator. </summary>
        Subtract,

        // * 乘运算
        /// <summary> *= operator. </summary>
        Multiply,

        // / 除运算
        /// <summary> /= operator. </summary>
        Divide
    }

    /// <summary>
    /// Scope types for Variables.
    /// </summary>

    // 变量作用域
    public enum VariableScope
    {
        /// <summary> Can only be accessed by commands in the same Flowchart. </summary>
        Private,
        /// <summary> Can be accessed from any command in any Flowchart. </summary>
        Public,
        /// <summary> Creates and/or references a global variable of that name, all variables of this name and scope share the same underlying fungus variable and exist for the duration of the instance of Unity.</summary>
        Global,
    }

    /// <summary>
    /// Attribute class for variables.
    /// </summary>

    // [VariableInfo] 标签
    // category: ??
    // variableType: ??
    // order: ??
    public class VariableInfoAttribute : Attribute
    {
        public VariableInfoAttribute(string category, string variableType, int order = 0)
        {
            this.Category = category;
            this.VariableType = variableType;
            this.Order = order;
        }
        
        public string Category { get; set; }
        public string VariableType { get; set; }
        public int Order { get; set; }
    }

    /// <summary>
    /// Attribute class for variable properties.
    /// </summary>

    // [VariableProperty]标签
    //  * defaultText: ???
    //  * variableTypes: ???
    // PropertyAttribute
    //  * 是Unity类
    //  * 可以和PropertyDrawer配合使用，自定义类中变量的，inspector显示
    // 
    public class VariablePropertyAttribute : PropertyAttribute 
    {
        // 构造函数1
        public VariablePropertyAttribute (params System.Type[] variableTypes) 
        {
            this.VariableTypes = variableTypes;
        }

        // 构造函数2
        public VariablePropertyAttribute (string defaultText, params System.Type[] variableTypes) 
        {
            this.defaultText = defaultText;
            this.VariableTypes = variableTypes;
        }

        public String defaultText = "<None>";

        public Type[] VariableTypes { get; set; }
    }

    /// <summary>
    /// Abstract base class for variables.
    /// </summary>

    // 变量基类
    // Q: 是挂载在Flowchart上的!
    [RequireComponent(typeof(Flowchart))]
    public abstract class Variable : MonoBehaviour
    {
        // 变量作用域
        [SerializeField] 
        protected VariableScope scope;

        // 变量名称
        [SerializeField] 
        protected string key = "";

        #region Public members

        /// <summary>
        /// Visibility scope for the variable.
        /// </summary>

        // 变量作用域
        public virtual VariableScope Scope {
            get {
                return scope; 
            } 
            set {
                scope = value; 
            } 
        }

        /// <summary>
        /// String identifier for the variable.
        /// </summary>

        // 变量名称
        public virtual string Key {
            get {
                return key; 
            } 
            set {
                key = value; 
            } 
        }

        /// <summary>
        /// Callback to reset the variable if the Flowchart is reset.
        /// </summary>
        public abstract void OnReset();

        #endregion
    }

    /// <summary>
    /// Generic concrete base class for variables.
    /// </summary>

    // 变量基类
    // VariableTypes/* 目录下有各类型变量具体实现
    public abstract class VariableBase<T> : Variable
    {
        //caching mechanism for global static variables

        //
        // 是全局变量的时
        // 访问到这里
        // 运行的时候才起作用
        // 通过FungusManager.Instance.GlobalVariables进行操作
        //
        private VariableBase<T> _globalStaicRef;
        private VariableBase<T> globalStaicRef
        {
            get
            {
                if (_globalStaicRef != null)
                {
                    return _globalStaicRef;
                }
                else if(Application.isPlaying)
                {
                    return _globalStaicRef = FungusManager.Instance.GlobalVariables.GetOrAddVariable(Key, value, this.GetType());
                }
                else
                {
                    return null;
                }
            }
        }

        // 变量值
        [SerializeField] 
        protected T value;
        public virtual T Value
        {
            get
            {
                // 不是Global变量
                // 或者是，没有在运行
                // 直接返回值
                // Q: 为什么是Global的时候，需要通过FungusManager来访问？
                if (scope != VariableScope.Global || !Application.isPlaying)
                {
                    return this.value;
                }
                else
                { 
                    // 是Global变量，
                    // 并且在运行
                    // 通过FungusManager.Instance.GlobalVariables来访问
                    return globalStaicRef.value;
                }
            }

            set
            {
                if (scope != VariableScope.Global || !Application.isPlaying)
                {
                    // 不是Global变量
                    // 或者是，没有在运行
                    // 直接设置值
                    this.value = value;
                }
                else
                {
                    // 是Global变量，并且在运行
                    // 通过FungusManager.Instance.GlobalVariables来访问
                    globalStaicRef.Value = value;
                }
            }
        }

        // 默认初始值
        protected T startValue;

        public override void OnReset()
        {
            Value = startValue;
        }
        
        public override string ToString()
        {
            return Value.ToString();
        }
        
        protected virtual void Start()
        {
            // Remember the initial value so we can reset later on
            startValue = Value;
        }

        public virtual void Apply(SetOperator setOperator, T value) {
            Debug.LogError("Variable doesn't have any operators.");
        }
    }
}
