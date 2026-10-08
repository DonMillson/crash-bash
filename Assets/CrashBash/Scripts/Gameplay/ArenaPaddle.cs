using UnityEngine;
namespace CrashBashRemake {
 [RequireComponent(typeof(Rigidbody))]
 public class ArenaPaddle:MonoBehaviour {
  public ArenaSide side;public bool isHuman;public float speed=7.5f;public Transform ballTarget;
  public bool eliminated;public float pulseRadius=1.8f,pulseCooldown=.65f;
  Rigidbody body;Vector3 home;float nextPulse;int missedPulses;const float extent=2.15f;
  void Awake(){body=GetComponent<Rigidbody>();body.isKinematic=true;home=transform.position;}
  void Update(){
   if(eliminated)return;
   bool press=isHuman?Input.GetKeyDown(KeyCode.X)||Input.GetKeyDown(KeyCode.Space):BotPulse();
   if(press && Time.time>=nextPulse)Pulse();
  }
  void FixedUpdate(){
   if(eliminated)return;
   float input=isHuman?Input.GetAxisRaw("Horizontal"):BotInput();
   bool run=isHuman?(Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.Q)||Input.GetKey(KeyCode.E)):BotRun();
   Vector3 p=body.position;float delta=input*speed*(run?1.5f:1f)*Time.fixedDeltaTime;
   if(side==ArenaSide.Bottom||side==ArenaSide.Top)p.x=Mathf.Clamp(p.x+delta,-extent,extent);
   else p.z=Mathf.Clamp(p.z+delta,-extent,extent);
   body.MovePosition(p);
  }
  float BotInput(){if(!ballTarget)return 0;float d=(side==ArenaSide.Bottom||side==ArenaSide.Top)?ballTarget.position.x-transform.position.x:ballTarget.position.z-transform.position.z;return Mathf.Abs(d)<.14f?0:Mathf.Sign(d);}
  bool BotRun(){return ballTarget && Vector3.Distance(ballTarget.position,transform.position)<5f;}
  bool BotPulse(){return ballTarget && Vector3.Distance(ballTarget.position,transform.position)<1.45f && Random.value<.075f;}
  void Pulse(){
   bool hit=false;
   foreach(var c in Physics.OverlapSphere(transform.position,pulseRadius)){
    var b=c.GetComponent<ArenaBall>();if(!b)continue;
    Vector3 away=b.transform.position-transform.position;away.y=0;
    if(away.sqrMagnitude<.01f)away=side switch{ArenaSide.Bottom=>Vector3.forward,ArenaSide.Top=>Vector3.back,ArenaSide.Left=>Vector3.right,_=>Vector3.left};
    b.Kick(away,1.5f);hit=true;
   }
   if(hit){missedPulses=0;nextPulse=Time.time+pulseCooldown;}
   else {missedPulses=Mathf.Min(3,missedPulses+1);nextPulse=Time.time+pulseCooldown+missedPulses*.7f;}
  }
  public void Eliminate(){eliminated=true;GetComponent<Collider>().enabled=false;GetComponent<Renderer>().enabled=false;}
  public void ResetPaddle(){eliminated=false;GetComponent<Collider>().enabled=true;GetComponent<Renderer>().enabled=true;if(body)body.position=home;else transform.position=home;missedPulses=0;nextPulse=0;}
 }
}