using Robust.Shared.Prototypes;

namespace Content.Shared.Random;

// TODO: replace all uses of this with entity tables
/// <summary>
/// Linter-friendly version of weightedRandom for Entity prototypes.
/// </summary>
[Prototype]
public sealed partial class WeightedRandomEntityPrototype : IWeightedRandomPrototype<EntityPrototype>
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public Dictionary<ProtoId<EntityPrototype>, float> Weights { get; private set; } = new(); // trust
}
