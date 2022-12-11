using UnityEngine;

public class DoorOpen : MonoBehaviour
{
	private Animator animator;

	public void Start(){
		animator = this.GetComponent<Animator>();
	}
	public void OnTriggerEnter(Collider col){
		if(col.gameObject.tag == "Player"){
			this.animator.SetTrigger("Open");

		}

	}
}