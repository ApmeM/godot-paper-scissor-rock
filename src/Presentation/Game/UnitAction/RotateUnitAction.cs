using Godot;

namespace IsometricGame.Presentation.Game.UnitAction
{
    public class RotateUnitAction : IUnitAction
    {
        private const float ROTATION_SPEED = 0.05f;
        
        private readonly Unit unit;
        private readonly Vector2 destination;

        public RotateUnitAction(Unit unit, Vector2 destination)
        {
            this.unit = unit;
            this.destination = destination;
        }

        public bool Process(float delta)
        {
            var current = new Vector2(Mathf.Cos(this.unit.Rotation + Mathf.Pi / 2), Mathf.Sin(this.unit.Rotation + Mathf.Pi / 2));
            var path = current.AngleTo(destination - this.unit.Position);
            if (Mathf.Abs(path) > ROTATION_SPEED)
            {
                this.unit.Rotation += Mathf.Sign(path) * ROTATION_SPEED;
            }
            else
            {
                this.unit.Rotation = (destination - this.unit.Position).Angle() - Mathf.Pi / 2;
                return true;
            }

            return false;
        }
    }
}
