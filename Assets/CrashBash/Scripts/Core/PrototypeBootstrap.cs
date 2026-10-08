using System.Collections.Generic;
using UnityEngine;

namespace CrashBashRemake
{
    public class PrototypeBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (FindFirstObjectByType<PrototypeBootstrap>() == null)
                new GameObject("CrashBash Prototype").AddComponent<PrototypeBootstrap>();
        }

        void Start()
        {
            BuildCameraAndLight();
            BuildFloor();

            var manager = new GameObject("Match Manager").AddComponent<MatchManager>();
            var ball = CreateBall();

            var slots = new List<PlayerSlot>
            {
                NewSlot(0, ArenaSide.Bottom, CharacterId.Crash, true),
                NewSlot(1, ArenaSide.Right, CharacterId.Tiny, false),
                NewSlot(2, ArenaSide.Top, CharacterId.Dingodile, false),
                NewSlot(3, ArenaSide.Left, CharacterId.Cortex, false)
            };

            foreach (var slot in slots)
            {
                slot.paddle = CreatePaddle(slot, ball.transform);
                CreateGoal(slot.side, manager);
            }

            BuildCornerWalls();
            manager.Configure(slots, ball);
        }

        PlayerSlot NewSlot(int id, ArenaSide side, CharacterId character, bool human) =>
            new PlayerSlot { slotId = id, side = side, character = character, isHuman = human, lives = 5 };

        void BuildCameraAndLight()
        {
            Camera cam = Camera.main;
            if (!cam)
            {
                var go = new GameObject("Main Camera");
                cam = go.AddComponent<Camera>();
                go.tag = "MainCamera";
            }
            cam.transform.position = new Vector3(0, 13.5f, -10.5f);
            cam.transform.rotation = Quaternion.Euler(52f, 0, 0);
            cam.fieldOfView = 55f;

            if (!FindFirstObjectByType<Light>())
            {
                var l = new GameObject("Directional Light").AddComponent<Light>();
                l.type = LightType.Directional;
                l.intensity = 1.2f;
                l.transform.rotation = Quaternion.Euler(50, -30, 0);
            }
        }

        void BuildFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Arena Floor";
            floor.transform.position = new Vector3(0, -.25f, 0);
            floor.transform.localScale = new Vector3(13, .5f, 13);
        }

        ArenaBall CreateBall()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Ball";
            go.transform.position = new Vector3(0, .55f, 0);
            go.transform.localScale = Vector3.one * .75f;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = .8f;
            var ball = go.AddComponent<ArenaBall>();
            return ball;
        }

        ArenaPaddle CreatePaddle(PlayerSlot slot, Transform target)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"Slot {slot.slotId} - {slot.character}";
            Vector3 p = slot.side switch
            {
                ArenaSide.Bottom => new Vector3(0, .7f, -5.4f),
                ArenaSide.Top => new Vector3(0, .7f, 5.4f),
                ArenaSide.Left => new Vector3(-5.4f, .7f, 0),
                _ => new Vector3(5.4f, .7f, 0)
            };
            go.transform.position = p;
            go.transform.localScale = new Vector3(.75f, .75f, .75f);
            go.AddComponent<Rigidbody>();
            var paddle = go.AddComponent<ArenaPaddle>();
            paddle.side = slot.side;
            paddle.isHuman = slot.isHuman;
            paddle.ballTarget = target;
            return paddle;
        }

        void CreateGoal(ArenaSide side, MatchManager manager)
        {
            var go = new GameObject($"Goal {side}");
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            Vector3 p;
            Vector3 size;
            if (side == ArenaSide.Bottom || side == ArenaSide.Top)
            {
                p = new Vector3(0, .5f, side == ArenaSide.Bottom ? -6.35f : 6.35f);
                size = new Vector3(5f, 2f, .5f);
            }
            else
            {
                p = new Vector3(side == ArenaSide.Left ? -6.35f : 6.35f, .5f, 0);
                size = new Vector3(.5f, 2f, 5f);
            }
            go.transform.position = p;
            box.size = size;
            var goal = go.AddComponent<GoalZone>();
            goal.side = side;
            goal.match = manager;
        }

        void BuildCornerWalls()
        {
            Wall(new Vector3(-4.55f,.6f,-5.95f), new Vector3(4f,1.2f,.35f));
            Wall(new Vector3(4.55f,.6f,-5.95f), new Vector3(4f,1.2f,.35f));
            Wall(new Vector3(-4.55f,.6f,5.95f), new Vector3(4f,1.2f,.35f));
            Wall(new Vector3(4.55f,.6f,5.95f), new Vector3(4f,1.2f,.35f));
            Wall(new Vector3(-5.95f,.6f,-4.55f), new Vector3(.35f,1.2f,4f));
            Wall(new Vector3(-5.95f,.6f,4.55f), new Vector3(.35f,1.2f,4f));
            Wall(new Vector3(5.95f,.6f,-4.55f), new Vector3(.35f,1.2f,4f));
            Wall(new Vector3(5.95f,.6f,4.55f), new Vector3(.35f,1.2f,4f));
        }

        void Wall(Vector3 position, Vector3 scale)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = "Arena Wall";
            w.transform.position = position;
            w.transform.localScale = scale;
        }
    }
}
