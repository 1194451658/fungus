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

    // FungusManager创建的时候
    // 会被挂载上
    public class GlobalVariables : MonoBehaviour
    {
        // Q: 为什么还要将变量，保存到FlowChart里？
        private Flowchart holder;

        // Variable:
        //  * Components/Variable.cs中定义
        //  * Variable : MonoBehaviour
        //  定义和管理的变量
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

            // 返回：是否包含此变量
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

                // Q: 这里，为什么还要保存到一个FlowChart里？
                holder.Variables.Add(vAsT);
            }

            return vAsT;
        }
    }
}
