using System;

namespace CrashBashRemake
{
    [Serializable]
    public class PlayerSlot
    {
        public int slotId;
        public ArenaSide side;
        public CharacterId character;
        public bool isHuman;
        public int inputIndex;
        [NonSerialized] public ArkenoidHeroController hero;
        [NonSerialized] public ArenaPaddle paddle;
        public int lives = 15;
    }
}
