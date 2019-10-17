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

    // 复制运算
    public enum SetOperator
    {
        /// <summary> = operator. </summary>
        Assign,

        /// <summary> =! operator. </summary>
        Negate,

        /// <summary> += operator. </summary>
        Add,

        /// <summary> -= operator. </summary>
        Subtract,

        /// <summary> *= operator. </summary>
        Multiply,

        /// <summary> /= operator. </summary>
        Divide
    }

    /// <summary>
    /// Scope types for Variables.
    /// </summary>

    // 变量
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

    // [VariableInfo]标签
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

    // PropertyAttribute
    //  * 是Unity类
    //  * 可以和PropertyDrawer配合使用，自定义类中变量的，inspector显示
    // 
    // [VariableProperty]标签
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

    // 定义变量？！
    // 是挂载在Flowchart上的!
    [RequireComponent(typeof(Flowchart))]
    public abstract class Variable : MonoBehaviour
    {
        [SerializeField] protected VariableScope scope;

        // 变量名称
        [SerializeField] protected string key = "";

        #region Public members

        /// <summary>
        /// Visibility scope for the variable.
        /// </summary>
        public virtual VariableScope Scope { get { return scope; } set { scope = value; } }

        /// <summary>
        /// String identifier for the variable.
        /// </summary>
        public virtual string Key { get { return key; } set { key = value; } }

        /// <summary>
        /// Callback to reset the variable if the Flowchart is reset.
        /// </summary>
        public abstract void OnReset();

        #endregion
    }

    /// <summary>
    /// Generic concrete base class for variables.
    /// </summary>
    public abstract class VariableBase<T> : Variable
    {
        //
        // 是全局变量的时候
        // 会缓存，向GlobalVariables中的访问？！
        //
        //caching mechanism for global static variables
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

        // 变量的值
        [SerializeField] protected T value;
        public virtual T Value
        {
            get
            {
                if (scope != VariableScope.Global || !Application.isPlaying)
                {
                    // 不是Global变量
                    // 或者是，没有在运行
                    return this.value;
                }
                else
                { 
                    // 是Global变量，并且在运行
                    return globalStaicRef.value;
                }
            }
            set
            {
                if (scope != VariableScope.Global || !Application.isPlaying)
                {
                    // 不是Global变量
                    // 或者是，没有在运行
                    this.value = value;
                }
                else
                {
                    // 是Global变量，并且在运行
                    globalStaicRef.Value = value;
                }
            }
        }

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
