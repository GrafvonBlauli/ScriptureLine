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

    [Fact]
    public void BuildDiagram_PlacesRelativesOnExpectedGenerationRows()
    {
        var builder = new FamilyTreeBuilder();
        var parent = new Person { Id = Guid.NewGuid(), MainName = "Vater" };
        var focus = new Person { Id = Guid.NewGuid(), MainName = "Kind" };
        var partner = new Person { Id = Guid.NewGuid(), MainName = "Partner" };
        var sibling = new Person { Id = Guid.NewGuid(), MainName = "Geschwister" };
        var child = new Person { Id = Guid.NewGuid(), MainName = "Nachkomme" };
        var people = new[] { parent, focus, partner, sibling, child };
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
                PersonBId = sibling.Id,
                RelationshipType = RelationshipType.Sibling,
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

        var diagram = builder.BuildDiagram(focus, people, relationships, new FamilyTreeLayoutOptions(2, false));
        var parentNode = diagram.Nodes.Single(node => node.PersonId == parent.Id);
        var focusNode = diagram.Nodes.Single(node => node.PersonId == focus.Id);
        var partnerNode = diagram.Nodes.Single(node => node.PersonId == partner.Id);
        var siblingNode = diagram.Nodes.Single(node => node.PersonId == sibling.Id);
        var childNode = diagram.Nodes.Single(node => node.PersonId == child.Id);

        Assert.True(parentNode.Y < focusNode.Y);
        Assert.Equal(focusNode.Y, partnerNode.Y);
        Assert.Equal(focusNode.Y, siblingNode.Y);
        Assert.True(childNode.Y > focusNode.Y);
        Assert.Equal(FamilyTreeNodeKind.Sibling, siblingNode.Kind);
    }

    [Fact]
    public void BuildDiagram_AppliesGenerationLimitAndAllConnectedMode()
    {
        var builder = new FamilyTreeBuilder();
        var grandParent = new Person { Id = Guid.NewGuid(), MainName = "Großvater" };
        var parent = new Person { Id = Guid.NewGuid(), MainName = "Vater" };
        var focus = new Person { Id = Guid.NewGuid(), MainName = "Kind" };
        var people = new[] { grandParent, parent, focus };
        var relationships = new[]
        {
            new Relationship
            {
                PersonAId = grandParent.Id,
                PersonBId = parent.Id,
                RelationshipType = RelationshipType.ParentChild,
                Direction = RelationshipDirection.PersonAToPersonB
            },
            new Relationship
            {
                PersonAId = parent.Id,
                PersonBId = focus.Id,
                RelationshipType = RelationshipType.ParentChild,
                Direction = RelationshipDirection.PersonAToPersonB
            }
        };

        var limitedDiagram = builder.BuildDiagram(focus, people, relationships, new FamilyTreeLayoutOptions(1, false));
        var fullDiagram = builder.BuildDiagram(focus, people, relationships, new FamilyTreeLayoutOptions(int.MaxValue, true));

        Assert.DoesNotContain(limitedDiagram.Nodes, node => node.PersonId == grandParent.Id);
        Assert.Contains(fullDiagram.Nodes, node => node.PersonId == grandParent.Id);
    }

    [Fact]
    public void BuildDiagram_MarksUncertainLinksAndNodes()
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

        var diagram = builder.BuildDiagram(focus, new[] { focus, other }, new[] { relationship }, FamilyTreeLayoutOptions.Default);

        Assert.True(diagram.Links.Single().IsUncertain);
        Assert.True(diagram.Nodes.Single(node => node.PersonId == other.Id).IsUncertain);
    }

    [Fact]
    public void BuildDiagram_CreatesFamilyConnectorForParentsAndChild()
    {
        var builder = new FamilyTreeBuilder();
        var father = new Person { Id = Guid.NewGuid(), MainName = "Amram", Gender = Gender.Male };
        var mother = new Person { Id = Guid.NewGuid(), MainName = "Jochebed", Gender = Gender.Female };
        var child = new Person { Id = Guid.NewGuid(), MainName = "Moses", Gender = Gender.Male };
        var relationships = new[]
        {
            new Relationship
            {
                PersonAId = father.Id,
                PersonBId = child.Id,
                RelationshipType = RelationshipType.ParentChild,
                Direction = RelationshipDirection.PersonAToPersonB
            },
            new Relationship
            {
                PersonAId = mother.Id,
                PersonBId = child.Id,
                RelationshipType = RelationshipType.ParentChild,
                Direction = RelationshipDirection.PersonAToPersonB
            }
        };

        var diagram = builder.BuildDiagram(child, new[] { father, mother, child }, relationships, FamilyTreeLayoutOptions.Default);
        var connector = Assert.Single(diagram.Connectors);
        var fatherNode = diagram.Nodes.Single(node => node.PersonId == father.Id);
        var motherNode = diagram.Nodes.Single(node => node.PersonId == mother.Id);
        var childNode = diagram.Nodes.Single(node => node.PersonId == child.Id);

        Assert.Equal(child.Id, connector.ChildPersonId);
        Assert.Equal(father.Id, connector.FatherPersonId);
        Assert.Equal(mother.Id, connector.MotherPersonId);
        Assert.Equal(fatherNode.Y, motherNode.Y);
        Assert.True(fatherNode.Y < childNode.Y);
        Assert.True(connector.Y > fatherNode.Y);
        Assert.True(connector.Y < childNode.Y);
        Assert.DoesNotContain(diagram.Links, link => link.RelationshipType == RelationshipType.ParentChild);
    }

    [Fact]
    public void BuildDiagram_AddsMissingParentPlaceholdersForFocusPerson()
    {
        var builder = new FamilyTreeBuilder();
        var child = new Person { Id = Guid.NewGuid(), MainName = "Moses", Gender = Gender.Male };

        var diagram = builder.BuildDiagram(child, new[] { child }, Array.Empty<Relationship>(), FamilyTreeLayoutOptions.Default);
        var fatherPlaceholder = diagram.Nodes.Single(node => node.PlaceholderKind == FamilyTreePlaceholderKind.Father);
        var motherPlaceholder = diagram.Nodes.Single(node => node.PlaceholderKind == FamilyTreePlaceholderKind.Mother);
        var connector = Assert.Single(diagram.Connectors);

        Assert.True(fatherPlaceholder.IsPlaceholder);
        Assert.True(motherPlaceholder.IsPlaceholder);
        Assert.Equal(child.Id, fatherPlaceholder.SourcePersonId);
        Assert.Equal(child.Id, motherPlaceholder.SourcePersonId);
        Assert.Equal(fatherPlaceholder.PersonId, connector.FatherPlaceholderId);
        Assert.Equal(motherPlaceholder.PersonId, connector.MotherPlaceholderId);
    }

    [Fact]
    public void BuildDiagram_ParentPlaceholdersDoNotOverlapFocusPerson()
    {
        var builder = new FamilyTreeBuilder();
        var child = new Person { Id = Guid.NewGuid(), MainName = "Noah", Gender = Gender.Male };

        var diagram = builder.BuildDiagram(child, new[] { child }, Array.Empty<Relationship>(), FamilyTreeLayoutOptions.Default);
        var childNode = diagram.Nodes.Single(node => node.PersonId == child.Id);
        var fatherPlaceholder = diagram.Nodes.Single(node => node.PlaceholderKind == FamilyTreePlaceholderKind.Father);
        var motherPlaceholder = diagram.Nodes.Single(node => node.PlaceholderKind == FamilyTreePlaceholderKind.Mother);

        Assert.True(fatherPlaceholder.Y < childNode.Y);
        Assert.True(motherPlaceholder.Y < childNode.Y);
        Assert.True(fatherPlaceholder.X < childNode.X);
        Assert.True(motherPlaceholder.X > childNode.X);
        Assert.False(NodesOverlap(fatherPlaceholder, childNode));
        Assert.False(NodesOverlap(motherPlaceholder, childNode));
        Assert.False(NodesOverlap(fatherPlaceholder, motherPlaceholder));
    }

    [Fact]
    public void BuildDiagram_MultipleChildrenDoNotOverlap()
    {
        var builder = new FamilyTreeBuilder();
        var focus = new Person { Id = Guid.NewGuid(), MainName = "Noah", Gender = Gender.Male };
        var firstChild = new Person { Id = Guid.NewGuid(), MainName = "Sem", Gender = Gender.Male };
        var secondChild = new Person { Id = Guid.NewGuid(), MainName = "Ham", Gender = Gender.Male };
        var relationships = new[]
        {
            new Relationship
            {
                PersonAId = focus.Id,
                PersonBId = firstChild.Id,
                RelationshipType = RelationshipType.ParentChild,
                Direction = RelationshipDirection.PersonAToPersonB
            },
            new Relationship
            {
                PersonAId = focus.Id,
                PersonBId = secondChild.Id,
                RelationshipType = RelationshipType.ParentChild,
                Direction = RelationshipDirection.PersonAToPersonB
            }
        };

        var diagram = builder.BuildDiagram(focus, new[] { focus, firstChild, secondChild }, relationships, FamilyTreeLayoutOptions.Default);
        var firstChildNode = diagram.Nodes.Single(node => node.PersonId == firstChild.Id);
        var secondChildNode = diagram.Nodes.Single(node => node.PersonId == secondChild.Id);

        Assert.Equal(firstChildNode.Y, secondChildNode.Y);
        Assert.False(NodesOverlap(firstChildNode, secondChildNode));
    }

    [Fact]
    public void BuildDiagram_CreatesConnectionModelForFamilyConnectors()
    {
        var builder = new FamilyTreeBuilder();
        var father = new Person { Id = Guid.NewGuid(), MainName = "Amram", Gender = Gender.Male };
        var mother = new Person { Id = Guid.NewGuid(), MainName = "Jochebed", Gender = Gender.Female };
        var child = new Person { Id = Guid.NewGuid(), MainName = "Moses", Gender = Gender.Male };
        var relationships = new[]
        {
            new Relationship
            {
                PersonAId = father.Id,
                PersonBId = child.Id,
                RelationshipType = RelationshipType.ParentChild,
                Direction = RelationshipDirection.PersonAToPersonB
            },
            new Relationship
            {
                PersonAId = mother.Id,
                PersonBId = child.Id,
                RelationshipType = RelationshipType.ParentChild,
                Direction = RelationshipDirection.PersonAToPersonB
            }
        };

        var diagram = builder.BuildDiagram(child, new[] { father, mother, child }, relationships, FamilyTreeLayoutOptions.Default);

        Assert.Equal(2, diagram.Connections.Count(connection => connection.Type == FamilyTreeConnectionType.ParentToFamily));
        Assert.Single(diagram.Connections, connection => connection.Type == FamilyTreeConnectionType.FamilyToChild);
        Assert.All(diagram.Connections, connection => Assert.NotEqual(connection.Start, connection.End));
    }

    [Fact]
    public void BuildDiagram_CreatesPlaceholderConnectionsForMissingParents()
    {
        var builder = new FamilyTreeBuilder();
        var child = new Person { Id = Guid.NewGuid(), MainName = "Noah", Gender = Gender.Male };

        var diagram = builder.BuildDiagram(child, new[] { child }, Array.Empty<Relationship>(), FamilyTreeLayoutOptions.Default);

        Assert.Equal(2, diagram.Connections.Count(connection => connection.Type == FamilyTreeConnectionType.Placeholder));
        Assert.Single(diagram.Connections, connection => connection.Type == FamilyTreeConnectionType.FamilyToChild);
    }

    [Fact]
    public void BuildDiagram_ChildConnectorDoesNotMoveFocusPersonIntoChildParentSlot()
    {
        var builder = new FamilyTreeBuilder();
        var focus = new Person { Id = Guid.NewGuid(), MainName = "Noah", Gender = Gender.Male };
        var child = new Person { Id = Guid.NewGuid(), MainName = "Sem", Gender = Gender.Male };
        var relationship = new Relationship
        {
            PersonAId = focus.Id,
            PersonBId = child.Id,
            RelationshipType = RelationshipType.ParentChild,
            Direction = RelationshipDirection.PersonAToPersonB
        };

        var diagram = builder.BuildDiagram(focus, new[] { focus, child }, new[] { relationship }, FamilyTreeLayoutOptions.Default);
        var focusNode = diagram.Nodes.Single(node => node.PersonId == focus.Id);
        var childNode = diagram.Nodes.Single(node => node.PersonId == child.Id);
        var childConnector = diagram.Connectors.Single(connector => connector.ChildPersonId == child.Id);

        Assert.True(focusNode.Y < childNode.Y);
        Assert.True(childConnector.Y > focusNode.Y);
        Assert.True(childConnector.Y < childNode.Y);
        Assert.NotEqual(childConnector.FamilyGroupId, focusNode.FamilyGroupId);
    }

    private static bool NodesOverlap(FamilyTreeDiagramNode firstNode, FamilyTreeDiagramNode secondNode)
    {
        return firstNode.X < secondNode.X + FamilyTreeLayoutMetrics.NodeWidth
            && firstNode.X + FamilyTreeLayoutMetrics.NodeWidth > secondNode.X
            && firstNode.Y < secondNode.Y + FamilyTreeLayoutMetrics.NodeHeight
            && firstNode.Y + FamilyTreeLayoutMetrics.NodeHeight > secondNode.Y;
    }
}
