using UnityEngine;
using System.Collections;

	namespace RootMotion.FinalIK {
		
	/// <summary>
	/// Branch of FABRIK components in the FABRIKRoot hierarchy.
	/// </summary>
	[System.Serializable]
	public class FABRIKChain {
		
		#region Main Interface
		
		/// <summary>
		/// The FABRIK component.
		/// </summary>
		public FABRIK ik;
		/// <summary>
		/// Parent pull weight.
		/// </summary>
		public float pull = 1f;
		/// <summary>
		/// Resistance to being pulled by child chains.
		/// </summary>
		public float pin = 1f;
		/// <summary>
		/// The child chains.
		/// </summary>
		public FABRIKChain[] children = new FABRIKChain[0];
		
		/// <summary>
		/// Checks whether this FABRIKChain is valid.
		/// </summary>
		public bool IsValid(Warning.Logger logger) {
			if (ik == null) {
				if (logger != null) logger("IK unassigned in FABRIKChain.");
				return false;
			}
			
			if (!ik.solver.IsValid(true)) return false;
			
			foreach (FABRIKChain c in children) {
				if (!c.IsValid(logger)) return false;
			}
			
			return true;
		}
		
		#endregion Main Interface
		
		private Vector3 position;

		/*
		 * Set weight of all IK solvers
		 * */
		public void SetWeight(float weight) {
			ik.solver.IKPositionWeight = weight;
			
			for (int i = 0; i < children.Length; i++) children[i].SetWeight(weight);
		}
		
		/*
		 * Initiate the chain
		 * */
		public void Initiate() {
			ik.Disable();
			
			position = ik.solver.bones[ik.solver.bones.Length - 1].transform.position;
			
			for (int i = 0; i < children.Length; i++)  children[i].Initiate();
		}
	
		/*
		 * Solving stage 1 of the FABRIK algorithm from end effectors towards the root.
		 * */
		public void Stage1() {
			// Solving children first
			for (int i = 0; i < children.Length; i++) children[i].Stage1();
			
			// The last chains
			if (children.Length == 0) {
				ik.solver.SolveForward(ik.solver.GetIKPosition());
				return;
			}
			
			// Finding the centroid of child root solver positions
			position = ik.solver.GetIKPosition();
			Vector3 centroid = position;
			
			float pullSum = 0f;
			for (int i = 0; i < children.Length; i++) pullSum += children[i].pull;
			
			for (int i = 0; i < children.Length; i++) {
				if (children[i].children.Length == 0) children[i].ik.solver.SolveForward(children[i].ik.solver.GetIKPosition());
				
				if (pullSum > 0) centroid += (children[i].ik.solver.bones[0].solverPosition - position) * (children[i].pull / Mathf.Clamp(pullSum, 1f, pullSum));
			}
			
			// Solve this chain forward
			ik.solver.SolveForward(Vector3.Lerp(centroid, position, pin));
		}
		
		/*
		 * Solving stage 2 of the FABRIK algoright from the root to the end effectors.
		 * */
		public void Stage2(Vector3 rootPosition) {
			// Solve this chain backwards
			ik.solver.SolveBackward(rootPosition);
			
			// Solve child chains
			for (int i = 0; i < children.Length; i++) {
				children[i].Stage2(ik.solver.bones[ik.solver.bones.Length - 1].transform.position);
			}
		}
	}
}
