using System;
using UnityEngine;

namespace CrashBashRemake
{
    public enum ArenaSide { Bottom, Right, Top, Left }
    public enum CharacterId { Crash, Coco, Cortex, NBrio, Tiny, Dingodile, KoalaKong, RillaRoo }

    [Serializable]
    public class PlayerSlot
    {
        public int slotId;
        public ArenaSide side;
        public CharacterId character;
        public bool isHuman;
        [NonSerialized] public ArenaPaddle paddle;
        public int lives = 5;
    }
}
