using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Grain.Gallery;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

[RequireComponent(typeof(RawImage))]
public class GalleryEntryImageElement : GalleryBaseEntryElement, IGalleryElement
{
	public string Reference { get { return reference; } }

	private AsyncOperationHandle<Texture> m_handle;
	private bool m_addressableLoaded = false;
	private bool m_hasLoaded = false;

	public override Texture Tex()
	{
		if (imageScript != null)
		{
			return imageScript.texture;
		}

		return null;
	}

	private RawImage imageScript;
	
	private void OnDrawGizmosSelected()
	{
		if (string.IsNullOrEmpty(reference))
		{
			reference = "image";
		}
	}

	private void OnDisable()
	{
		Clear();
	}
	
	public void Recieve (string data)
	{
		if (imageScript == null)
		{
			imageScript = GetComponent<RawImage> ();
		}

		Clear();
		
		if (string.IsNullOrEmpty(data))
		{
			return;
		}

		if (imageScript != null)
        {
	        AspectRatioFitter ratio = null;
			
			if (GetComponentsInChildren<AspectRatioFitter>(true).Length > 0)
			{
				ratio = GetComponentsInChildren<AspectRatioFitter>(true)[0];
			}
			
			bool setAspect = false;

			if (loadType.Equals(FolderSource._Resource))
			{
				string[] split = data.Split('/');
				string file = Path.GetFileNameWithoutExtension(data);

				split[split.Length - 1] = file;
				file = "";
				
				for (int i = 0; i < split.Length; i++)
				{
					file += split[i] + ((i < split.Length - 1) ? "/" : "");
				}
				
				imageScript.texture = Resources.Load(file) as Texture2D;

				if (imageScript.texture != null)
				{
					imageScript.SetNativeSize();

					setAspect = true;

					m_hasLoaded = true;
				
					imageScript.CrossFadeAlpha(1.0f, 0.5f, true);
				}
			}
			else if (loadType.Equals(FolderSource._StreamingAsset))
			{
				string path = "";
				
				if (!data.Contains(Application.streamingAssetsPath))
				{
					path = Application.streamingAssetsPath + "/" + data;
				}
				else
				{
					path = data;
				}
				
				StartCoroutine(WebRequest(path));

			}
			else if (loadType.Equals(FolderSource._Addressable))
			{
				Addressables.LoadAssetAsync<Texture>(data).Completed += OnAddessableLoadComplete;
			}
			else 
			{
                StartCoroutine(WebRequest(data));
			}
			
			if (ratio != null && setAspect)
			{
				float texWidth = imageScript.texture.width;
				float texHeight = imageScript.texture.height;
				float aspectRatio = texWidth / texHeight;
				ratio.aspectRatio = aspectRatio;
			}
		}
	}
	public void Recieve (IResourceLocation resourceLocation)
	{
		if (imageScript == null)
		{
			imageScript = GetComponent<RawImage> ();
		}

		Clear();
		
		if (string.IsNullOrEmpty(resourceLocation.InternalId))
		{
			return;
		}

		if (imageScript != null)
		{
			AspectRatioFitter ratio = null;
			
			if (GetComponentsInChildren<AspectRatioFitter>(true).Length > 0)
			{
				ratio = GetComponentsInChildren<AspectRatioFitter>(true)[0];
			}
			
			bool setAspect = false;

			Addressables.LoadAssetAsync<Texture>(resourceLocation).Completed += OnAddessableLoadComplete;
			
			if (ratio != null && setAspect)
			{
				float texWidth = imageScript.texture.width;
				float texHeight = imageScript.texture.height;
				float aspectRatio = texWidth / texHeight;
				ratio.aspectRatio = aspectRatio;
			}
		}
	}

	public void Clear()
	{
		StopAllCoroutines();

		m_hasLoaded = false;

		if (imageScript == null)
		{
			imageScript = GetComponent<RawImage>();
		}

		if (loadType.Equals(FolderSource._Addressable))
		{
			if (m_addressableLoaded)
			{
				Resources.UnloadAsset(m_handle.Result);
				Addressables.Release(m_handle);
			}
		}

		m_addressableLoaded = false;

		imageScript.CrossFadeAlpha(0.0f, 0.0f, true);

		if (!loadType.Equals(FolderSource._Resource))
		{
			imageScript.texture = null;
			DestroyImmediate(imageScript.texture);
		}
		else
		{
			imageScript.texture = null;
		}

		Resources.UnloadUnusedAssets();
	}

	private void OnAddessableLoadComplete(AsyncOperationHandle<Texture> handle)
	{
		if (handle.Status == AsyncOperationStatus.Succeeded)
		{
			imageScript.texture = handle.Result;
			m_handle = handle;
			imageScript.SetNativeSize();
			m_addressableLoaded = true;
			m_hasLoaded = true;
		}
		
		AspectRatioFitter ratio = null;

		if (GetComponentsInChildren<AspectRatioFitter>(true).Length > 0)
		{
			ratio = GetComponentsInChildren<AspectRatioFitter>(true)[0];
		}
        
		if (ratio != null)
		{
			float texWidth = imageScript.texture.width;
			float texHeight = imageScript.texture.height;
			float aspectRatio = texWidth / texHeight;
			ratio.aspectRatio = aspectRatio;
		}
		
		if (imageScript.texture != null)
		{
			imageScript.CrossFadeAlpha(1.0f, 0.5f, true);
		}
	}

	public void Resize()
    {
		if(imageScript != null)
        {
			if(m_hasLoaded)
            {
				AspectRatioFitter ratio = null;

				if (GetComponentsInChildren<AspectRatioFitter>(true).Length > 0)
				{
					ratio = GetComponentsInChildren<AspectRatioFitter>(true)[0];
				}

				if (ratio != null)
				{
					float texWidth = imageScript.texture.width;
					float texHeight = imageScript.texture.height;
					float aspectRatio = texWidth / texHeight;
					ratio.aspectRatio = aspectRatio;
				}
			}
        }
    }

	private IEnumerator WebRequest(string data)
	{
		bool check = false;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        check = true;
#elif !UNITY_EDITOR && UNITY_IOS
        check = true;
#endif
		if (check)
		{
			if (!data.Contains("file://"))
			{
				data = "file://" + data;
			}
		}

		UnityWebRequest request = UnityWebRequestTexture.GetTexture(data, true);

        yield return request.SendWebRequest();

        if(request.result != UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(request.error))
        {
            imageScript.texture = DownloadHandlerTexture.GetContent(request);
        }

        request.Dispose();

		m_hasLoaded = true;
        
        AspectRatioFitter ratio = null;
			
        if (GetComponentsInChildren<AspectRatioFitter>(true).Length > 0)
        {
	        ratio = GetComponentsInChildren<AspectRatioFitter>(true)[0];
        }
        
        if (ratio != null)
        {
	        float texWidth = imageScript.texture.width;
	        float texHeight = imageScript.texture.height;
	        float aspectRatio = texWidth / texHeight;
	        ratio.aspectRatio = aspectRatio;
        }

        if (imageScript.texture != null)
        {
	        imageScript.CrossFadeAlpha(1.0f, 0.5f, true);
        }
        else
        {
			Resources.UnloadUnusedAssets();
		}
	}
}
