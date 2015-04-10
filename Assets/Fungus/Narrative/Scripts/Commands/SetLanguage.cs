﻿using UnityEngine;
using System.Collections;

namespace Fungus
{
	[CommandInfo("Narrative", 
	             "Set Language", 
	             "Set the active language for the scene. A Localization object with a localization file must be present in the scene.")]
	[AddComponentMenu("")]
	public class SetLanguage : Command 
	{
		public string languageCode; 

		public override void OnEnter()
		{
			Localization localization = GameObject.FindObjectOfType<Localization>();
			if (localization != null)
			{
				localization.SetActiveLanguage(languageCode);
			}

			Continue();
		}

		public override string GetSummary()
		{
			return languageCode;
		}

		public override Color GetButtonColor()
		{
			return new Color32(184, 210, 235, 255);
		}
	}
}