using UnityEngine;
using System.Collections;

	namespace RootMotion.FinalIK {

	/// <summary>
	/// Rotates a hierarchy of bones to face a target.
	/// </summary>
	[System.Serializable]
	public class IKSolverLookAt : IKSolver {
		
		#region Main Interface
		
		/// <summary>
		/// The spine hierarchy.
		/// </summary>
		public LookAtBone[] spine = new LookAtBone[0];
		/// <summary>
		/// The head bone.
		/// </summary>
		public LookAtBone head = new LookAtBone();
		/// <summary>
		/// The eye bones.
		/// </summary>
		public LookAtBone[] eyes = new LookAtBone[0];
		/// <summary>
		/// The body weight.
		/// </summary>
		public float bodyWeight = 0.5f;
		/// <summary>
		/// The head weight.
		/// </summary>
		public float headWeight = 0.5f;
		/// <summary>
		/// The eyes weight.
		/// </summary>
		public float eyesWeight = 1f;
		/// <summary>
		/// Clamp weight for the body.
		/// </summary>
		public float clampWeight = 0.5f;
		/// <summary>
		/// Clamp weight for the head.
		/// </summary>
		public float clampWeightHead = 0.5f;
		/// <summary>
		/// Clamp weight for the eyes.
		/// </summary>
		public float clampWeightEyes = 0.5f;
		/// <summary>
		/// Number of sine smoothing iterations applied on clamping to make the clamping point smoother.
		/// </summary>
		public int clampSmoothing = 2;
		/// <summary>
		/// Weight distribution between spine bones.
		/// </summary>
		public AnimationCurve spineWeightCurve = new AnimationCurve(new Keyframe[2] { new Keyframe(0f, 0.3f), new Keyframe(1f, 1f) });
		
		/// <summary>
		/// Sets the look at weight. NOTE: You are welcome edit the weights directly, this method is here only to match the Unity's built in %IK API.
		/// </summary>
		public void SetLookAtWeight(float weight) {
			this.IKPositionWeight = Mathf.Clamp(weight, 0f, 1f);
		}
		
		/// <summary>
		/// Sets the look at weight. NOTE: You are welcome to edit the weights directly, this method is here only to match the Unity's built in %IK API.
		/// </summary>
		public void SetLookAtWeight(float weight, float bodyWeight) {
			this.IKPositionWeight = Mathf.Clamp(weight, 0f, 1f);
			this.bodyWeight = Mathf.Clamp(bodyWeight, 0f, 1f);
		}
		
		/// <summary>
		/// Sets the look at weight. NOTE: You are welcome to edit the weights directly, this method is here only to match the Unity's built in %IK API.
		/// </summary>
		public void SetLookAtWeight(float weight, float bodyWeight, float headWeight) {
			this.IKPositionWeight = Mathf.Clamp(weight, 0f, 1f);
			this.bodyWeight = Mathf.Clamp(bodyWeight, 0f, 1f);
			this.headWeight = Mathf.Clamp(headWeight, 0f, 1f);
		}
		
		/// <summary>
		/// Sets the look at weight. NOTE: You are welcome to edit the weights directly, this method is here only to match the Unity's built in %IK API.
		/// </summary>
		public void SetLookAtWeight(float weight, float bodyWeight, float headWeight, float eyesWeight) {
			this.IKPositionWeight = Mathf.Clamp(weight, 0f, 1f);
			this.bodyWeight = Mathf.Clamp(bodyWeight, 0f, 1f);
			this.headWeight = Mathf.Clamp(headWeight, 0f, 1f);
			this.eyesWeight = Mathf.Clamp(eyesWeight, 0f, 1f);
		}
		
		/// <summary>
		/// Sets the look at weight. NOTE: You are welcome to edit the weights directly, this method is here only to match the Unity's built in %IK API. 
		/// </summary>
		public void SetLookAtWeight(float weight, float bodyWeight, float headWeight, float eyesWeight, float clampWeight) {
			this.IKPositionWeight = Mathf.Clamp(weight, 0f, 1f);
			this.bodyWeight = Mathf.Clamp(bodyWeight, 0f, 1f);
			this.headWeight = Mathf.Clamp(headWeight, 0f, 1f);
			this.eyesWeight = Mathf.Clamp(eyesWeight, 0f, 1f);
			this.clampWeight = Mathf.Clamp(clampWeight, 0f, 1f);
			this.clampWeightHead = this.clampWeight;
			this.clampWeightEyes = this.clampWeight;
		}
		
		/// <summary>
		/// Sets the look at weight. NOTE: You are welcome to edit the weights directly, this method is here only to match the Unity's built in %IK API.
		/// </summary>
		public void SetLookAtWeight(float weight, float bodyWeight = 0f, float headWeight = 1f, float eyesWeight = 0.5f, float clampWeight = 0.5f, float clampWeightHead = 0.5f, float clampWeightEyes = 0.3f) {
			this.IKPositionWeight = Mathf.Clamp(weight, 0f, 1f);
			this.bodyWeight = Mathf.Clamp(bodyWeight, 0f, 1f);
			this.headWeight = Mathf.Clamp(headWeight, 0f, 1f);
			this.eyesWeight = Mathf.Clamp(eyesWeight, 0f, 1f);
			this.clampWeight = Mathf.Clamp(clampWeight, 0f, 1f);
			this.clampWeightHead = Mathf.Clamp(clampWeightHead, 0f, 1f);
			this.clampWeightEyes = Mathf.Clamp(clampWeightEyes, 0f, 1f);
		}
		
		public override bool IsValid (bool log) {
			if (root == null) {
				if (log) LogWarning("Root transform unassigned in IKSolverLookAt. Can't initiate solver.");
				return false;
			}
			if (!spineIsValid) {
				if (log) LogWarning("IKSolverLookAt spine setup is invalid. Can't initiate solver.");
				return false;
			}
			if (!headIsValid) {
				if (log) LogWarning("IKSolverLookAt head transform is null. Can't initiate solver.");
				return false;
			}
			if (!eyesIsValid) {
				if (log) LogWarning("IKSolverLookAt eyes setup is invalid. Can't initiate solver.");
				return false;
			}
			Transform spineDuplicate = ContainsDuplicateBone(spine);
			if (spineDuplicate != null) {
				if (log) LogWarning(spineDuplicate.name + " is represented multiple times in a single IK chain. Can't initiate solver.");
				return false;
			}
			Transform eyeDuplicate = ContainsDuplicateBone(eyes);
			if (eyeDuplicate != null) {
				if (log) LogWarning(eyeDuplicate.name + " is represented multiple times in a single IK chain. Can't initiate solver.");
				return false;
			}
			return true;
		}
			
		public override IKSolver.Point[] GetPoints() {
			IKSolver.Point[] allPoints = new IKSolver.Point[spine.Length + eyes.Length + (head.transform != null? 1: 0)];
			for (int i = 0; i < spine.Length; i++) allPoints[i] = spine[i] as IKSolver.Point;
			
			int eye = 0;
			for (int i = spine.Length; i < allPoints.Length; i++) {
				allPoints[i] = eyes[eye] as IKSolver.Point;
				eye ++;
			}
			
			if (head.transform != null) allPoints[allPoints.Length - 1] = head as IKSolver.Point;
			return allPoints;
		}
		
		public override IKSolver.Point GetPoint(Transform transform) {
			foreach (IKSolverLookAt.LookAtBone b in spine) if (b.transform == transform) return b as IKSolver.Point;
			foreach (IKSolverLookAt.LookAtBone b in eyes) if (b.transform == transform) return b as IKSolver.Point;
			if (head.transform == transform) return head as IKSolver.Point;
			return null;
		}

		/// <summary>
		/// Look At bone class.
		/// </summary>
		[System.Serializable]
		public class LookAtBone: IKSolver.Bone {

			#region Public methods
			
			/*
			 * Initiates the bone, precalculates values.
			 * */
			public void Initiate(Transform root) {
				if (transform == null) return;

				axis = Quaternion.Inverse(transform.rotation) * root.forward;
			}

			/*
			 * Rotates the bone to look at a world direction.
			 * */
			public void LookAt(Vector3 direction, float weight) {
				Quaternion fromTo = Quaternion.FromToRotation(forward, direction);
				transform.rotation = Quaternion.Lerp(transform.rotation, fromTo * transform.rotation, weight);
			}

			/*
			 * Gets the local axis to goal in world space.
			 * */
			public Vector3 forward {
				get {
					return transform.rotation * axis;
				}
			}
		
			#endregion Public methods
		}

		/// <summary>
		/// Reinitiate the solver with new bone Transforms.
		/// </summary>
		/// <returns>
		/// Returns true if the new chain is valid.
		/// </returns>
		public bool SetChain(Transform[] spine, Transform head, Transform[] eyes, Transform root) {
			if (this.spine.Length != spine.Length) this.spine = new LookAtBone[spine.Length];
			for (int i = 0; i < spine.Length; i++) {
				if (this.spine[i] == null) this.spine[i] = new LookAtBone();
				this.spine[i].transform = spine[i];
			}

			this.head = new LookAtBone();
			this.head.transform = head;

			if (this.eyes.Length != eyes.Length) this.eyes = new LookAtBone[eyes.Length];
			for (int i = 0; i < eyes.Length; i++) {
				if (this.eyes[i] == null) this.eyes[i] = new LookAtBone();
				this.eyes[i].transform = eyes[i];
			}
			
			Initiate(root);
			return initiated;
		}

		#endregion Main Interface

		private Vector3[] spineForwards = new Vector3[0];
		private Vector3[] headForwards = new Vector3[1];
		private Vector3[] eyeForward = new Vector3[1];

		protected override void OnInitiate() {
			// Set IKPosition to default value
			if (firstInitiation || !Application.isPlaying) {
				if (spine.Length > 0) IKPosition = spine[spine.Length - 1].transform.position + root.forward * 3f;
				else if (head.transform != null) IKPosition = head.transform.position + root.forward * 3f;
				else if (eyes.Length > 0 && eyes[0].transform != null) IKPosition = eyes[0].transform.position + root.forward * 3f;
			}
			
			// Initiating the bones
			foreach (LookAtBone s in spine) s.Initiate(root);
			if (head != null) head.Initiate(root);
			foreach (LookAtBone eye in eyes) eye.Initiate(root);

			if (spineForwards.Length != spine.Length) spineForwards = new Vector3[spine.Length];
		}

		protected override void OnUpdate() {
			if (IKPositionWeight <= 0) return;

			// Solving the hierarchies
			SolveSpine();
			SolveHead();
			SolveEyes();
		}

		private bool spineIsValid {
			get {
				if (spine.Length == 0) return true;
				if (spine[0] == null) return false;
				for (int i = 0; i < spine.Length; i++) if (spine[i].transform == null) return false;
				return true;
			}
		}
		
		// Solving the spine hierarchy
		private void SolveSpine() {
			if (bodyWeight <= 0) return;
			if (spine.Length == 0) return;

			// Get the look at vectors for each bone
			GetForwards(ref spineForwards, spine[0].forward, (IKPosition - spine[spine.Length - 1].transform.position).normalized, spine.Length, clampWeight);

			// Rotate each bone to face their look at vectors
			for (int i = 0; i < spine.Length; i++) {
				spine[i].LookAt(spineForwards[i], bodyWeight * IKPositionWeight);
			}
		}
		
		private bool headIsValid {
			get {
				if (head == null || head.transform == null) return false;
				return true;
			}
		}
		
		// Solving the head rotation
		private void SolveHead() {
			if (headWeight <= 0) return;

			// Get the look at vector for the head
			Vector3 baseForward = spine.Length > 0 && spine[spine.Length - 1].transform != null? spine[spine.Length - 1].forward: head.forward;

			GetForwards(ref headForwards, baseForward, (IKPosition - head.transform.position).normalized, 1, clampWeightHead);

			// Rotate the head to face its look at vector
			head.LookAt(headForwards[0], headWeight * IKPositionWeight);
		}
		
		private bool eyesIsValid {
			get {
				for (int i = 0; i < eyes.Length; i++) if (eyes[i].transform == null) return false;
				return headIsValid;
			}
		}

		// Solving the eye rotations
		private void SolveEyes() {
			if (eyesWeight <= 0) return;

			for (int i = 0; i < eyes.Length; i++) {
				// Get the look at vector for the eye
				Vector3 baseForward = head.transform != null? head.forward: eyes[i].forward;
				GetForwards(ref eyeForward, baseForward, (IKPosition - eyes[i].transform.position).normalized, 1, clampWeightEyes);

				// Rotate the eye to face its look at vector
				eyes[i].LookAt(eyeForward[0], eyesWeight * IKPositionWeight);
			}
		}

		/*
		 * Returns forwards for a number of bones rotating from baseForward to targetForward.
		 * NB! Make sure baseForward and targetForward are normalized.
		 * */
		private Vector3[] GetForwards(ref Vector3[] forwards, Vector3 baseForward, Vector3 targetForward, int bones, float clamp) {
			// If clamp >= 1 make all the forwards match the base
			if (clamp >= 1 || IKPositionWeight <= 0) {
				for (int i = 0; i < forwards.Length; i++) forwards[i] = baseForward;
				return forwards;
			}
			
			// Get normalized dot product. 
			float angle = Vector3.Angle(baseForward, targetForward);
			float dot = 1f - (angle / 180f);

			// Clamping the targetForward so it doesn't exceed clamp
			float targetClampMlp = clamp > 0? Mathf.Clamp(1f - ((clamp - dot) / (1f - dot)), 0f, 1f): 1f;
			
			// Calculating the clamp multiplier
			float clampMlp = clamp > 0? Mathf.Clamp(dot / clamp, 0f, 1f): 1f;
			
			for (int i = 0; i < clampSmoothing; i++) {
				float sinF = clampMlp * Mathf.PI * 0.5f;
				clampMlp = Mathf.Sin(sinF);
			}
			
			// Rotation amount for 1 bone
			if (forwards.Length == 1) {
				forwards[0] = Vector3.Slerp(baseForward, targetForward, clampMlp * targetClampMlp);
			} else {
				float step = 1f / (float)(forwards.Length - 1);

				// Calculate the forward for each bone
				for (int i = 0; i < forwards.Length; i++) {
					forwards[i] = Vector3.Slerp(baseForward, targetForward, spineWeightCurve.Evaluate(step * i) * clampMlp * targetClampMlp);
				}
			}

			return forwards;
		}
	}
}
