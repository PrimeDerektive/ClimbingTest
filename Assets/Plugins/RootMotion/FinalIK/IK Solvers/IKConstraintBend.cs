using UnityEngine;
using System.Collections;

	namespace RootMotion.FinalIK {

	/// <summary>
	/// %Constraint used for fixing bend direction of 3-segment node chains in a node based %IK solver. 
	/// </summary>
	[System.Serializable]
	public class IKConstraintBend {
		
		#region Main Interface

		[System.Serializable]
		public enum BendBone {
			First,
			Second,
			Third
		}

		/// <summary>
		/// The first bone.
		/// </summary>
		public Transform bone1;
		/// <summary>
		/// The second (bend) bone.
		/// </summary>
		public Transform bone2;
		/// <summary>
		/// The third bone.
		/// </summary>
		public Transform bone3;
		
		/// <summary>
		/// The bend direction.
		/// </summary>
		public Vector3 direction = Vector3.right;
		/// <summary>
		/// The bone, that's rotation will be used to sample the bend direction.
		/// </summary>
		public BendBone bendBone = BendBone.First;

		/// <summary>
		/// The bend rotation offset.
		/// </summary>
		public Quaternion rotationOffset;
		
		/// <summary>
		/// The weight. If weight is 1, will override effector rotation and the joint will be rotated at the direction. This enables for direct manipulation of the bend direction independent of effector rotation.
		/// </summary>
		public float weight = 0f;
		
		/// <summary>
		/// Determines whether this IKConstraintBend is valid.
		/// </summary>
		public bool IsValid(IKSolverFullBody solver, Warning.Logger logger) {
			if (bone1 == null || bone2 == null || bone3 == null) {
				if (logger != null) logger("Bend Constraint contains a null reference.");
				return false;
			}
			if (solver.GetPoint(bone1) == null) {
				if (logger != null) logger("Bend Constraint is referencing to a bone '" + bone1.name + "' that does not excist in the Node Chain.");
				return false;
			}
			if (solver.GetPoint(bone2) == null) {
				if (logger != null) logger("Bend Constraint is referencing to a bone '" + bone2.name + "' that does not excist in the Node Chain.");
				return false;
			}
			if (solver.GetPoint(bone3) == null) {
				if (logger != null) logger("Bend Constraint is referencing to a bone '" + bone3.name + "' that does not excist in the Node Chain.");
				return false;
			}
			return true;
		}
		
		#endregion Main Interface
		
		private IKSolver.Node node1, node2, node3;
		private Vector3 defaultLocalDirectionNode1, defaultLocalDirectionNode2, defaultLocalDirectionNode3, defaultChildDirection;

		public IKConstraintBend() {}
		
		public IKConstraintBend(Transform bone1, Transform bone2, Transform bone3) {
			SetBones(bone1, bone2, bone3);
		}
		
		public void SetBones(Transform bone1, Transform bone2, Transform bone3) {
			this.bone1 = bone1;
			this.bone2 = bone2;
			this.bone3 = bone3;
		}
		
		/*
		 * Initiate the constraint and set defaults
		 * */
		public void Initiate(IKSolverFullBody solver) {
			node1 = solver.GetPoint(bone1) as IKSolver.Node;
			node2 = solver.GetPoint(bone2) as IKSolver.Node;
			node3 = solver.GetPoint(bone3) as IKSolver.Node;
		
			// Find the default bend direction orthogonal to the chain direction
			direction = OrthoToLimb(node2.transform.position - node1.transform.position);
			
			// Default bend direction relative to the first node
			defaultLocalDirectionNode1 = Quaternion.Inverse(node1.transform.rotation) * direction;
			defaultLocalDirectionNode2 = Quaternion.Inverse(node2.transform.rotation) * direction;
			defaultLocalDirectionNode3 = Quaternion.Inverse(node3.transform.rotation) * direction;
			
			// Default plane normal
			Vector3 defaultNormal = Vector3.Cross(node3.solverPosition - node1.solverPosition, direction);
			
			// Default plane normal relative to the third node
			defaultChildDirection = Quaternion.Inverse(node3.transform.rotation) * defaultNormal;
		}

		/*
		 * Apply the bend constraint
		 * */
		public void Solve() {
			weight = Mathf.Clamp(weight, 0f, 1f);

			direction = direction.normalized;
			Vector3 dir = direction;
			dir = rotationOffset * dir;

			// Maintaining bend
			// Rotating the default bend direction by offset from the initial chain direction
			Quaternion f = Quaternion.FromToRotation(node3.transform.position - node1.transform.position, node3.solverPosition - node1.solverPosition);
			dir = f * (rotationOffset * bendBoneTransform.rotation * defaultLocalDirection);

			// Effector rotation
			if (node3.effectorRotationWeight > 0f) {
				// Bend direction according to the effector rotation
				Vector3 effectorDirection = -Vector3.Cross(node3.solverPosition - node1.solverPosition, node3.solverRotation * defaultChildDirection);
				dir = Vector3.Lerp(dir, effectorDirection, node3.effectorRotationWeight);
			}
			
			dir = Vector3.Lerp(dir, direction, weight);
			
			// Get the direction to node2 ortho-normalized to the chain direction
			Vector3 directionTangent = OrthoToLimb(dir);
			Vector3 node2Tangent = OrthoToLimb(node2.solverPosition - node1.solverPosition);
			
			// Rotation from the current position to the desired position
			Quaternion fromTo = QuaTools.FromToAroundAxis(node2Tangent, directionTangent, (node3.solverPosition - node1.solverPosition).normalized);

			// Repositioning node2
			Vector3 to2 = node2.solverPosition - node1.solverPosition;
			node2.solverPosition = node1.solverPosition + fromTo * to2;
		}

		/*
		 *  Gets the default local direction of the bend bone
		 * */
		private Vector3 defaultLocalDirection {
			get {
				switch(bendBone) {
				case BendBone.First: return defaultLocalDirectionNode1;
				case BendBone.Second: return defaultLocalDirectionNode2;
				default: return defaultLocalDirectionNode3;
				}
			}
		}

		/*
		 *  Gets the bend bone Transform.
		 * */
		private Transform bendBoneTransform {
			get {
				switch(bendBone) {
				case BendBone.First: return node1.transform;
				case BendBone.Second: return node2.transform;
				default: return node3.transform;
				}
			}
		}
		
		/*
		 * Ortho-Normalize a vector to the chain direction
		 * */
		private Vector3 OrthoToLimb(Vector3 tangent) {
			Vector3 normal = node3.solverPosition - node1.solverPosition;
			Vector3.OrthoNormalize(ref normal, ref tangent);
			return tangent;
		}
	}
}
