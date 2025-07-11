// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Features.Testing;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Testing;

[ExportLanguageService(typeof(ITestMethodFinder), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpTestMethodFinder([ImportMany] IEnumerable<ITestFrameworkMetadata> testFrameworks) : AbstractTestMethodFinder<MethodDeclarationSyntax>(testFrameworks)
{
    protected override bool DescendIntoChildren(SyntaxNode node)
    {
        // We only need to look in type declarations for test methods (and therefore namespaces to find types).
        return node is BaseNamespaceDeclarationSyntax or TypeDeclarationSyntax;
    }

    protected override bool IsTestMethod(MethodDeclarationSyntax method)
    {
        foreach (var attributeList in method.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var isTestAttribute = IsTestAttribute(attribute);
                if (isTestAttribute)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsTestAttribute(AttributeSyntax attribute)
    {
        var attributeName = attribute.Name.GetNameToken().Text;

        var matches = TestFrameworkMetadata.Any(metadata => metadata.MatchesAttributeSyntacticName(attributeName));
        return matches;
    }
}
