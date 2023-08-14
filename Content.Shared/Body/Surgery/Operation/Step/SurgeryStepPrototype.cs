using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Body.Surgery.Operation.Step;

[Prototype("surgeryStep")]
public sealed class SurgeryStepPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;
}
