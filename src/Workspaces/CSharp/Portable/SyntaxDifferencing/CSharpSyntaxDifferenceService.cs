// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SyntaxDifferencing;

namespace Microsoft.CodeAnalysis.CSharp.SyntaxDifferencing
{
    [ExportLanguageService(typeof(SyntaxDifferenceService), LanguageNames.CSharp), Shared]
    internal class CSharpSyntaxDifferenceService : SyntaxDifferenceService
    {
        public override string Language => LanguageNames.CSharp;

        internal override SyntaxMatch ComputeBodyLevelMatch(SyntaxNode oldBody, SyntaxNode newBody)
        {
            return new SyntaxMatch(StatementSyntaxComparer.Instance.ComputeMatch(oldBody, newBody));
        }

        public override SyntaxMatch ComputeTopLevelMatch(SyntaxNode oldRoot, SyntaxNode newRoot)
        {
            return new SyntaxMatch(TopSyntaxComparer.Instance.ComputeMatch(oldRoot, newRoot));
        }
    }
}
