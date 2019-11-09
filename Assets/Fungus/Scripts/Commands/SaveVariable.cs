// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEngine;

namespace Fungus
{
    /// <summary>
    /// Save an Boolean, Integer, Float or String variable to persistent storage using a string key.
    /// The value can be loaded again later using the Load Variable command. You can also 
    /// use the Set Save Profile command to manage separate save profiles for multiple players.
    /// </summary>
    [CommandInfo("Variable", 
                 "Save Variable", 
                 "Save an Boolean, Integer, Float or String variable to persistent storage using a string key. " +
                 "The value can be loaded again later using the Load Variable command. You can also " +
                 "use the Set Save Profile command to manage separate save profiles for multiple players.")]
    [AddComponentMenu("")]

    // 保存一个变量，到持久化存储
    public class SaveVariable : Command
    {
        [Tooltip("Name of the saved value. Supports variable substition e.g. \"player_{$PlayerNumber}")]
        [SerializeField] 

        // 要保存的key
        protected string key = "";
        
        [Tooltip("Variable to read the value from. Only Boolean, Integer, Float and String are supported.")]
        [VariableProperty(typeof(BooleanVariable),
                          typeof(IntegerVariable), 
                          typeof(FloatVariable), 
                          typeof(StringVariable))]
        [SerializeField] 

        // 要保存的变量
        protected Variable variable;

        #region Public members

        public override void OnEnter()
        {
            // 如果没有key
            // 没有变量
            // 返回
            if (key == "" ||
                variable == null)
            {
                Continue();
                return;
            }
            
            var flowchart = GetFlowchart();
            
            // Prepend the current save profile (if any)

            // 从SetSaveProfile命令中，获取设置的前缀
            // key: 中也支持变量展开
            string prefsKey = SetSaveProfile.SaveProfile + "_" + flowchart.SubstituteVariables(key);
            
            System.Type variableType = variable.GetType();

            //
            // 使用PlayerPrefs保存变量的值
            //

            // bool类型
            if (variableType == typeof(BooleanVariable))
            {
                BooleanVariable booleanVariable = variable as BooleanVariable;
                if (booleanVariable != null)
                {
                    // PlayerPrefs does not have bool accessors, so just use int
                    PlayerPrefs.SetInt(prefsKey, booleanVariable.Value ? 1 : 0);
                }
            }

            // int类型
            else if (variableType == typeof(IntegerVariable))
            {
                IntegerVariable integerVariable = variable as IntegerVariable;
                if (integerVariable != null)
                {
                    PlayerPrefs.SetInt(prefsKey, integerVariable.Value);
                }
            }

            // float类型
            else if (variableType == typeof(FloatVariable))
            {
                FloatVariable floatVariable = variable as FloatVariable;
                if (floatVariable != null)
                {
                    PlayerPrefs.SetFloat(prefsKey, floatVariable.Value);
                }
            }
            // string类型
            else if (variableType == typeof(StringVariable))
            {
                StringVariable stringVariable = variable as StringVariable;
                if (stringVariable != null)
                {
                    PlayerPrefs.SetString(prefsKey, stringVariable.Value);
                }
            }
            
            Continue();
        }
        
        public override string GetSummary()
        {
            if (key.Length == 0)
            {
                return "Error: No stored value key selected";
            }
            
            if (variable == null)
            {
                return "Error: No variable selected";
            }
            
            return variable.Key + " into '" + key + "'";
        }
        
        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }

        public override bool HasReference(Variable in_variable)
        {
            return this.variable == in_variable || base.HasReference(in_variable);
        }

        #endregion
        #region Editor caches
#if UNITY_EDITOR
        protected override void RefreshVariableCache()
        {
            base.RefreshVariableCache();

            var f = GetFlowchart();

            f.DetermineSubstituteVariables(key, referencedVariables);
        }
#endif
        #endregion Editor caches
    }
}
