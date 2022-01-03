using Godot;
using System.Collections.Generic;

[Tool]
public class WaveGenerator : Node2D
{
    [Export]
    public Texture WaveItem;

    [Export]
    public float Speed = 10;

    [Export]
    public float Lifetime = 1;

    [Export]
    public float Interval = 0.2f;

    [Export]
    public Vector2 ScaleFrom = Vector2.One / 2;

    [Export]
    public Vector2 ScaleTo = Vector2.One;

    private List<Node2D> waveSources;
    private readonly List<Node2D> waveNodes = new List<Node2D>();
    private float timeSinceLastWave = 0f;

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (waveSources == null)
        {
            return;
        }

        timeSinceLastWave += delta;
        if (timeSinceLastWave >= Interval)
        {
            timeSinceLastWave = 0f;

            foreach (var unit in waveSources)
            {
                var nodeLeft = new Sprite
                {
                    Texture = WaveItem,
                    Position = unit.Position,
                    Rotation = unit.Rotation,
                    ZIndex = unit.ZIndex - 1,
                    Scale = ScaleFrom
                };

                var nodeRight = new Sprite
                {
                    Texture = WaveItem,
                    Position = unit.Position,
                    Rotation = unit.Rotation + Mathf.Pi,
                    ZIndex = unit.ZIndex - 1,
                    Scale = ScaleFrom
                };

                this.AddChild(nodeLeft);
                this.AddChild(nodeRight);

                this.waveNodes.Add(nodeLeft);
                this.waveNodes.Add(nodeRight);
            }
        }

        foreach (var wave in waveNodes)
        {
            wave.Position += Vector2.Right.Rotated(wave.Rotation) * delta * Speed;

            var oldPct = (wave.Scale - ScaleFrom).Length() / (ScaleTo - ScaleFrom).Length();

            var oldTime = oldPct * this.Lifetime;
            var newTime = oldTime + delta;

            var newPct = newTime / this.Lifetime;

            wave.Scale = (ScaleTo - ScaleFrom) * newPct + ScaleFrom;

            if (newPct > 1)
            {
                wave.QueueFree();
            }
        }

        waveNodes.RemoveAll(wave => (wave.Scale - ScaleFrom).Length() / (ScaleTo - ScaleFrom).Length() > 1);
    }

    public void Start(List<Node2D> nodes)
    {
        if (WaveItem == null)
        {
            GD.PrintErr("Cant start waves as WaveItem texture is not set.");
            return;
        }

        this.waveSources = nodes;
    }

    public void Stop()
    {
        foreach (var wave in this.waveNodes)
        {
            wave.QueueFree();
        }

        this.waveNodes.Clear();
        this.waveSources = null;
    }
}
