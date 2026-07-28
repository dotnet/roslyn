using System.IO;
using Microsoft.CodeAnalysis;

namespace MSBuildWorkspaceTester.ViewModels
{
    internal class MetadataReferenceViewModel : HierarchyItemViewModel
    {
        private readonly MetadataReference _metadataReference;

        public MetadataReferenceViewModel(Workspace workspace, MetadataReference metadataReference)
            : base(workspace)
        {
            _metadataReference = metadataReference;
        }

        protected override string GetDisplayName()
        {
            if (_metadataReference is PortableExecutableReference peReference)
            {
                return Path.GetFileNameWithoutExtension(peReference.FilePath);
            }

            return _metadataReference.Display;
        }
    }
}
