using System.Diagnostics.CodeAnalysis;
using Content.Server.StationRecords.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.StationRecords;
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Access.Components;
using Content.Shared.PDA;


namespace Content.Server.CriminalRecords.Systems;

public sealed class CriminalHudSystem : EntitySystem
{

    [Dependency] private readonly CriminalRecordsSystem _criminalRecords = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    /// [Dependency] private readonly SharedIdCardSystem _sharedIdCardSystem = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<GetHUDCriminal>(HandleHUDRequest);

    }
    //Receive the request of the state by a client, find the id and the key in station record associate.
    private void HandleHUDRequest(GetHUDCriminal msg, EntitySessionEventArgs eventArgs)
    {
        //Console.WriteLine($"HUDRequest : {msg.EntityID}, {eventArgs}");
        EntityUid entityUid = _entManager.GetEntity(msg.EntityID);

        //Console.WriteLine($"Server UID gotten : {entityUid}");
        var record = new HUDComponent();
        if (GetIdCard(entityUid, out var targetID))
        {
            //Console.WriteLine($"Server target ID gotten : {targetID}");
            if (_entManager.TrySystem<StationRecordsSystem>(out var recordsSystem)
            && _entManager.TryGetComponent(targetID, out StationRecordKeyStorageComponent? keyStorage)
            && keyStorage.Key is {} key)
            {
                //Console.WriteLine($"Key gotten : {key}");
                if (recordsSystem.TryGetRecord<GeneralStationRecord>(key, out var generalRecord)
                && recordsSystem.TryGetRecord<CriminalRecord>(key, out var criminalRecord))
                {
                    //Console.WriteLine($"Nameget : {generalRecord.Name}, {criminalRecord.Status} ");

                    record.FullName = generalRecord.Name;
                    record.SecurityState = criminalRecord.Status;
                }
            }
        }
        RaiseNetworkEvent(new SendHUDCriminal(msg.EntityID, record));
    }
    //Check the inventory of the entity, for find the primary(shown) ID
    //For now only use ID in PDA, but can be modified.
    private bool GetIdCard(EntityUid entityUid, out Entity<IdCardComponent> idCard)
    {
        //Console.WriteLine($"Server Get Id Card Scanning...");
        if (_accessReader.FindAccessItemsInventory(entityUid, out var items))
        {
            foreach (var item in items)
            {
                // ID Card (Make crash because of permission error)
                //if (TryComp(item, out IdCardComponent? id))
                //{
                //    Console.WriteLine($"Server IdCardComponent gotten via ID: {id}");
                //    idCard = (entityUid, id);
                //    return true;
                //}

                // PDA
                if (TryComp(item, out PdaComponent? pda)
                    && pda.ContainedId != null
                    && TryComp(pda.ContainedId, out IdCardComponent? id))
                {
                    //Console.WriteLine($"Server IdCardComponent gotten via PDA: {id}");
                    idCard = (pda.ContainedId.Value, id);
                    return true;
                }
            }
        }
        idCard = default;
        return false;
    }
}
