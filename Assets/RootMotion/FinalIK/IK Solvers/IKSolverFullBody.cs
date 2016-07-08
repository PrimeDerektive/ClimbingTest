using UnityEngine;
using System.Collections;

namespace RootMotion.FinalIK {

	/// <summary>
	/// Generic FBIK solver. In each solver update, %IKSolverFullBody first reads the character's pose, then solves the %IK and writes the solved pose back to the character via IKMapping.
	/// </summary>
	[System.Serializable]
	public class IKSolverFullBody : IKSolver {
		
		#region Main Interface
		
		/// <summary>
		/// Number of solver iterations.
		/// </summary>
		public int iterations = 4;
		/// <summary>
		/// The root node chain.
		/// </summary>
		public FBIKChain chain = new FBIKChain();
		/// <summary>
		/// The effectors.
		/// </summary>
		public IKEffector[] effectors = new IKEffector[0];
		/// <summary>
		/// The bend constraints make sure 3 segmented node chains such as biped limbs are bent in a certain direction
		/// </summary>
		public IKConstraintBend[] bendConstraints = new IKConstraintBend[0];
		/// <summary>
		/// Mapping spine bones to the solver.
		/// </summary>
		public IKMappingSpine spineMapping = new IKMappingSpine();
		/// <summary>
		/// Mapping individual bones to the solver
		/// </summary>
		public IKMappingBone[] boneMappings = new IKMappingBone[0];
		/// <summary>
		/// Mapping 3 segment limbs to the solver
		/// </summary>
		public IKMappingLimb[] limbMappings = new IKMappingLimb[0];
		
		/// <summary>
		/// Gets the effector of the specified Transform.
		/// </summary>
		public IKEffector GetEffector(Transform t) {
			for (int i = 0; i < effectors.Length; i++) if (effectors[i].bone == t) return effectors[i];
			return null;
		}
		
		/// <summary>
		/// Gets the chain that contains the specified Transform.
		/// </summary>
		public FBIKChain GetChain(Transform t) {
			return chain.GetChain(t);
		}
		
		public override IKSolver.Point[] GetPoints() {
			return chain.GetPoints();
		}
		
		public override IKSolver.Point GetPoint(Transform transform) {
			return chain.GetPoint(transform);
		}
		
		public override bool IsValid(bool log) {
			if (log) {
				if (!chain.IsValid(LogWarning)) return false;
			} else if (!chain.IsValid()) return false;

			foreach (IKEffector e in effectors) if (!e.IsValid(this, LogWarning)) return false;
			foreach (IKConstraintBend b in bendConstraints) if (!b.IsValid(this, LogWarning)) return false;
			
			if (!spineMapping.IsValid(this, LogWarning)) return false;
			foreach (IKMappingLimb l in limbMappings) if (!l.IsValid(this, LogWarning)) return false;
			foreach (IKMappingBone b in boneMappings) if (!b.IsValid(this, LogWarning)) return false;
			
			return true;
		}

		/// <summary>
		/// Delegates solver update events.
		/// </summary>
		public delegate void UpdateDelegate();
		/// <summary>
		/// Delegates solver iteration events.
		/// </summary>
		public delegate void IterationDelegate(int i);
		
		/// <summary>
		/// Called before initiating the solver.
		/// </summary>
		public UpdateDelegate OnPreInitiate;
		/// <summary>
		/// Called after initiating the solver.
		/// </summary>
		public UpdateDelegate OnPostInitiate;
		/// <summary>
		/// Called before updating.
		/// </summary>
		public UpdateDelegate OnPreUpdate;
		/// <summary>
		/// Called before reading the pose
		/// </summary>
		public UpdateDelegate OnPreRead;
		/// <summary>
		/// Called before solving.
		/// </summary>
		public UpdateDelegate OnPreSolve;
		/// <summary>
		/// Called before each iteration
		/// </summary>
		public IterationDelegate OnPreIteration;
		/// <summary>
		/// Called after each iteration
		/// </summary>
		public IterationDelegate OnPostIteration;
		/// <summary>
		/// Called before applying bend constraints.
		/// </summary>
		public UpdateDelegate OnPreBend;
		/// <summary>
		/// Called after updating the solver
		/// </summary>
		public UpdateDelegate OnPostSolve;
		/// <summary>
		/// Called after writing the solved pose
		/// </summary>
		public UpdateDelegate OnPostUpdate;
		
		#endregion Main Interface
		
		protected override void OnInitiate() {
			if (OnPreInitiate != null) OnPreInitiate();
			
			// Initiate chain
			chain.Initiate(this);
			
			// Initiate effectors
			foreach (IKEffector e in effectors) e.Initiate(this);
			
			// Initiate bend constraints
			foreach (IKConstraintBend b in bendConstraints) b.Initiate(this);
			
			// Initiate IK mapping
			spineMapping.Initiate(this);
			foreach (IKMappingBone boneMapping in boneMappings) boneMapping.Initiate(this);
			foreach (IKMappingLimb limbMapping in limbMappings) limbMapping.Initiate(this);
			
			if (OnPostInitiate != null) OnPostInitiate();
		}

		protected override void OnUpdate() {
			if (OnPreUpdate != null) OnPreUpdate();

			if (IKPositionWeight <= 0) return;

			if (OnPreRead != null) OnPreRead();

			// Phase 1: Read the pose of the biped
			ReadPose();

			if (OnPreSolve != null) OnPreSolve();

			// Phase 2: Solve IK
			Solve();

			if (OnPostSolve != null) OnPostSolve();

			// Phase 3: Map biped to it's solved state
			WritePose();

			// Reset effector position offsets to Vector3.zero
			for (int i = 0; i < effectors.Length; i++) effectors[i].OnPostWrite();

			if (OnPostUpdate != null) OnPostUpdate();
		}
		
		protected virtual void ReadPose() {
			// Presolve effectors, apply effector offset to the nodes
			for (int i = 0; i < effectors.Length; i++) effectors[i].ResetOffset();
			for (int i = 0; i < effectors.Length; i++) effectors[i].OnPreSolve();

			// Set solver positions to match the current bone positions of the biped
			chain.ReadPose();

			// IKMapping
			spineMapping.ReadPose();
			for (int i = 0; i < boneMappings.Length; i++) boneMappings[i].ReadPose();
			for (int i = 0; i < limbMappings.Length; i++) limbMappings[i].ReadPose();
		}

		protected virtual void Solve() {
			// Iterate solver
			for (int i = 0; i < iterations; i++) {
				if (OnPreIteration != null) OnPreIteration(i);
				
				// Apply end-effectors
				for (int e = 0; e < effectors.Length; e++) if (effectors[e].isEndEffector) effectors[e].Update();
			
				// Reaching
				chain.Reach(i);

				// Apply non end-effectors
				for (int e = 0; e < effectors.Length; e++) if (!effectors[e].isEndEffector) effectors[e].Update();

				// Trigonometric pass to release push tension from the solver
				chain.SolveTrigonometric();

				// Solving FABRIK forward
				chain.Stage1();

				// Apply non end-effectors again
				for (int e = 0; e < effectors.Length; e++) if (!effectors[e].isEndEffector) effectors[e].Update();

				// Solving FABRIK backwards
				chain.Stage2(chain.nodes[0].solverPosition, iterations);

				if (OnPostIteration != null) OnPostIteration(i);
			}

			// Final end-effector pass
			for (int i = 0; i < effectors.Length; i++) if (effectors[i].isEndEffector) effectors[i].Update();
			
			// Final trigonometric pass
			chain.SolveTrigonometric();
			
			// Before applying bend constraints (last chance to modify the bend direction)
			if (OnPreBend != null) OnPreBend();

			ApplyBendConstraints();
		}

		protected virtual void ApplyBendConstraints() {
			// Solve bend constraints
			for (int i = 0; i < bendConstraints.Length; i++) bendConstraints[i].Solve();
		}

		protected virtual void WritePose() {
			// Apply IK mapping
			spineMapping.WritePose();
			for (int i = 0; i < boneMappings.Length; i++) boneMappings[i].WritePose();
			for (int i = 0; i < limbMappings.Length; i++) limbMappings[i].WritePose();
		}
	}
}
