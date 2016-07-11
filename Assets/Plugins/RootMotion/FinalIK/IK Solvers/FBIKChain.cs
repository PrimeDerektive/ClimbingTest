using UnityEngine;
using System.Collections;
using System;

namespace RootMotion.FinalIK {

	/// <summary>
	/// A chain of bones in IKSolverFullBody.
	/// </summary>
	[System.Serializable]
	public class FBIKChain {
		
		#region Main Interface
		
		/// <summary>
		/// Linear constraint between child chains of a FBIKChain.
		/// </summary>
		[System.Serializable]
		public class ChildConstraint {
			
			/// <summary>
			/// The first bone.
			/// </summary>
			public Transform bone1;
			/// <summary>
			/// The second bone.
			/// </summary>
			public Transform bone2;
			/// <summary>
			/// The push elasticity.
			/// </summary>
			public float pushElasticity = 0f;
			/// <summary>
			/// The pull elasticity.
			/// </summary>
			public float pullElasticity = 0f;
			/// <summary>
			/// Gets the nominal (animated) distance between the two bones.
			/// </summary>
			public float nominalDistance { get; private set; }
			/// <summary>
			/// Gets the chain of the first bone.
			/// </summary>
			public FBIKChain chain1 { get; private set; }
			/// <summary>
			/// Gets the chain of the second bone.
			/// </summary>
			public FBIKChain chain2 { get; private set; }
			/// <summary>
			/// The constraint is rigid if both push and pull elasticity are 0.
			/// </summary>
			public bool isRigid { get { return pushElasticity <= 0 && pullElasticity <= 0; }}
			
			private IKSolver.Node node1, node2;
			
			/*
			 * Constructor
			 * */
			public ChildConstraint(Transform bone1, Transform bone2, float pushElasticity = 0f, float pullElasticity = 0f) {
				this.bone1 = bone1;
				this.bone2 = bone2;
				this.pushElasticity = pushElasticity;
				this.pullElasticity = pullElasticity;
			}
			
			/*
			 * Initiating the constraint
			 * */
			public void Initiate(IKSolverFullBody solver) {
				chain1 = solver.GetChain(bone1);
				chain2 = solver.GetChain(bone2);
				
				node1 = chain1.nodes[0];
				node2 = chain2.nodes[0];
				
				OnPreSolve();
			}
			
			/*
			 * Updating nominal distance because it might have changed in the animation
			 * */
			public void OnPreSolve() {
				nominalDistance = Vector3.Distance(node1.transform.position, node2.transform.position);
			}
			
			/*
			 * Solving the constraint
			 * */
			public void Solve(float pull) {
				if (pushElasticity >= 1 && pullElasticity >= 1) return;
				
				float distance = Vector3.Distance(node1.solverPosition, node2.solverPosition);
				
				float elasticity = distance > nominalDistance? pullElasticity: pushElasticity;
				
				float force = 1f - Mathf.Clamp(elasticity, 0f, 1f);
				
				force *= 1f - nominalDistance / distance;
				if (force == 0) return;
				
				Vector3 offset = (node2.solverPosition - node1.solverPosition) * force;
				
				node1.solverPosition += offset * pull;
				node2.solverPosition -= offset * (1f - pull);
			}
		}

		/// <summary>
		/// The pin weight. If closer to 1, the chain will be less influenced by child chains.
		/// </summary>
		public float pin;
		/// <summary>
		/// The weight of pulling the parent chain.
		/// </summary>
		public float pull = 1f;
		/// <summary>
		/// Only used in 3 segmented chains, pulls the first node closer to the third node.
		/// </summary>
		public float reach;
		/// <summary>
		/// The nodes in this chain.
		/// </summary>
		public IKSolver.Node[] nodes = new IKSolver.Node[0];
		/// <summary>
		/// The child chains.
		/// </summary>
		public FBIKChain[] children = new FBIKChain[0];
		/// <summary>
		/// The child constraints are used for example for fixing the distance between left upper arm and right upper arm
		/// </summary>
		public ChildConstraint[] childConstraints = new ChildConstraint[0];
		
		#endregion Main Interface
		
		private float rootLength;
		private bool initiated;
		private IKSolver.Point p;

		public FBIKChain() {}
		
		public FBIKChain (float pin, float pull, params Transform[] nodeTransforms) {
			this.pin = pin;
			this.pull = pull;
			
			SetNodes(nodeTransforms);
			
			children = new FBIKChain[0];
		}
		
		/*
		 * Set nodes to the following bone transforms.
		 * */
		public void SetNodes(params Transform[] boneTransforms) {
			nodes = new IKSolver.Node[boneTransforms.Length];
			for (int i = 0; i < boneTransforms.Length; i++) {
				nodes[i] = new IKSolver.Node(boneTransforms[i]);
			}
		}
		
		/*
		 * Check if this chain is valid or not.
		 * */
		public bool IsValid(Warning.Logger logger = null) {
			if (nodes.Length == 0) {
				if (logger != null) logger("FBIK chain contains no nodes.");
				return false;
			}
			
			foreach (IKSolver.Node node in nodes) if (node.transform == null) {
				if (logger != null) logger("Node transform is null in FBIK chain.");
				return false;
			}
			
			foreach (FBIKChain c in children) if (!c.IsValid(logger)) return false;

			return true;
		}
		
		#region Recursive Methods
		
		/*
		 * Initiating the chain.
		 * */
		public void Initiate(IKSolver solver) {
			initiated = false;
			
			foreach (IKSolver.Node node in nodes) {
				node.solverPosition = node.transform.position;
			}
			
			// Calculating bone lengths
			for (int i = 0; i < nodes.Length - 1; i++) {
				nodes[i].length = Vector3.Distance(nodes[i].transform.position, nodes[i + 1].transform.position);
				if (nodes[i].length == 0) return;
			}
			
			foreach (FBIKChain c in children) {
				c.rootLength = (c.nodes[0].transform.position - nodes[nodes.Length - 1].transform.position).magnitude;
				if (c.rootLength == 0) return; 
			}
			
			// Initiating child chains
			foreach (FBIKChain c in children) {
				c.Initiate(solver);
			}
			
			// Initiating child constraints
			InitiateConstraints(solver);
			
			initiated = true;
		}

		/*
		 * Set solver positions
		 * */
		private void PreSolveNodes() {
			for (int i = 0; i < nodes.Length; i++) {

				nodes[i].solverPosition = nodes[i].transform.position + nodes[i].offset;
			}

			// Pre-update child chains
			for (int i = 0; i < children.Length; i++) children[i].PreSolveNodes();
		}
		
		/*
		 * In case animation has changed bone lengths
		 * */
		private void UpdateBoneLengths() {
			// Calculating bone lengths
			for (int i = 0; i < nodes.Length - 1; i++) {
				nodes[i].length = Vector3.Distance(nodes[i].transform.position, nodes[i + 1].transform.position);
			}

			for (int i = 0; i < children.Length; i++) {
				children[i].rootLength = (children[i].nodes[0].transform.position - nodes[nodes.Length - 1].transform.position).magnitude;
			}
			
			// Pre-update child constraints
			PreSolveConstraints();
			
			// Initiating child chains
			for (int i = 0; i < children.Length; i++) {
				children[i].UpdateBoneLengths();
			}
		}

		/*
		 * Reaching limbs
		 * */
		public void Reach(int iteration) {
			if (!initiated) return;

			// Solve children first
			for (int i = 0; i < children.Length; i++) children[i].Reach(iteration);

			if (nodes.Length != 3) return;

			float r = reach * Mathf.Clamp(nodes[2].effectorPositionWeight, 0f, 1f);

			if (r > 0) {

				float limbLength = nodes[0].length + nodes[1].length;

				Vector3 limbDirection = nodes[2].solverPosition - nodes[0].solverPosition;
				if (limbDirection == Vector3.zero) return;
				
				float currentLength = limbDirection.magnitude;

				//Reaching
				Vector3 straight = (limbDirection / currentLength) * limbLength;
				
				float delta = currentLength / limbLength;
				delta = Mathf.Clamp(delta, 1 - r, 1 + r);
				delta -= 1f;
				delta = Mathf.Clamp(delta + r, -1f, 1f);
				
				Vector3 offset = straight * Mathf.Clamp(delta, 0f, currentLength);

				nodes[0].solverPosition += offset * (1f - nodes[0].effectorPositionWeight);
				nodes[2].solverPosition += offset;
			}
		}

		/*
		 * Applying trigonometric IK solver on the 3 segmented chains to relieve tension from the solver and increase accuracy.
		 * */
		public void SolveTrigonometric() {
			if (!initiated) return;
			
			// Solve children first
			for (int i = 0; i < children.Length; i++) children[i].SolveTrigonometric();
			
			if (nodes.Length != 3) return;
			
			float limbLength = nodes[0].length + nodes[1].length;

			// Trigonometry
			Vector3 limbDirection = nodes[2].solverPosition - nodes[0].solverPosition;
			if (limbDirection == Vector3.zero) return;

			float limbMag = limbDirection.magnitude;

			float maxMag = Mathf.Clamp(limbMag, 0f, limbLength * 0.999f);
			Vector3 direction = (limbDirection / limbMag) * maxMag;

			Vector3 bendDirection = GetBendDirection(direction, maxMag, nodes[0], nodes[1]);
			
			nodes[1].solverPosition = nodes[0].solverPosition + bendDirection;
		}
		
		/*
		 * Stage 1 of the FABRIK algorithm
		 * */
		public void Stage1() {
			// Stage 1
			for (int i = 0; i < children.Length; i++) children[i].Stage1();
			
			// If is the last chain in this hierarchy, solve immediatelly and return
			if (children.Length == 0) {
				ForwardReach(nodes[nodes.Length - 1].solverPosition);
				return;
			}
			
			// Finding the total pull force by all child chains
			float pullParentSum = 0f;
			for (int i = 0; i < children.Length; i++) pullParentSum += children[i].pull;
			Vector3 centroid = nodes[nodes.Length - 1].solverPosition;
			
			// Satisfying child constraints
			SolveChildConstraints();

			// Finding the centroid position of all child chains according to their individual pull weights
			for (int i = 0; i < children.Length; i++) {
				Vector3 childPosition = children[i].nodes[0].solverPosition;

				if (children[i].rootLength > 0) {
					childPosition = IKSolverFABRIK.SolveJoint(nodes[nodes.Length - 1].solverPosition, children[i].nodes[0].solverPosition, children[i].rootLength);
				}
					
				if (pullParentSum > 0) centroid += (childPosition - nodes[nodes.Length - 1].solverPosition) * (children[i].pull / Mathf.Clamp(pullParentSum, 1f, Mathf.Infinity));
			}
			
			// Forward reach to the centroid (unless pinned)
			ForwardReach(Vector3.Lerp(centroid, nodes[nodes.Length - 1].solverPosition, pin));
		}
		
		/*
		 * Stage 2 of the FABRIK algorithm.
		 * */
		public void Stage2(Vector3 position, int iterations) {
			// Stage 2
			BackwardReach(position);

			// Iterating child constraints and child chains to make sure they are not conflicting
			for (int i = 0; i < Mathf.Min(iterations, 4); i++) SolveConstraintSystems();

			// Stage 2 for the children
			for (int i = 0; i < children.Length; i++) children[i].Stage2(nodes[nodes.Length - 1].solverPosition, iterations);
		}

		/*
		 * Returns the IKSolver.Point that has the specified Transform.
		 * */
		public IKSolver.Point GetPoint(Transform transform) {
			for (int i = 0; i < nodes.Length; i++) if (nodes[i].transform == transform) return nodes[i] as IKSolver.Point;
			
			p = null;
			for (int i = 0; i < children.Length; i++) {
				p = children[i].GetPoint(transform);
				if (p != null) return p;
			}
			
			return null;
		}
		
		/*
		 * Returns all IKSolver.Points from this chain and it's children.
		 * */
		public IKSolver.Point[] GetPoints() {
			IKSolver.Point[] pointArray = new IKSolver.Point[0];
			AddPoints(ref pointArray, nodes as IKSolver.Point[]);
			
			for (int i = 0; i < children.Length; i++) AddPoints(ref pointArray, children[i].nodes as IKSolver.Point[]);
			
			return pointArray;
		}

		/*
		 * Gets the FBIKChain that contains the specified Transform.
		 * */
		public FBIKChain GetChain(Transform t) {
			for (int i = 0; i < nodes.Length; i++) if (nodes[i].transform == t) return this;

			for (int i = 0; i < children.Length; i++) if (children[i].GetChain(t) != null) return children[i].GetChain(t);
			return null;
		}

		#endregion Recursive Methods

		/*
		 * Before updating the chain
		 * */
		public void ReadPose() {
			if (!initiated) return;
			
			PreSolveNodes();
			UpdateBoneLengths();
		}
		
		/*
		 * Initiating child constraints
		 * */
		public void InitiateConstraints(IKSolver solver) {
			foreach (ChildConstraint c in childConstraints) c.Initiate(solver as IKSolverFullBody);
		}
		
		/*
		 * Pre-update child constraints
		 * */
		private void PreSolveConstraints() {
			for (int i = 0; i < childConstraints.Length; i++) childConstraints[i].OnPreSolve();
		}
		
		/*
		 * Calculates the bend direction based on the law of cosines (from IKSolverTrigonometric). 
		 * */
		protected Vector3 GetBendDirection(Vector3 direction, float directionMagnitude, IKSolver.Node node1, IKSolver.Node node2) {
			float sqrMag1 = node1.length * node1.length;
			float sqrMag2 = node2.length * node2.length;
			
			float x = ((directionMagnitude * directionMagnitude) + sqrMag1 - sqrMag2) / 2f / directionMagnitude;
			float y = (float)Math.Sqrt(Mathf.Clamp(sqrMag1 - x * x, 0, Mathf.Infinity));

			return Quaternion.LookRotation(direction) * new Vector3(0f, y, x);
		}
		
		/*
		 * Cross-fading pull weights
		 * */
		private static float GetWeight(float v1, float v2) {
			float offset = v1 - v2;
			return 0.5f + (offset * 0.5f);
		}
		
		/*
		 * Satisfying child constraints
		 * */
		private void SolveChildConstraints() {
			for (int i = 0; i < childConstraints.Length; i++) {
				float crossFade = childConstraints[i].isRigid? GetWeight(childConstraints[i].chain1.pull, childConstraints[i].chain2.pull): 0.5f;
				childConstraints[i].Solve(1f - crossFade);	
			}
		}
		
		/*
		 * Iterating child constraints and child chains to make sure they are not conflicting
		 * */
		public void SolveConstraintSystems() {
			if (childConstraints.Length == 0) return;
			
			// Satisfy child constraints
			SolveChildConstraints();

			float pullSum = nodes[nodes.Length - 1].effectorPositionWeight;

			for (int i = 0; i < children.Length; i++) pullSum += children[i].nodes[0].effectorPositionWeight * children[i].pull;

			for (int i = 0; i < children.Length; i++) {
				float crossFade = ((children[i].nodes[0].effectorPositionWeight * children[i].pull) / Mathf.Clamp(pullSum, 1f, Mathf.Infinity));

				SolveLinearConstraint(nodes[nodes.Length - 1], children[i].nodes[0], crossFade, children[i].rootLength);
			}
		}

		/*
		 * Solve simple linear constraint
		 * */
		private static void SolveLinearConstraint(IKSolver.Node node1, IKSolver.Node node2, float crossFade, float distance) {
			float currentDistance = Vector3.Distance(node1.solverPosition, node2.solverPosition);
			
			float force = 1f - distance / currentDistance;
			if (force == 0) return;
			
			Vector3 offset = (node2.solverPosition - node1.solverPosition) * force;
			
			node1.solverPosition += offset * crossFade;
			node2.solverPosition -= offset * (1f - crossFade);
		}
		
		/*
		 * FABRIK Forward reach
		 * */
		public void ForwardReach(Vector3 position) {
			// Lerp last node's solverPosition to position
			nodes[nodes.Length - 1].solverPosition = position;
			
			for (int i = nodes.Length - 2; i > -1; i--) {
				// Finding joint positions
				nodes[i].solverPosition = IKSolverFABRIK.SolveJoint(nodes[i].solverPosition, nodes[i + 1].solverPosition, nodes[i].length);
			}
		}
		
		/*
		 * FABRIK Backward reach
		 * */
		private void BackwardReach(Vector3 position) {
			// Solve forst node only if it already hasn't been solved in SolveConstraintSystems
			if (rootLength > 0) position = IKSolverFABRIK.SolveJoint(nodes[0].solverPosition, position, rootLength);
			nodes[0].solverPosition = position;

			// Finding joint positions
			for (int i = 1; i < nodes.Length; i++) {
				nodes[i].solverPosition = IKSolverFABRIK.SolveJoint(nodes[i].solverPosition, nodes[i - 1].solverPosition, nodes[i - 1].length);
			}
		}
		
		/*
		 * Add points to array
		 * */
		public static IKSolver.Point[] AddPoints(ref IKSolver.Point[] array, IKSolver.Point[] addPoints) {
			Array.Resize(ref array, array.Length + addPoints.Length);

			int added = 0;
			for (int i = array.Length - addPoints.Length; i < array.Length; i++) {
				array[i] = addPoints[added];
				added ++;
			}

			return array;
		}
	}
}