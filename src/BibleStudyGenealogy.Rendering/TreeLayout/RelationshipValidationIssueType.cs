namespace BibleStudyGenealogy.Rendering.TreeLayout;

public enum RelationshipValidationIssueType
{
    DuplicateRelationship,
    MoreThanTwoParents,
    CycleDetected,
    InvalidDateLogic
}
