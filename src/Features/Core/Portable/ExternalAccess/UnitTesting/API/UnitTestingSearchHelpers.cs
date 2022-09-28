// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal sealed class UnitTestingSearchQuery
    {
        public readonly string FullyQualifiedTypeName;
        public readonly string? MethodName;
        public readonly int MethodArity;
        public readonly int MethodParameterCount;

        public UnitTestingSearchQuery(string fullyQualifiedTypeName, string? methodName, int methodArity, int methodParameterCount)
        {
            FullyQualifiedTypeName = fullyQualifiedTypeName;
            MethodName = methodName;
            MethodArity = methodArity;
            MethodParameterCount = methodParameterCount;
        }
    }

    internal readonly record struct UnitTestingNavigationOptions(
        bool PreferProvisionalTab = false,
        bool ActivateTab = true)
    {
        public UnitTestingNavigationOptions()
            : this(PreferProvisionalTab: false)
        {
        }
    }

    internal readonly struct UnitTestingDocumentSpan
    {
        private readonly DocumentSpan _documentSpan;

        internal UnitTestingDocumentSpan(DocumentSpan documentSpan, FileLinePositionSpan span)
        {
            _documentSpan = documentSpan;
            Span = span;
        }

        public FileLinePositionSpan Span { get; }

        public async Task NavigateToAsync(UnitTestingNavigationOptions options, CancellationToken cancellationToken)
        {
            var location = await _documentSpan.GetNavigableLocationAsync(cancellationToken).ConfigureAwait(false);
            if (location != null)
                await location.NavigateToAsync(new NavigationOptions(options.PreferProvisionalTab, options.ActivateTab), cancellationToken).ConfigureAwait(false);
        }

        internal static class UnitTestingSearchHelpers
        {
            public static async Task<ImmutableArray<UnitTestingDocumentSpan>> GetSourceLocations(
                Solution solution, UnitTestingSearchQuery query, CancellationToken cancellationToken)
            {

            }
        }
    }
}
