using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using Grain.Gallery;
using UnityEditor;
using System.Xml.Linq;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.Video;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GalleryAppender : MonoBehaviour
{
	[SerializeField]
	private Gallery append;

	[SerializeField]
	private FolderSource root = FolderSource._Resource;

	[SerializeField]
	private AppendType method = AppendType._Manual;

	[SerializeField]
	private TextAsset textAsset;

	[SerializeField]
	private string[] assetGroupLabels;
	
	[SerializeField]
	private UnityEngine.Object[] assetToConvert;

	[SerializeField]
	private string runtimeFolder = "";

    [SerializeField]
	private List<GEntry> entries = new List<GEntry>();

	private bool hasInit = false;
	
	private void Awake()
	{
		assetToConvert = new UnityEngine.Object[0];
		textAsset = null;
		
		Initialise();
	}

	public void Initialise()
	{
		if (hasInit) return;
		
		hasInit = true;
		
		if(method.Equals(AppendType._RuntimeFolder) || method.Equals(AppendType._AddressableGroup))
		{
			entries.Clear();
		}
		
		for (int i = 0; i < entries.Count; i++)
		{
			if (root.Equals(FolderSource._StreamingAsset))
			{
				entries[i].elements[0].data = Application.streamingAssetsPath + "/" + entries[i].elements[0].data;
			}
			else if(root.Equals(FolderSource._DataPath))
			{
				int legnth = Application.cloudProjectId.Length + 6;
				
#if UNITY_EDITOR
				entries[i].elements[0].data = Application.dataPath.Substring(0, Application.dataPath.Length - 7) + "/" + entries[i].elements[0].data;
#elif UNITY_STANDALONE
				entries[i].elements[0].data = Application.dataPath.Substring(0, Application.dataPath.Length - legnth) + "/" + entries[i].elements[0].data;
#endif
			}
			else if (root.Equals(FolderSource._PersistantDataPath))
			{
				entries[i].elements[0].data = Application.persistentDataPath + "/" + entries[i].elements[0].data;
			}
			else
			{
				//entries[i].elements[0].data = "/" + entries[i].elements[0].data;	
			}
		}
	}

	public void Append()
	{
		if (append == null) return;

		append.LoadType = root;

		for (int i = 0; i < entries.Count; i++)
		{
			entries[i].id = entries[i].id + "_" + i.ToString();
		}

		GEntryGroup eGroup = new GEntryGroup();
		eGroup.id = append.ID;
		eGroup.entries = new List<GEntry>();

		if (method.Equals(AppendType._RuntimeFolder) || method.Equals(AppendType._AddressableGroup))
		{
			entries.Clear();
			string dir = "";
			bool isResourcesFolder = false;
			string[] files = null;
			bool isAddressable = false;
			UnityEngine.Object[] temp = null;

			if (root.Equals(FolderSource._StreamingAsset))
			{
				dir = Application.streamingAssetsPath + "/" + runtimeFolder;
			}
			else if(root.Equals(FolderSource._DataPath))
			{
				dir = Application.dataPath + "/" + runtimeFolder;
			}
			else if (root.Equals(FolderSource._PersistantDataPath))
			{
				dir = Application.persistentDataPath + "/" + runtimeFolder;
			}
			else if (root == FolderSource._Addressable)
			{
				isAddressable = true;
			}
			else
			{
				isResourcesFolder = true;
				dir = runtimeFolder;
			}

			if (isAddressable)
			{
				StartCoroutine(AsyncFindResources(eGroup));
				return;
			}
			else
			{
				if (isResourcesFolder)
				{
					temp = Resources.LoadAll<UnityEngine.Object>(dir);

					files = new string [temp.Length];

					for (int i = 0; i < temp.Length; i++)
					{
						files[i] = runtimeFolder + temp[i].name;
					}
				}
				else
				{
					files = Directory.GetFiles(dir);
				}

				for (int i = 0; i < files.Length; i++)
				{
					if(files[i].Contains(".meta")) continue;
				
					GEntry ge = new GEntry();
					ge.id = files[i];
					ge.elements = new List<GEntryElement>();

					if(temp != null)
                    {
						if (temp[i] is VideoClip)
						{
							ge.elements.Add(new GEntryElement("video", files[i]));
						}
						else
						{
							ge.elements.Add(new GEntryElement("image", files[i]));
						}
					}
					else
                    {
						if (IsVideo(Path.GetExtension(files[i])))
						{
							ge.elements.Add(new GEntryElement("video", files[i]));
						}
						else
						{
							ge.elements.Add(new GEntryElement("image", files[i]));
						}
					}

					entries.Add(ge);
				}
			}
		}

		entries.ForEach (e => eGroup.entries.Add(e));

		if(append != null)
		{
			append.Append(JsonUtility.ToJson(eGroup));
        }
	}

	private IEnumerator AsyncFindResources(GEntryGroup eGroup)
	{
		List<string> keys = assetGroupLabels.ToList();

		AsyncOperationHandle<IList<IResourceLocation>> handle = Addressables.LoadResourceLocationsAsync(keys, Addressables.MergeMode.Union);

		yield return handle;

		if (handle.Status == AsyncOperationStatus.Succeeded)
		{
			for (int i = 0; i < handle.Result.Count; i++)
			{
				string file = System.IO.Path.GetFileName(handle.Result[i].ToString());
				string extension = System.IO.Path.GetExtension(file);
				string fileWithoutExtension = file.Replace(extension, "");

				GEntry ge = new GEntry();
				ge.id = fileWithoutExtension;
				ge.elements = new List<GEntryElement>();

				if (IsVideo(extension))
				{
					ge.elements.Add(new GEntryElement("video", handle.Result[i].ToString()));
				}
				else
				{
					ge.elements.Add(new GEntryElement("image", handle.Result[i].ToString()));
				}

				entries.Add(ge);

			}

			entries.ForEach(e => eGroup.entries.Add(e));

			if (append != null)
			{
				append.Append(JsonUtility.ToJson(eGroup));
			}
		}

		Addressables.Release(handle);
	}

	public void EditorTool()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			if (append == null) return;
		
			entries.Clear();
			
			switch (method)
			{
				case AppendType._TextAsset:
					ReadXML();
					break;
				case AppendType._RuntimeFolder:
					break;
				case AppendType._AddressableGroup:
					break;
				case AppendType._AddressableAsset:
					
					for (int i = 0; i < assetToConvert.Length; i++)
					{
						GEntry ge = new GEntry();
						ge.id = assetToConvert[i].name;
						ge.elements = new List<GEntryElement>();

						bool add = true;
						
						if (IsAssetAddressable(assetToConvert[i]))
						{
							if(assetToConvert[i] is Texture)
							{
								ge.elements.Add(new GEntryElement("image", GetAddressablePath(assetToConvert[i])));
							}
							else if(assetToConvert[i] is VideoClip)
							{
								ge.elements.Add(new GEntryElement("video", GetAddressablePath(assetToConvert[i])));
							}
							else
							{
								add = false;
							}
						}
						else
						{
							add = false;
						}

						if(add) entries.Add(ge);
					}
					
					assetToConvert = new UnityEngine.Object[0];
					
					break;
				case AppendType._AssetConversion:

					for (int i = 0; i < assetToConvert.Length; i++)
					{
						GEntry ge = new GEntry();
						ge.id = assetToConvert[i].name;
						ge.elements = new List<GEntryElement>();
					
						string path = AssetDatabase.GetAssetPath(assetToConvert[i]);
						string extension = Path.GetExtension(path);

						bool add = true;

						if (IsImage(extension))
						{
							ge.elements.Add(new GEntryElement("image", runtimeFolder + assetToConvert[i].name + extension));
						}
						else if(IsVideo(extension))
						{
							ge.elements.Add(new GEntryElement("video", runtimeFolder + assetToConvert[i].name + extension));
						}
						else
						{
							add = false;
						}

						if(add) entries.Add(ge);
					}
					
					assetToConvert = new UnityEngine.Object[0];
					
					break;
				default:
					break;
			}
		}
#endif
	}

	private bool IsImage(string extension)
	{
		if (extension.ToLower().Contains("jpg") || extension.ToLower().Contains("jpeg") ||
		    extension.ToLower().Contains("png"))
		{
			return true;
		}

		return false;
	}
	
	private bool IsVideo(string extension)
	{
		if (extension.ToLower().Contains("mp4") || extension.ToLower().Contains("mov") ||
		    extension.ToLower().Contains("webm") )
		{
			return true;
		}

		return false;
	}

#if UNITY_EDITOR
	private bool IsAssetAddressable(UnityEngine.Object obj)
	{
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)));
		return entry != null;
	}
	
	private string GetAddressablePath(UnityEngine.Object obj)
	{
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)));

		if (entry != null)
		{
			return entry.AssetPath;
		}

		return "";
	}
#endif

	private void ReadXML()
	{
		if (textAsset == null) return;

		var doc = XDocument.Parse(textAsset.text);

		entries.Clear();

		foreach (XElement e in doc.Elements())
		{
			foreach (XElement inner in e.Nodes())
			{
				GEntry ge = new GEntry();
				ge.elements = new List<GEntryElement>();

				foreach (XElement entry in inner.Nodes())
				{
					if (entry.Name.ToString().Equals("ID"))
					{
						ge.id = entry.Value.ToString();
					}
					else if (entry.Name.ToString().Equals("Elements"))
					{
						foreach (XElement vals in entry.Nodes())
						{
							GEntryElement gee = new GEntryElement("", "");

							foreach (XElement eleInner in vals.Nodes())
							{
								if (eleInner.Name.ToString().Equals("Name"))
								{
									gee.id = eleInner.Value.ToString();
								}
								else
								{
									gee.data = eleInner.Value.ToString();
								}
							}

							ge.elements.Add(gee);
						}
					}
				}

				entries.Add(ge);
			}
		}
	}
}
