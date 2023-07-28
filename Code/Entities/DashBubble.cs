using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.VortexHelper.Entities;

[CustomEntity("VortexHelper/DashBubble")]
[Tracked(false)]
public class DashBubble : Entity
{
    private const float RespawnTime = 3f;

    private readonly Image sprite;
    private readonly SineWave sine;

    private readonly Wiggler moveWiggle, sizeWiggle;
    private Vector2 moveWiggleDir;

    private readonly bool spiked, singleUse, wobble;
    private float respawnTimer;

    public DashBubble(Vector2 position, bool spiked, bool singleUse, bool wobble)
        : base(position)
    {
        this.spiked = spiked;
        this.wobble = wobble;
        this.singleUse = singleUse;

        this.Collider = new Circle(12);

        this.moveWiggle = Wiggler.Create(0.8f, 2f);
        this.moveWiggle.StartZero = true;
        Add(this.moveWiggle);

        Add(new PlayerCollider(OnPlayer));

        Add(this.sine = new SineWave(0.6f, 0f).Randomize());
        Add(this.sprite = new Image(GFX.Game["objects/VortexHelper/dashBubble/" + (spiked ? "spiked" : "idle") + "00"]));
        this.sprite.CenterOrigin();

        this.sizeWiggle = Wiggler.Create(1f, 4f, v => this.sprite.Scale = Vector2.One * (1f + v / 8));
        this.sizeWiggle.StartZero = true;
        Add(this.sizeWiggle);
    }

    public DashBubble(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Bool("spiked"), data.Bool("singleUse"), data.Bool("wobble")) { }

    public override void Added(Scene scene) => base.Added(scene);

    public override void Update()
    {
        base.Update();
        if (this.respawnTimer > 0f)
        {
            this.respawnTimer -= Engine.DeltaTime;
            if (this.respawnTimer <= 0f)
                Respawn();
        }

        if (this.wobble)
            UpdateY();
    }

    private void Respawn()
    {
        if (this.Collidable)
            return;

        this.Collidable = true;
        this.sprite.Visible = true;
        this.sizeWiggle.Start();
        Audio.Play(SFX.game_04_greenbooster_reappear, this.Position);
    }

    private void OnPlayer(Player player)
    {
        if (!player.DashAttacking)
        {
            int dashes = player.Dashes;
            float stamina = player.Stamina;

            player.PointBounce(this.Center);
            player.Dashes = dashes;
            player.Stamina = stamina;

            Audio.Play(SFX.game_06_feather_bubble_bounce, this.Position);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);

            this.moveWiggle.Start();
            this.moveWiggleDir = (this.Center - player.Center).SafeNormalize(Vector2.UnitY);
            this.sizeWiggle.Start();

            if (this.spiked)
                player.Die((player.Position - this.Position).SafeNormalize());

            return;
        }

        // dashed into
        Audio.Play(SFX.game_05_redbooster_end);
        this.sprite.Visible = false;
        this.Collidable = false;
        Celeste.Freeze(0.05f);
        if (!this.singleUse)
            this.respawnTimer = RespawnTime;

        SceneAs<Level>().ParticlesFG.Emit(Player.P_CassetteFly, 6, this.Center, Vector2.One * 7f);
    }

    private void UpdateY() => this.sprite.Position = Vector2.UnitY * this.sine.Value * 2f + this.moveWiggleDir * this.moveWiggle.Value * -8f;
}
