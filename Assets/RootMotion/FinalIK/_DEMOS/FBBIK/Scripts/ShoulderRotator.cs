using UnityEngine;
using System.Collections;
using RootMotion.FinalIK;

namespace RootMotion.FinalIK.Demos {

	/// <summary>
	/// Shoulder rotator is a workaround for FBBIK not rotating the shoulder bones when pulled by hands.
	/// It get's the job done if you need it, but will take 2 solving iterations.
	/// </summary>
	public class ShoulderRotator : MonoBehaviour {

		public FullBodyBipedIK ik; // Reference to the FullBodyBipedIK component
		public float weight = 1.5f; // Weight of shoulder rotation
		public float offset = 0.2f; // The greater the offset, the sooner the shoulder will start rotating

		void LateUpdate () {
			if (ik == null) return;
			if (ik.enabled) ik.Disable();

			// We need to update FFBIK first, so we get the more-or-less pose and we know how the chest of the character is rotated. We can calculate shoulder rotation offset based on that.

			ik.solver.Update(); // Update the FBBIK once. You might want to set ShoulderRotator's script execution order to execute after any other script that you have.

			RotateShoulder(FullBodyBipedChain.LeftArm, weight, offset); // Rotate the left shoulder
			RotateShoulder(FullBodyBipedChain.RightArm, weight, offset); // Rotate the right shoulder

			ik.solver.Update(); // Update FBBIK again with the rotated shoulders
		}

		// Rotates a shoulder of a FBBIK character
		private void RotateShoulder(FullBodyBipedChain chain, float weight, float offset) {
			// Get FromToRotation from the current swing direction of the shoulder to the IK target direction
			Quaternion fromTo = Quaternion.FromToRotation(GetParentBoneMap(chain).swingDirection, ik.solver.GetEndEffector(chain).position - GetParentBoneMap(chain).transform.position);

			// Direction to the IK target
			Vector3 toTarget = ik.solver.GetEndEffector(chain).position - ik.solver.GetLimbMapping(chain).bone1.position;

			// Length of the limb
			float limbLength = ik.solver.GetChain(chain).nodes[0].length + ik.solver.GetChain(chain).nodes[1].length;

			// Divide IK Target direction magnitude by limb length to know how much the limb is being pulled
			float delta = (toTarget.magnitude / limbLength) - 1f + offset;
			delta = Mathf.Clamp(delta * weight, 0f, 1f);

			// Calculate the rotation offset for the shoulder
			Quaternion rotationOffset = Quaternion.Lerp(Quaternion.identity, fromTo, delta * ik.solver.GetEndEffector(chain).positionWeight);

			// Rotate the shoulder
			ik.solver.GetLimbMapping(chain).parentBone.rotation = rotationOffset * ik.solver.GetLimbMapping(chain).parentBone.rotation;
		}

		// Get the shoulder BoneMap
		private IKMapping.BoneMap GetParentBoneMap(FullBodyBipedChain chain) {
			return ik.solver.GetLimbMapping(chain).GetBoneMap(IKMappingLimb.BoneMapType.Parent);
		}
	}
}
