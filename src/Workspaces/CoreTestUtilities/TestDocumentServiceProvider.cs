﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

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

            SpanMappingService = new TestSpanMappingService();
        }

        public IDocumentOperationService DocumentOperationService { get; }

        public ISpanMappingService SpanMappingService { get; }

        public TService GetService<TService>() where TService : class, IDocumentService
        {
            if (DocumentOperationService is TService service)
            {
                return service;
            }
            else if (SpanMappingService is TService spanMappingService)
            {
                return spanMappingService;
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

        private class TestSpanMappingService : ISpanMappingService
        {
            public Task<ImmutableArray<MappedSpanResult>> MapSpansAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
            {
                return Task.FromResult(ImmutableArray<MappedSpanResult>.Empty);
            }
        }
    }
}
