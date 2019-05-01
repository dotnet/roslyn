// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.LanguageServices
{
    [ExportLanguageService(typeof(ISyntaxKindsService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpSyntaxKindsService : ISyntaxKindsService
    {
        [ImportingConstructor]
        public CSharpSyntaxKindsService()
        {
        }

        public int IfKeyword => (int)SyntaxKind.IfKeyword;
        public int LogicalAndExpression => (int)SyntaxKind.LogicalAndExpression;
        public int LogicalOrExpression => (int)SyntaxKind.LogicalOrExpression;
    }
}
