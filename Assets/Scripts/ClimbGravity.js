#pragma strict
import RootMotion.FinalIK;

var climbSensor : Transform;
var climbTarget : Transform;
var climbLayerMask : LayerMask;

var ikLeftHandRayOrigin : Transform;
var ikRightHandRayOrigin : Transform;
var ikLeftFootRayOrigin : Transform;
var ikRightFootRayOrigin : Transform;

var col : CapsuleCollider;
var anim : Animator;
var rb : Rigidbody;

var fbbik : FullBodyBipedIK;

private var inTransition : boolean = false;

function Start(){
	if(!col) col = GetComponent.<CapsuleCollider>();
	if(!anim) anim = GetComponent.<Animator>();
	if(!rb) rb = GetComponent.<Rigidbody>();
	if(!fbbik) fbbik = GetComponent.<FullBodyBipedIK>();

	//move center of collider
	col.center.y = 0.0;
	//shorten collider height
	col.height = 1.0;
}

function FixedUpdate () {

	//capture input
	var horizontal = Input.GetAxisRaw("Horizontal"); 
	var vertical = Input.GetAxisRaw("Vertical");

	//set the vars on the animator to start climbing animation
	anim.SetFloat("SpeedX", Input.GetAxis("Horizontal"));
	anim.SetFloat("SpeedY", Input.GetAxis("Vertical"));

	if(!inTransition){

		//movement direction is on x and z axes
		var moveDir = Vector3(horizontal, vertical, 0);

		//move the climbSensor around the player on the x-z axes
		climbSensor.transform.localPosition =  moveDir;
		climbSensor.transform.localPosition.z = -0.5;
		climbSensor.transform.localPosition.x = Mathf.Clamp(climbSensor.transform.localPosition.x, -0.5, 0.5);
		climbSensor.transform.localPosition.y = Mathf.Clamp(climbSensor.transform.localPosition.y, -0.5, 0.5);

		var hit : RaycastHit;

		if(horizontal != 0.0 || vertical != 0.0){

			//check for perpendicular transitions
			//raycast from root to climbSensor with a longer distance
			var dirToClimbSensor = (climbSensor.position - transform.position).normalized;
			if(Physics.Raycast(transform.position, dirToClimbSensor, hit, 1.0, climbLayerMask)){
				rb.velocity = Vector3.zero;
				climbTarget.position = hit.point + (hit.normal * 0.25);
				climbTarget.rotation = Quaternion.FromToRotation(climbTarget.forward, -hit.normal) * climbTarget.rotation;
				if(vertical < -0.1) climbTarget.position += 0.25 * climbTarget.up; //downward transfer
				else if(vertical > 0.1) climbTarget.position -= 0.25 * climbTarget.up; //upward transfer
				StartCoroutine(TransitionToTarget());	

			}//regular climbing
			if(Physics.Linecast(climbSensor.position, climbSensor.position + climbSensor.forward*1.5, hit, climbLayerMask)){
				//if(transform.parent != hit.collider.transform) transform.parent = hit.collider.transform;

				var targetPos = hit.point + hit.normal * 0.25;
				var targetDir = (targetPos - transform.position).normalized;
				var targetRot = Quaternion.FromToRotation(transform.forward, -hit.normal) * transform.rotation;

				//transform.Translate(targetDir * Time.deltaTime);
				rb.velocity = targetDir;
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5.0);
				Debug.DrawRay(climbSensor.position, climbSensor.forward, Color.green);
			}
			else{ //can no longer climb, check around corner

				if(Physics.Linecast(climbSensor.position + climbSensor.forward, transform.position + transform.forward*0.5, hit, climbLayerMask)){
					rb.velocity = Vector3.zero;
					climbTarget.position = hit.point + (hit.normal * 0.25);
					climbTarget.rotation = Quaternion.FromToRotation(climbTarget.forward, -hit.normal) * climbTarget.rotation;
					if(vertical < -0.1) climbTarget.position -= 0.25 * climbTarget.up; //downward transfer
					else if(vertical > 0.1) climbTarget.position += 0.25 * climbTarget.up; //upward transfer
					StartCoroutine(TransitionToTarget());	
				}
				else{
					Debug.DrawRay(climbSensor.position, climbSensor.forward, Color.red);
					rb.velocity = Vector3.zero;
				}

			}

		}
		else{
			rb.velocity = Vector3.zero;
		}

	}

}

function LateUpdate(){

	var leftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
	ikLeftFootRayOrigin.position = leftFoot.position - ikLeftFootRayOrigin.forward*0.1;
	var leftFootIkPos : Vector3 = Vector3.zero;
	var hit : RaycastHit;
	if(Physics.Raycast(ikLeftFootRayOrigin.position, ikLeftFootRayOrigin.forward, hit, 1.0, climbLayerMask)){
		Debug.DrawRay(ikLeftFootRayOrigin.position,  ikLeftFootRayOrigin.forward, Color.green);
		leftFootIkPos = hit.point + hit.normal * 0.17;
	}
	else if(Physics.Raycast(ikLeftFootRayOrigin.position, Quaternion.Euler(-45, 0, 0) * ikLeftFootRayOrigin.forward, hit, 1.0, climbLayerMask)){
		Debug.DrawRay(ikLeftFootRayOrigin.position, Quaternion.Euler(-45, 0, 0) * ikLeftFootRayOrigin.forward, Color.red);
		leftFootIkPos = hit.point + hit.normal * 0.17;
	}
	if(leftFootIkPos != Vector3.zero){
		fbbik.solver.leftFootEffector.position = Vector3.Lerp(fbbik.solver.leftFootEffector.position, leftFootIkPos, Time.deltaTime*10.0);
		fbbik.solver.leftFootEffector.positionWeight = 1;
	}

	var rightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
	ikRightFootRayOrigin.position = rightFoot.position - ikRightFootRayOrigin.forward*0.1;
	var rightFootIkPos : Vector3 = Vector3.zero;
	if(Physics.Raycast(ikRightFootRayOrigin.position, ikRightFootRayOrigin.forward, hit, 1.0, climbLayerMask)){
		Debug.DrawRay(ikRightFootRayOrigin.position,  ikRightFootRayOrigin.forward, Color.green);
		rightFootIkPos = hit.point + hit.normal * 0.17;
	}
	else if(Physics.Raycast(ikRightFootRayOrigin.position, Quaternion.Euler(-45, 0, 0) * ikRightFootRayOrigin.forward, hit, 1.0, climbLayerMask)){
		Debug.DrawRay(ikRightFootRayOrigin.position, Quaternion.Euler(-45, 0, 0) * ikRightFootRayOrigin.forward, Color.red);
		rightFootIkPos = hit.point + hit.normal * 0.17;
	}

	if(rightFootIkPos != Vector3.zero){
		fbbik.solver.rightFootEffector.position = Vector3.Lerp(fbbik.solver.rightFootEffector.position, rightFootIkPos, Time.deltaTime*10.0);
		fbbik.solver.rightFootEffector.positionWeight = 1;
	}

	if(inTransition){
		fbbik.solver.bodyEffector.positionOffset += transform.rotation * Vector3(0, 0, -0.5);
	}

	var leftHand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
	var dirToLeftHand = (leftHand.position - ikLeftHandRayOrigin.position).normalized;
	var leftHandIkPos : Vector3 = Vector3.zero;
	if(Physics.Raycast(ikLeftHandRayOrigin.position, dirToLeftHand, hit, 10, climbLayerMask)){
		Debug.DrawRay(ikLeftHandRayOrigin.position,  dirToLeftHand, Color.green);
		leftHandIkPos = hit.point + hit.normal * 0.1;
	}
	if(leftHandIkPos != Vector3.zero){
		fbbik.solver.leftHandEffector.position = Vector3.Lerp(fbbik.solver.leftHandEffector.position, leftHandIkPos, Time.deltaTime*10.0);
		fbbik.solver.leftHandEffector.positionWeight = 1;
	}

}


function OnAnimatorIK(){

	/*
	var rightHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
	var dirToRightHand = (rightHand.position - ikRightHandRayOrigin.position).normalized;
	if(Physics.Raycast(ikRightHandRayOrigin.position, dirToRightHand, hit, 10.0, climbLayerMask)){
		anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
		anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
		anim.SetIKPosition(AvatarIKGoal.RightHand, hit.point + hit.normal * 0.1);
		anim.SetIKRotation(AvatarIKGoal.RightHand, Quaternion.LookRotation(rightHand.up, hit.normal));
	}
	*/

}

function TransitionToTarget(){
	inTransition = true;
	//col.enabled = false;
	var i = 0.0;
    var rate = 1.0/1.0; // 1.0 / duration
    var startPos = transform.position;
    var startRot = transform.rotation;
    while (i < 1.0){
        i += Time.deltaTime * rate;
        transform.position = Vector3.Slerp(startPos, climbTarget.position, i);
        transform.rotation = Quaternion.Slerp(startRot, climbTarget.rotation, i);
        yield; 
    }
    //col.enabled = true;
    inTransition = false;
}