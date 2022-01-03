using Godot;

namespace IsometricGame.Presentation.Game.UnitAction
{
    public class ShootUnitAction : IUnitAction
    {
        private readonly Cannon cannon;

        public ShootUnitAction(Cannon cannon)
        {
            this.cannon = cannon;
        }

        public bool Process(float delta)
        {
            this.cannon.Shoot();
            return true;
        }
    }
}
