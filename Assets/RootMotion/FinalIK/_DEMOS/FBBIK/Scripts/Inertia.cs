using UnityEngine;
using System.Collections;
using RootMotion.FinalIK;

namespace RootMotion.FinalIK.Demos {

	/// <summary>
	/// Demo script that adds the illusion of mass to your character using FullBodyBipedIK.
	/// </summary>
	public class Inertia : MonoBehaviour {

		/// <summary>
		/// Body is just following it's transform in a lazy and bouncy way.
		/// </summary>
		[System.Serializable]
		public class Body {

			/// <summary>
			/// Linking this to an effector
			/// </summary>
			[System.Serializable]
			public class EffectorLink {
				public FullBodyBipedEffector effector;
				public float weight;
			}

			public Transform transform; // The Transform to follow, can be any bone of the character
			public EffectorLink[] effectorLinks; // Linking the body to effectors. One Body can be used to offset more than one effector.
			public float speed = 10f; // The speed to follow the Transform
			public float acceleration = 3f; // The acceleration, smaller values means lazyer following
			public float matchVelocity; // 0-1 matching target velocity
			public float gravity; // gravity applied to the Body

			[HideInInspector] public Vector3 delta;

			private Vector3 lazyPoint;
			private Vector3 direction;
			private Vector3 lastPosition;
			private bool firstUpdate = true;
			private float lastTime;

			// Reset to Transform
			public void Reset() {
				lazyPoint = transform.position;
				lastPosition = transform.position;
				direction = Vector3.zero;
				lastTime = Time.deltaTime;
			}

			// Update this body, apply the offset to the effector
			public void Update(IKSolverFullBodyBiped solver, float weight) {
				// If first update, set this body to Transform
				if (firstUpdate) {
					Reset();
					firstUpdate = false;
				}

				// not using Time.deltaTime or Time.fixedDeltaTime here, because we don't know if animatePhysics is true or not on the character, so we have to keep track of time ourselves.
				float deltaTime = Time.time - lastTime;

				// Acceleration
				direction = Vector3.Lerp(direction, transform.position - lazyPoint, deltaTime * acceleration);

				// Lazy follow
				lazyPoint += direction * deltaTime * speed;

				// Match velocity
				delta = transform.position - lastPosition;
				lazyPoint += delta * matchVelocity;
				
				// Gravity
				lazyPoint.y += gravity * deltaTime;

				// Apply position offset to the effector
				foreach (EffectorLink effectorLink in effectorLinks) {
					solver.GetEffector(effectorLink.effector).positionOffset += (lazyPoint - transform.position) * effectorLink.weight * weight;
				}

				lastPosition = transform.position;
				lastTime = Time.time;
			}
		}

		/// <summary>
		/// Limiting effector position offsets
		/// </summary>
		[System.Serializable]
		public class OffsetLimits {

			public FullBodyBipedEffector effector; // The effector type (this is just an enum)
			public float spring = 0f; // Spring force, if zero then this is a hard limit, if not, offset can exceed the limit.
			public bool x, y, z; // Which axes to limit the offset on?
			public float minX, maxX, minY, maxY, minZ, maxZ; // The limits

			// Apply the limit to the effector
			public void Apply(IKEffector e, Quaternion rootRotation) {
				Vector3 offset = Quaternion.Inverse(rootRotation) * e.positionOffset;

				if (spring <= 0f) {
					// Hard limits
					if (x) offset.x = Mathf.Clamp(offset.x, minX, maxX);
					if (y) offset.y = Mathf.Clamp(offset.y, minY, maxY);
					if (z) offset.z = Mathf.Clamp(offset.z, minZ, maxZ);
				} else {
					// Soft limits
					if (x) offset.x = SpringAxis(offset.x, minX, maxX);
					if (y) offset.y = SpringAxis(offset.y, minY, maxY);
					if (z) offset.z = SpringAxis(offset.z, minZ, maxZ);
				}

				// Apply to the effector
				e.positionOffset = rootRotation * offset;
			}

			// Just math for limiting floats
			private float SpringAxis(float value, float min, float max) {
				if (value > min && value < max) return value;
				if (value < min) return Spring(value, min, true);
				return Spring(value, max, false);
			}

			// Spring math
			private float Spring(float value, float limit, bool negative) {
				float illegal = value - limit;
				float s = illegal * spring;

				if (negative) return value + Mathf.Clamp(-s, 0, -illegal);
				return value - Mathf.Clamp(s, 0, illegal);
			}
		}

		public FullBodyBipedIK ik; // Reference to the IK component
		public float weight = 1f; // The master Weight
		public Body[] bodies; // The array of Bodies
		public OffsetLimits[] limits; // The array of OffsetLimits

		// Reset all Bodies
		public void ResetBodies() {
			foreach (Body body in bodies) body.Reset();
		}

		void Awake() {
			// You can use just LateUpdate, but note that it doesn't work when you have animatePhysics turned on for the character.
			ik.solver.OnPreUpdate += ModifyOffset;

			// Apply the default effector modes
			ik.solver.leftHandEffector.mode = IKEffector.Mode.Free;
			ik.solver.rightHandEffector.mode = IKEffector.Mode.Free;

			ik.solver.bodyEffector.effectChildNodes = false;
		}

		// Called by IKSolverFullBody before updating
		// You can use just LateUpdate, but note that it doesn't work when you have animatePhysics turned on for the character.
		private void ModifyOffset() {
			weight = Mathf.Clamp(weight, 0f, weight);
			
			// Update the Bodies
			foreach (Body body in bodies) body.Update(ik.solver, weight);

			// Apply the OffsetLimits
			foreach (OffsetLimits limit in limits) {
				limit.Apply(ik.solver.GetEffector(limit.effector), transform.rotation);
			}
		}
	}
}
