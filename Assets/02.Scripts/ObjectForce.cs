using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectForce : MonoBehaviour {

	// public GameObject underWaterObj;
	private GameObject sea;

	private Mesh underWaterMesh;
	public UpdateObjMesh updateObjMesh; 
	private Rigidbody rigidBody;
	public const float RHO_WATER = 1000f;

	void Awake () {
		rigidBody = GetComponent<Rigidbody> ();
		sea = GameObject.FindGameObjectWithTag ("SEA");
		updateObjMesh = new UpdateObjMesh (this.gameObject, this.sea);
		// underWaterMesh = underWaterObj.GetComponent<MeshFilter> ().mesh;
	}

	void Update () {
		updateObjMesh.UpdateUnderWaterMesh ();
		// updateObjMesh.ApplyUnderWaterMesh (underWaterMesh);
		// Debug.Log (updateObjMesh.underWaterTriangles.Count);
	}

	public void ComputeObjectForces () {
		// rigidBody.velocity = new Vector3 (0f, 0f, 0.5f);
		if (updateObjMesh.HasUnderWaterMesh ()) {
			this.ApplyWaterToObjForce ();
		}
	}

	void ApplyWaterToObjForce () {
		List<TriangleData> underWaterTriangles = updateObjMesh.underWaterTriangles;
		foreach (TriangleData triangleData in underWaterTriangles) {
			Vector3 buoyancy = ComputeBuoyancy(triangleData);
			//Vector3 drag = PressureDragForce(triangleData);
			Vector3 drag = Vector3.zero;
			rigidBody.AddForceAtPosition (buoyancy + drag, triangleData.center);
		}
	}

	private Vector3 ComputeBuoyancy (TriangleData triangleData) {
		Vector3 buoyancy = ObjectForce.RHO_WATER * Physics.gravity.y * triangleData.height * triangleData.area * triangleData.normal;
		if (triangleData.center.y > 0f) {
			buoyancy.y = 0f;
		}
		// buoyancy.x = 0f;
		// buoyancy.z = 0f;

		buoyancy = this.CheckForceIsValid(buoyancy, "Buoyancy force");
		// Debug.Log (buoyancy);
		return buoyancy;
	}

	private Vector3 PressureDragForce(TriangleData triangleData) {
		//Modify for different turning behavior and planing forces
		//f_p and f_S - falloff power, should be smaller than 1
		//C - coefficients to modify 

		float velocity = triangleData.velocity.magnitude;

		//A reference speed used when modifying the parameters
		float velocityReference = velocity;

		velocity = velocity / velocityReference;

		Vector3 pressureDragForce = Vector3.zero;

		if (triangleData.cosTheta > 0f) {
			float C_PD1 = 10f;
			float C_PD2 = 10f;
			float f_P = 0.5f;

			// To change the variables real-time - add the finished values later
			// float C_PD1 = DebugPhysics.current.C_PD1;
			// float C_PD2 = DebugPhysics.current.C_PD2;
			// float f_P = DebugPhysics.current.f_P;

			pressureDragForce = -(C_PD1 * velocity + C_PD2 * (velocity * velocity)) * triangleData.area * Mathf.Pow(triangleData.cosTheta, f_P) * triangleData.normal;
		} else {
			float C_SD1 = 10f;
			float C_SD2 = 10f;
			float f_S = 0.5f;

			//To change the variables real-time - add the finished values later
			// float C_SD1 = DebugPhysics.current.C_SD1;
			// float C_SD2 = DebugPhysics.current.C_SD2;
			// float f_S = DebugPhysics.current.f_S;

			pressureDragForce = (C_SD1 * velocity + C_SD2 * (velocity * velocity)) * triangleData.area * Mathf.Pow(Mathf.Abs(triangleData.cosTheta), f_S) * triangleData.normal;
		}

		pressureDragForce = this.CheckForceIsValid(pressureDragForce, "Pressure drag");
		// Debug.Log (pressureDragForce);

		return pressureDragForce;
	}

	private Vector3 CheckForceIsValid(Vector3 force, string forceName) {
		if (!float.IsNaN(force.x + force.y + force.z)) {
			return force;
		} else {
			Debug.Log(forceName += " force is NaN");

			return Vector3.zero;
		}
	}
}


