namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record FamilyTreeDiagramConnector(
    Guid FamilyGroupId,
    Guid ChildPersonId,
    Guid? FatherPersonId,
    Guid? MotherPersonId,
    Guid? FatherPlaceholderId,
    Guid? MotherPlaceholderId,
    double X,
    double Y,
    bool IsUncertain);
