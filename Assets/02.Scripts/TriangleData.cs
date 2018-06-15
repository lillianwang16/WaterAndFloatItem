using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleData {

	public Vector3 point0;
	public Vector3 point1;
	public Vector3 point2;

	public Vector3 center;
	public float area;
	public Vector3 normal;
	public float height;

	public Vector3 velocity;

	// Normalized velocity
	public Vector3 velocityDir;

	// Angle between velocity and normal
	public float cosTheta;

	private HeightField heightField;

	public TriangleData (Vector3 p0, Vector3 p1, Vector3 p2, Rigidbody rbObj ,HeightField heightField) {
		this.point0 = p0;
		this.point1 = p1;
		this.point2 = p2;

		this.heightField = heightField;

		this.center = (p0 + p1 + p2) / 3.0f;

		this.height = Mathf.Abs(this.heightField.DistanceToWater(this.center));

		this.normal = Vector3.Cross(p1 - p0, p2 - p0).normalized;

		this.area = (Vector3.Distance(p1, p0) * Vector3.Distance(p2, p0) * Mathf.Sin(Vector3.Angle(p1 - p0, p2 - p0) * Mathf.Deg2Rad)) / 2f;
	
		this.velocity = this.GetTriangleVelocity(rbObj);

		//Velocity direction
		this.velocityDir = this.velocity.normalized;

		//Angle between the normal and the velocity
		//Negative if pointing in the opposite direction
		//Positive if pointing in the same direction
		this.cosTheta = Vector3.Dot(this.velocityDir, this.normal);
	}


	private Vector3 GetTriangleVelocity(Rigidbody objRB) {
		
		// v_T = v_Object + (angularVelocity_Object) cross (distance in between)

		Vector3 v_obj = objRB.velocity;

		Vector3 av_obj = objRB.angularVelocity;

		Vector3 r = this.center - objRB.worldCenterOfMass;

		Vector3 v_T = v_obj + Vector3.Cross(av_obj, r);

		return v_T;
	}


}
