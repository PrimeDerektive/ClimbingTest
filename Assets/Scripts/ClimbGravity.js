#pragma strict

var climbSensor : Transform;
var climbLayerMask : LayerMask;

var col : CapsuleCollider;
var anim : Animator;
var rb : Rigidbody;

function Start(){
	if(!col) col = GetComponent.<CapsuleCollider>();
	if(!anim) anim = GetComponent.<Animator>();
	if(!rb) rb = GetComponent.<Rigidbody>();

	//move center of collider
	col.center.y = 0.0;
}

function FixedUpdate () {

	//capture input
	var horizontal = Input.GetAxis("Horizontal"); 
	var vertical = Input.GetAxis("Vertical");

	//set the vars on the animator to start climbing animation
	anim.SetFloat("SpeedX", Input.GetAxis("Horizontal"));
	anim.SetFloat("SpeedY", Input.GetAxis("Vertical"));

	//movement direction is on x and z axes
	var moveDir = Vector3(horizontal, vertical, 0);

	//move the climbSensor around the player on the x-z axes
	climbSensor.transform.localPosition =  moveDir;

	var hit : RaycastHit;

	if(Physics.Linecast(climbSensor.position, climbSensor.forward, hit, climbLayerMask)){
		if(transform.parent != hit.collider.transform) transform.parent = hit.collider.transform;
		var targetPos = hit.point + hit.normal * 0.5;
		var targetDir = (targetPos - transform.position).normalized;
		var targetRot = Quaternion.FromToRotation(transform.forward, -hit.normal) * transform.rotation;
		transform.Translate(moveDir * Time.deltaTime);
		//rb.velocity = targetDir;
		transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5.0);
	}

}