using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CrashBashRemake {
 public class MatchManager:MonoBehaviour {
  public readonly List<PlayerSlot> slots=new();public ArenaBall ball;
  readonly List<ArenaBall> balls=new();readonly Dictionary<ArenaSide,GameObject> barriers=new();
  bool finished;int winsNeeded=3;readonly int[] wins=new int[4];
  public void Configure(List<PlayerSlot> configuredSlots,ArenaBall arenaBall){
   slots.Clear();slots.AddRange(configuredSlots);ball=arenaBall;balls.Clear();balls.Add(ball);
   foreach(var s in slots){s.lives=15;CreateBarrier(s.side);}
   StartCoroutine(SpawnMoreBalls());
  }
  void CreateBarrier(ArenaSide side){
   bool horizontal=side==ArenaSide.Bottom||side==ArenaSide.Top;
   float e=(side==ArenaSide.Bottom||side==ArenaSide.Left)?-5.98f:5.98f;
   var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="Eliminated Goal Barrier "+side;
   g.transform.position=horizontal?new Vector3(0,.55f,e):new Vector3(e,.55f,0);
   g.transform.localScale=horizontal?new Vector3(5.35f,1.1f,.4f):new Vector3(.4f,1.1f,5.35f);
   var r=g.GetComponent<Renderer>();r.material.color=new Color(.1f,.65f,1f);g.SetActive(false);barriers[side]=g;
  }
  IEnumerator SpawnMoreBalls(){
   while(true){
    yield return new WaitForSeconds(8f);
    if(finished||balls.Count>=5)continue;
    var g=Instantiate(ball.gameObject);g.name="Ball "+(balls.Count+1);g.SetActive(true);
    var b=g.GetComponent<ArenaBall>();balls.Add(b);
    int corner=Random.Range(0,4);Vector3 origin=new Vector3(corner%2==0?-4.4f:4.4f,.55f,corner<2?-4.4f:4.4f);
    Vector3 dir=(new Vector3(Random.Range(-2f,2f),0,Random.Range(-2f,2f))-origin).normalized;
    b.LaunchFrom(origin,dir,7f+balls.Count*.6f);
   }
  }
  public void GoalConceded(ArenaSide side,ArenaBall scoredBall){
   if(finished)return;
   var s=slots.Find(x=>x.side==side);if(s==null||s.lives<=0)return;
   s.lives--;Debug.Log(s.character+" conceded: "+s.lives);
   if(s.lives==0){s.paddle.Eliminate();barriers[side].SetActive(true);}
   scoredBall.ResetBall();
   int alive=0;PlayerSlot winner=null;
   foreach(var slot in slots)if(slot.lives>0){alive++;winner=slot;}
   if(alive==1&&winner!=null)StartCoroutine(EndRound(winner));
  }
  IEnumerator EndRound(PlayerSlot winner){
   finished=true;wins[winner.slotId]++;Debug.Log("ROUND WINNER: "+winner.character+" / wins: "+wins[winner.slotId]);
   yield return new WaitForSeconds(2f);
   if(wins[winner.slotId]>=winsNeeded){Debug.Log("MATCH WINNER: "+winner.character);yield return new WaitForSeconds(2f);System.Array.Clear(wins,0,wins.Length);}
   ResetRound();
  }
  void ResetRound(){
   foreach(var s in slots){s.lives=15;s.paddle.ResetPaddle();barriers[s.side].SetActive(false);}
   for(int i=balls.Count-1;i>=1;i--){if(balls[i])Destroy(balls[i].gameObject);balls.RemoveAt(i);}
   ball.ResetBall();finished=false;
  }
  void Update(){if(Input.GetKeyDown(KeyCode.R)){StopAllCoroutines();System.Array.Clear(wins,0,wins.Length);ResetRound();StartCoroutine(SpawnMoreBalls());}}
  void OnGUI(){
   var st=new GUIStyle(GUI.skin.label){fontSize=18,alignment=TextAnchor.MiddleCenter};
   GUILayout.BeginArea(new Rect(5,5,Screen.width-10,75));GUILayout.BeginHorizontal();
   foreach(var s in slots)GUILayout.Label(s.character+"  "+s.lives.ToString("00")+" | "+wins[s.slotId]+"/"+winsNeeded,st,GUILayout.ExpandWidth(true));
   GUILayout.EndHorizontal();GUILayout.EndArea();
   GUI.Label(new Rect(12,Screen.height-28,700,24),"Move: A/D or arrows  |  Run: Shift/Q/E  |  Pulse: X/Space  |  Restart: R");
  }
 }
}