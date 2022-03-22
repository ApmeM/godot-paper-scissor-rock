using System;

namespace IsometricGame.Presentation.Game.UnitAction
{
    public class CallbackUnitAction : IUnitAction
    {
        private readonly Unit unit;
        private readonly Action<Unit> callback;

        public CallbackUnitAction(Unit unit, Action<Unit> callback)
        {
            this.unit = unit;
            this.callback = callback;
        }

        public bool Process(float delta)
        {
            callback(unit);
            return true;
        }
    }
}
