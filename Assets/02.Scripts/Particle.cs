using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle {
	public float amplitude;
	public Vector3 origin;
	public Vector3 position;
	public float velocity = 4.0f;
	public float radius = 1.0f;
	public float angle;
	public float dispersionAngle;
	public Vector3 direction;
	public float createTime;
	public float splitTime;
	public float deathTime;
	public bool isDead;

	public Particle(float amplitude, Vector3 origin, float angle, float dispersionAngle, float createTime, float currTime) {
		this.amplitude = amplitude;
		this.origin = origin + this.direction * 4.0f;
		this.angle = angle;
		this.direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
		this.direction.Normalize ();
		this.dispersionAngle = dispersionAngle;
		this.createTime = createTime;
		this.deathTime = createTime + 5.0f;
		this.isDead = false;
		this.SetSplitTime ();
		this.SetPosition(currTime);
	}


	public void SetSplitTime () {
		this.splitTime = this.createTime + this.radius / (2f * this.dispersionAngle * this.velocity);
	}

	public void SetPosition (float currTime) {
		this.position = this.origin + this.direction * (this.velocity * (currTime - this.createTime));
	}
}
