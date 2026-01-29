using System;

namespace PlunkAndPlunder.Players
{
    [Serializable]
    public class Player
    {
        public int id;
        public string name;
        public PlayerType type;
        public bool isReady;
        public bool isEliminated;
        public int gold; // Doubloons currency

        public Player(int id, string name, PlayerType type)
        {
            this.id = id;
            this.name = name;
            this.type = type;
            this.isReady = false;
            this.isEliminated = false;
            this.gold = 100; // Starting gold
        }
    }

    public enum PlayerType
    {
        Human,
        AI,
        Remote // For future network players
    }
}
