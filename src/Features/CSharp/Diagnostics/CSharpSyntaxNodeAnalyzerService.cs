// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics.EngineV1;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics
{
    [ExportLanguageService(typeof(ISyntaxNodeAnalyzerService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpSyntaxNodeAnalyzerService : AbstractSyntaxNodeAnalyzerService<SyntaxKind>
    {
        public CSharpSyntaxNodeAnalyzerService() { }

        protected override IEqualityComparer<SyntaxKind> GetSyntaxKindEqualityComparer()
        {
            return SyntaxFacts.EqualityComparer;
        }

        protected override SyntaxKind GetKind(SyntaxNode node)
        {
            return node.Kind();
        }
    }
}
