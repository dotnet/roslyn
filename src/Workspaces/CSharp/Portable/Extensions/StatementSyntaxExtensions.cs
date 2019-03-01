// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class StatementSyntaxExtensions
    {
        public static bool IsFirstStatementInEnclosingBlock(this StatementSyntax statement)
        {
            var enclosingBlock = statement.Ancestors().OfType<BlockSyntax>().FirstOrDefault();
            if (enclosingBlock == null)
            {
                return false;
            }

            return statement == enclosingBlock.Statements.First();
        }

        public static bool IsFirstStatementInSwitchSection(this StatementSyntax statement)
        {
            var enclosingSwitchSection = statement.Ancestors().OfType<SwitchSectionSyntax>().FirstOrDefault();
            if (enclosingSwitchSection == null)
            {
                return false;
            }

            return statement == enclosingSwitchSection.Statements.First();
        }

        public static StatementSyntax GetPreviousStatement(this StatementSyntax statement)
        {
            if (statement != null)
            {
                var previousToken = statement.GetFirstToken().GetPreviousToken();
                return previousToken.GetAncestors<StatementSyntax>().FirstOrDefault(s => s.Parent == statement.Parent);
            }

            return null;
        }

        public static StatementSyntax GetNextStatement(this StatementSyntax statement)
        {
            if (statement != null)
            {
                var nextToken = statement.GetLastToken().GetNextToken();
                return nextToken.GetAncestors<StatementSyntax>().FirstOrDefault(s => s.Parent == statement.Parent);
            }

            return null;
        }
    }
}
