using UnityEngine;
using System.Collections;

namespace RootMotion.FinalIK {

	/// <summary>
	/// Manages solver initiation and updating
	/// </summary>
	[AddComponentMenu("Scripts/SolverManager")]
	public class SolverManager: MonoBehaviour {
		
		#region Main Interface
		
		/// <summary>
		/// The updating frequency
		/// </summary>
		public float timeStep;

		/// <summary>
		/// Gets a value indicating whether this <see cref="RootMotion.FinalIK.SolverManager"/> is animated.
		/// </summary>
		public bool isAnimated {
			get {
				if (animator != null) {
					if (!animator.enabled) return false;

					if (animator.runtimeAnimatorController == null) {
						// Checking if a warning has already been logged before composing that large string in memory
						if (!Warning.logged) Warning.Log("Animator on " + name + " does not have a controller. The IK component will not update. Please assign an Animator Controller to the Animator component.", transform);
						return false;
					}

					return true;
				}
				if (animation != null) {
					if (!animation.enabled) return false;

					if (!animation.isPlaying) {
						// Checking if a warning has already been logged before composing that large string in memory
						if (!Warning.logged) Warning.Log("Animation on " + name + " is not playing. The IK component will not update.", transform);
						return false;
					}

					return true;
				}

				return true;
			}
		}

		/// <summary>
		/// Safely disables this component, making sure the solver is still initated. Use this instead of "enabled = false" if you need to disable the component to manually control it's updating.
		/// </summary>
		public void Disable() {
			Initiate();
			enabled = false;
		}
		
		#endregion Main
		
		protected virtual void UpdateSolver() {}
		protected virtual void InitiateSolver() {}
		
		private float lastTime;
		private Animator animator;
		private new Animation animation;
		private bool updateFrame;
		private bool componentInitiated;
		private bool initialAnimatePhysics;

		private bool animatePhysics {
			get {
				// Note: changing animatePhysics of Animator in runtime does not work. This is a Mecanim bug, Legacy does not have that issue.
				if (animator != null) return initialAnimatePhysics; //return animator.animatePhysics;
				if (animation != null) return animation.animatePhysics;
				return false;
			}
		}

		void Start() {
			Initiate();
		}

		private void Initiate() {
			if (componentInitiated) return;

			animator = GetComponent<Animator>();
			animation = GetComponent<Animation>();

			// Workaround for a Mecanim bug that does not allow changing animatePhysics in runtime
			if (animator != null) initialAnimatePhysics = animator.animatePhysics;

			InitiateSolver();
			componentInitiated = true;
		}

		/*
		 * Workaround hack for the solver to work with animatePhysics
		 * */
		void FixedUpdate() {
			updateFrame = true;
		}

		/*
		 * Updating by timeStep
		 * */
		void LateUpdate() {
			// Check if either animatePhysics is false or FixedUpdate has been called
			if (!animatePhysics) updateFrame = true;
			if (!updateFrame) return;
			updateFrame = false;

			if (!isAnimated) return;

			if (timeStep == 0) UpdateSolver();
			else {
				if (Time.time >= lastTime + timeStep) {
					UpdateSolver();
					lastTime = Time.time;
				}
			}
		}
	}
}
