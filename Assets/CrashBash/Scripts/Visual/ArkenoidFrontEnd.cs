using UnityEngine;
using System.Collections.Generic;

namespace CrashBashRemake
{
    /// <summary>Character and side choices are independently editable before a new match.</summary>
    public sealed class ArkenoidFrontEnd : MonoBehaviour
    {
        PrototypeBootstrap bootstrap;
        bool setup;
        readonly List<PlayerSlot> draft = new List<PlayerSlot>();
        GUIStyle title, label, small;
        public void Configure(PrototypeBootstrap owner){bootstrap=owner;}
        void Update()
        {
            if(!bootstrap || !bootstrap.Match)return;
            if(setup && Input.GetKeyDown(KeyCode.Escape)){OpenSetup(false);return;}
            if(Input.GetKeyDown(KeyCode.Tab))OpenSetup(!setup);
        }
        void OpenSetup(bool open)
        {
            setup=open; bootstrap.Match.SetMenuOpen(open);
            draft.Clear();
            if(open)foreach(var slot in bootstrap.players)
                draft.Add(new PlayerSlot{slotId=slot.slotId,side=slot.side,character=slot.character,isHuman=slot.isHuman,inputIndex=slot.inputIndex});
        }
        void Styles()
        {
            if(title!=null)return;
            title=new GUIStyle(GUI.skin.label){fontSize=27,alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
            label=new GUIStyle(GUI.skin.label){fontSize=17,alignment=TextAnchor.MiddleCenter};
            small=new GUIStyle(GUI.skin.label){fontSize=13,alignment=TextAnchor.MiddleCenter};
            title.normal.textColor=label.normal.textColor=small.normal.textColor=new Color(.87f,.94f,1);
        }
        void Panel(Rect rectangle,Color color)
        {Color old=GUI.color;GUI.color=color;GUI.DrawTexture(rectangle,Texture2D.whiteTexture);GUI.color=old;}
        void OnGUI()
        {
            if(!bootstrap || !bootstrap.Match || bootstrap.Match.Simulation==null)return;
            Styles();var match=bootstrap.Match;var sim=match.Simulation;
            float scale=Mathf.Min(Screen.width/960f,Screen.height/640f);
            Matrix4x4 old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            float width=Screen.width/scale,height=Screen.height/scale;
            GUI.Label(new Rect(width/2-160,7,320,36),"CRASHBALL",title);
            float cardWidth=Mathf.Min(210,(width-60)/4);
            for(int i=0;i<4;i++)
            {
                var slot=bootstrap.players[i];var hero=sim.Hero(slot.slotId);float x=width/2-cardWidth*2+i*cardWidth;
                Panel(new Rect(x+3,48,cardWidth-6,61),new Color(.025f,.045f,.065f,.92f));
                Panel(new Rect(x+3,48,cardWidth-6,3),ArkenoidArenaVisual.SideColors[(int)hero.Side]);
                GUI.Label(new Rect(x+4,56,cardWidth-8,23),ArkenoidPilotVisual.CharacterName(hero.Character),label);
                GUI.Label(new Rect(x+4,81,cardWidth-8,20),(hero.IsEliminated?"OUT":hero.Lives.ToString("00")+" POINTS")+"    "+hero.Wins+" / "+sim.Tuning.winsNeeded,small);
            }
            string result=match.Paused?"PAUSED":sim.Phase==ArkenoidMatchPhase.Countdown?"READY  "+Mathf.CeilToInt(sim.PhaseTime)
                :sim.Phase==ArkenoidMatchPhase.MatchResult?ArkenoidPilotVisual.CharacterName(sim.Hero(sim.MatchWinnerSlot).Character)+" WINS THE MATCH"
                :sim.Phase==ArkenoidMatchPhase.RoundResult?ArkenoidPilotVisual.CharacterName(sim.Hero(sim.RoundWinnerSlot).Character)+" WINS THE ROUND":"";
            if(result.Length>0 && !setup)
            {
                Panel(new Rect(width/2-275,height/2-35,550,76),new Color(.018f,.03f,.055f,.88f));
                GUI.Label(new Rect(width/2-270,height/2-22,540,45),result,title);
                if(sim.Phase==ArkenoidMatchPhase.MatchResult && GUI.Button(new Rect(width/2-80,height/2+52,160,32),"NEW MATCH"))match.RestartMatch();
            }
            if(GUI.Button(new Rect(width-117,height-42,105,30),"PLAYERS [TAB]"))OpenSetup(!setup);
            string controls="A / D: move   SHIFT: sprint   SPACE: kick   R: new match   ESC: pause";
            if(sim.Tuning.allowAttractForCalibration)controls+="   CTRL: attract";
            if(sim.Tuning.allowCornerPickupsForCalibration)controls+="   X: repulse";
            GUI.Label(new Rect(12,height-36,width-145,23),controls,small);
            if(setup)
            {
                float x=width/2-325,y=height/2-164;
                Panel(new Rect(x,y,650,330),new Color(.016f,.032f,.056f,.98f));
                GUI.Label(new Rect(x+10,y+12,630,40),"BALLISTIX · PLAYERS",title);
                for(int i=0;i<draft.Count;i++)
                {
                    var slot=draft[i];float row=y+65+i*49;
                    GUI.Label(new Rect(x+14,row,65,30),"P"+(slot.slotId+1),label);
                    if(GUI.Button(new Rect(x+86,row,235,34),ArkenoidPilotVisual.CharacterName(slot.character)+"  >"))
                        slot.character=(CharacterId)(((int)slot.character+1)%8);
                    if(GUI.Button(new Rect(x+330,row,126,34),slot.side+"  >"))
                    {
                        ArenaSide oldSide=slot.side;ArenaSide next=(ArenaSide)(((int)slot.side+1)%4);
                        foreach(var other in draft)if(other!=slot && other.side==next){other.side=oldSide;break;}
                        slot.side=next;
                    }
                    string device=slot.isHuman?"KEYBOARD "+(slot.inputIndex+1):"CPU";
                    if(GUI.Button(new Rect(x+466,row,164,34),device+"  >"))
                    {
                        if(!slot.isHuman){slot.isHuman=true;slot.inputIndex=0;}
                        else if(slot.inputIndex==0)slot.inputIndex=1;
                        else slot.isHuman=false;
                        if(slot.isHuman)foreach(var other in draft)
                            if(other!=slot && other.isHuman && other.inputIndex==slot.inputIndex)other.isHuman=false;
                    }
                }
                if(GUI.Button(new Rect(x+110,y+274,210,38),"START NEW MATCH"))
                {
                    foreach(var choice in draft)
                    {
                        var slot=bootstrap.players.Find(p=>p.slotId==choice.slotId);
                        slot.side=choice.side;slot.character=choice.character;slot.isHuman=choice.isHuman;slot.inputIndex=choice.inputIndex;
                    }
                    setup=false;draft.Clear();bootstrap.BuildSession();
                }
                if(GUI.Button(new Rect(x+330,y+274,210,38),"BACK TO GAME"))OpenSetup(false);
            }
            GUI.matrix=old;
        }
    }
}
