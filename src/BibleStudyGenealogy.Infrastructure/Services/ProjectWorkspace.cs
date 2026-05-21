using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Infrastructure.Services;

public sealed record ProjectWorkspace(
    string RootDirectory,
    string DatabasePath,
    string ManifestPath,
    ProjectMetadata Metadata,
    ProjectSettings Settings);
