// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal class TestDocumentServiceProvider : IDocumentServiceProvider
    {
        public TestDocumentServiceProvider(bool canApplyChange = true, bool supportDiagnostics = true)
        {
            DocumentOperationService = new TestDocumentOperationService()
            {
                CanApplyChange = canApplyChange,
                SupportDiagnostics = supportDiagnostics
            };
        }

        public IDocumentOperationService DocumentOperationService { get; }

        public TService GetService<TService>() where TService : class, IDocumentService
        {
            if (DocumentOperationService is TService service)
            {
                return service;
            }

            return null;
        }

        private class TestDocumentOperationService : IDocumentOperationService
        {
            public TestDocumentOperationService()
            {
            }

            public bool CanApplyChange { get; set; }
            public bool SupportDiagnostics { get; set; }
        }
    }
}
