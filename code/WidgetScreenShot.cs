using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using D = Unit5.WebPlayerDebug;
#else
using D = Unit5.DebugTool;
#endif

using System;
using System.Collections;
using System.Collections.Generic;

//----------
namespace Unit5
{

//----------
sealed public class WidgetScreenShot : MonoBehaviour
{
	
	//----------
	public enum CropMode
	{
		Option = 0,
		Area,
	}
	
	//----------
	[Space(10)]
	[SerializeField] Camera[] cameras = null;
	
	[Space(10)]
	[SerializeField] [DelayedAttribute] int size = 320;
	
	[Space(10)]
	[SerializeField] CropMode cropMode = CropMode.Option;
	[SerializeField] ScreenShotOption cropOption = null;
	[SerializeField] SpriteRenderer cropArea = null;
	
	[Space(10)]
	[SerializeField] Texture2D overlay = null;
	[SerializeField] ScreenShotOption merge = null;
	
	//----------
	[NonSerialized] [HideInInspector] public Texture2D lastScreenShot;
	
	//----------
	public bool isEnabled => (!cameras.IsNullOrEmpty());
	
	public ScreenShotOption crop => (cropMode == CropMode.Option ? cropOption : CalcArea());
	ScreenShotOption CalcArea()
	{
		ScreenShotOption option = (cropOption == null ? new ScreenShotOption() : cropOption.Clone());
		if(cropArea == null)return option;
		
		float fh = Camera.main.orthographicSize * 2f;
		float fw = fh / Camera.main.pixelHeight * Camera.main.pixelWidth;
		float iw = cropArea.bounds.size.x;
		float ih = cropArea.bounds.size.y;
		
		option.anchorX = AnchorHorizontal.Left;
		option.anchorY = AnchorVertical.Top;
		option.width = iw / fw;
		option.height = ih / fh;
		option.offsetX = ((fw - iw) * 0.5f - (Camera.main.transform.position.x - cropArea.bounds.center.x)) / fw;
		option.offsetY = ((fh - ih) * 0.5f - (Camera.main.transform.position.y - cropArea.bounds.center.y)) / fh;
		return option;
	}
	
	//----------
	void OnDestroy()
	{
		Clear();
	}
	
	//----------
	public void Clear()
	{
		if(lastScreenShot != null)GameObject.Destroy(lastScreenShot);
		lastScreenShot = null;
	}
	
	//----------
	public Texture2D TakeScreenShot()
	{
		lastScreenShot = TakeCroppedScreenShot(Screen.width, Screen.height);
		
		return lastScreenShot;
	}
	
	//----------
	public Texture2D TakeCroppedScreenShot(int width, int height)
	{
		int w = size;
		int h = Mathf.RoundToInt(size * (height / (float)width));
		
		return TakeCroppedScreenShot(w, h, crop);
	}
	public Texture2D TakeCroppedScreenShot(int width, int height, ScreenShotOption option)
	{
		Texture2D image = CaptureFrame(width, height);
		if(option.width == 1.0f && option.height == 1.0f && option.offsetX == 0.0f && option.offsetY == 0.0f)
		{
			if(overlay != null)
			{
				MergeOverlay(ref image);
			}
			
			return image;
		}
		
		Texture2D result = CropImage(ref image, option);
		
#if UNITY_EDITOR
		DestroyImmediate(image);
#else
		Destroy(image);
#endif
		
		if(overlay != null)
		{
			MergeOverlay(ref result);
		}
		
		return result;
	}
	
	//----------
	public Texture2D CaptureFrame(int width, int height)
	{
		RenderTexture rt = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
		foreach(Camera cam in cameras)
		{
			RenderTexture tt = cam.targetTexture;
			cam.targetTexture = rt;
			cam.Render();
			cam.targetTexture = tt;
		}
		RenderTexture.active = rt;
		
		Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
		image.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
		image.Apply();
		
		RenderTexture.active = null;
#if UNITY_EDITOR
		DestroyImmediate(rt);
#else
		Destroy(rt);
#endif
		
		return image;
	}
	
	public Texture2D CropImage(ref Texture2D source, ScreenShotOption option)
	{
		Rect rect = option.Crop(source.width, source.height);
		
		Texture2D result = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
		result.SetPixels(source.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height));
		result.Apply();
		
		return result;
	}
	
	public Texture2D ResizeImage(ref Texture2D source, int width, int height)
	{
		Texture2D result = new Texture2D(width, height, source.format, false);
		
		float w = (float)width;
		float h = (float)height;
		
		for(int y = 0; y < result.height; y += 1)
		{
			for(int x = 0; x < result.width; x += 1)
			{
				result.SetPixel(x, y, source.GetPixelBilinear(x / w, y / h));
			}
		}
		result.Apply();
		return result;
	}
	
	public void MergeOverlay(ref Texture2D source)
	{
		MergeOverlay(ref source, merge);
	}
	public void MergeOverlay(ref Texture2D source, ScreenShotOption option)
	{
		int ow = source.width;
		int oh = Mathf.FloorToInt(source.width * (overlay.height / (float)overlay.width));
		
		Rect rect = option.Place(source.width, source.height, ow, oh);
		ow = (int)rect.width;
		oh = (int)rect.height;
		
		Texture2D resizedOverlay = ResizeImage(ref overlay, ow, oh);
		
		int sx = Mathf.Max(0, Mathf.Min(source.width - ow, (int)rect.x));
		int sy = Mathf.Max(0, Mathf.Min(source.height - oh, (int)rect.y));
		int sw = Mathf.Min(source.width, sx + ow);
		int sh = Mathf.Min(source.height, sy + oh);
		
		int px;
		int py;
		Color sourceColor;
		Color overlayColor;
		Color finalColor;
		for(int x = sx; x < sw; x += 1)
		{
			for(int y = sy; y < sh; y += 1)
			{
				px = x - sx;
				py = y - sy;
				
				sourceColor = source.GetPixel(x, y);
				overlayColor = resizedOverlay.GetPixel(px, py);
				finalColor = Color.Lerp(sourceColor, overlayColor, overlayColor.a / 1.0f);
				source.SetPixel(x, y, finalColor);
			}
		}
		
		source.Apply();
		
#if UNITY_EDITOR
		DestroyImmediate(resizedOverlay);
#else
		Destroy(resizedOverlay);
#endif
	}
}

}
