using UnityEngine;
namespace CrashBashRemake {
 [RequireComponent(typeof(Rigidbody),typeof(SphereCollider))]
 public class ArenaBall:MonoBehaviour {
  public float launchSpeed=7f,maxSpeed=16f;
  Rigidbody body;
  void Awake(){body=GetComponent<Rigidbody>();body.useGravity=false;body.constraints=RigidbodyConstraints.FreezePositionY;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.linearDamping=0f;var c=GetComponent<SphereCollider>();c.material=new PhysicMaterial("BallBounce"){bounciness=1f,dynamicFriction=0f,staticFriction=0f,bounceCombine=PhysicMaterialCombine.Maximum,frictionCombine=PhysicMaterialCombine.Minimum};}
  void FixedUpdate(){Vector3 v=body.linearVelocity;v.y=0; if(v.sqrMagnitude<1f)v=new Vector3(1,0,1)*launchSpeed;body.linearVelocity=v.normalized*Mathf.Clamp(v.magnitude,launchSpeed*.65f,maxSpeed);}
  public void ResetBall(){body.position=new Vector3(0,.55f,0);body.linearVelocity=Vector3.zero;Launch();}
  public void Launch(){Vector2 d=Random.insideUnitCircle.normalized;if(d.sqrMagnitude<.1f)d=Vector2.right;body.linearVelocity=new Vector3(d.x,0,d.y)*launchSpeed;}
  public void Freeze(bool frozen){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=frozen;}
  public void LaunchFrom(Vector3 origin,Vector3 direction,float speed){body.isKinematic=false;body.position=origin;body.linearVelocity=direction.normalized*speed;}
  public void Kick(Vector3 away,float multiplier){Vector3 v=body.linearVelocity;body.linearVelocity=(v.normalized+away.normalized*.65f).normalized*Mathf.Min(maxSpeed,Mathf.Max(launchSpeed,v.magnitude)*multiplier);}
 }
}
