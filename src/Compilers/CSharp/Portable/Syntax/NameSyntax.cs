// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public abstract partial class NameSyntax
    {
        public int Arity
        {
            get
            {
                return this is GenericNameSyntax ? ((GenericNameSyntax)this).TypeArgumentList.Arguments.Count : 0;
            }
        }

        /// <summary>
        /// Returns the unqualified (right-most) part of a qualified or alias-qualified name, or the name itself if already unqualified. 
        /// </summary>
        /// <returns>The unqualified (right-most) part of a qualified or alias-qualified name, or the name itself if already unqualified.
        /// If called on an instance of <see cref="AliasQualifiedNameSyntax"/> returns the value of the <see cref="AliasQualifiedNameSyntax.Name"/> property.
        /// If called on an instance of <see cref="QualifiedNameSyntax"/> returns the value of the <see cref="QualifiedNameSyntax.Right"/> property.
        /// If called on an instance of <see cref="SimpleNameSyntax"/> returns the instance itself.
        /// </returns>
        internal abstract SimpleNameSyntax GetUnqualifiedName();

        /// <summary>
        /// Return the name in string form, without trivia or generic arguments, for use in diagnostics.
        /// </summary>
        internal abstract string ErrorDisplayName();

        /// <remarks>
        /// This inspection is entirely syntactic.  We are not trying to find the alias corresponding to the assembly symbol
        /// containing the explicitly implemented interface symbol - there may be more than one.  We just want to know
        /// how the name was qualified in source so that we can make a similar qualification (for uniqueness purposes).
        /// </remarks>
        internal string GetAliasQualifierOpt()
        {
            NameSyntax name = this;
            while (true)
            {
                switch (name.Kind())
                {
                    case SyntaxKind.QualifiedName:
                        name = ((QualifiedNameSyntax)name).Left;
                        continue;
                    case SyntaxKind.AliasQualifiedName:
                        return ((AliasQualifiedNameSyntax)name).Alias.Identifier.ValueText;
                }

                return null;
            }
        }
    }
}
