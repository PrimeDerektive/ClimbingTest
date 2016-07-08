using UnityEngine;
using System.Collections;

namespace RootMotion.FinalIK.Demos {

	/// <summary>
	/// Posing the children of a Transform to match the children of another Transform
	/// </summary>
	public class HandPoser : MonoBehaviour {
		
		public Transform poseRoot; // Reference to the other Transform (should be identical to this one)
		public float localRotationWeight = 1f; // Weight of localRotation matching
		public float localPositionWeight; // Weight of localPosition matching
		
		private Transform _poseRoot;
		private Transform[] children;
		private Transform[] poseChildren;
		
		void Start() {
			// Find the children
			children = (Transform[])GetComponentsInChildren<Transform>();
		}
		
		void LateUpdate() {
			if (localPositionWeight <= 0 && localRotationWeight <= 0) return;
			if (poseRoot == null) return;

			// Get the children, if we don't have them already
			if (_poseRoot != poseRoot) {
				poseChildren = (Transform[])poseRoot.GetComponentsInChildren<Transform>();
				_poseRoot = poseRoot;
			}

			// Something went wrong
			if (children.Length != poseChildren.Length) {
				Debug.LogWarning("Number of children does not match with the pose");
				return;
			}

			// Lerping the localRotation and the localPosition
			for (int i = 0; i < children.Length; i++) {
				if (children[i] != transform) {
					children[i].localRotation = Quaternion.Lerp(children[i].localRotation, poseChildren[i].localRotation, localRotationWeight);
					children[i].localPosition = Vector3.Lerp(children[i].localPosition, poseChildren[i].localPosition, localPositionWeight);
				}
			}
		}
	}
}
