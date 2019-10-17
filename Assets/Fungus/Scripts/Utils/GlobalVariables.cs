// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEngine;
using System.Collections.Generic;
using System;

namespace Fungus
{
    /// <summary>
    /// Storage for a collection of fungus variables that can then be accessed globally.
    /// </summary>
    public class GlobalVariables : MonoBehaviour
    {
        private Flowchart holder;

        Dictionary<string, Variable> variables = new Dictionary<string, Variable>();

        void Awake()
        {
            // 创建一个叫GlobalVariables的go
            // 上面还挂着Flowchart
            holder = new GameObject("GlobalVariables").AddComponent<Flowchart>();
            holder.transform.parent = transform;
        }

        // 获取变量
		public Variable GetVariable(string variableKey)
		{
			Variable v = null;
			variables.TryGetValue(variableKey, out v);
			return v;
		}

        // 获取，或添加变量
        public VariableBase<T> GetOrAddVariable<T>(string variableKey, T defaultvalue, Type type)
        {
            Variable v = null;
            VariableBase<T> vAsT = null;
            var res = variables.TryGetValue(variableKey, out v);

            // 有找到变量
            if(res && v != null)
            {
                vAsT = v as VariableBase<T>;

                if (vAsT != null)
                {
                    return vAsT;
                }
                else
                {
                    Debug.LogError("A fungus variable of name " + variableKey + " already exists, but of a different type");
                }
            }
            else
            {
                // 没有找到变量
                // 创建变量
                // 添加到GlobalVariables的Flowchart中
                //create the variable
                vAsT = holder.gameObject.AddComponent(type) as VariableBase<T>;
                vAsT.Value = defaultvalue;
                vAsT.Key = variableKey;
                vAsT.Scope = VariableScope.Public;
                variables[variableKey] = vAsT;
                holder.Variables.Add(vAsT);
            }

            return vAsT;
        }
    }
}
