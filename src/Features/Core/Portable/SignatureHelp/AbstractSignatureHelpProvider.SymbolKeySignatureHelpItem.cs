// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.SignatureHelp;

internal abstract partial class AbstractSignatureHelpProvider
{
    internal class SymbolKeySignatureHelpItem(
        ISymbol symbol,
        bool isVariadic,
        Func<CancellationToken, IEnumerable<TaggedText>>? documentationFactory,
        IEnumerable<TaggedText> prefixParts,
        IEnumerable<TaggedText> separatorParts,
        IEnumerable<TaggedText> suffixParts,
        IEnumerable<SignatureHelpParameter> parameters,
        IEnumerable<TaggedText>? descriptionParts) : SignatureHelpItem(isVariadic, documentationFactory, prefixParts, separatorParts, suffixParts, parameters, descriptionParts), IEquatable<SymbolKeySignatureHelpItem>
    {
        public SymbolKey? SymbolKey { get; } = symbol?.GetSymbolKey();

        public override bool Equals(object? obj)
            => Equals(obj as SymbolKeySignatureHelpItem);

        public bool Equals(SymbolKeySignatureHelpItem? obj)
        {
            return ReferenceEquals(this, obj) ||
                (obj?.SymbolKey != null &&
                 SymbolKey != null &&
                 CodeAnalysis.SymbolKey.GetComparer(ignoreCase: false, ignoreAssemblyKeys: false).Equals(SymbolKey.Value, obj.SymbolKey.Value));
        }

        public override int GetHashCode()
        {
            if (SymbolKey == null)
            {
                return 0;
            }

            var comparer = CodeAnalysis.SymbolKey.GetComparer(ignoreCase: false, ignoreAssemblyKeys: false);
            return comparer.GetHashCode(SymbolKey.Value);
        }
    }
}
