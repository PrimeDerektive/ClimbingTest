using UnityEngine;
using System.Collections;
using RootMotion.FinalIK;

namespace RootMotion.FinalIK.Demos {

	/// <summary>
	/// Procedural body tilting with FBBIK.
	/// </summary>
	public class BodyTilt: MonoBehaviour {

		public FullBodyBipedIK ik; // Reference to the FullBodyBipedIK component
		public float tiltSpeed = 6f; // Speed of tilting
		public float tiltSensitivity = 0.07f; // Sensitivity of tilting
		public OffsetPose poseLeft, poseRight; // The OffsetPose components

		private float tiltAngle;
		private Vector3 lastForward;
		private float lastTime;

		void Awake() {
			// You can use just LateUpdate, but note that it doesn't work when you have animatePhysics turned on for the character.
			ik.solver.OnPreUpdate += ModifyOffset;

			// Apply the default effector modes
			ik.solver.leftHandEffector.mode = IKEffector.Mode.Free;
			ik.solver.rightHandEffector.mode = IKEffector.Mode.Free;

			ik.solver.bodyEffector.effectChildNodes = false;

			// Store current character forward axis and Time
			lastForward = transform.forward;
			lastTime = Time.time;
		}

		// Called by IKSolverFullBody before updating
		private void ModifyOffset() {
			// Implementing our own delta time, because we don't know if this function will be called in each LateUpdate (normal characters) or after each FixedUpdate for characters with animatePhysics enabled.
			float deltaTime = Time.time - lastTime;
			if (deltaTime == 0) return;

			// Calculate the angular delta in character rotation
			Quaternion change = Quaternion.FromToRotation(lastForward, transform.forward);
			float deltaAngle = 0;
			Vector3 axis = Vector3.zero;
			change.ToAngleAxis(out deltaAngle, out axis);
			if (axis.y > 0) deltaAngle = -deltaAngle;

			deltaAngle *= tiltSensitivity * 0.01f;
			deltaAngle /= deltaTime;
			deltaAngle = Mathf.Clamp(deltaAngle, -1f, 1f);

			tiltAngle = Mathf.Lerp(tiltAngle, deltaAngle, deltaTime * tiltSpeed);

			// Applying positionOffsets
			float tiltF = Mathf.Abs(tiltAngle) / 1f;
			if (tiltAngle < 0) poseRight.Apply(ik.solver, tiltF);
			else poseLeft.Apply(ik.solver, tiltF);

			// Store current character forward axis and Time
			lastForward = transform.forward;
			lastTime = Time.time;
		}
	}
}
