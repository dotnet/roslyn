// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            return default;
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
