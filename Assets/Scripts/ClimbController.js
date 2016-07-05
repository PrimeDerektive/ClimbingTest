#pragma strict

var climbRayOrigin : Transform;
var climbLayerMask : LayerMask;
var targetPos : Transform;

var moveScripts : MonoBehaviour[];

//store the initial local offset of the climb ray origin
private var climbRayInitialPos : Vector3;

private var rb : Rigidbody;

function Start(){
	rb = GetComponent.<Rigidbody>();
	climbRayInitialPos = climbRayOrigin.transform.localPosition;
}


function FixedUpdate () {

	if(Input.GetButton("Fire1")){

		//capture input
		var horizontal = Input.GetAxis("Horizontal"); 
		var vertical = Input.GetAxis("Vertical");

		if(horizontal != 0.0 || vertical != 0.0){

			//movement direction is on x and z axes
			var moveDir = Vector3(horizontal, vertical, 0);

			//move the climbRayOrigin around the player on the x-z axes
			climbRayOrigin.transform.localPosition = moveDir;

			//cast from climbRayOrigin to the front of the player to detect climbable surfaces
			var hit : RaycastHit;



			if(
				Physics.Linecast(transform.position, climbRayOrigin.position, hit, climbLayerMask) ||
				Physics.Linecast(climbRayOrigin.position, transform.position + transform.forward, hit, climbLayerMask)
			){
				for(var moveScript : MonoBehaviour in moveScripts){
					moveScript.enabled = false;
				}
				rb.useGravity = false;
				var destination = hit.point + (hit.normal * 0.5);
				targetPos.position = destination;
				var targetDir = (destination - transform.position).normalized;
				rb.velocity = targetDir;
				var targetRot = Quaternion.FromToRotation(transform.forward, -hit.normal) * transform.rotation;
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime);
				//transform.forward = Vector3.Slerp(transform.forward, -hit.normal, Time.deltaTime);

			}

		}
		else{
			rb.velocity = Vector3.zero;
		}

	}
 

}