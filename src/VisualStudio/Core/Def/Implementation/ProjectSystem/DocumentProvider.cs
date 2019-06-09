using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Obsolete("This is a compatibility shim for TypeScript and Live Share; please do not use it.")]
    internal sealed class DocumentProvider
    {
        [Obsolete("This overload is a compatibility shim for TypeScript; please do not use it.")]
        public IVisualStudioHostDocument TryGetDocumentForFile(
            AbstractProject hostProject,
            string filePath,
            SourceCodeKind sourceCodeKind,
            Func<ITextBuffer, bool> canUseTextBuffer,
            Func<uint, IReadOnlyList<string>> getFolderNames,
            EventHandler updatedOnDiskHandler = null,
            EventHandler<bool> openedHandler = null,
            EventHandler<bool> closingHandler = null)
        {
            return new ShimDocument(hostProject, DocumentId.CreateNewId(hostProject.Id), filePath, sourceCodeKind);
        }

        internal class ShimDocument : IVisualStudioHostDocument
        {
            public ShimDocument(AbstractProject hostProject, DocumentId id, string filePath, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
            {
                Project = hostProject;
                Id = id ?? DocumentId.CreateNewId(hostProject.Id, filePath);
                FilePath = filePath;
                SourceCodeKind = sourceCodeKind;
            }

            public AbstractProject Project { get; }

            public DocumentId Id { get; }

            public string FilePath { get; }

            public SourceCodeKind SourceCodeKind { get; }
        }
    }
}
