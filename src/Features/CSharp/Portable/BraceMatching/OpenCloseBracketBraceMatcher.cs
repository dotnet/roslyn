// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.BraceMatching
{
    [ExportBraceMatcher(LanguageNames.CSharp), Shared]
    internal class OpenCloseBracketBraceMatcher : AbstractCSharpBraceMatcher
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OpenCloseBracketBraceMatcher()
            : base(SyntaxKind.OpenBracketToken, SyntaxKind.CloseBracketToken)
        {
        }
    }
}
