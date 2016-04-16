// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Utilities
{
    internal class UsingsAndExternAliasesDirectiveComparer : IComparer<SyntaxNode>
    {
        public static readonly IComparer<SyntaxNode> NormalInstance = new UsingsAndExternAliasesDirectiveComparer(
            NameSyntaxComparer.Create(TokenComparer.NormalInstance),
            TokenComparer.NormalInstance);

        public static readonly IComparer<SyntaxNode> SystemFirstInstance = new UsingsAndExternAliasesDirectiveComparer(
            NameSyntaxComparer.Create(TokenComparer.SystemFirstInstance),
            TokenComparer.SystemFirstInstance);

        private readonly IComparer<NameSyntax> _nameComparer;
        private readonly IComparer<SyntaxToken> _tokenComparer;

        private UsingsAndExternAliasesDirectiveComparer(
            IComparer<NameSyntax> nameComparer,
            IComparer<SyntaxToken> tokenComparer)
        {
            Contract.Requires(nameComparer != null);
            Contract.Requires(tokenComparer != null);
            _nameComparer = nameComparer;
            _tokenComparer = tokenComparer;
        }

        public int Compare(SyntaxNode directive1, SyntaxNode directive2)
        {
            if (directive1 == directive2)
            {
                return 0;
            }

            var using1 = directive1 as UsingDirectiveSyntax;
            var using2 = directive2 as UsingDirectiveSyntax;
            var extern1 = directive1 as ExternAliasDirectiveSyntax;
            var extern2 = directive2 as ExternAliasDirectiveSyntax;

            var directive1IsExtern = extern1 != null;
            var directive2IsExtern = extern2 != null;

            var directive1IsNamespace = using1 != null && using1.Alias == null && !using1.StaticKeyword.IsKind(SyntaxKind.StaticKeyword);
            var directive2IsNamespace = using2 != null && using2.Alias == null && !using2.StaticKeyword.IsKind(SyntaxKind.StaticKeyword);

            var directive1IsUsingStatic = using1 != null && using1.StaticKeyword.IsKind(SyntaxKind.StaticKeyword);
            var directive2IsUsingStatic = using2 != null && using2.StaticKeyword.IsKind(SyntaxKind.StaticKeyword);

            var directive1IsAlias = using1 != null && using1.Alias != null;
            var directive2IsAlias = using2 != null && using2.Alias != null;

            // different types of usings get broken up into groups.
            //  * externs
            //  * usings
            //  * using statics
            //  * aliases

            if (directive1IsExtern && !directive2IsExtern)
            {
                return -1;
            }
            else if (directive2IsExtern && !directive1IsExtern)
            {
                return 1;
            }
            else if (directive1IsNamespace && !directive2IsNamespace)
            {
                return -1;
            }
            else if (directive2IsNamespace && !directive1IsNamespace)
            {
                return 1;
            }
            else if (directive1IsUsingStatic && !directive2IsUsingStatic)
            {
                return -1;
            }
            else if (directive2IsUsingStatic && !directive1IsUsingStatic)
            {
                return 1;
            }
            else if (directive1IsAlias && !directive2IsAlias)
            {
                return -1;
            }
            else if (directive2IsAlias && !directive1IsAlias)
            {
                return 1;
            }

            // ok, it's the same type of using now.
            if (directive1IsExtern)
            {
                // they're externs, sort by the alias
                return _tokenComparer.Compare(extern1.Identifier, extern2.Identifier);
            }
            else if (directive1IsAlias)
            {
                var aliasComparisonResult = _tokenComparer.Compare(using1.Alias.Name.Identifier, using2.Alias.Name.Identifier);

                if (aliasComparisonResult == 0)
                {
                    // They both use the same alias, so compare the names.
                    return _nameComparer.Compare(using1.Name, using2.Name);
                }
                else
                {
                    return aliasComparisonResult;
                }
            }
            else
            {
                return _nameComparer.Compare(using1.Name, using2.Name);
            }
        }
    }
}
