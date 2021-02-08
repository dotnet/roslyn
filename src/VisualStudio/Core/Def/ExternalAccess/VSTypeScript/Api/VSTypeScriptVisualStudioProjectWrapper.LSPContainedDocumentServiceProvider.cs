// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.VSTypeScript.Api
{
    internal sealed partial class VSTypeScriptVisualStudioProjectWrapper
    {
        private sealed class LspContainedDocumentServiceProvider : IDocumentServiceProvider, IDocumentOperationService
        {
            private readonly VirtualDocumentPropertiesService _documentPropertiesService;

            private LspContainedDocumentServiceProvider()
            {
                _documentPropertiesService = VirtualDocumentPropertiesService.Instance;
            }

            public static LspContainedDocumentServiceProvider Instance = new LspContainedDocumentServiceProvider();

            bool IDocumentOperationService.CanApplyChange => true;

            bool IDocumentOperationService.SupportDiagnostics => true;

            TService? IDocumentServiceProvider.GetService<TService>() where TService : class
            {
                if (typeof(TService) == typeof(DocumentPropertiesService))
                {
                    return (TService)(object)_documentPropertiesService;
                }

                return this as TService;
            }

            private sealed class VirtualDocumentPropertiesService : DocumentPropertiesService
            {
                private const string _lspClientName = "TypeScript";

                private VirtualDocumentPropertiesService() { }

                public static VirtualDocumentPropertiesService Instance = new VirtualDocumentPropertiesService();

                public override string? DiagnosticsLspClientName => _lspClientName;
            }
        }
    }
}
