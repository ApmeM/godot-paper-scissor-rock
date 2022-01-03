using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IsometricGame.Repository
{
    public class LadderRepository
    {
        private static readonly HashSet<int> SearchingClients = new HashSet<int>();
        
        public void JoinLadder(int clientId)
        {
            SearchingClients.Add(clientId);
        }

        public void LeaveLadder(int clientId)
        {
            SearchingClients.Remove(clientId);
        }

        public Tuple<int, int> FindPair()
        {
            if (SearchingClients.Count < 2)
            {
                return null;
            }

            var p1 = SearchingClients.First();
            SearchingClients.Remove(p1);
            var p2 = SearchingClients.First();
            SearchingClients.Remove(p2);
            return new Tuple<int, int>(p1, p2);
        }
    }
}
