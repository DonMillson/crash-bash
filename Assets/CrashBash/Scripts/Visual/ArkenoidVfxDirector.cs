using UnityEngine;

namespace CrashBashRemake
{
    /// <summary>Pooled authored effects driven by actual gameplay events.</summary>
    public sealed class ArkenoidVfxDirector : MonoBehaviour
    {
        const int Count=24, Segments=40;
        readonly LineRenderer[] rings=new LineRenderer[Count];
        readonly float[] ages=new float[Count], durations=new float[Count], from=new float[Count], to=new float[Count];
        readonly Color[] colors=new Color[Count];
        readonly Vector3[] centres=new Vector3[Count];
        readonly bool[] kickWaves=new bool[Count];
        readonly int[] owners=new int[Count];
        MatchManager match; int cursor;
        AudioSource source;
        AudioClip deflect,kick,goal,win;
        public void Configure(MatchManager manager,ArkenoidRenderResources resources)
        {
            match=manager; match.GameplayEvent+=OnEvent;match.MatchRestarted+=Clear;
            for(int i=0;i<Count;i++)
            {
                var go=new GameObject("Arkenoid pulse "+i);go.transform.SetParent(transform,false);
                var line=go.AddComponent<LineRenderer>();line.sharedMaterial=resources.Glow();line.useWorldSpace=true;line.loop=true;
                line.positionCount=Segments;line.widthMultiplier=.065f;line.numCornerVertices=2;line.enabled=false;
                rings[i]=line;durations[i]=0;
            }
            source=gameObject.AddComponent<AudioSource>();source.spatialBlend=0;source.playOnAwake=false;source.volume=.25f;
            deflect=Tone(resources,"Authored ball contact",.08f,760,220);
            kick=Tone(resources,"Authored kick",.16f,280,90);
            goal=Tone(resources,"Authored goal",.35f,100,42);
            win=Tone(resources,"Authored win",.75f,440,880);
        }
        static AudioClip Tone(ArkenoidRenderResources r,string name,float duration,float start,float end)
        {
            const int sampleRate=22050;
            int count=Mathf.CeilToInt(duration*sampleRate);var data=new float[count];float phase=0;
            for(int i=0;i<count;i++)
            {
                float u=(float)i/count;phase+=Mathf.Lerp(start,end,u)/sampleRate*Mathf.PI*2;
                data[i]=Mathf.Sin(phase)*Mathf.Pow(1-u,2)*Mathf.Min(1,u*30)*.6f;
            }
            var clip=r.Own(AudioClip.Create(name,count,1,sampleRate,false));clip.SetData(data,0);return clip;
        }
        void OnEvent(ArkEvent ev)
        {
            Color color=ev.SlotId>=0?ArkenoidArenaVisual.SideColors[(int)match.Simulation.Hero(ev.SlotId).Side]:new Color(.2f,.8f,1);
            float radius=.3f,end=.8f,duration=.22f;
            switch(ev.Kind)
            {
                case ArkEventKind.Kick:
                    if(match.Simulation.Tuning.crashballBallSpeedReference)
                    {
                        var tuning=match.Simulation.Tuning;
                        radius=tuning.referenceKickInitialRadius;
                        end=radius+tuning.referenceKickRadiusGrowth*(tuning.referenceKickLastActiveTick-1);
                        duration=tuning.referenceKickLastActiveTick*tuning.simulationTickSeconds;
                    }
                    else {end=match.Simulation.Tuning.kickRadius;duration=.28f;}
                    source.PlayOneShot(kick);break;
                case ArkEventKind.Repulse: end=match.Simulation.Tuning.repulseRadius;duration=.4f;color=new Color(1,.23f,.12f);source.PlayOneShot(kick);break;
                case ArkEventKind.Attract: radius=match.Simulation.Tuning.attractionRadius;end=.25f;duration=.5f;color=new Color(.83f,.3f,1);break;
                case ArkEventKind.Grab: radius=.7f;end=.25f;color=new Color(.85f,.4f,1);break;
                case ArkEventKind.Release: end=1.2f;source.PlayOneShot(kick);break;
                case ArkEventKind.Deflect: radius=.1f;end=.48f;duration=.14f;source.PlayOneShot(deflect,.45f);break;
                case ArkEventKind.Goal: end=1.1f;duration=.4f;source.PlayOneShot(goal);break;
                case ArkEventKind.BallLaunched: radius=.15f;end=.9f;source.PlayOneShot(kick,.35f);break;
                case ArkEventKind.Eliminated: end=2;duration=.6f;break;
                case ArkEventKind.RoundWon: case ArkEventKind.MatchWon: end=3;duration=1;source.PlayOneShot(win);break;
                case ArkEventKind.Pickup: radius=.1f;end=1;color=new Color(.83f,.3f,1);break;
                default: return;
            }
            int index=cursor++%Count;
            ages[index]=0;durations[index]=duration;from[index]=radius;to[index]=end;colors[index]=color;
            kickWaves[index]=ev.Kind==ArkEventKind.Kick && match.Simulation.Tuning.crashballBallSpeedReference;
            owners[index]=ev.SlotId;
            centres[index]=ArkenoidPlayerMotor.ToWorld(ev.Position,match.Simulation.Tuning.ballHeight-.1f);
            rings[index].enabled=true;
        }
        void Update()
        {
            if(!match) return;
            for(int i=0;i<Count;i++)
            {
                if(durations[i]<=0) continue;
                if(!match.Paused) ages[i]+=Time.deltaTime;
                float u=ages[i]/durations[i];
                if(u>=1){rings[i].enabled=false;durations[i]=0;continue;}
                float radius=Mathf.Lerp(from[i],to[i],1-Mathf.Pow(1-u,2));
                if(kickWaves[i])
                {
                    var tuning=match.Simulation.Tuning;
                    radius=Mathf.Min(to[i],from[i]+ages[i]/tuning.simulationTickSeconds*tuning.referenceKickRadiusGrowth);
                    var hero=match.Simulation.Hero(owners[i]);
                    if(hero!=null)centres[i]=ArkenoidPlayerMotor.ToWorld(match.Simulation.Geometry.HeroPosition(hero.Side,hero.Lateral),tuning.ballHeight-.1f);
                }
                Color color=colors[i];color.a=(1-u)*.8f;
                rings[i].startColor=rings[i].endColor=color;
                for(int j=0;j<Segments;j++)
                {
                    float angle=j*Mathf.PI*2/Segments;
                    rings[i].SetPosition(j,centres[i]+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));
                }
            }
        }
        public void Clear()
        { for(int i=0;i<Count;i++){durations[i]=0;rings[i].enabled=false;}if(source)source.Stop(); }
        void OnDestroy(){if(match){match.GameplayEvent-=OnEvent;match.MatchRestarted-=Clear;}}
    }
}
