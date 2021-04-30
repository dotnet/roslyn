// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// This <see cref="ArgumentProvider"/> attempts to locate a matching value in the context of a method invocation.
    /// </summary>
    internal abstract class AbstractContextVariableArgumentProvider : ArgumentProvider
    {
        public override Task ProvideArgumentAsync(ArgumentContext context)
        {
            if (context.PreviousValue is not null)
            {
                // This argument provider does not attempt to replace arguments already in code.
                return Task.CompletedTask;
            }

            var requireExactType = context.Parameter.Type.IsSpecialType()
                || context.Parameter.RefKind != RefKind.None;
            var symbols = context.SemanticModel.LookupSymbols(context.Position);

            // First try to find a local variable
            ISymbol? bestSymbol = null;
            CommonConversion bestConversion = default;
            foreach (var symbol in symbols)
            {
                ISymbol candidate;
                if (symbol.IsKind(SymbolKind.Parameter, out IParameterSymbol? parameter))
                    candidate = parameter;
                else if (symbol.IsKind(SymbolKind.Local, out ILocalSymbol? local))
                    candidate = local;
                else
                    continue;

                CheckCandidate(candidate);
            }

            if (bestSymbol is not null)
            {
                context.DefaultValue = bestSymbol.Name;
                return Task.CompletedTask;
            }

            foreach (var symbol in symbols)
            {
                ISymbol candidate;
                if (symbol.IsKind(SymbolKind.Field, out IFieldSymbol? field))
                    candidate = field;
                else if (symbol.IsKind(SymbolKind.Property, out IPropertySymbol? property))
                    candidate = property;
                else
                    continue;

                // Require a name match for primitive types
                if (candidate.GetSymbolType().IsSpecialType()
                    && !string.Equals(candidate.Name, context.Parameter.Name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                CheckCandidate(candidate);
            }

            if (bestSymbol is not null)
            {
                context.DefaultValue = bestSymbol.Name;
                return Task.CompletedTask;
            }

            return Task.CompletedTask;

            // Local functions
            void CheckCandidate(ISymbol candidate)
            {
                if (candidate.GetSymbolType() is not { } symbolType)
                {
                    return;
                }

                if (requireExactType && !SymbolEqualityComparer.Default.Equals(context.Parameter.Type, symbolType))
                {
                    return;
                }

                var conversion = context.SemanticModel.Compilation.ClassifyCommonConversion(symbolType, context.Parameter.Type);
                if (!conversion.IsImplicit)
                {
                    return;
                }

                if (bestSymbol is not null && !IsNewConversionSameOrBetter(conversion))
                {
                    if (!IsNewConversionSameOrBetter(conversion))
                        return;

                    if (!IsNewNameSameOrBetter(candidate))
                        return;
                }

                bestSymbol = candidate;
                bestConversion = conversion;
            }

            bool IsNewConversionSameOrBetter(CommonConversion conversion)
            {
                if (bestConversion.IsIdentity && !conversion.IsIdentity)
                    return false;

                if (bestConversion.IsImplicit && !conversion.IsImplicit)
                    return false;

                return true;
            }

            bool IsNewNameSameOrBetter(ISymbol symbol)
            {
                if (string.Equals(bestSymbol.Name, context.Parameter.Name))
                    return string.Equals(symbol.Name, context.Parameter.Name);

                if (string.Equals(bestSymbol.Name, context.Parameter.Name, StringComparison.OrdinalIgnoreCase))
                    return string.Equals(symbol.Name, context.Parameter.Name, StringComparison.OrdinalIgnoreCase);

                return true;
            }
        }
    }
}
