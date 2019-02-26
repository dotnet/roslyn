// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal abstract partial class AbstractSignatureHelpProvider
    {
        internal class SymbolKeySignatureHelpItem : SignatureHelpItem, IEquatable<SymbolKeySignatureHelpItem>
        {
            public SymbolKey? SymbolKey { get; }

            public SymbolKeySignatureHelpItem(
                ISymbol symbol,
                bool isVariadic,
                Func<CancellationToken, IEnumerable<TaggedText>> documentationFactory,
                IEnumerable<TaggedText> prefixParts,
                IEnumerable<TaggedText> separatorParts,
                IEnumerable<TaggedText> suffixParts,
                IEnumerable<SignatureHelpParameter> parameters,
                IEnumerable<TaggedText> descriptionParts) :
                base(isVariadic, documentationFactory, prefixParts, separatorParts, suffixParts, parameters, descriptionParts)
            {
                this.SymbolKey = symbol?.GetSymbolKey();
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as SymbolKeySignatureHelpItem);
            }

            public bool Equals(SymbolKeySignatureHelpItem obj)
            {
                return ReferenceEquals(this, obj) ||
                    (obj?.SymbolKey != null &&
                     this.SymbolKey != null &&
                     CodeAnalysis.SymbolKey.GetComparer(ignoreCase: false, ignoreAssemblyKeys: false).Equals(this.SymbolKey.Value, obj.SymbolKey.Value));
            }

            public override int GetHashCode()
            {
                if (this.SymbolKey == null)
                {
                    return 0;
                }

                var comparer = CodeAnalysis.SymbolKey.GetComparer(ignoreCase: false, ignoreAssemblyKeys: false);
                return comparer.GetHashCode(this.SymbolKey.Value);
            }
        }
    }
}
