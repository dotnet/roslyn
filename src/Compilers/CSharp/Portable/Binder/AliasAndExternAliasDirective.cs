// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal readonly struct AliasAndExternAliasDirective
    {
        public readonly AliasSymbol Alias;
        public readonly SyntaxReference? ExternAliasDirectiveReference;
        public readonly bool SkipInLookup;

        public AliasAndExternAliasDirective(AliasSymbol alias, ExternAliasDirectiveSyntax? externAliasDirective, bool skipInLookup)
        {
            this.Alias = alias;
            this.ExternAliasDirectiveReference = externAliasDirective?.GetReference();
            this.SkipInLookup = skipInLookup;
        }

        public ExternAliasDirectiveSyntax? ExternAliasDirective => (ExternAliasDirectiveSyntax?)ExternAliasDirectiveReference?.GetSyntax();
    }
}
