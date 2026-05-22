using BibleStudyGenealogy.Core.Models;
using ScriptureEvent = BibleStudyGenealogy.Core.Models.Event;

namespace BibleStudyGenealogy.Rendering.Timeline;

public sealed class TimelineBuilder
{
    public IReadOnlyList<TimelineEntry> Build(IReadOnlyList<ScriptureEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        return events
            .Select(CreateEntry)
            .OrderByDescending(entry => entry.HasDate)
            .ThenBy(entry => entry.DateText, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static TimelineEntry CreateEntry(ScriptureEvent scriptureEvent)
    {
        var dateText = scriptureEvent.DateInfo?.ApproximationText.Trim() ?? string.Empty;
        var hasDate = !string.IsNullOrWhiteSpace(dateText);

        return new TimelineEntry(
            scriptureEvent.Id,
            scriptureEvent.Title,
            scriptureEvent.EventType,
            hasDate ? dateText : "ohne Datierung",
            scriptureEvent.CertaintyLevel,
            hasDate,
            scriptureEvent.ShortDescription);
    }
}
