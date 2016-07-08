using UnityEngine;
using System.Collections;
using RootMotion.FinalIK;

namespace RootMotion.FinalIK.Demos {

	/// <summary>
	/// Demo for offsetting Effectors.
	/// </summary>
	public class EffectorOffset : MonoBehaviour {

		public FullBodyBipedIK ik; // Reference to the FBBIK component

		// Effector modes
		public IKEffector.Mode handsEffectorMode = IKEffector.Mode.Free;
		public IKEffector.Mode feetEffectorMode = IKEffector.Mode.MaintainAnimatedPosition;

		// The offset vectors for each effector
		public Vector3 bodyOffset, leftShoulderOffset, rightShoulderOffset, leftThighOffset, rightThighOffset, leftHandOffset, rightHandOffset, leftFootOffset, rightFootOffset;

		void LateUpdate() {
			// Setting effector modes each frame here so you could see the effect they have.

			// EffectorMode.Free - effector has no effect on the bones if it is not weighed in.
			// EffectorMode.MaintainAnimatedPosition - the bone of the effector will maintain it's animated position until pulled from it
			// EffectorMode.MaintainRelativePosition - the bone of the effector will maintain it's position relative to it's parent triangle's rotation {root node, left shoulder, right shoulder} for the hands
			// {root node, left thigh, right thigh} for the feet.
			ik.solver.leftHandEffector.mode = handsEffectorMode;
			ik.solver.rightHandEffector.mode = handsEffectorMode;
			ik.solver.leftFootEffector.mode = feetEffectorMode;
			ik.solver.rightFootEffector.mode = feetEffectorMode;

			// Apply position offsets relative to this GameObject's rotation.
			ik.solver.bodyEffector.positionOffset += transform.rotation * bodyOffset;
			ik.solver.leftShoulderEffector.positionOffset += transform.rotation * leftShoulderOffset;
			ik.solver.rightShoulderEffector.positionOffset += transform.rotation * rightShoulderOffset;
			ik.solver.leftThighEffector.positionOffset += transform.rotation * leftThighOffset;
			ik.solver.rightThighEffector.positionOffset += transform.rotation * rightThighOffset;
			ik.solver.leftHandEffector.positionOffset += transform.rotation * leftHandOffset;
			ik.solver.rightHandEffector.positionOffset += transform.rotation * rightHandOffset;
			ik.solver.leftFootEffector.positionOffset += transform.rotation * leftFootOffset;
			ik.solver.rightFootEffector.positionOffset += transform.rotation * rightFootOffset;

			// NB! effector position offsets are reset to Vector3.zero after FBBIK update is complete. 
			// This enables to have more than one script modifying the position offset of effectors.
			// Therefore instead of writing effector.positionOffset = value, write effector.positionOffset += value instead.
		}
	}
}
