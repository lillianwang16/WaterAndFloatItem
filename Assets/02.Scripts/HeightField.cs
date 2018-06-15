using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightField : MonoBehaviour {
	// The division of X and Y.
	public int numX;
	public int numZ;

	// The weight and height of the mesh.
	public float width;
	public float height;

	// Material.
	private Material material;
	// private Transform tr;

	// Using a flag to tell whether we shoud update the mesh.
	private bool isDirty;

	private Mesh mesh;
	private Vector3[] verArr;
	private Vector3[] deviations; 
	public delegate Vector3 SetDivDelegate(Vector3 position);
	public SetDivDelegate setDivDelegate;

	void Awake  () {
		InitializeMesh ();
		GetComponent<MeshFilter> ().mesh = this.mesh;
		BoxCollider boxCollider = base.gameObject.AddComponent<BoxCollider> ();
		boxCollider.center = Vector3.zero;
		boxCollider.size = new Vector3 (width, 0.01f, height);
		boxCollider.isTrigger = true;
	}

	private void OnDrawGizmos() {
		Vector3 position = base.transform.position - base.transform.right * this.width / 2 - base.transform.forward * this.height / 2;
		Vector3 vector = base.transform.position + base.transform.right * this.width / 2 - base.transform.forward * this.height / 2;
		Vector3 vector2 = base.transform.position + base.transform.right * this.width / 2 + base.transform.forward * this.height / 2;
		Vector3 vector3 = base.transform.position - base.transform.right * this.width / 2 + base.transform.forward * this.height / 2;
		Gizmos.color = Color.magenta;
		Gizmos.DrawLine(position, vector);
		Gizmos.DrawLine(vector, vector2);
		Gizmos.DrawLine(vector2, vector3);
		Gizmos.DrawLine(vector3, position);

	}

	/*
	void Awake () {
		this.mesh = GetComponent<MeshFilter> ().mesh;
		deviations = new Vector3[this.mesh.vertexCount];
		this.verArr = this.mesh.vertices;
	}

	void Awake () {
		Mesh m = GetComponent<MeshFilter> ().mesh;
		Debug.Log (m.vertexCount);
		transform = GetComponent<Transform> ();
		width = transform.lossyScale.x;
		height = transform.lossyScale.z;
		Debug.Log (width);
		Debug.Log (height);
		InitializeMesh ();
		GetComponent<MeshFilter> ().mesh = this.mesh;
	}
	*/

	public void Update () {
		if (this.isDirty) {
			this.UpdateMesh();
			this.isDirty = false;
		}
	}

	private void UpdateMesh () {
		Vector3[] nextVertix = this.mesh.vertices;
		for (int i = 0; i < nextVertix.Length; ++i) {
			nextVertix[i] = this.verArr[i] + this.deviations[i];
		}
		this.mesh.vertices = nextVertix;
		this.mesh.RecalculateNormals ();
		this.mesh.RecalculateBounds ();
	}

	public void SetDiv() {
		this.isDirty = true;
		for (int i = 0; i < this.verArr.Length; i++) {
			this.deviations [i] = Vector3.zero;
			this.deviations [i] = setDivDelegate(base.transform.position + this.verArr[i]);
		}
	}
		

	void InitializeMesh	(){
		this.mesh = new Mesh (); 
		this.mesh.name = "MyMesh";
		this.verArr = new Vector3[this.numX * this.numZ];
		this.deviations = new Vector3[this.numX * this.numZ];
		Vector2[] uvArr = new Vector2[this.numX * this.numZ];
		int[] triArr = new int[6 * this.numX * this.numZ];
		int numDivX = numX - 1;
		int numDivZ = numZ - 1;
		for (int i = 0; i < this.numZ; i++) {
			for (int j = 0; j < this.numX; j++) {
				int index = j + i * this.numX;
				Vector3 vertix = new Vector3 (this.width * (float)j / (float)numDivX - this.width / 2.0f, 0f, this.height * (float)i / (float)numDivZ - this.height / 2.0f);
				Vector2 vector2 = new Vector2 ((float)j / (float)numDivX, (float)i / (float)numDivZ);
				this.verArr [index] = vertix;
				uvArr [index] = vector2;
			}
		}

		for (int k = 0; k < numDivZ; k++) {
			for (int l = 0; l < numDivX; l++) {
				int num2 = 6 * (l + k * numDivX);
				triArr [num2] = l + k * this.numX;
				triArr [num2 + 1] = l + (k + 1) * this.numX;
				triArr [num2 + 2] = l + 1 + k * this.numX;
				triArr [num2 + 3] = l + 1 + k * this.numX;
				triArr [num2 + 4] = l + (k + 1) * this.numX;
				triArr [num2 + 5] = l + 1 + (k + 1) * this.numX;
			}
		}
		this.mesh.vertices = this.verArr;
		this.mesh.normals = this.verArr;
		this.mesh.uv = uvArr;
		this.mesh.triangles = triArr;
		this.mesh.RecalculateNormals ();
		this.mesh.MarkDynamic ();
	}

	public float DistanceToWater(Vector3 position) {
		float waterHeight = setDivDelegate(position).y;
		float height = position.y - waterHeight;

		return height;
	}
}

