// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private sealed class EventSymbolKey : AbstractSymbolKey<IEventSymbol>
        {
            public static readonly EventSymbolKey Instance = new();

            public sealed override void Create(IEventSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteString(symbol.MetadataName);
                visitor.WriteSymbolKey(symbol.ContainingType);
                visitor.WriteSymbolKey(symbol.Type);
            }

            protected sealed override SymbolKeyResolution Resolve(
                SymbolKeyReader reader, IEventSymbol? contextualSymbol, out string? failureReason)
            {
                var metadataName = reader.ReadString();
                var containingTypeResolution = reader.ReadSymbolKey(contextualSymbol?.ContainingType, out var containingTypeFailureReason);

                using var result = GetMembersOfNamedType<IEventSymbol>(containingTypeResolution, metadataName);

                var beforeReturnType = reader.Position;

                // We don't actually ever expect more than one result here, but enumeration is cheap so may as well
                // be future proof
                IEventSymbol? @event = null;
                foreach (var candidate in result)
                {
                    var returnType = (ITypeSymbol?)reader.ReadSymbolKey(contextualSymbol: candidate.Type, out _).GetAnySymbol();
                    if (reader.IgnoreReturnTypes || reader.Comparer.Equals(returnType, candidate.Type))
                    {
                        @event = candidate;
                        break;
                    }

                    // reset ourselves so we can check the return-type against the next candidate.
                    reader.Position = beforeReturnType;
                }

                if (reader.Position == beforeReturnType)
                {
                    // We didn't find a match.  Read through the stream one final time so we're at the correct location
                    // after this EventSymbolKey.

                    // Read the return type.
                    _ = reader.ReadSymbolKey(contextualSymbol: null, out _);
                }

                if (containingTypeFailureReason != null)
                {
                    failureReason = $"({nameof(EventSymbolKey)} {nameof(containingTypeResolution)} failed -> {containingTypeFailureReason})";
                    return default;
                }

                if (@event == null)
                {
                    failureReason = $"({nameof(EventSymbolKey)} '{metadataName}' not found)";
                    return default;
                }

                failureReason = null;
                return new SymbolKeyResolution(@event);
            }
        }
    }
}
