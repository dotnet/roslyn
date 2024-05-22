// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ForEachCast;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.ForEachCast;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ForEachCast), Shared]
internal class CSharpForEachCastCodeFixProvider : AbstractForEachCastCodeFixProvider<CommonForEachStatementSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpForEachCastCodeFixProvider()
    {
    }

    protected override ITypeSymbol GetForEachElementType(
        SemanticModel semanticModel, CommonForEachStatementSyntax forEachStatement)
    {
        var forEachInfo = semanticModel.GetForEachStatementInfo(forEachStatement);
        var result = forEachInfo.ElementType;
        // We should only get here if our analyzer found an issue, and it already checked that this property
        // was non-null.  So we can safely know it's non-null at fix time as well.
        Contract.ThrowIfNull(result);
        return result;
    }
}
