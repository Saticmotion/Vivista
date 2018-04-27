﻿using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoPanel : MonoBehaviour
{
	public RenderTexture videoRenderTexture;
	public GameObject videoContainer;
	public GameObject controllButton;
	public Canvas canvas;
	public Texture iconPlay;
	public Texture iconPause;

	public Text title;
	public string url;

	private RawImage videoSurface;
	private VideoPlayer videoPlayer;
	private AudioSource audioSource;


	public void Init(Vector3 position, string newTitle, string fullPath, string guid, bool prepareNow = false)
	{
		videoPlayer = videoContainer.GetComponent<VideoPlayer>();
		videoSurface = videoContainer.GetComponent<RawImage>();
		videoRenderTexture = Instantiate(videoRenderTexture);
		videoPlayer.targetTexture = videoRenderTexture;
		videoSurface.texture = videoRenderTexture;
		videoSurface.color = Color.white;

		if (Player.hittables != null)
		{
			GetComponentInChildren<Hittable>().enabled = true;
		}

		var folder = Path.Combine(Application.persistentDataPath, guid);

		if (!File.Exists(fullPath))
		{
			var pathNoExtension = Path.Combine(Path.Combine(folder, "extra"), Path.GetFileNameWithoutExtension(fullPath));
			if (!File.Exists(pathNoExtension))
			{
				Debug.LogWarningFormat("Cannot find extension-less file: {1} {0}", pathNoExtension, File.Exists(pathNoExtension));
				return;
			}

			try
			{
				File.Move(pathNoExtension, fullPath);
			}
			catch (IOException e)
			{
				Debug.LogWarningFormat("Cannot add extension to file: {0}\n{2}\n{1}", pathNoExtension, e.Message, fullPath);
				return;
			}

		}
		videoPlayer.url = fullPath;
		transform.localPosition = position;
		title.text = newTitle;
	}

	void Start()
	{
		//NOTE(Kristof): Initial rotation towards the camera 
		canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y);

		audioSource = videoPlayer.gameObject.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;

		videoPlayer.EnableAudioTrack(0, true);
		videoPlayer.SetTargetAudioSource(0, audioSource);
		videoPlayer.controlledAudioTrackCount = 1;

		//NOTE(Lander): duct tape
		videoPlayer.enabled = false;
		videoPlayer.enabled = true;
	}

	// Update is called once per frame
	void Update()
	{
		if (!videoSurface) return;
		var texture = videoSurface.texture;

		controllButton.GetComponent<RawImage>().texture = videoPlayer.isPlaying ? iconPause : iconPlay;

		// NOTE(Lander): Rotate the panels to the camera
		if (SceneManager.GetActiveScene().Equals(SceneManager.GetSceneByName("Editor")))
		{
			canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y, Camera.main.transform.eulerAngles.z);
		}
	}

	public void TogglePlay()
	{
		// HACK(Lander): toggle play
		(videoPlayer.isPlaying ? (Action)videoPlayer.Pause : videoPlayer.Play)();

		controllButton.GetComponent<RawImage>().texture = videoPlayer.isPlaying ? iconPause : iconPlay;
	}

	// NOTE(Lander): copied from image panel
	public void Move(Vector3 position)
	{
		Vector3 newPos;

		newPos = Vector3.Lerp(position, Camera.main.transform.position, 0.001f);
		newPos.y += 0.015f;

		canvas.GetComponent<RectTransform>().position = newPos;
		canvas.transform.rotation = Camera.main.transform.rotation;
	}

	private void OnDestroy()
	{
		// TODO(Lander): this can be empty in some cases.
		var filename = videoPlayer.url;
		if (File.Exists(filename) && Path.GetExtension(filename) != "")
		{
			var newfilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));
			try
			{
				Debug.LogFormat("Moving {0} to {1}", filename, newfilename);
				File.Move(filename, newfilename);
			}
			catch (IOException e)
			{
				try
				{
					Debug.LogFormat("File Already exists? deleting: {0}", newfilename);
					File.Delete(filename);
				}
				catch (IOException e2)
				{
					Debug.LogErrorFormat("Something went wrong while moving the file. Stopping. \n{} ", e2.Message);
				}
			}
		}
	}
}
