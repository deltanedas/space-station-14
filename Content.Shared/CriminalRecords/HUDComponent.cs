using Content.Shared.Damage;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;
using Content.Shared.Security;
using Content.Shared.Access.Components;
using Content.Shared.CriminalRecords;

namespace Content.Shared.CriminalRecords;

[Serializable, NetSerializable]
public sealed partial class HUDComponent
{

    public string? FullName;
    public SecurityStatus SecurityState = SecurityStatus.None;
}



[Serializable, NetSerializable]
public sealed class GetHUDCriminal : EntityEventArgs
{
    /// <summary>
    /// ID of the scanned entity
    /// </summary>
    public NetEntity EntityID;


    public GetHUDCriminal(NetEntity entityID)
    {
        EntityID = entityID;
    }
}



[Serializable, NetSerializable]
public sealed class ClearHUDCriminal : EntityEventArgs
{

}

[Serializable, NetSerializable]
public sealed class SendHUDCriminal : EntityEventArgs
{
    /// <summary>
    /// NetEntity of the scanned entity
    /// </summary>
    public NetEntity EntityID;
    public HUDComponent HudInformation;

    public SendHUDCriminal(NetEntity entityID, HUDComponent hudInformation)
    {
        EntityID = entityID;
        HudInformation = hudInformation;
    }
}

