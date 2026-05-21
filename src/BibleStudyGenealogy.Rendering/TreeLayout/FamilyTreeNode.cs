using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record FamilyTreeNode(
    Guid PersonId,
    string DisplayName,
    string Role,
    FamilyTreeNodeKind Kind);
