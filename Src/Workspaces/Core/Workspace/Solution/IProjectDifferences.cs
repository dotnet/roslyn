using System.Collections.Generic;
using Roslyn.Compilers;

namespace Roslyn.Services
{
    public interface IProjectDifferences
    {
        IEnumerable<ProjectReference> GetAddedProjectReferences();
        IEnumerable<ProjectReference> GetRemovedProjectReferences();

        IEnumerable<MetadataReference> GetAddedMetadataReferences();
        IEnumerable<MetadataReference> GetRemovedMetadataReferences();

        IEnumerable<DocumentId> GetAddedDocuments();
        IEnumerable<DocumentId> GetChangedDocuments();
        IEnumerable<DocumentId> GetRemovedDocuments();
    }
}