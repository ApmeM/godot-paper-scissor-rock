using Godot;

namespace IsometricGame.Business.Logic
{
    public class TurnLogic
    {
        public Vector2 RotateToPlayer(Vector2 pos, Vector2 mapSize, int playerNumber)
        {
            if (playerNumber == 0)
            {
                return pos;
            }
            else
            {
                return new Vector2(mapSize.x - 1 - pos.x, mapSize.y - 1 - pos.y);
            }
        }

        public int GetNextPlayerNumber(int playerNumber)
        {
            return 1 - playerNumber;
        }
    }
}
