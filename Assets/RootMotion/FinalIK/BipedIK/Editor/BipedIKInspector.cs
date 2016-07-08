using UnityEditor;
using UnityEngine;
using System.Collections;
using System;

	namespace RootMotion.FinalIK {

	/*
	 * Custom inspector for Biped IK.
	 * */
	[CustomEditor(typeof(BipedIK))]
	public class BipedIKInspector : Editor {
		
		private BipedIK script { get { return target as BipedIK; }}
		
		private int selectedSolver = -1;
		
		private SerializedProperty references, solvers;
		private SerializedProperty[] solversProps;
		private SerializedContent[] leftFootContent, rightFootContent, leftHandContent, rightHandContent, spineContent, aimContent, lookAtContent, pelvisContent;
		private SerializedContent[][] solverContents;
		
		public void OnEnable() {
			// Store the MonoScript for changing script execution order
			if (!Application.isPlaying) {
				MonoScript monoScript = MonoScript.FromMonoBehaviour(script);

				// Changing the script execution order to make sure BipedIK always executes after any other script except FullBodyBipedIK
				int executionOrder = MonoImporter.GetExecutionOrder(monoScript);
				if (executionOrder != 9998) MonoImporter.SetExecutionOrder(monoScript, 9998);
			}

			references = serializedObject.FindProperty("references");
			solvers = serializedObject.FindProperty("solvers");
			solversProps = BipedIKSolversInspector.FindProperties(solvers);
			
			// Caching Solver Contents
			leftFootContent = IKSolverLimbInspector.FindContent(solversProps[0]);
			rightFootContent = IKSolverLimbInspector.FindContent(solversProps[1]);
			leftHandContent = IKSolverLimbInspector.FindContent(solversProps[2]);
			rightHandContent = IKSolverLimbInspector.FindContent(solversProps[3]);
			spineContent = IKSolverHeuristicInspector.FindContent(solversProps[4]);
			aimContent = IKSolverAimInspector.FindContent(solversProps[5]);
			lookAtContent = IKSolverLookAtInspector.FindContent(solversProps[6]);
			pelvisContent = ConstraintsInspector.FindContent(solversProps[7]);
			
			solverContents = new SerializedContent[8][] {
				leftFootContent, rightFootContent, leftHandContent, rightHandContent, spineContent, aimContent, lookAtContent, pelvisContent
			};
			
			// Automatically detecting references
			if (!Application.isPlaying) {
				if (script.references.isEmpty) {
					BipedReferences.AutoDetectReferences(ref script.references, script.transform, new BipedReferences.AutoDetectParams(false, true));
					
					references.isExpanded = true;
					solvers.isExpanded = false;
					for (int i = 0; i < solversProps.Length; i++) solversProps[i].isExpanded = false;
					
					// Setting default values and initiating
					script.InitiateBipedIK();
					script.SetToDefaults();
					EditorUtility.SetDirty(script);
				} else script.InitiateBipedIK();
				
				Warning.logged = false;
				BipedReferences.CheckSetup(script.references);
			}
		}
		
		public override void OnInspectorGUI() {
			serializedObject.Update();

			EditorGUILayout.Space();
			
			// Editing References
			if (BipedReferencesInspector.AddModifiedInspector(references)) {
				if (!Application.isPlaying) {
					Warning.logged = false;
					BipedReferences.CheckSetup(script.references);
					script.InitiateBipedIK();
				}
			}
			
			// Editing Solvers
			BipedIKSolversInspector.AddInspector(solvers, solversProps, solverContents);
			
			EditorGUILayout.Space();
			
			serializedObject.ApplyModifiedProperties();
		}
		
		void OnSceneGUI() {
			if (!script.enabled) return;
			if (Application.isPlaying && !script.isAnimated) return;

			// Draw the scene view helpers for the solvers
			BipedIKSolversInspector.AddScene(script.solvers, ref selectedSolver);
		}
	}
}
	
