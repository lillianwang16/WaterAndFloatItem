using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveController : MonoBehaviour {

	private LinkedList<Particle> particles;
	private int initialParticles = 5;
	private HeightField heightfield;
	private Plane[] bouncePlanes;
	private Plane[] deathPlanes;
	private HashSet<Particle>[,] buckets;
	private Rigidbody floatItemRB;
	private float volecityDir;
	public int gridnum = 9;
	private UpdateObjMesh updateObjMesh; 
	public float threshold = 0.001f;
	private ObjectForce objectForce;

	void Awake() {
		this.particles = new LinkedList<Particle>();
		this.heightfield = GetComponent<HeightField>();
		this.CreateSidePlanes ();
		this.buckets = new HashSet<Particle>[gridnum + 1, gridnum + 1];
		GameObject floatItem = GameObject.FindGameObjectWithTag ("ITEM");
		floatItemRB = floatItem.GetComponent<Rigidbody> ();
		objectForce = floatItem.GetComponent<ObjectForce> ();
		volecityDir = 0f;
		updateObjMesh = objectForce.updateObjMesh;
		for (int i = 0; i < gridnum + 1; ++i) {
			for (int j = 0; j < gridnum + 1; ++j) {
				this.buckets[i, j] = new HashSet<Particle> ();
			}
		}
	}

	private void CreateSidePlanes () {
		this.deathPlanes = new Plane[2];
		this.bouncePlanes = new Plane[2];
		deathPlanes[0] = new Plane (Vector3.forward, new Vector3(0f, 0f, - heightfield.height / 2));
		deathPlanes[1] = new Plane (Vector3.back, heightfield.height / 2);
		bouncePlanes[0] = new Plane (Vector3.right, new Vector3(- heightfield.width / 2, 0f, 0f));
		bouncePlanes[1] = new Plane (Vector3.left, heightfield.width / 2);
	}

	 void Update() {
		RaycastHit raycastHit;
		if (Input.GetButtonDown("Fire1") && Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out raycastHit)) {
			Vector3 origin = new Vector3 (raycastHit.point.x, 0f, raycastHit.point.z);
			Debug.DrawRay (Camera.main.transform.position, origin - Camera.main.transform.position);
			this.CreateWave (origin, 0.5f, 10f, 2f);
			// Debug.Log (particles.Count);
		}
		// Debug.Log (this.particles.Count);
	}

	void FixedUpdate() {
		this.IterateWaveParticles ();
		this.objectForce.ComputeObjectForces ();
		this.GenerateParticleFromObject ();
		this.UpdateBuckets ();
		this.RenderHeightFields ();
	}

	private void OnDrawGizmos() {
		if (this.particles != null) {
			Gizmos.color = Color.yellow;
			foreach (Particle particle in this.particles)
			{
				Gizmos.DrawWireSphere(particle.position, particle.radius);
			}
		}
	}

	private void CreateWave (Vector3 origin, float amplitude, float radius, float velocity) {
		float dispersionAngle = 2 * Mathf.PI / (float)this.initialParticles;
		float curtime = Time.time;
		for (int i = 0; i < this.initialParticles; i++) {
			float angle = (float)i * dispersionAngle;
			Particle particle = new Particle(amplitude, origin, angle, dispersionAngle, curtime, curtime);
			this.particles.AddLast(particle);
		}
	}

	private void IterateWaveParticles () {
		float time = Time.time;
		LinkedListNode<Particle> currNode = this.particles.First;
		while (currNode != null) {
			Particle particle = currNode.Value;
			this.CheckParticleValidation (particle, time);
			if (!particle.isDead) {
				particle.amplitude -= particle.amplitude / 100.0f;
				foreach (Plane plane in this.bouncePlanes) {
					this.Bounce(particle, plane, time);
				}
				if (time > particle.splitTime) {
					this.Split (particle, time);
				}
				particle.SetPosition (time);
				currNode = currNode.Next;
			} else {
				LinkedListNode<Particle> nextNode = currNode.Next;
				this.particles.Remove(currNode);
				currNode = nextNode;
			}
		}
	}

	private void CheckParticleValidation (Particle particle, float time) {
		if (particle.amplitude <= this.threshold || particle.deathTime < time)
			particle.isDead = true;
		foreach (Plane plane in this.deathPlanes) {
			if (!plane.GetSide (particle.position)) {
				particle.isDead = true;
			}
		}
	}
		
	private void Split (Particle par, float time) {
		par.dispersionAngle /= 3f;
		par.amplitude /= 3f;
		par.SetSplitTime ();
		Particle par1 = new Particle(par.amplitude, par.origin, par.angle - par.dispersionAngle, par.dispersionAngle, par.createTime, time);
		Particle par2 = new Particle(par.amplitude, par.origin, par.angle + par.dispersionAngle, par.dispersionAngle, par.createTime, time);
		this.particles.AddLast (par1);
		this.particles.AddLast (par2);
	}

	private void Bounce (Particle particle, Plane plane, float currentTime) {
		float dist;
		if (!plane.GetSide(particle.position) && plane.Raycast(new Ray(particle.origin, particle.direction), out dist)) {
			Vector3 a = particle.origin + particle.direction * dist;
			particle.direction = Vector3.Reflect(particle.direction, plane.normal).normalized;
			particle.angle = Mathf.Atan2(particle.direction.z, particle.direction.x);
			particle.origin = a - particle.direction * dist;
		}
	}
		
	private void GenerateParticleFromObject () {
		List<TriangleData> intersectTriangles = updateObjMesh.intersectTriangles;
		/*
		if (intersectTriangles.Count <= 0)
			return;
		if ((volecityDir > 0 && this.floatItemRB.velocity.y > 0) || (volecityDir < 0 && this.floatItemRB.velocity.y < 0)) {
			return;
		}
		*/

		volecityDir = this.floatItemRB.velocity.y;
		bool isSubmerged = updateObjMesh.isSubmerged;		
		float dispersionAngle = 2 * Mathf.PI / (float)this.initialParticles;
		float curtime = Time.time;
		foreach (TriangleData triangleData in intersectTriangles) {
			if (triangleData.area > 0.0f) {
				this.GenerateParticleFromTriangle (triangleData, isSubmerged, dispersionAngle, curtime);
			}
		}
		
	}

	private void GenerateParticleFromTriangle (TriangleData triangleData, bool isSubmerged, float dispersionAngle, float curtime) {
		float C = 150.0f;
		C = triangleData.velocity.y > 0.0f ? -C : C;
		float amplitude = C * triangleData.area * Vector3.Dot(triangleData.velocity, triangleData.normal) * Time.deltaTime;
		// Debug.Log (amplitude);
		Vector3 origin = new Vector3(triangleData.center.x, 0.0f, triangleData.center.z);
		Vector3 direction = triangleData.normal;
		direction = isSubmerged ? -direction : direction;
		float angle = Mathf.Atan2(direction.z, direction.x);
		Particle particle = new Particle(amplitude, origin, angle, dispersionAngle, curtime, curtime);
		this.particles.AddLast (particle);
	}

	private void UpdateBuckets() {
		this.clearBuckets ();
		foreach (Particle particle in this.particles) {
			this.AddToBucket (particle);
		}
	}

	private void clearBuckets () {
		for (int i = 0; i <= gridnum; ++i) {
			for (int j = 0; j <= gridnum; ++j) {
				buckets [i, j].Clear ();
			}
		}
	}

	private void AddToBucket (Particle particle) {
		int index_i;
		int index_j;
		this.GetBucketIndex (particle.position, out index_i, out index_j);
		this.buckets [index_i, index_j].Add (particle);
	}

	private void GetBucketIndex (Vector3 point, out int index_i, out int index_j) {
		index_i = (int)((point.x + this.heightfield.width / 2) / this.heightfield.width * this.gridnum);
		index_j = (int)((point.z + this.heightfield.height / 2) / this.heightfield.height * this.gridnum);
		index_i = Mathf.Max (0, index_i);
		index_j = Mathf.Max (0, index_j);
		index_i = Mathf.Min (this.gridnum, index_i);
		index_j = Mathf.Min (this.gridnum, index_j);
	}
		

	private void RenderHeightFields() {
		heightfield.setDivDelegate = this.ParticleToDiv;
		this.heightfield.SetDiv();
	}

	private Vector3 ParticleToDiv (Vector3 point) {
		
		Vector3 sumDiv = Vector3.zero;
		int index_i_low;
		int index_j_low;
		this.GetBucketIndex (point - Vector3.one * 1.0f, out index_i_low, out index_j_low);
		int index_i_high;
		int index_j_high;
		this.GetBucketIndex (point + Vector3.one * 1.0f, out index_i_high, out index_j_high);
		for (int i = index_i_low; i <= index_i_high; i++) {
			for (int j = index_j_low; j <= index_j_high; j++) {
				foreach (Particle particle in this.buckets[i, j]) {
					float dist = Vector3.Distance(point, particle.position);
					if (dist < particle.radius) {
						dist += (- Mathf.Sin(Mathf.PI * dist / particle.radius));
						sumDiv.y += particle.amplitude * (Mathf.Cos(Mathf.PI * dist / particle.radius) + 1f);
					}
				}
			}
		}
		return sumDiv;
	}
}
