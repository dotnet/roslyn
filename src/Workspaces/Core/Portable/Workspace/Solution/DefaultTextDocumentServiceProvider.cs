// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// <see cref="IDocumentServiceProvider"/> for regular C#/VB files.
    /// </summary>
    internal sealed class DefaultTextDocumentServiceProvider : IDocumentServiceProvider
    {
        public static readonly DefaultTextDocumentServiceProvider Instance = new DefaultTextDocumentServiceProvider();

        private DefaultTextDocumentServiceProvider() { }

        public TService GetService<TService>() where TService : class, IDocumentService
        {
            // right now, it doesn't implement much services but we expect it to implements all 
            // document services in future so that we can remove all if branches in feature code
            // but just delegate work to default document services.
            if (DocumentOperationService.Instance is TService service)
            {
                return service;
            }

            return default;
        }

        private class DocumentOperationService : IDocumentOperationService
        {
            public static readonly DocumentOperationService Instance = new DocumentOperationService();

            // right now, we return CanApplyChange for all C# documents, but we probably want to return
            // false for generated files such as resx files or winform designer files.
            // right now, we have a bug where if user renames Resource.[ResourceName] we actually do the rename
            // but not actually change resx files which in turn, break code since generated file go back to 
            // original next time someone changes resx files but reference left as renamed.
            // with this, we now should be able to say no text changes for such files so that rename fails
            // in those cases. if resx people adapt IDocumentService pattern, then they should be able to
            // even support rename through IDynamicFileInfoProvider pattern once we address that in next
            // iteration for razor. for now, we keep existing behavior
            public bool CanApplyChange => true;
            public bool SupportDiagnostics => true;
        }
    }
}
