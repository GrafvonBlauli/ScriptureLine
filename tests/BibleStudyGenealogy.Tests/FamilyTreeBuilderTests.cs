using BibleStudyGenealogy.Core.Models;
using BibleStudyGenealogy.Rendering.TreeLayout;

namespace BibleStudyGenealogy.Tests;

public sealed class FamilyTreeBuilderTests
{
    [Fact]
    public void Build_GroupsParentsPartnersAndChildrenForFocusPerson()
    {
        var builder = new FamilyTreeBuilder();
        var parent = new Person { Id = Guid.NewGuid(), MainName = "Vater" };
        var focus = new Person { Id = Guid.NewGuid(), MainName = "Kind" };
        var partner = new Person { Id = Guid.NewGuid(), MainName = "Partner" };
        var child = new Person { Id = Guid.NewGuid(), MainName = "Nachkomme" };
        var people = new[] { parent, focus, partner, child };
        var relationships = new[]
        {
            new Relationship
            {
                PersonAId = parent.Id,
                PersonBId = focus.Id,
                RelationshipType = RelationshipType.ParentChild,
                Direction = RelationshipDirection.PersonAToPersonB,
                CertaintyLevel = CertaintyLevel.ExplicitlyMentioned
            },
            new Relationship
            {
                PersonAId = focus.Id,
                PersonBId = partner.Id,
                RelationshipType = RelationshipType.Spouse,
                Direction = RelationshipDirection.Undirected,
                CertaintyLevel = CertaintyLevel.Likely
            },
            new Relationship
            {
                PersonAId = focus.Id,
                PersonBId = child.Id,
                RelationshipType = RelationshipType.ParentChild,
                Direction = RelationshipDirection.PersonAToPersonB,
                CertaintyLevel = CertaintyLevel.ExplicitlyMentioned
            }
        };

        var snapshot = builder.Build(focus, people, relationships);

        Assert.Equal("Kind", snapshot.FocusPerson.DisplayName);
        Assert.Single(snapshot.Parents);
        Assert.Equal("Vater", snapshot.Parents[0].DisplayName);
        Assert.Single(snapshot.Partners);
        Assert.Equal("Partner", snapshot.Partners[0].DisplayName);
        Assert.Single(snapshot.Children);
        Assert.Equal("Nachkomme", snapshot.Children[0].DisplayName);
    }

    [Fact]
    public void Build_TreatsUndirectedParentChildAsOtherAndUncertain()
    {
        var builder = new FamilyTreeBuilder();
        var focus = new Person { Id = Guid.NewGuid(), MainName = "Fokus" };
        var other = new Person { Id = Guid.NewGuid(), MainName = "Unklar" };
        var relationship = new Relationship
        {
            PersonAId = focus.Id,
            PersonBId = other.Id,
            RelationshipType = RelationshipType.ParentChild,
            Direction = RelationshipDirection.Undirected,
            CertaintyLevel = CertaintyLevel.Possible
        };

        var snapshot = builder.Build(focus, new[] { focus, other }, new[] { relationship });

        Assert.Empty(snapshot.Parents);
        Assert.Empty(snapshot.Children);
        Assert.Single(snapshot.OtherRelations);
        Assert.True(snapshot.Links[0].IsUncertain);
        Assert.False(snapshot.Links[0].IsDirectional);
    }

    [Fact]
    public void Build_IgnoresArchivedRelationships()
    {
        var builder = new FamilyTreeBuilder();
        var focus = new Person { Id = Guid.NewGuid(), MainName = "Fokus" };
        var child = new Person { Id = Guid.NewGuid(), MainName = "Archiviertes Kind" };
        var relationship = new Relationship
        {
            PersonAId = focus.Id,
            PersonBId = child.Id,
            RelationshipType = RelationshipType.ParentChild,
            Direction = RelationshipDirection.PersonAToPersonB,
            Status = RelationshipStatus.Archived
        };

        var snapshot = builder.Build(focus, new[] { focus, child }, new[] { relationship });

        Assert.Empty(snapshot.Children);
        Assert.Empty(snapshot.Links);
    }
}
