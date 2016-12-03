using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    [Export(typeof(IDocumentOptionsProviderFactory))]
    class EditorConfigDocumentOptionsProviderFactory : IDocumentOptionsProviderFactory
    {
        public IDocumentOptionsProvider Create(Workspace workspace)
        {
            return new EditorConfigDocumentOptionsProvider(workspace);
        }
    }
}
