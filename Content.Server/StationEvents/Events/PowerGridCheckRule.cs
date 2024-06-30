using System.Threading;
using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.StationEvents.Events
{
    [UsedImplicitly]
    public sealed class PowerGridCheckRule : StationEventSystem<PowerGridCheckRuleComponent>
    {
        [Dependency] private readonly ApcSystem _apcSystem = default!;
        [Dependency] private readonly ExplosionSystem _explosion = default!;

        private EntityQuery<ExplosiveComponent> _explosiveQuery;

        public override void Initialize()
        {
            base.Initialize();

            _explosiveQuery = GetEntityQuery<ExplosiveComponent>();

            SubscribeLocalEvent<ApcToggledEvent>(OnApcToggled);
        }

        protected override void Started(EntityUid uid, PowerGridCheckRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, component, gameRule, args);

            if (!TryGetRandomStation(out var chosenStation))
                return;

            component.AffectedStation = chosenStation.Value;

            var query = AllEntityQuery<ApcComponent, TransformComponent>();
            while (query.MoveNext(out var apcUid, out var apc, out var xform))
            {
                if (apc.MainBreakerEnabled && StationSystem.GetGridStation(xform.GridUid) == chosenStation)
                    component.Powered.Add(apcUid);
            }

            RobustRandom.Shuffle(component.Powered);

            component.NumberPerSecond = Math.Max(1, (int)(component.Powered.Count / component.SecondsUntilOff)); // Number of APCs to turn off every second. At least one.
        }

        protected override void Ended(EntityUid uid, PowerGridCheckRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
        {
            base.Ended(uid, component, gameRule, args);

            foreach (var entity in component.Unpowered)
            {
                if (Deleted(entity))
                    continue;

                if (TryComp(entity, out ApcComponent? apcComponent))
                {
                    if(!apcComponent.MainBreakerEnabled)
                        _apcSystem.ApcToggleBreaker(entity, apcComponent);
                }
            }

            // Can't use the default EndAudio
            component.AnnounceCancelToken?.Cancel();
            component.AnnounceCancelToken = new CancellationTokenSource();
            Timer.Spawn(3000, () =>
            {
                Audio.PlayGlobal("/Audio/Announcements/power_on.ogg", Filter.Broadcast(), true, AudioParams.Default.WithVolume(-4f));
            }, component.AnnounceCancelToken.Token);
            component.Unpowered.Clear();
        }

        protected override void ActiveTick(EntityUid uid, PowerGridCheckRuleComponent component, GameRuleComponent gameRule, float frameTime)
        {
            base.ActiveTick(uid, component, gameRule, frameTime);

            var updates = 0;
            component.FrameTimeAccumulator += frameTime;
            if (component.FrameTimeAccumulator > component.UpdateRate)
            {
                updates = (int) (component.FrameTimeAccumulator / component.UpdateRate);
                component.FrameTimeAccumulator -= component.UpdateRate * updates;
            }

            for (var i = 0; i < updates; i++)
            {
                if (component.Powered.Count == 0)
                    break;

                var selected = component.Powered.Pop();
                if (Deleted(selected))
                    continue;
                if (TryComp<ApcComponent>(selected, out var apcComponent))
                {
                    if (apcComponent.MainBreakerEnabled)
                        _apcSystem.ApcToggleBreaker(selected, apcComponent);
                }
                component.Unpowered.Add(selected);
            }
        }

        private void OnApcToggled(ref ApcToggledEvent args)
        {
            var grid = Transform(args.Apc).GridUid;
            if (StationSystem.GetGridStation(grid) is not {} target)
                return;

            if (!_explosiveQuery.TryComp(args.Apc, out var explosive))
                return;

            var query = QueryActiveRules();
            while (query.MoveNext(out var uid, out _, out var comp, out _))
            {
                if (comp.AffectedStation != target)
                    return;

                if (!RobustRandom.Prob(comp.ExplosionChance))
                    return;

                _explosion.TriggerExplosive(args.Apc, explosive);
            }
        }
    }
}
