﻿using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

namespace Fungus
{
	[CommandInfo("Narrative", 
	             "Set Language", 
	             "Set the active language for the scene. A Localization object with a localization file must be present in the scene.")]
	[AddComponentMenu("")]
	[ExecuteInEditMode]
	public class SetLanguage : Command
	{
		[Tooltip("Code of the language to set. e.g. ES, DE, JA")]
		public StringData _languageCode = new StringData(); 

		public static string mostRecentLanguage = "";

		public override void OnEnter()
		{
			Localization localization = GameObject.FindObjectOfType<Localization>();
			if (localization != null)
			{
				localization.SetActiveLanguage(_languageCode.Value, true);

				// Cache the most recently set language code so we can continue to 
				// use the same language in subsequent scenes.
				mostRecentLanguage = _languageCode.Value;
			}

			Continue();
		}

		public override string GetSummary()
		{
			return _languageCode.Value;
		}

		public override Color GetButtonColor()
		{
			return new Color32(184, 210, 235, 255);
		}

		#region Backwards compatibility

		[HideInInspector] [FormerlySerializedAs("languageCode")] public string languageCodeOLD;

		protected virtual void OnEnable()
		{
			if (languageCodeOLD != "")
			{
				_languageCode.Value = languageCodeOLD;
				languageCodeOLD = "";
			}
		}

		#endregion
	}
}