using Godot;
using IsometricGame.Presentation;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Cannon : Node2D
{
    [Export]
    public Texture CannonBall;

    [Export]
    public float Speed = 100;

    [Export]
    public float Lifetime = 1;

    public async Task Shoot()
    {
        if (CannonBall == null)
        {
            return;
        }

        var cannonBall = new Sprite
        {
            Texture = CannonBall,
            Scale = Vector2.One * 2,
            ZIndex = 1
        };

        var tween = new Tween
        {
        };

        this.AddChild(tween);
        this.AddChild(cannonBall);
        tween.InterpolateProperty(cannonBall, "position", Vector2.Zero, Vector2.Right * this.Speed * this.Lifetime, this.Lifetime);
        tween.Start();
        await ToSignal(tween, "tween_all_completed");
        tween.QueueFree();
        cannonBall.QueueFree();
    }
}
