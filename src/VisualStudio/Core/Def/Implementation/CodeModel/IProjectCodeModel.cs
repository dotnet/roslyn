using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal interface IProjectCodeModel
    {
        EnvDTE.FileCodeModel GetOrCreateFileCodeModel(string filePath, object parent);
        EnvDTE.CodeModel GetOrCreateRootCodeModel(Project parent);
        void OnSourceFileRemoved(string fileName);
        void OnSourceFileRenaming(string filePath, string newFilePath);
        void OnProjectClosed();
    }
}
