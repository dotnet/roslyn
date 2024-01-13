// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class UsingDirectiveSyntax
    {
        /// <summary>
        /// Returns the name this <see cref="UsingDirectiveSyntax"/> points at, or <see langword="null"/> if it does not
        /// point at a name.  A normal <c>using X.Y.Z;</c> or <c>using static X.Y.Z;</c> will always point at a name and
        /// will always return a value for this.  However, a using-alias (e.g. <c>using x = ...;</c>) may or may not
        /// point at a name and may return <see langword="null"/> here.  An example of when that may happen is the type
        /// on the right side of the <c>=</c> is not a name.  For example <c>using x = (X.Y.Z, A.B.C);</c>.  Here, as
        /// the type is a tuple-type there is no name to return.
        /// </summary>
        public NameSyntax? Name => this.NamespaceOrType as NameSyntax;

        public UsingDirectiveSyntax Update(SyntaxToken usingKeyword, SyntaxToken staticKeyword, NameEqualsSyntax? alias, NameSyntax name, SyntaxToken semicolonToken)
            => this.Update(this.GlobalKeyword, usingKeyword, staticKeyword, this.UnsafeKeyword, alias, namespaceOrType: name, semicolonToken);

        public UsingDirectiveSyntax Update(SyntaxToken globalKeyword, SyntaxToken usingKeyword, SyntaxToken staticKeyword, NameEqualsSyntax? alias, NameSyntax name, SyntaxToken semicolonToken)
            => this.Update(globalKeyword, usingKeyword, staticKeyword, this.UnsafeKeyword, alias, namespaceOrType: name, semicolonToken);

        public UsingDirectiveSyntax WithName(NameSyntax name)
            => WithNamespaceOrType(name);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        /// <summary>Creates a new UsingDirectiveSyntax instance.</summary>
        public static UsingDirectiveSyntax UsingDirective(SyntaxToken staticKeyword, NameEqualsSyntax? alias, NameSyntax name)
            => UsingDirective(globalKeyword: default, usingKeyword: Token(SyntaxKind.UsingKeyword), staticKeyword, unsafeKeyword: default, alias, namespaceOrType: name, semicolonToken: Token(SyntaxKind.SemicolonToken));

        /// <summary>Creates a new UsingDirectiveSyntax instance.</summary>
        public static UsingDirectiveSyntax UsingDirective(SyntaxToken globalKeyword, SyntaxToken usingKeyword, SyntaxToken staticKeyword, NameEqualsSyntax? alias, NameSyntax name, SyntaxToken semicolonToken)
            => UsingDirective(globalKeyword, usingKeyword, staticKeyword, unsafeKeyword: default, alias, namespaceOrType: name, semicolonToken);

        /// <summary>Creates a new UsingDirectiveSyntax instance.</summary>
        public static UsingDirectiveSyntax UsingDirective(NameSyntax name)
            => UsingDirective(namespaceOrType: name);
    }
}
