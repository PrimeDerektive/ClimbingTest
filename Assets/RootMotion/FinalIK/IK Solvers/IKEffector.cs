using UnityEngine;
using System.Collections;

namespace RootMotion.FinalIK {

	/// <summary>
	///  Effector for manipulating node based %IK solvers.
	/// </summary>
	[System.Serializable]
	public class IKEffector {
		
		#region Main Interface

		/// <summary>
		/// Gets the main node.
		/// </summary>
		public IKSolver.Node GetNode() {
			return node;
		}

		/// <summary>
		/// If MaintainAnimatedPosition, keeps the bone at it's animated position as long as possible (applies only to end-effectors).
		/// If MaintainRelativePosition, keeps the node position relative to the triangle defined by the plane bones (overrides maintainAnimatedPositionWeight and applies only to end-effectors).
		/// This is used for instance when we need to maintain the hand's position relative to the solved chest rotation, not maintain the hand's animated position
		/// </summary>
		[System.Serializable]
		public enum Mode {
			Free,
			MaintainAnimatedPosition,
			MaintainRelativePosition
		}
		
		/// <summary>
		/// The node transform used by this effector.
		/// </summary>
		public Transform bone;
		/// <summary>
		/// The optinal list of other bones that positionOffset and position of this effector will be applied to.
		/// </summary>
		public Transform[] childBones = new Transform[0];
		/// <summary>
		/// The first bone defining the parent plane.
		/// </summary>
		public Transform planeBone1;
		/// <summary>
		/// The second bone defining the parent plane.
		/// </summary>
		public Transform planeBone2;
		/// <summary>
		/// The third bone defining the parent plane.
		/// </summary>
		public Transform planeBone3;
		/// <summary>
		/// The position weight.
		/// </summary>
		public float positionWeight;
		/// <summary>
		/// The rotation weight.
		/// </summary>
		public float rotationWeight;
		/// <summary>
		/// The effector position in world space.
		/// </summary>
		public Vector3 position = Vector3.zero;
		/// <summary>
		/// The effector rotation relative to default rotation in world space.
		/// </summary>
		public Quaternion rotation = Quaternion.identity;
		/// <summary>
		/// The position offset in world space. positionOffset will be reset to Vector3.zero each frame after the solver is complete.
		/// </summary>
		public Vector3 positionOffset;
		/// <summary>
		/// Is this the last effector of a node chain?
		/// </summary>
		public bool isEndEffector;
		/// <summary>
		/// If MaintainAnimatedPosition, keeps the bone at it's animated position as long as possible (applies only to end-effectors).
		/// If MaintainRelativePosition, keeps the node position relative to the triangle defined by the plane bones (overrides maintainAnimatedPositionWeight and applies only to end-effectors).
		/// This is used for instance when we need to maintain the hand's position relative to the solved chest rotation, not maintain the hand's animated position
		/// </summary>
		public Mode mode;
		/// <summary>
		/// If false, child nodes will be ignored by this effector (if it has any).
		/// </summary>
		public bool effectChildNodes = true;

		#endregion Main Interface

		public Quaternion planeRotationOffset = Quaternion.identity; // Used by Bend Constraints
		
		private IKSolver.Node node = new IKSolver.Node(), planeNode1 = new IKSolver.Node(), planeNode2 = new IKSolver.Node(), planeNode3 = new IKSolver.Node();
		private IKSolver.Node[] childNodes = new IKSolver.Node[0];
		private IKSolver solver;
		private float posW, rotW;
		private Vector3[] localPositions = new Vector3[0];
		private bool usePlaneNodes;
		private Quaternion animatedPlaneRotation = Quaternion.identity;
		
		public IKEffector() {}
		
		public IKEffector (Transform bone, Transform[] childBones) {
			this.bone = bone;
			this.childBones = childBones;
		}
		
		/*
		 * Determines whether this IKEffector is valid or not.
		 * */
		public bool IsValid(IKSolver solver, Warning.Logger logger) {
			if (bone == null) {
				if (logger != null) logger("IK Effector bone is null.");
				return false;
			}
			
			if (solver.GetPoint(bone) == null) {
				if (logger != null) logger("IK Effector is referencing to a bone '" + bone.name + "' that does not excist in the Node Chain.");
				return false;
			}

			foreach (Transform b in childBones) {
				if (b == null) {
					if (logger != null) logger("IK Effector contains a null reference.");
					return false;
				}
			}
			
			foreach (Transform b in childBones) {
				if (solver.GetPoint(b) == null) {
					if (logger != null) logger("IK Effector is referencing to a bone '" + b.name + "' that does not excist in the Node Chain.");
					return false;
				}
			}

			if (planeBone1 != null && solver.GetPoint(planeBone1) == null) {
				if (logger != null) logger("IK Effector is referencing to a bone '" + planeBone1.name + "' that does not excist in the Node Chain.");
				return false;
			}

			if (planeBone2 != null && solver.GetPoint(planeBone2) == null) {
				if (logger != null) logger("IK Effector is referencing to a bone '" + planeBone2.name + "' that does not excist in the Node Chain.");
				return false;
			}

			if (planeBone3 != null && solver.GetPoint(planeBone3) == null) {
				if (logger != null) logger("IK Effector is referencing to a bone '" + planeBone3.name + "' that does not excist in the Node Chain.");
				return false;
			}

			return true;
		}
		
		/*
		 * Initiate the effector, set default values
		 * */
		public void Initiate(IKSolver solver) {
			this.solver = solver;
			position = bone.position;
			rotation = bone.rotation;

			// Getting the node
			node = solver.GetPoint(bone) as IKSolver.Node;

			// Child nodes
			if (childNodes.Length != childBones.Length) childNodes = new IKSolver.Node[childBones.Length];

			for (int i = 0; i < childBones.Length; i++) {
				childNodes[i] = solver.GetPoint(childBones[i]) as IKSolver.Node;
			}

			if (localPositions.Length != childBones.Length) localPositions = new Vector3[childBones.Length];

			// Plane nodes
			usePlaneNodes = false;

			if (planeBone1 != null) {
				planeNode1 = solver.GetPoint(planeBone1) as IKSolver.Node;

				if (planeBone2 != null) {
					planeNode2 = solver.GetPoint(planeBone2) as IKSolver.Node;

					if (planeBone3 != null) {
						planeNode3 = solver.GetPoint(planeBone3) as IKSolver.Node;
						usePlaneNodes = true;
					}
				}
			}
		}
		
		/*
		 * Clear node offset
		 * */
		public void ResetOffset() {
			node.offset = Vector3.zero;
			for (int i = 0; i < childNodes.Length; i++) childNodes[i].offset = Vector3.zero;
		}
		
		/*
		 * Presolving, applying offset
		 * */
		public void OnPreSolve() {
			positionWeight = Mathf.Clamp(positionWeight, 0f, 1f);
			rotationWeight = Mathf.Clamp(rotationWeight, 0f, 1f);

			// Calculating weights
			posW = positionWeight * solver.GetIKPositionWeight() * solver.GetIKPositionWeight();
			rotW = rotationWeight * solver.GetIKPositionWeight();

			node.effectorPositionWeight = posW;
			node.effectorRotationWeight = rotW;

			node.solverRotation = rotation;

			if (float.IsInfinity(positionOffset.x) ||
				float.IsInfinity(positionOffset.y) ||
				float.IsInfinity(positionOffset.z)
			    ) Debug.LogError("Invalid IKEffector.positionOffset (contains Infinity)! Please make sure not to set IKEffector.positionOffset to infinite values.", bone);

			if (float.IsNaN(positionOffset.x) ||
			    float.IsNaN(positionOffset.y) ||
			    float.IsNaN(positionOffset.z)
			    ) Debug.LogError("Invalid IKEffector.positionOffset (contains NaN)! Please make sure not to set IKEffector.positionOffset to NaN values.", bone);

			if (positionOffset.sqrMagnitude > 10000000000f) Debug.LogError("Additive effector positionOffset detected in Full Body IK (extremely large value). Make sure you are not adding to effector positionOffset in each frame.");

			node.offset += positionOffset;

			if (effectChildNodes) {
				for (int i = 0; i < childNodes.Length; i++) {
				localPositions[i] = childNodes[i].transform.position - node.transform.position;
					childNodes[i].offset += positionOffset;
				}
			}

			// Relative to Plane
			if (usePlaneNodes && mode == Mode.MaintainRelativePosition) {
				animatedPlaneRotation = Quaternion.LookRotation(planeNode2.transform.position - planeNode1.transform.position, planeNode3.transform.position - planeNode1.transform.position);;
			}
		}

		/// <summary>
		/// Called after writing the pose
		/// </summary>
		public void OnPostWrite() {
			positionOffset = Vector3.zero;
		}

		/*
		* Rotation of plane nodes in the solver
		* */
		private Quaternion planeRotation {
			get {
				return Quaternion.LookRotation(planeNode2.solverPosition - planeNode1.solverPosition, planeNode3.solverPosition - planeNode1.solverPosition);
			}
		}

		/*
		 * Manipulating node solverPosition
		 * */
		public void Update() {
			if (float.IsInfinity(position.x) ||
			    float.IsInfinity(position.y) ||
			    float.IsInfinity(position.z)
			    ) Debug.LogError("Invalid IKEffector.position (contains Infinity)!");
			/*
			// Translating the main node
			Vector3 p = isEndEffector && (maintainAnimatedPosition || maintainRelativePosition)? node.transform.position: node.solverPosition;
			
			if (maintainRelativePosition) {

				Vector3 dir = p - planeNode1.transform.position;

				planeRotationOffset = planeRotation * Quaternion.Inverse(animatedPlaneRotation);

				p = planeNode1.solverPosition + planeRotationOffset * dir;
				planeRotationOffset = Quaternion.Lerp(planeRotationOffset, Quaternion.identity, posW);
			} else planeRotationOffset = Quaternion.identity;

			if (isEndEffector && maintainAnimatedPosition || maintainRelativePosition) p += node.offset;

			node.solverPosition = Vector3.Lerp(p, position, posW);
			*/

			node.solverPosition = Vector3.Lerp(GetPosition(out planeRotationOffset), position, posW);

			// Child nodes
			if (!effectChildNodes) return;
			
			for (int i = 0; i < childNodes.Length; i++) {
				childNodes[i].solverPosition = Vector3.Lerp(childNodes[i].solverPosition, node.solverPosition + localPositions[i], posW);
			}
		}

		private Vector3 GetPosition(out Quaternion planeRotationOffset) {
			planeRotationOffset = Quaternion.identity;
			if (!isEndEffector) return node.solverPosition;

			switch(mode) {
			case Mode.MaintainAnimatedPosition: return node.transform.position + node.offset;
			case Mode.MaintainRelativePosition:
				Vector3 p = node.transform.position;
				
				Vector3 dir = p - planeNode1.transform.position;

				planeRotationOffset = planeRotation * Quaternion.Inverse(animatedPlaneRotation);
				
				p = planeNode1.solverPosition + planeRotationOffset * dir;
				planeRotationOffset = Quaternion.Lerp(planeRotationOffset, Quaternion.identity, posW);

				return p + node.offset;
			default: return node.solverPosition;
			}
		}
	}
}