// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This binder keeps track of the type for which we are trying to determine
    /// whether the params collection type has an applicable indexer.
    /// </summary>
    internal sealed class ParamsCollectionTypeApplicableIndexerInProgress : Binder
    {
        internal readonly SyntaxNode Syntax;
        internal readonly TypeSymbol CollectionType;

        internal ParamsCollectionTypeApplicableIndexerInProgress(SyntaxNode syntax, TypeSymbol collectionType, Binder next) :
            base(next)
        {
            Syntax = syntax;
            CollectionType = collectionType;
        }
    }
}
