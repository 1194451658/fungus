// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEngine;
using System.Collections.Generic;

namespace Fungus
{
    /// <summary>
    /// Create custom tags for use in Say text.
    /// </summary>
    [ExecuteInEditMode]

    // 自定义Tag设置
    public class CustomTag : MonoBehaviour
    {
        [Tooltip("String that defines the start of the tag.")]
        [SerializeField] protected string tagStartSymbol;

        [Tooltip("String that defines the end of the tag.")]
        [SerializeField] protected string tagEndSymbol;

        [Tooltip("String to replace the start tag with.")]
        [SerializeField] protected string replaceTagStartWith;

        [Tooltip("String to replace the end tag with.")]
        [SerializeField] protected string replaceTagEndWith;

        // 添加到，全局列表
        protected virtual void OnEnable()
        {
            if (!activeCustomTags.Contains(this))
            {
                activeCustomTags.Add(this);
            }
        }
        
           // 从全局列表，移除
        protected virtual void OnDisable()
        {
            activeCustomTags.Remove(this);
        }

        #region Public members

        // 全局列表
        public static List<CustomTag> activeCustomTags = new List<CustomTag>();

        /// <summary>
        /// String that defines the start of the tag.
        /// </summary>
        public virtual string TagStartSymbol { get { return tagStartSymbol; } }

        /// <summary>
        /// String that defines the end of the tag.
        /// </summary>
        public virtual string TagEndSymbol { get { return tagEndSymbol; } }

        /// <summary>
        /// String to replace the start tag with.
        /// </summary>
        public virtual string ReplaceTagStartWith { get { return replaceTagStartWith; } }

        /// <summary>
        /// String to replace the end tag with.
        /// </summary>
        public virtual string ReplaceTagEndWith { get { return replaceTagEndWith; } }

        #endregion
    }
}
