using Robust.Shared.Random; // GreyStation

namespace Content.Shared.Speech
{
    public sealed class SpeechSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!; // GreyStation

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<SpeechComponent, MapInitEvent>(OnMapInit); // GreyStation
        }

        public void SetSpeech(EntityUid uid, bool value, SpeechComponent? component = null)
        {
            if (value && !Resolve(uid, ref component))
                return;

            component = EnsureComp<SpeechComponent>(uid);

            if (component.Enabled == value)
                return;

            component.Enabled = value;

            Dirty(uid, component);
        }

        private void OnSpeakAttempt(SpeakAttemptEvent args)
        {
            if (!TryComp(args.Uid, out SpeechComponent? speech) || !speech.Enabled)
                args.Cancel();
        }

        /// <summary>
        /// GreyStation: Randomize speech color for everything.
        /// This gets overriden by humanoid character profile loading.
        /// </summary>
        private void OnMapInit(Entity<SpeechComponent> ent, ref MapInitEvent args)
        {
            ent.Comp.MessageColor = new Color(_random.NextFloat(1), _random.NextFloat(1), _random.NextFloat(1), 1);
        }
    }
}
