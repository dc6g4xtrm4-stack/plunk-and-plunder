using System;
using System.Collections.Generic;
using System.Linq;

namespace PlunkAndPlunder.Players
{
    [Serializable]
    public class PlayerManager
    {
        public List<Player> players = new List<Player>();

        public Player AddPlayer(string name, PlayerType type)
        {
            int id = players.Count;
            Player player = new Player(id, name, type);
            players.Add(player);
            return player;
        }

        public Player GetPlayer(int id)
        {
            return players.FirstOrDefault(p => p.id == id);
        }

        public List<Player> GetActivePlayers()
        {
            return players.Where(p => !p.isEliminated).ToList();
        }

        public List<Player> GetAIPlayers()
        {
            return players.Where(p => p.type == PlayerType.AI && !p.isEliminated).ToList();
        }

        public bool AllPlayersReady()
        {
            return players.All(p => p.isReady || p.isEliminated);
        }

        public void EliminatePlayer(int playerId)
        {
            Player player = GetPlayer(playerId);
            if (player != null)
            {
                player.isEliminated = true;
            }
        }

        public Player GetWinner()
        {
            List<Player> active = GetActivePlayers();
            return active.Count == 1 ? active[0] : null;
        }
    }
}
