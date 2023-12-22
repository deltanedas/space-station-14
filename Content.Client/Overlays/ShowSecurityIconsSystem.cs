using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Mindshield.Components;
using Content.Shared.Overlays;
using Content.Shared.PDA;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using Content.Shared.CriminalRecords;
using Content.Shared.Security;


namespace Content.Client.Overlays;
public sealed class ShowSecurityIconsSystem : EquipmentHudSystem<ShowSecurityIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeMan = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

    //Client dictionary, for not asking the state to the server every tick
    public Dictionary<EntityUid, (HUDComponent, bool)> HUDSecurityList = new Dictionary<EntityUid, (HUDComponent, bool)>();


    private int _cooldown = 1000;

    //Do not have a actual purpose, can be usefull if we want to send radio message for report wanted crew finded in the sec channel
    private string? _idName;

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string WantedStatusIcon = "WantedIcon";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string JobIconForNoId = "JobIconNoId";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusIconComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
        SubscribeNetworkEvent<SendHUDCriminal>(HandleHUDRequest);
        SubscribeNetworkEvent<ClearHUDCriminal>(ClearHUDRequest);

    }

    private void OnGetStatusIconsEvent(EntityUid uid, StatusIconComponent _, ref GetStatusIconsEvent @event)
    {
        if (!IsActive || @event.InContainer)
        {
            return;
        }

        //Remove the recent state (bool), for been refresh when the client see the UID again
        //Can be good to change with a implemented timer, because the more user is scanned by the HUD client, the more fast the cooldown go to 0
        if (_cooldown <= 0)
        {
            _cooldown = 1000;
            foreach (KeyValuePair<EntityUid, (HUDComponent, bool)> entry in HUDSecurityList)
            {
                EntityUid entityKeyUid = entry.Key;
                HUDSecurityList[entityKeyUid] = (HUDSecurityList[entityKeyUid].Item1, true);
            }

        }
        _cooldown--;
        var healthIcons = DecideSecurityIcon(uid);

        @event.StatusIcons.AddRange(healthIcons);

    }

    private IReadOnlyList<StatusIconPrototype> DecideSecurityIcon(EntityUid uid)
    {
        var result = new List<StatusIconPrototype>();

        var jobIconToGet = JobIconForNoId;
        if (_accessReader.FindAccessItemsInventory(uid, out var items))
        {
            foreach (var item in items)
            {
                // ID Card
                if (TryComp(item, out IdCardComponent? id))
                {
                    jobIconToGet = id.JobIcon;
                    _idName = id.FullName;
                    break;
                }

                // PDA
                if (TryComp(item, out PdaComponent? pda)
                    && pda.ContainedId != null
                    && TryComp(pda.ContainedId, out id))
                {
                    jobIconToGet = id.JobIcon;
                    _idName = id.FullName;
                    break;
                }
            }
        }

        if (_prototypeMan.TryIndex<StatusIconPrototype>(jobIconToGet, out var jobIcon))
            result.Add(jobIcon);
        else
            Log.Error($"Invalid job icon prototype: {jobIcon}");

        if (TryComp<MindShieldComponent>(uid, out var comp))
        {
            if (_prototypeMan.TryIndex<StatusIconPrototype>(comp.MindShieldStatusIcon.Id, out var icon))
                result.Add(icon);
        }

        // arrest icons here, WYCI.
        var wantedIcon = WantedStatusIcon;
        if (CheckCriminalState(uid))
        {
            if (_prototypeMan.TryIndex<StatusIconPrototype>(wantedIcon, out var icon))
                result.Add(icon);
        }


        return result;
    }
    //Receive the state of the uid by the server
    private void HandleHUDRequest(SendHUDCriminal msg, EntitySessionEventArgs eventArgs)
    {
        EntityUid entityUid = _entManager.GetEntity(msg.EntityID);
        if (!HUDSecurityList.ContainsKey(entityUid))
        {
            HUDSecurityList.Add(entityUid, (msg.HudInformation, false));
        }
        else
        {
            HUDSecurityList[entityUid] = (msg.HudInformation, false);
        }
    }

    private void ClearHUDRequest(ClearHUDCriminal msg)
    {
        HUDSecurityList.Clear();
    }

    //Client check the uid on his private dictionary and send true when the icon need to be active.
    //If the key not exist or been tag to be refresh, ask the server the new state
    private bool CheckCriminalState(EntityUid uid)
    {
        if (HUDSecurityList.ContainsKey(uid))
        {
            if (HUDSecurityList[uid].Item2 == true)
            {
                NetEntity requestentity = _entManager.GetNetEntity(uid);
                RaiseNetworkEvent(new GetHUDCriminal(requestentity));
            }
            var securityStatus = HUDSecurityList[uid].Item1.SecurityState;
            if (securityStatus == SecurityStatus.Wanted)
            {
                return true;
            }
            return false;
        }
        else
        {
            NetEntity requestentity = _entManager.GetNetEntity(uid);
            RaiseNetworkEvent(new GetHUDCriminal(requestentity));
            return false;
        }


    }
}
