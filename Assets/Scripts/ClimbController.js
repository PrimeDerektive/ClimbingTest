#pragma strict

var climbRayOrigin : Transform;
var climbRayDestination : Transform;
var climbLayerMask : LayerMask;
var targetPos : Transform;

var climbing : boolean = false;
var inTransition : boolean = false;

var col : CapsuleCollider;
var animator : Animator;
var rb : Rigidbody;
var moveScripts : MonoBehaviour[];

function Start(){
	if(!col) col = GetComponent.<CapsuleCollider>();
	if(!animator) animator = GetComponent.<Animator>();
	if(!rb) rb = GetComponent.<Rigidbody>();
}


function FixedUpdate () {

	if(Input.GetKey(KeyCode.Q)){

		//capture input
		var horizontal = Input.GetAxis("Horizontal"); 
		var vertical = Input.GetAxis("Vertical");

		if(horizontal != 0.0 || vertical != 0.0){

			//movement direction is on x and z axes
			var moveDir = Vector3(horizontal, vertical, 0);

			//move the climbRayOrigin around the player on the x-z axes
			climbRayOrigin.transform.localPosition =  moveDir;

			if(!inTransition){

				//cast from climbRayOrigin to the front of the player to detect climbable surfaces
				var hit : RaycastHit;

				//check for adjacent wall to transfer

				if(Physics.Linecast(transform.position, climbRayOrigin.position, hit, climbLayerMask)){
					rb.velocity = Vector3.zero;
					targetPos.position = hit.point + (hit.normal * 0.5);
					if(vertical < -0.1) targetPos.position.y -= 1.6;
					targetPos.rotation = Quaternion.FromToRotation(targetPos.forward, -hit.normal) * targetPos.rotation;
					if(Physics.Raycast(targetPos.position, targetPos.up, 1.6, climbLayerMask)){
						Debug.DrawRay(targetPos.position, targetPos.up*1.6, Color.red);
					}
					else{
						StartCoroutine(TransitionToTargetPos());
					}
				}
				else if(
					Physics.Linecast(climbRayOrigin.position + climbRayOrigin.forward, -climbRayOrigin.up, hit, climbLayerMask) 
				){
					rb.velocity = Vector3.zero;
					targetPos.position = hit.point + (hit.normal * 0.5);
					//if(vertical < -0.1) targetPos.position.y -= 1.6;
					targetPos.rotation = Quaternion.FromToRotation(targetPos.forward, -hit.normal) * targetPos.rotation;
					if(Physics.Raycast(targetPos.position, targetPos.up, 1.6, climbLayerMask)){
						Debug.DrawRay(targetPos.position, targetPos.up*1.6, Color.red);
					}
					else{
						if(!Physics.Linecast(climbRayOrigin.position, climbRayOrigin.forward, climbLayerMask))
							StartCoroutine(TransitionToTargetPos());
					}
				}
				else if(
					Physics.Linecast(climbRayOrigin.position, transform.position + transform.forward, hit, climbLayerMask) 
				){
					rb.useGravity = false;
					for(var moveScript : MonoBehaviour in moveScripts){
						moveScript.enabled = false;
					}
					if(!animator.GetBool('Climbing')){
						animator.SetBool('Climbing', true);
					}
					if(col.center.y != 0.0) col.center.y = 0.0;

					var destination = hit.point + (hit.normal * 0.5);
					targetPos.position = destination;
					var targetDir = (destination - transform.position).normalized;
					var targetRot = Quaternion.FromToRotation(transform.forward, -hit.normal) * transform.rotation;
					targetPos.rotation = targetRot;


					rb.velocity = targetDir;
					transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5.0);
					//transform.forward = Vector3.Slerp(transform.forward, -hit.normal, Time.deltaTime);

				}

			} //eof if(!inTransition)

		}
		else{
			rb.velocity = Vector3.zero;
		}

	}

	if(Input.GetKeyUp(KeyCode.Q)){
		rb.useGravity = true;
		transform.up = Vector3.up;
		for(var moveScript : MonoBehaviour in moveScripts){
			moveScript.enabled = true;
		}
	}

}

function TransitionToTargetPos(){
	inTransition = true;
	var i = 0.0;
    var rate = 1.0/0.5; // 1.0 / duration
    var startPos = transform.position;
    var startRot = transform.rotation;
    while (i < 1.0){
        i += Time.deltaTime * rate;
        transform.position = Vector3.Lerp(startPos, targetPos.position, i);
        transform.rotation = Quaternion.Slerp(startRot, targetPos.rotation, i);
        yield; 
    }
    inTransition = false;
}
