using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Infrastructure.Services;

public sealed record ProjectCreationRequest(
    string ParentDirectory,
    string ProjectName,
    string Description = "",
    string Language = ProjectDefaults.Language,
    string PreferredBibleTranslation = ProjectDefaults.PreferredBibleTranslation);
