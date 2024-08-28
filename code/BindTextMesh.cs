using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using D = Unit5.WebPlayerDebug;
#else
using D = Unit5.DebugTool;
#endif
using TMPro;

using System;
using System.Collections;
using System.Collections.Generic;

//----------
namespace Unit5
{

//----------
public class BindTextMesh : DataSetter<string>
{
	
	//----------
	[Space(10)]
	[SerializeField] TextMeshPro lblText = null;
	[SerializeField] UILocalizedLabel lblLocalize = null;
	[SerializeField] TweenLabelCounter tweenLabel = null;
	
	[Space(10)]
	[SerializeField] string format;
	
	[Space(10)]
	[SerializeField] float duration = 1.0f;
	[SerializeField] float amountPerDuration = 0.0f;
	[SerializeField] float durationMin = 0.3f;
	[SerializeField] float durationMax = 2.1f;
	
	//----------
	string text;
	
	//----------
	override protected void OnValueChanged(string newValue)
	{
		UpdateText(newValue);
	}
	
	//----------
	void OnLocalize()
	{
		UpdateText(text);
	}
	
	//----------
	void UpdateText(string text)
	{
		if(lblLocalize != null)
		{
			UpdateLocalize(text);
		}
		else if(tweenLabel == null)
		{
			UpdateDirect(text);
		}
		else
		{
			UpdateTween(text);
		}
		
		this.text = text;
	}
	
	//----------
	void UpdateLocalize(string text)
	{
		lblLocalize.text = text;
	}
	void UpdateDirect(string text)
	{
		if(string.IsNullOrEmpty(format))
		{
			lblText.text = text;
		}
		else
		{
			if(format.StartsWith("@"))
			{
				lblText.text = Localization2.Get(format, text);
			}
			else
			{
				lblText.text = string.Format(format, text);
			}
		}
	}
	void UpdateTween(string text)
	{
		int v = (string.IsNullOrEmpty(text) ? 0 : int.Parse(text));
		
		if(isReady)
		{
			float d = (amountPerDuration > 0.0f ? Mathf.Abs(v - tweenLabel.currentCount) / amountPerDuration : 1.0f) * duration;
			if(durationMin > 0.0f)d = Mathf.Max(durationMin, d);
			if(durationMax > 0.0f)d = Mathf.Min(durationMax, d);
			
			tweenLabel.Play(d, tweenLabel.currentCount, v);
		}
		else
		{
			tweenLabel.from = tweenLabel.to = v;
			tweenLabel.Sample(1.0f, true);
			
			isReady = true;
		}
	}
}

}
