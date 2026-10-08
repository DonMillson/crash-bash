using UnityEngine;
namespace CrashBashRemake {
 [RequireComponent(typeof(Rigidbody))]
 public class ArenaPaddle:MonoBehaviour{
  public ArenaSide side; public bool isHuman; public float speed=9f; public Transform ballTarget;
  Rigidbody body; Vector3 home; const float extent=2.15f;
  void Awake(){body=GetComponent<Rigidbody>();body.isKinematic=true;home=transform.position;}
  void FixedUpdate(){
   float input=isHuman?Input.GetAxisRaw("Horizontal"):BotInput();
   Vector3 p=body.position;
   if(side==ArenaSide.Bottom||side==ArenaSide.Top) p.x=Mathf.Clamp(p.x+input*speed*Time.fixedDeltaTime,-extent,extent);
   else p.z=Mathf.Clamp(p.z+input*speed*Time.fixedDeltaTime,-extent,extent);
   body.MovePosition(p);
  }
  float BotInput(){if(!ballTarget)return 0; float d=(side==ArenaSide.Bottom||side==ArenaSide.Top)?ballTarget.position.x-transform.position.x:ballTarget.position.z-transform.position.z;return Mathf.Abs(d)<.15f?0:Mathf.Sign(d);}
  public void ResetPaddle(){if(body)body.position=home;else transform.position=home;}
 }
}