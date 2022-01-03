using Godot;

namespace IsometricGame.Presentation.Game.UnitAction
{
    public class MoveUnitAction : IUnitAction
    {
        private const int MOVE_SPEED = 160;

        private readonly Unit unit;
        private readonly Vector2 destination;

        public MoveUnitAction(Unit unit, Vector2 destination)
        {
            this.unit = unit;
            this.destination = destination;
        }

        public bool Process(float delta)
        {
            var current = this.unit.Position;
            var path = destination - current;
            var motion = path.Normalized() * MOVE_SPEED * delta;
            if (path.LengthSquared() > motion.LengthSquared())
            {
                this.unit.Position += motion;
            }
            else
            {
                this.unit.Position = this.destination;
                return true;
            }

            return false;
        }
    }
}
