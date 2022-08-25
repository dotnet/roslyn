// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Venus
{
    [ExportLanguageService(typeof(IVenusBraceMatchingService), LanguageNames.CSharp), Shared]
    internal class CSharpVenusBraceMatchingService : IVenusBraceMatchingService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpVenusBraceMatchingService()
        {
        }

        public bool TryGetCorrespondingOpenBrace(SyntaxToken token, out SyntaxToken openBrace)
        {
            if (token.Kind() == SyntaxKind.CloseBraceToken)
            {
                var tuple = token.GetRequiredParent().GetBraces();

                openBrace = tuple.openBrace;
                return openBrace.Kind() == SyntaxKind.OpenBraceToken;
            }

            openBrace = default;
            return false;
        }
    }
}
