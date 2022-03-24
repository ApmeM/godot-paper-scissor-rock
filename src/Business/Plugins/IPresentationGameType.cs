using Godot;
using System.Collections.Generic;

namespace IsometricGame.Business.Plugins
{
    public interface IPresentationGameType : IGameType
    {
        List<Vector2> Position { get; }
        Vector2 ShootPosition { get; }
        string Text { get; }
    }
}
