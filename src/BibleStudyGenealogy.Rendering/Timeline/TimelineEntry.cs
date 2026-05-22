using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Rendering.Timeline;

public sealed record TimelineEntry(
    Guid EventId,
    string Title,
    EventType EventType,
    string DateText,
    CertaintyLevel CertaintyLevel,
    bool HasDate,
    string Description);
