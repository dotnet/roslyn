// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private sealed class EventSymbolKey : AbstractSymbolKey<IEventSymbol>
    {
        public static readonly EventSymbolKey Instance = new();

        public sealed override void Create(IEventSymbol symbol, SymbolKeyWriter visitor)
        {
            visitor.WriteString(symbol.MetadataName);
            visitor.WriteSymbolKey(symbol.ContainingType);
            visitor.WriteBoolean(symbol.PartialDefinitionPart is not null);
        }

        protected sealed override SymbolKeyResolution Resolve(
            SymbolKeyReader reader, IEventSymbol? contextualSymbol, out string? failureReason)
        {
            var metadataName = reader.ReadString();
            var containingTypeResolution = reader.ReadSymbolKey(contextualSymbol?.ContainingType, out var containingTypeFailureReason);
            var isPartialImplementationPart = reader.ReadBoolean();

            if (containingTypeFailureReason != null)
            {
                failureReason = $"({nameof(EventSymbolKey)} {nameof(containingTypeResolution)} failed -> {containingTypeFailureReason})";
                return default;
            }

            using var events = GetMembersOfNamedType<IEventSymbol>(containingTypeResolution, metadataName);

            if (isPartialImplementationPart)
            {
                for (var i = 0; i < events.Builder.Count; i++)
                {
                    var candidate = events.Builder[i];
                    events.Builder[i] = candidate.PartialImplementationPart ?? candidate;
                }
            }

            return CreateResolution(events, $"({nameof(EventSymbolKey)} '{metadataName}' not found)", out failureReason);
        }
    }
}
