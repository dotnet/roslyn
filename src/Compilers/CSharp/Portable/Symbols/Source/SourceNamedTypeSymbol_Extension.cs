// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class SourceNamedTypeSymbol
    {
        private ExtensionInfo _lazyExtensionInfo;

        private class ExtensionInfo
        {
            public StrongBox<ParameterSymbol?>? LazyExtensionParameter;
            public ImmutableDictionary<MethodSymbol, MethodSymbol>? LazyImplementationMap;
        }

        internal override string ExtensionName
        {
            get
            {
                if (!IsExtension)
                {
                    throw ExceptionUtilities.Unreachable();
                }

                MergedNamespaceOrTypeDeclaration declaration;
                if (ContainingType is not null)
                {
                    declaration = ((SourceNamedTypeSymbol)this.ContainingType).declaration;
                }
                else
                {
                    declaration = ((SourceNamespaceSymbol)this.ContainingSymbol).MergedDeclaration;
                }

                var index = declaration.Children.IndexOf(this.declaration);
                return GeneratedNames.MakeExtensionName(index);
            }
        }

        internal sealed override ParameterSymbol? ExtensionParameter
        {
            get
            {
                if (!IsExtension)
                {
                    return null;
                }

                if (_lazyExtensionInfo is null)
                {
                    Interlocked.CompareExchange(ref _lazyExtensionInfo, new ExtensionInfo(), null);
                }

                if (_lazyExtensionInfo.LazyExtensionParameter == null)
                {
                    var extensionParameter = makeExtensionParameter(this);
                    Interlocked.CompareExchange(ref _lazyExtensionInfo.LazyExtensionParameter, new StrongBox<ParameterSymbol?>(extensionParameter), null);
                }

                return _lazyExtensionInfo.LazyExtensionParameter.Value;

                static ParameterSymbol? makeExtensionParameter(SourceNamedTypeSymbol symbol)
                {
                    var markerMethod = symbol.GetMembers(WellKnownMemberNames.ExtensionMarkerMethodName).OfType<SynthesizedExtensionMarker>().SingleOrDefault();

                    if (markerMethod is not { Parameters: [var parameter, ..] })
                    {
                        return null;
                    }

                    return new ReceiverParameterSymbol(symbol, parameter);
                }
            }
        }

        public sealed override MethodSymbol? TryGetCorrespondingExtensionImplementationMethod(MethodSymbol method)
        {
            Debug.Assert(this.IsExtension);
            Debug.Assert(method.IsDefinition);
            Debug.Assert(method.ContainingType == (object)this);

            var containingType = this.ContainingType;

            if (containingType is null)
            {
                return null; // PROTOTYPE: Test this code path
            }

            if (_lazyExtensionInfo is null)
            {
                Interlocked.CompareExchange(ref _lazyExtensionInfo, new ExtensionInfo(), null); // PROTOTYPE: Test this code path
            }

            if (_lazyExtensionInfo.LazyImplementationMap is null)
            {
                var builder = ImmutableDictionary.CreateBuilder<MethodSymbol, MethodSymbol>(Roslyn.Utilities.ReferenceEqualityComparer.Instance);

                builder.AddRange(
                    containingType.GetMembersUnordered().OfType<SourceExtensionImplementationMethodSymbol>().
                    Select(static m => new KeyValuePair<MethodSymbol, MethodSymbol>(m.UnderlyingMethod, m)));

                Interlocked.CompareExchange(ref _lazyExtensionInfo.LazyImplementationMap, builder.ToImmutable(), null);
            }

            return _lazyExtensionInfo.LazyImplementationMap.GetValueOrDefault(method);
        }
    }
}
