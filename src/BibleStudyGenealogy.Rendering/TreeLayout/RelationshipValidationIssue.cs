namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record RelationshipValidationIssue(
    RelationshipValidationIssueType IssueType,
    string Message,
    Guid? PersonId = null,
    Guid? RelationshipId = null);
