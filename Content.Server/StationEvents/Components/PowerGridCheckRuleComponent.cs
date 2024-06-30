using System.Threading;
using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(PowerGridCheckRule))]
public sealed partial class PowerGridCheckRuleComponent : Component
{
    public CancellationTokenSource? AnnounceCancelToken;

    public EntityUid AffectedStation;
    public readonly List<EntityUid> Powered = new();
    public readonly List<EntityUid> Unpowered = new();

    /// <summary>
    /// Chance for an APC to explode when turned back on early.
    /// Happens both when using the UI and (re)constructing an APC.
    /// </summary>
    [DataField]
    public float ExplosionChance = 0.2f;

    public float SecondsUntilOff = 30.0f;

    public int NumberPerSecond = 0;
    public float UpdateRate => 1.0f / NumberPerSecond;
    public float FrameTimeAccumulator = 0.0f;
}
