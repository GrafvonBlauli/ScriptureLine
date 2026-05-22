using BibleStudyGenealogy.Core.Models;
using BibleStudyGenealogy.Rendering.Timeline;
using ScriptureEvent = BibleStudyGenealogy.Core.Models.Event;

namespace BibleStudyGenealogy.Tests;

public sealed class TimelineBuilderTests
{
    [Fact]
    public void Build_KeepsDatedAndUndatedEventsVisible()
    {
        var builder = new TimelineBuilder();
        var datedEvent = new ScriptureEvent
        {
            Title = "Tempelweihe",
            EventType = EventType.Other,
            DateInfo = new DateInfo { ApproximationText = "um 960 v. Chr." },
            ShortDescription = "Datiertes Ereignis"
        };
        var undatedEvent = new ScriptureEvent
        {
            Title = "Nicht datierte Begegnung",
            EventType = EventType.Teaching,
            ShortDescription = "Ohne Datierung"
        };

        var entries = builder.Build([undatedEvent, datedEvent]);

        Assert.Equal(2, entries.Count);
        Assert.True(entries[0].HasDate);
        Assert.Equal("um 960 v. Chr.", entries[0].DateText);
        Assert.False(entries[1].HasDate);
        Assert.Equal("ohne Datierung", entries[1].DateText);
    }

    [Fact]
    public void Build_SortsDatedEventsBeforeUndatedEvents()
    {
        var builder = new TimelineBuilder();

        var entries = builder.Build(
        [
            new ScriptureEvent { Title = "Später ohne Datum", EventType = EventType.Other },
            new ScriptureEvent
            {
                Title = "Frühes Ereignis",
                EventType = EventType.Journey,
                DateInfo = new DateInfo { ApproximationText = "Jahr 01" }
            }
        ]);

        Assert.Equal("Frühes Ereignis", entries[0].Title);
        Assert.Equal("Später ohne Datum", entries[1].Title);
    }
}
