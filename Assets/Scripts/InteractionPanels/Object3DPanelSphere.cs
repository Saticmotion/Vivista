﻿using AsImpL;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

public class Object3DPanelSphere : MonoBehaviour
{
	public Text title;
	public GameObject object3d;
	public Material transparent;
	public Material hoverMaterial;
	public Button resetTransform;
	public SteamVR_Input_Sources inputSourceLeft = SteamVR_Input_Sources.LeftHand;
	public SteamVR_Input_Sources inputSourceRight = SteamVR_Input_Sources.RightHand;

	private GameObject objectRenderer;
	private GameObject objectHolder;

	private string filePath = "";
	private string objectName = "";
	private ImportOptions importOptions = new ImportOptions();

	private ObjectImporter objImporter;
	private Renderer rend;
	private UISphere uiSphere;

	//NOTE(Jitse): Values used for interacting with 3D object
	private Vector3 cameraPosition;
	private Vector3 prevMousePos;
	private Vector3 mouseOffset;
	private float sensitivity = 250f;
	private float centerOffset = 95f;
	private bool isRotating;
	private bool isMoving;
	private bool isScaling;
	private bool mouseDown;
	private bool leftTriggerDown;
	private bool rightTriggerDown;
	private MeshCollider objectCollider;
	private Controller controllerLeft;
	private Controller controllerRight;

	//NOTE(Jitse): Camera culling mask layers
	private int objects3dLayer;
	private int interactionPointsLayer;

	public void Init(string newTitle, List<string> newUrls)
	{
		title.text = newTitle;

		if (newUrls.Count > 0)
		{
			filePath = newUrls[0];
		}

		objectRenderer = GameObject.Find("ObjectRenderer");
		objImporter = objectRenderer.GetComponent<ObjectImporter>();
		importOptions.hideWhileLoading = true;
		importOptions.inheritLayer = true;
		objImporter.ImportingComplete += SetObjectProperties;

		objects3dLayer = LayerMask.NameToLayer("3DObjects");
		interactionPointsLayer = LayerMask.NameToLayer("interactionPoints");

		cameraPosition = Camera.main.transform.position;

		resetTransform.onClick.AddListener(ResetTransform);

		var controllers = FindObjectsOfType<Controller>();

		foreach (Controller controller in controllers)
		{
			if (controller.name == "LeftHand")
			{
				controllerLeft = controller;
			}
			else if (controller.name == "RightHand")
			{
				controllerRight = controller;
			}
		}

		SteamVR_Actions.default_Trigger[inputSourceLeft].onStateDown += TriggerDownLeft;
		SteamVR_Actions.default_Trigger[inputSourceLeft].onStateUp += TriggerUp;
		SteamVR_Actions.default_Trigger[inputSourceRight].onStateDown += TriggerDownRight;
		SteamVR_Actions.default_Trigger[inputSourceRight].onStateUp += TriggerUp;
	}

	private void SetObjectProperties()
	{
		var objects3d = objectRenderer.GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < objects3d.Length; i++)
		{
			var currentObject = objects3d[i];
			if (currentObject.name == objectName)
			{
				object3d = currentObject.gameObject;

				var transforms = object3d.GetComponentsInChildren<Transform>();
				Vector3 objectCenter = Vector3.zero;

				float maxX = float.MinValue;
				float maxY = float.MinValue;
				float maxZ = float.MinValue;

				//NOTE(Jitse): Encapsulate the bounds to get the correct center of the 3D object.
				//NOTE(cont.): Also calculate the bounds size of the object.
				var meshes = object3d.GetComponentsInChildren<MeshRenderer>();
				var bounds = meshes[0].bounds;
				for (int j = 0; j < meshes.Length; j++)
				{
					var currentRend = meshes[j];
					var boundsSize = currentRend.bounds.size;
					if (boundsSize.x > maxX)
					{
						maxX = boundsSize.x;
					}
					if (boundsSize.y > maxY)
					{
						maxY = boundsSize.y;
					}
					if (boundsSize.z > maxZ)
					{
						maxZ = boundsSize.z;
					}

					if (j > 0)
					{
						bounds.Encapsulate(meshes[j].bounds);
					}
				}

				objectCenter = bounds.center;

				//NOTE(Jitse): Set the scaling value; 100f was chosen by testing which size would be most appropriate.
				//NOTE(cont.): Lowering or raising this value respectively decreases or increases the object size.
				const float desiredScale = 30f;
				var scale = desiredScale / Math.Max(Math.Max(maxX, maxY), maxZ);

				//NOTE(Jitse): Ensure every child object has the correct position within the object.
				//NOTE(cont.): Set object position to the bounding box center, this fixes when objects have an offset from their pivot point.
				var children = object3d.GetComponentsInChildren<Transform>();
				for (int j = 1; j < children.Length; j++)
				{
					children[j].localPosition = -objectCenter;
				}

				//NOTE(Jitse): Setting correct parameters of the object.
				var objRotation = object3d.transform.localRotation.eulerAngles;
				objRotation.x = -90;
				object3d.transform.localRotation = Quaternion.Euler(objRotation);
				object3d.transform.localPosition = new Vector3(0, 0, 70);
				object3d.transform.localScale = new Vector3(scale, scale, scale);
				object3d.SetLayer(objects3dLayer);

				//TODO(Jitse): Is there a way to avoid using combined meshes for hit detection?
				{ 
					//NOTE(Jitse): Combine the meshes, so we can assign it to the MeshCollider for hit detection
					MeshFilter mainMesh;
					rend = objectHolder.AddComponent<MeshRenderer>();

					//NOTE(Jitse): We don't want to see the combined "parent" mesh, because we already see the separate children meshes with their respective materials, so we assign a transparent material
					rend.material = transparent;
					mainMesh = objectHolder.AddComponent<MeshFilter>();

					//NOTE(Jitse): Combine the meshes of the object into one mesh, to correctly calculate the bounds
					var meshFilters = object3d.GetComponentsInChildren<MeshFilter>();
					var combine = new CombineInstance[meshFilters.Length];

					int k = 0;
					while (k < meshFilters.Length)
					{
						combine[k].mesh = meshFilters[k].sharedMesh;
						combine[k].transform = meshFilters[k].transform.localToWorldMatrix;

						k++;
					}

					mainMesh.mesh = new Mesh();
					mainMesh.mesh.CombineMeshes(combine);
				}

				objectCollider = objectHolder.AddComponent<MeshCollider>();
				objectCollider.convex = true;

				//NOTE(Jitse): If the user has the preview panel active when the object has been loaded, do not hide the object
				//NOTE(cont.): Also get SphereUIRenderer to position the 3D object in the center of the sphere panel
				if (isActiveAndEnabled)
				{
					uiSphere = GameObject.Find("SphereUIRenderer").GetComponent<UISphere>();
					objectHolder.transform.position = Vector3.zero;
					objectHolder.transform.rotation = Quaternion.Euler(new Vector3(0, uiSphere.offset + centerOffset, 0));
				} 
				else
				{
					objectHolder.SetActive(false);
				}
				break;
			}
		}

		//NOTE(Jitse): After completion, remove current event handler, so that it won't be called again when another Init is called.
		objImporter.ImportingComplete -= SetObjectProperties;
	}

	private void OnEnable()
	{
		if (!objectHolder)
		{
			objectName = Path.GetFileName(Path.GetDirectoryName(filePath));

			if (File.Exists(filePath))
			{
				//NOTE(Jitse): Create a parent object for the 3D object, to ensure it has the correct position for rotation
				if (GameObject.Find("/ObjectRenderer/holder_" + objectName) == null)
				{
					objectHolder = new GameObject("holder_" + objectName);
					objectHolder.transform.parent = objectRenderer.transform;
					objectHolder.layer = objects3dLayer;
					objImporter.ImportModelAsync(objectName, filePath, objectHolder.transform, importOptions);

					//NOTE(Jitse): Use SphereUIRenderer to get the offset to position the 3D object in the center of the window.
					//NOTE(cont.): Get SphereUIRenderer object here, because it would be inactive otherwise.
					if (uiSphere == null && object3d != null)
					{
						uiSphere = GameObject.Find("SphereUIRenderer").GetComponent<UISphere>();
						objectHolder.transform.position = Vector3.zero;
						objectHolder.transform.rotation = Quaternion.Euler(new Vector3(0, uiSphere.offset + centerOffset, 0));
					}
				}
			}
		}

		//NOTE(Jitse): Prevents null reference errors, which could occur if the object file could not be found
		if (objectHolder != null)
		{
			objectHolder.SetActive(true);
			Camera.main.cullingMask |= 1 << objects3dLayer;
			Camera.main.cullingMask &= ~(1 << interactionPointsLayer);
		}
	}

	private void OnDisable()
	{
		//NOTE(Jitse): Prevents null reference errors, which could occur if the object file could not be found
		if (objectHolder != null)
		{
			objectHolder.SetActive(false);
			Camera.main.cullingMask |= 1 << interactionPointsLayer;
			Camera.main.cullingMask &= ~(1 << objects3dLayer);
		}
	}

	private void Update()
	{
		//NOTE(Jitse): If the object hasn't been loaded yet.
		if (objectCollider != null)
		{
			//NOTE(Jitse): If mouse is over the object, change the collider's material to emphasize that the object can be interacted with.
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if ((objectCollider.Raycast(ray, out hit, Mathf.Infinity) || controllerLeft.object3dHovering || controllerRight.object3dHovering) && !(isMoving || isRotating || isScaling))
			{
				rend.material = hoverMaterial;
			}
			else
			{
				rend.material = transparent;
			}
		}

		//NOTE(Jitse): Rotate objectHolders by rotating them around their child object.
		if (isRotating)
		{
			var speedHorizontal = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
			var speedVertical = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

			objectHolder.transform.RotateAround(object3d.transform.position, Vector3.down, speedHorizontal);
			objectHolder.transform.RotateAround(object3d.transform.position, object3d.transform.right, speedVertical);
		}

		//NOTE(Jitse): Move objects by rotating them around the VRCamera.
		if (isMoving)
		{
			var speedHorizontal = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
			var speedVertical = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

			//NOTE(Jitse): Horizontal movement
			if (Input.GetAxis("Mouse X") > 0)
			{
				objectHolder.transform.RotateAround(cameraPosition, Vector3.up, speedHorizontal);
			}
			else if (Input.GetAxis("Mouse X") < 0)
			{
				objectHolder.transform.RotateAround(cameraPosition, Vector3.down, -speedHorizontal);
			}

			//NOTE(Jitse): Vertical movement
			if (Input.GetAxis("Mouse Y") > 0)
			{
				objectHolder.transform.RotateAround(cameraPosition, Vector3.left, speedVertical);
			}
			else if (Input.GetAxis("Mouse Y") < 0)
			{
				objectHolder.transform.RotateAround(cameraPosition, Vector3.right, -speedVertical);
			}
		}

		if (isScaling)
		{
			mouseOffset = Input.mousePosition - prevMousePos;
			var increase = (mouseOffset.y + mouseOffset.x) * Time.deltaTime;
			objectHolder.transform.position = Vector3.MoveTowards(objectHolder.transform.position, new Vector3(cameraPosition.x, cameraPosition.y, cameraPosition.z), increase);
			prevMousePos = Input.mousePosition;
		}

		if (leftTriggerDown && rightTriggerDown)
		{
			float scale = (controllerLeft.transform.position - controllerRight.transform.position).magnitude;
			objectHolder.transform.localScale = new Vector3(scale, scale, scale);
		}
		else if (leftTriggerDown)
		{
			objectHolder.transform.position = controllerLeft.transform.position;
			objectHolder.transform.rotation = controllerLeft.transform.rotation;
		}
		else if (rightTriggerDown)
		{
			objectHolder.transform.position = controllerRight.transform.position;
			objectHolder.transform.rotation = controllerRight.transform.rotation;
		}

		GetMouseButtonStates();
	}

	private void LateUpdate()
	{
		if (!(mouseDown || leftTriggerDown || rightTriggerDown))
		{
			isRotating = false;
			isMoving = false;
			isScaling = false;
		}
	}

	private void GetMouseButtonStates()
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;

		if (Input.GetMouseButtonDown(0))
		{
			if (objectCollider.Raycast(ray, out hit, Mathf.Infinity))
			{
				isRotating = true;
				mouseDown = true;
				prevMousePos = Input.mousePosition;
			}
		}
		else if (Input.GetMouseButtonUp(0))
		{
			mouseDown = false;
		}

		if (Input.GetMouseButtonDown(1))
		{
			if (objectCollider.Raycast(ray, out hit, Mathf.Infinity))
			{
				isMoving = true;
				mouseDown = true;
			}
		}
		else if (Input.GetMouseButtonUp(1))
		{
			mouseDown = false;
		}

		if (Input.GetMouseButtonDown(2))
		{
			if (objectCollider.Raycast(ray, out hit, Mathf.Infinity))
			{
				isScaling = true;
				mouseDown = true;
				prevMousePos = Input.mousePosition;
			}
		}
		else if (Input.GetMouseButtonUp(2))
		{
			mouseDown = false;
		}
	}

	public void OnPointerUp()
	{
		isRotating = true;
		prevMousePos = Input.mousePosition;
	}

	public void OnPointerDown()
	{
		isRotating = false;
	}

	private void TriggerDownLeft(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
	{
		if (controllerLeft.object3dHovering)
		{
			leftTriggerDown = true;
		}
	}

	private void TriggerDownRight(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
	{
		if (controllerRight.object3dHovering)
		{
			rightTriggerDown = true;
		}
	}
	private void TriggerUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
	{
		leftTriggerDown = false;
		rightTriggerDown = false;
	}

	private void ResetTransform()
	{
		objectHolder.transform.position = Vector3.zero;
		objectHolder.transform.rotation = Quaternion.Euler(new Vector3(0, uiSphere.offset + centerOffset, 0));
	}
}
