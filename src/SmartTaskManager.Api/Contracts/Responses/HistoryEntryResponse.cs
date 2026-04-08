using System;
using SmartTaskManager.Domain.Records;

namespace SmartTaskManager.Api.Contracts.Responses;

public sealed record HistoryEntryResponse(
    DateTime OccurredOnUtc,
    string Action,
    string Details)
{
    public static HistoryEntryResponse FromDomain(HistoryEntry historyEntry)
    {
        ArgumentNullException.ThrowIfNull(historyEntry);

        return new HistoryEntryResponse(
            historyEntry.OccurredOnUtc,
            historyEntry.Action,
            historyEntry.Details);
    }
}
