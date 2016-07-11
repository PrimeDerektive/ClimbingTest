using UnityEngine;
using System.Collections;
using System;

namespace RootMotion.FinalIK {

	/// <summary>
	/// BipedIK solver collection.
	/// </summary>
	[System.Serializable]
	public class BipedIKSolvers {
		/// <summary>
		/// The left foot
		/// </summary>
		public IKSolverLimb leftFoot = new IKSolverLimb(AvatarIKGoal.LeftFoot);
		/// <summary>
		/// The right foot.
		/// </summary>
		public IKSolverLimb rightFoot = new IKSolverLimb(AvatarIKGoal.RightFoot);
		/// <summary>
		/// The left hand.
		/// </summary>
		public IKSolverLimb leftHand = new IKSolverLimb(AvatarIKGoal.LeftHand);
		/// <summary>
		/// The right hand.
		/// </summary>
		public IKSolverLimb rightHand = new IKSolverLimb(AvatarIKGoal.RightHand);
		/// <summary>
		/// The spine.
		/// </summary>
		public IKSolverFABRIK spine = new IKSolverFABRIK();
		/// <summary>
		/// The Look At %IK.
		/// </summary>
		public IKSolverLookAt lookAt = new IKSolverLookAt();
		/// <summary>
		/// The Aim %IK. Rotates the spine to aim a transform's forward towards the target.
		/// </summary>
		public IKSolverAim aim = new IKSolverAim();
		/// <summary>
		/// %Constraints for manipulating the character's pelvis.
		/// </summary>
		public Constraints pelvis = new Constraints();

		/// <summary>
		/// Gets the array containing all the limbs.
		/// </summary>
		public IKSolverLimb[] limbs {
			get {
				if (_limbs == null || (_limbs != null && _limbs.Length != 4)) _limbs = new IKSolverLimb[4] { leftFoot, rightFoot, leftHand, rightHand };
				return _limbs;
			}	
		}
		private IKSolverLimb[] _limbs;
		
		/// <summary>
		/// Gets the array containing all %IK solvers.
		/// </summary>
		public IKSolver[] ikSolvers {
			get {
				if (_ikSolvers == null || (_ikSolvers != null && _ikSolvers.Length != 7)) _ikSolvers = new IKSolver[7] { leftFoot, rightFoot, leftHand, rightHand, spine, lookAt, aim };
				return _ikSolvers;
			}
		}
		private IKSolver[] _ikSolvers;
		
		public void AssignReferences(BipedReferences references) {
			// Assigning limbs from references
			leftFoot.bone1.transform = references.leftThigh;
			leftFoot.bone2.transform = references.leftCalf;
			leftFoot.bone3.transform = references.leftFoot;
			
			rightFoot.bone1.transform = references.rightThigh;
			rightFoot.bone2.transform = references.rightCalf;
			rightFoot.bone3.transform = references.rightFoot;
			
			leftHand.bone1.transform = references.leftUpperArm;
			leftHand.bone2.transform = references.leftForearm;
			leftHand.bone3.transform = references.leftHand;
			
			rightHand.bone1.transform = references.rightUpperArm;
			rightHand.bone2.transform = references.rightForearm;
			rightHand.bone3.transform = references.rightHand;
			
			// Assigning spine bones from references
			Array.Resize(ref spine.bones, references.spine.Length);
			for (int i = 0; i < references.spine.Length; i++) {
				if (spine.bones[i] == null) spine.bones[i] = new IKSolver.Bone();
				spine.bones[i].transform = references.spine[i];
			}
			
			// Assigning lookAt bones from references
			Array.Resize(ref lookAt.spine, references.spine.Length);
			for (int i = 0; i < references.spine.Length; i++) {
				if (lookAt.spine[i] == null) lookAt.spine[i] = new IKSolverLookAt.LookAtBone();
				lookAt.spine[i].transform = references.spine[i];
			}
			
			// Assigning eye bones from references
			Array.Resize(ref lookAt.eyes, references.eyes.Length);
			for (int i = 0; i < references.eyes.Length; i++) {
				if (lookAt.eyes[i] == null) lookAt.eyes[i] = new IKSolverLookAt.LookAtBone();
				lookAt.eyes[i].transform = references.eyes[i];
			}
			
			// Assigning head bone from references
			lookAt.head.transform = references.head;
			
			// Assigning Aim bones from references
			Array.Resize(ref aim.bones, references.spine.Length);
			for (int i = 0; i < references.spine.Length; i++) {
				if (aim.bones[i] == null) aim.bones[i] = new IKSolver.Bone();
				aim.bones[i].transform = references.spine[i];
			}
			
			leftFoot.goal = AvatarIKGoal.LeftFoot;
			rightFoot.goal = AvatarIKGoal.RightFoot;
			leftHand.goal = AvatarIKGoal.LeftHand;
			rightHand.goal = AvatarIKGoal.RightHand;
		}
	}
}
