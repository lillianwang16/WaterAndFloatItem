using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateObjMesh {
	private Transform objTrans;
	private Rigidbody objRB;
	private Vector3[] verArr;
	private int[] triArr;
	private Vector3[] verArrGlobal;
	private float[] verHeight;
	private int verCount;
	private HeightField heightField;
	public bool isSubmerged;
	public List<TriangleData> underWaterTriangles = new List<TriangleData>();
	public List<TriangleData> intersectTriangles = new List<TriangleData>();

	public UpdateObjMesh (GameObject floatObject, GameObject sea) {
		objTrans = floatObject.GetComponent<Transform> ();
		objRB = floatObject.GetComponent<Rigidbody> ();
		heightField = sea.GetComponent<HeightField> ();
		Mesh mesh = floatObject.GetComponent<MeshFilter> ().mesh;
		verArr = mesh.vertices;
		triArr = mesh.triangles;
		verCount = mesh.vertexCount;
		verArrGlobal = new Vector3[verCount];
		verHeight = new float[verCount];
		isSubmerged = false;
	}

	public void UpdateUnderWaterMesh () {
		this.underWaterTriangles.Clear ();
		this.intersectTriangles.Clear ();
		for (int i = 0; i < verCount; i++) {
			Vector3 verGlobal = objTrans.TransformPoint(verArr[i]);
			verArrGlobal[i] = verGlobal;
			verHeight[i] = this.heightField.DistanceToWater(verGlobal);
		}
		// Debug.Log (verHeight[0]);
		this.UpdateUnderWaterTriangles ();
	}

	private void UpdateUnderWaterTriangles () {
		List<VertexInOneTriangle> triangleVertex = new List<VertexInOneTriangle>();
		triangleVertex.Add(new VertexInOneTriangle());
		triangleVertex.Add(new VertexInOneTriangle());
		triangleVertex.Add(new VertexInOneTriangle());
		int i = 0;
		isSubmerged = true;
		while (i < triArr.Length) {
			for (int j = 0; j < 3; j++) {
				triangleVertex [j].position = verArrGlobal [triArr [i]];
				triangleVertex [j].height = verHeight [triArr [i]];
				triangleVertex [j].index = j;
				i++;
			}
			// For each triangles, check the height of its vertexes.
			if (triangleVertex [0].height > 0f && triangleVertex [1].height > 0f && triangleVertex [2].height > 0f) {
				
				// 1. All vertexes are above water surface.
				isSubmerged = false;
				continue;
			}

			if (triangleVertex [0].height < 0f && triangleVertex [1].height < 0f && triangleVertex [2].height < 0f) {

				// 2. All vertexes are under water surface.
				underWaterTriangles.Add (new TriangleData (triangleVertex [0].position, triangleVertex [1].position, triangleVertex [2].position, this.objRB , this.heightField));
			} else {
				Vector3 p0 = triangleVertex [0].position;
				Vector3 p1 = triangleVertex [1].position;
				Vector3 p2 = triangleVertex [2].position;
				triangleVertex.Sort ((x, y) => x.height.CompareTo (y.height));
				triangleVertex.Reverse ();
				List<TriangleData> newTriangles = new List<TriangleData> ();

				if (triangleVertex [0].height > 0f && triangleVertex [1].height < 0f && triangleVertex [2].height < 0f) {

					// 3. There is only one vertex above water surface.
					intersectTriangles.Add (new TriangleData (p0, p1, p2, this.objRB, this.heightField));
					newTriangles = this.CutTriangle (triangleVertex, 1);

				} else if (triangleVertex [0].height > 0f && triangleVertex [1].height > 0f && triangleVertex [2].height < 0f) {

					// 4. There are two vertexes above water surface.
					intersectTriangles.Add (new TriangleData (p0, p1, p2, this.objRB, this.heightField));
					newTriangles = this.CutTriangle (triangleVertex, 2);
				}
				foreach (TriangleData triangle in newTriangles) {
					this.underWaterTriangles.Add (triangle);
				}
			}
		}
		// Debug.Log (underWaterTriangles.Count);
	}

	public void ApplyUnderWaterMesh (Mesh underWaterMesh) {
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		foreach (TriangleData triangle in this.underWaterTriangles) {
			Vector3 point0 = this.objTrans.InverseTransformPoint (triangle.point0);
			vertices.Add(point0);
			triangles.Add(vertices.Count - 1);

			Vector3 point1 = this.objTrans.InverseTransformPoint (triangle.point1);
			vertices.Add(point1);
			triangles.Add(vertices.Count - 1);

			Vector3 point2 = this.objTrans.InverseTransformPoint (triangle.point2);
			vertices.Add(point2);
			triangles.Add(vertices.Count - 1);
		}
		underWaterMesh.Clear();
		underWaterMesh.vertices = vertices.ToArray ();
		underWaterMesh.triangles = triangles.ToArray ();
		underWaterMesh.RecalculateBounds();
	}

	public bool HasUnderWaterMesh () {
		return this.underWaterTriangles.Count > 0;
	}

	public List<TriangleData> CutTriangle (List<VertexInOneTriangle> triangleVertex, int numVertexAbove) {
		List<TriangleData> newTriangles = new List<TriangleData> ();

		if (numVertexAbove == 1) {

			OneVertexAboveWater (triangleVertex, newTriangles);

		} else if (numVertexAbove == 2) {

			TwoVertexAboveWater (triangleVertex, newTriangles);

		} else {

			return null;
		}
		return newTriangles;
	}

	private void OneVertexAboveWater (List<VertexInOneTriangle> triangleVertex, List<TriangleData> newTriangles) {
		// H is always at position 0.
		Vector3 H = triangleVertex[0].position;

		// M is the left vertex of H.
		// L is the right vertex of H.
		int M_index = triangleVertex[0].index - 1;

		if (M_index < 0)	
			M_index = 2;

		// We also need the heights to water
		float h_H = triangleVertex[0].height;
		float h_M = 0f;
		float h_L = 0f;

		Vector3 M = Vector3.zero;
		Vector3 L = Vector3.zero;

		// Set the position of vertex M and L.
		if (triangleVertex[1].index == M_index) {
			M = triangleVertex[1].position;
			L = triangleVertex[2].position;

			h_M = triangleVertex[1].height;
			h_L = triangleVertex[2].height;
		} else {
			M = triangleVertex[2].position;
			L = triangleVertex[1].position;

			h_M = triangleVertex[2].height;
			h_L = triangleVertex[1].height;
		}

		// Now we can calculate where we should cut the triangle to form 2 new triangles
		// because the resulting area will always form a square

		// Point I_M
		Vector3 MH = H - M;

		float t_M = -h_M / (h_H - h_M);

		Vector3 MI_M = t_M * MH;

		Vector3 I_M = MI_M + M;


		// Point I_L
		Vector3 LH = H - L;

		float t_L = -h_L / (h_H - h_L);

		Vector3 LI_L = t_L * LH;

		Vector3 I_L = LI_L + L;

		// 2 triangles below the water  
		newTriangles.Add(new TriangleData(M, I_M, I_L, this.objRB, this.heightField));
		newTriangles.Add(new TriangleData(M, I_L, L, this.objRB, this.heightField));
	}

	private void TwoVertexAboveWater (List<VertexInOneTriangle> triangleVertex, List<TriangleData> newTriangles) {
		// L is always at position 2.
		Vector3 L = triangleVertex[2].position;

		// H is the left vertex of L.
		// M is the right vertex of L.
		int H_index = triangleVertex[2].index + 1;
		if (H_index > 2) {
			H_index = 0;
		}

		//We also need the heights to water
		float h_L = triangleVertex[2].height;
		float h_H = 0f;
		float h_M = 0f;

		Vector3 H = Vector3.zero;
		Vector3 M = Vector3.zero;

		// Set the position of vertex M and H.
		if (triangleVertex[1].index == H_index) {
			H = triangleVertex[1].position;
			M = triangleVertex[0].position;

			h_H = triangleVertex[1].height;
			h_M = triangleVertex[0].height;
		} else {
			H = triangleVertex[0].position;
			M = triangleVertex[1].position;

			h_H = triangleVertex[0].height;
			h_M = triangleVertex[1].height;
		}

		//Now we can find where to cut the triangle

		//Point J_M
		Vector3 LM = M - L;

		float t_M = -h_L / (h_M - h_L);

		Vector3 LJ_M = t_M * LM;

		Vector3 J_M = LJ_M + L;


		//Point J_H
		Vector3 LH = H - L;

		float t_H = -h_L / (h_H - h_L);

		Vector3 LJ_H = t_H * LH;

		Vector3 J_H = LJ_H + L;

		//1 triangle below the water
		newTriangles.Add(new TriangleData(L, J_H, J_M, this.objRB, this.heightField));
	}
}



