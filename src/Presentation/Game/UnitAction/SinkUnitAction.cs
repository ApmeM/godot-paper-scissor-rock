using Godot;

namespace IsometricGame.Presentation.Game.UnitAction
{
    public class SinkUnitAction : IUnitAction
    {
        private readonly Unit unit;
        private readonly AtlasTexture shipTexture;
        private float timePassed = 0;

        public SinkUnitAction(Unit unit, AtlasTexture shipTexture)
        {
            this.unit = unit;
            this.shipTexture = shipTexture;
        }

        public bool Process(float delta)
        {
            timePassed += delta;
            if (timePassed < 0.3f)
            {
                this.shipTexture.Region = new Rect2(70, this.shipTexture.Region.Position.y, this.shipTexture.Region.Size);
            }
            else if (timePassed < 0.6f)
            {
                this.shipTexture.Region = new Rect2(140, this.shipTexture.Region.Position.y, this.shipTexture.Region.Size);
            }
            else if (timePassed < 0.9f)
            {
                this.shipTexture.Region = new Rect2(210, this.shipTexture.Region.Position.y, this.shipTexture.Region.Size);
            }
            else
            {
                this.unit.GetParent().MoveChild(this.unit, 1);
                return true;
            }

            return false;
        }
    }
}
