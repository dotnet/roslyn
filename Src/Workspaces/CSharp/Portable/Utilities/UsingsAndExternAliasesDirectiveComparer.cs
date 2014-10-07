// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
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

        private readonly IComparer<NameSyntax> nameComparer;
        private readonly IComparer<SyntaxToken> tokenComparer;

        private UsingsAndExternAliasesDirectiveComparer(
            IComparer<NameSyntax> nameComparer,
            IComparer<SyntaxToken> tokenComparer)
        {
            Contract.Requires(nameComparer != null);
            Contract.Requires(tokenComparer != null);
            this.nameComparer = nameComparer;
            this.tokenComparer = tokenComparer;
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

            var directive1IsNamespace = using1 != null && using1.Alias == null;
            var directive2IsNamespace = using2 != null && using2.Alias == null;

            var directive1IsAlias = using1 != null && using1.Alias != null;
            var directive2IsAlias = using2 != null && using2.Alias != null;

            // different types of usings get broken up into groups.
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
                return tokenComparer.Compare(extern1.Identifier, extern2.Identifier);
            }
            else if (directive1IsAlias)
            {
                var aliasComparisonResult = tokenComparer.Compare(using1.Alias.Name.Identifier, using2.Alias.Name.Identifier);

                if (aliasComparisonResult == 0)
                {
                    // They both use the same alias, so compare the names.
                    return nameComparer.Compare(using1.Name, using2.Name);
                }
                else
                {
                    return aliasComparisonResult;
                }
            }
            else
            {
                return nameComparer.Compare(using1.Name, using2.Name);
            }
        }
    }
}