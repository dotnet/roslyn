// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
{
    internal abstract partial class AbstractSignatureHelpProvider
    {
        internal class SymbolKeySignatureHelpItem : SignatureHelpItem, IEquatable<SymbolKeySignatureHelpItem>
        {
            public SymbolKey SymbolKey { get; }

            public SymbolKeySignatureHelpItem(
                ISymbol symbol,
                bool isVariadic,
                Func<CancellationToken, IEnumerable<SymbolDisplayPart>> documentationFactory,
                IEnumerable<SymbolDisplayPart> prefixParts,
                IEnumerable<SymbolDisplayPart> separatorParts,
                IEnumerable<SymbolDisplayPart> suffixParts,
                IEnumerable<SignatureHelpParameter> parameters,
                IEnumerable<SymbolDisplayPart> descriptionParts) : base(isVariadic, documentationFactory, prefixParts, separatorParts, suffixParts, parameters, descriptionParts)
            {
                this.SymbolKey = symbol == null ? null : symbol.GetSymbolKey();
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as SymbolKeySignatureHelpItem);
            }

            public bool Equals(SymbolKeySignatureHelpItem obj)
            {
                return ReferenceEquals(this, obj) ||
                    (obj != null &&
                    this.SymbolKey != null &&
                    SymbolKey.GetComparer(ignoreCase: false, ignoreAssemblyKeys: false).Equals(this.SymbolKey, obj.SymbolKey));
            }

            public override int GetHashCode()
            {
                return this.SymbolKey == null ? 0 : this.SymbolKey.GetHashCode(
                    new SymbolKey.ComparisonOptions(ignoreCase: false, ignoreAssemblyKeys: false, compareMethodTypeParametersByName: false));
            }
        }
    }
}
