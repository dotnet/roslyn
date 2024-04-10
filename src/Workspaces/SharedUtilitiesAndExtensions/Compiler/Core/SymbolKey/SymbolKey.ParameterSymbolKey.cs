// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private sealed class ParameterSymbolKey : AbstractSymbolKey<IParameterSymbol>
    {
        public static readonly ParameterSymbolKey Instance = new();

        public sealed override void Create(IParameterSymbol symbol, SymbolKeyWriter visitor)
        {
            visitor.WriteString(symbol.MetadataName);
            visitor.WriteInteger(symbol.Ordinal);
            visitor.WriteSymbolKey(symbol.ContainingSymbol);
        }

        protected sealed override SymbolKeyResolution Resolve(
            SymbolKeyReader reader, IParameterSymbol? contextualSymbol, out string? failureReason)
        {
            var metadataName = reader.ReadRequiredString();
            var ordinal = reader.ReadInteger();

            // Parameters are owned by members, and members are never resolved in a way where we have contextual
            // types to guide how the outer parts of the member may resolve.  We can use contextual typing for the
            // *signature* portion of the member though.
            var containingSymbolResolution = reader.ReadSymbolKey(
                contextualSymbol?.ContainingSymbol, out var containingSymbolFailureReason);

            if (containingSymbolFailureReason != null)
            {
                failureReason = $"({nameof(ParameterSymbolKey)} {nameof(containingSymbolResolution)} failed -> {containingSymbolFailureReason})";
                return default;
            }

            using var result = PooledArrayBuilder<IParameterSymbol>.GetInstance();
            foreach (var container in containingSymbolResolution)
            {
                switch (container)
                {
                    case IMethodSymbol method:
                        Resolve(result, reader, metadataName, ordinal, method.Parameters);
                        break;
                    case IPropertySymbol property:
                        Resolve(result, reader, metadataName, ordinal, property.Parameters);
                        break;
                    case IEventSymbol eventSymbol:
                        // Parameters can be owned by events in VB.  i.e. it's legal in VB to have:
                        //
                        //      Public Event E(a As Integer, b As Integer);
                        //
                        // In this case it's equivalent to:
                        //
                        //      Public Delegate UnutterableCompilerName(a As Integer, b As Integer)
                        //      public Event E As UnutterableCompilerName
                        //
                        // So, in this case, to resolve the parameter, we go have to map the event,
                        // then find the delegate it returns, then find the parameter in the delegate's
                        // 'Invoke' method.
                        var delegateInvoke = (eventSymbol.Type as INamedTypeSymbol)?.DelegateInvokeMethod;

                        if (delegateInvoke != null)
                        {
                            Resolve(result, reader, metadataName, ordinal, delegateInvoke.Parameters);
                        }

                        break;
                }
            }

            return CreateResolution(result, $"({nameof(ParameterSymbolKey)} '{metadataName}' not found)", out failureReason);
        }

        private static void Resolve(
            PooledArrayBuilder<IParameterSymbol> result, SymbolKeyReader reader,
            string metadataName, int ordinal, ImmutableArray<IParameterSymbol> parameters)
        {
            // Try to resolve to a parameter with matching name first:
            var hasMatchingName = false;
            foreach (var parameter in parameters)
            {
                if (SymbolKey.Equals(reader.Compilation, parameter.MetadataName, metadataName))
                {
                    result.AddIfNotNull(parameter);
                    hasMatchingName = true;
                }
            }

            // The signatures of the containing member matches, so we can fall back to using parameter ordinal.
            // The fallback handles the case when the parameter has been renamed.
            if (!hasMatchingName)
            {
                result.AddIfNotNull(parameters[ordinal]);
            }
        }
    }
}
