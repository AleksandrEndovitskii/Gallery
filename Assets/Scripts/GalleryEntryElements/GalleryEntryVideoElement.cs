using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Grain.Gallery;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.ResourceManagement.AsyncOperations;

[RequireComponent(typeof(VideoPlayer))]
[RequireComponent(typeof(RawImage))]
public class GalleryEntryVideoElement : GalleryBaseEntryElement, IGalleryElement
{
    [SerializeField]
    private bool loop = false;

    [SerializeField]
    private GameObject unableToPlay;

    [SerializeField]
    private Color unableToPlayColor = Color.black;

    private VideoPlayer vPlayer;
    private RawImage vOutput;
    private bool hasInit = false;
    
    public string Reference { get { return reference; } }
    public bool VideoExists { get; private set; }

    private bool videoReady = false;
    private bool hasEnded = false;

    public System.Action OnLoopPointReached;
    public System.Action<VideoPlayer> OnClicked;

    private AsyncOperationHandle<VideoClip> m_handle;
    private bool m_addressableLoaded = false;
    private bool m_hasFailed = false;
    
    public override Texture Tex()
    {
        if (vOutput != null)
        {
            return vOutput.texture;
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        if (string.IsNullOrEmpty(reference))
        {
            reference = "video";
        }
    }
    
    private void Awake()
    {
        Initialise();
    }

    private void OnDisable()
    {
        Clear();
    }

    public void Recieve(string data)
    {
        Initialise();
        Clear();

        if (string.IsNullOrEmpty(data))
        {
            VideoExists = false;
            return;
        }

        VideoExists = true;
        videoReady = false;

        if (data.Contains("loop") || data.Contains("LOOP") || data.Contains("Loop"))
        {
            vPlayer.isLooping = true;
        }
        else
        {
            vPlayer.isLooping = loop;
        }

        if (data.Contains("file://"))
        {
            data = data.Replace("file://", "");
        }
        
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
            
            vPlayer.source = VideoSource.VideoClip;

            vPlayer.clip = Resources.Load(file) as VideoClip;

            if (vPlayer.clip != null)
            {
                StartCoroutine(Prepare());
            }
            else
            {
                m_hasFailed = true;

                if (vOutput != null)
                {
                    vOutput.color = unableToPlayColor;
                }

                if (unableToPlay != null)
                {
                    unableToPlay.SetActive(true);
                }
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

            vPlayer.source = VideoSource.Url;

            if (File.Exists(path))
            {
                vPlayer.url = path;
                StartCoroutine(Prepare());
            }
            else
            {
                m_hasFailed = true;

                if (vOutput != null)
                {
                    vOutput.color = unableToPlayColor;
                }

                if (unableToPlay != null)
                {
                    unableToPlay.SetActive(true);
                }
            }
        }
        else if (loadType.Equals(FolderSource._Addressable))
        {
            vPlayer.source = VideoSource.VideoClip;
            
            Addressables.LoadAssetAsync<VideoClip>(data).Completed += OnAddessableLoadComplete;
        }
        else 
        {
            vPlayer.source = VideoSource.Url;

            if (File.Exists(data))
            {
                vPlayer.url = data;
                StartCoroutine(Prepare());
            }
            else
            {
                m_hasFailed = true;

                if (vOutput != null)
                {
                    vOutput.color = unableToPlayColor;
                }

                if (unableToPlay != null)
                {
                    unableToPlay.SetActive(true);
                }
            }
        }
    }

    public void Clear()
    {
        if (vPlayer == null) return;
        
        vPlayer.Stop();

        if(loadType.Equals(FolderSource._Addressable))
        {
            if (m_addressableLoaded)
            {
                Resources.UnloadAsset(m_handle.Result);
                Addressables.Release(m_handle);
            }
        }

        m_addressableLoaded = false;

        vPlayer.clip = null;
        vOutput.texture = null;
        vOutput.color = Color.white;

        Resources.UnloadUnusedAssets();

        vOutput.CrossFadeAlpha(0.0f,0.0f, true);

        m_hasFailed = false;

        if (unableToPlay != null)
        {
            unableToPlay.SetActive(false);
        }
    }

    public void OnClick()
    {
        if (m_hasFailed) return;

        if (vPlayer != null)
        {
            if (videoReady)
            {
                if (hasEnded)
                {
                    if (OnClicked != null)
                    {
                        OnClicked.Invoke(vPlayer);
                    }
                    
                    Refresh();

                    return;
                }
                
                if (vPlayer.isPlaying)
                {
                    vPlayer.Pause();
                }
                else
                {
                    vPlayer.Play();
                }

                if (OnClicked != null)
                {
                    OnClicked.Invoke(vPlayer);
                }
            }
            else
            {
                StartCoroutine(Prepare());
            }
        }
    }

    private void Initialise()
    {
        if (hasInit) return;

        hasInit = true;
        
        vPlayer = GetComponent<VideoPlayer>();
        vOutput = GetComponent<RawImage>();
        
        vPlayer.loopPointReached += LoopPointReached;
        vPlayer.renderMode = VideoRenderMode.APIOnly;
        vPlayer.playOnAwake = false;

        vOutput.CrossFadeAlpha(0.0f,0.0f, true);
    }
    
    private void OnAddessableLoadComplete(AsyncOperationHandle<VideoClip> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            vPlayer.clip = handle.Result;
            m_handle = handle;
            m_addressableLoaded = true;

            StartCoroutine(Prepare());
        }
        else
        {
            m_hasFailed = true;

            if(vOutput != null)
            {
                vOutput.color = unableToPlayColor;
            }

            if (unableToPlay != null)
            {
                unableToPlay.SetActive(true);
            }
        }
    }
    
    private void LoopPointReached(VideoPlayer p)
    {
        if (!loop)
        {
            hasEnded = true;
        }

        if (OnLoopPointReached != null)
        {
            OnLoopPointReached.Invoke();
        }
    }

    public void Refresh(int current = 0)
    {
        vPlayer.Pause();
        hasEnded = false;
        vPlayer.frame = 0;
        
        if(current.Equals(0))
        {
            vPlayer.Play();
        
        }
        else
        {
       
        }
    }

    public void Resize()
    {
        if (vOutput != null)
        {
            if (vOutput.texture != null)
            {
                AspectRatioFitter ratio = null;

                if (GetComponentsInChildren<AspectRatioFitter>(true).Length > 0)
                {
                    ratio = GetComponentsInChildren<AspectRatioFitter>(true)[0];
                }

                if (ratio != null)
                {
                    float texWidth = vOutput.texture.width;
                    float texHeight = vOutput.texture.height;
                    float aspectRatio = texWidth / texHeight;
                    ratio.aspectRatio = aspectRatio;
                }
            }
        }
    }

    private IEnumerator Prepare()
    {
        vPlayer.Prepare();
        hasEnded = false;
        
        while (!vPlayer.isPrepared)
        {
            yield return null;
        }

        vOutput.texture = vPlayer.texture;
        vOutput.SetNativeSize();

        AspectRatioFitter ratio = null;
			
        if (GetComponentsInChildren<AspectRatioFitter>(true).Length > 0)
        {
            ratio = GetComponentsInChildren<AspectRatioFitter>(true)[0];
        }

        if (ratio != null)
        {
            float texWidth = vOutput.texture.width;
            float texHeight = vOutput.texture.height;
            float aspectRatio = texWidth / texHeight;
            ratio.aspectRatio = aspectRatio;
        }
        
        vPlayer.Play();
        
        if(GetComponentInParent<GalleryEntry>().transform.localPosition.x.Equals(0))
        {

        }
        else
        {
            StartCoroutine(WaitFrame());
        }

        videoReady = true;
        
        if (vOutput.texture != null)
        {
            vOutput.CrossFadeAlpha(1.0f,0.5f, true);
        }
    }

    private IEnumerator WaitFrame()
    {
        while (vPlayer.time < 0.1f)
        {
            yield return null;
        }

        vPlayer.Pause();
    }
}
