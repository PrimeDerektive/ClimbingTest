#pragma strict

var climbSensor : Transform;
var climbTarget : Transform;
var climbLayerMask : LayerMask;

var col : CapsuleCollider;
var anim : Animator;
var rb : Rigidbody;

private var inTransition : boolean = false;

function Start(){
	if(!col) col = GetComponent.<CapsuleCollider>();
	if(!anim) anim = GetComponent.<Animator>();
	if(!rb) rb = GetComponent.<Rigidbody>();

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
				if(vertical < -0.1) climbTarget.position -= 0.5 * climbTarget.up; //downward transfer
				else if(vertical > 0.1) climbTarget.position += 0.5 * climbTarget.up; //upward transfer
				StartCoroutine(TransitionToTarget());	

			} //regular climbing
			else if(Physics.Linecast(climbSensor.position, climbSensor.position + climbSensor.forward, hit, climbLayerMask)){
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

				if(Physics.Linecast(climbSensor.position + climbSensor.forward*0.5, transform.position + transform.forward*0.5, hit, climbLayerMask)){
					rb.velocity = Vector3.zero;
					climbTarget.position = hit.point + (hit.normal * 0.25);
					climbTarget.rotation = Quaternion.FromToRotation(climbTarget.forward, -hit.normal) * climbTarget.rotation;
					//if(vertical < -0.1) climbTarget.position -= 0.5 * climbTarget.up; //downward transfer
					//else if(vertical > 0.1) climbTarget.position += 0.5 * climbTarget.up; //upward transfer
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

function TransitionToTarget(){
	inTransition = true;
	col.enabled = false;
	var i = 0.0;
    var rate = 1.0/1.0; // 1.0 / duration
    var startPos = transform.position;
    var startRot = transform.rotation;
    while (i < 1.0){
        i += Time.deltaTime * rate;
        transform.position = Vector3.Lerp(startPos, climbTarget.position, i);
        transform.rotation = Quaternion.Slerp(startRot, climbTarget.rotation, i);
        yield; 
    }
    col.enabled = true;
    inTransition = false;
}