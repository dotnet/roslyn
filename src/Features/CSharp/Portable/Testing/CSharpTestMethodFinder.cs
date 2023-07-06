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
internal class CSharpTestMethodFinder([ImportMany] IEnumerable<ITestFrameworkMetadata> testFrameworks) : AbstractTestMethodFinder<MethodDeclarationSyntax>(testFrameworks)
{
    protected override bool DescendIntoChildren(SyntaxNode node)
    {
        // We only need to look in type declarations for test methods (and therefore namespaces to find types).
        return node is BaseNamespaceDeclarationSyntax or TypeDeclarationSyntax;
    }

    protected override bool IsTestMethod(MethodDeclarationSyntax method)
    {
        var attributes = method.AttributeLists.SelectMany(a => a.Attributes);
        foreach (var attribute in attributes)
        {
            var isTestAttribute = IsTestAttribute(attribute);
            if (isTestAttribute)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsTestAttribute(AttributeSyntax attribute)
    {
        var nameExpression = (ExpressionSyntax)attribute.Name;
        var rightmostName = nameExpression.GetRightmostName();
        if (rightmostName == null)
        {
            return false;
        }

        var attributeName = rightmostName.Identifier.Text;

        // Normalize the attribute name to always end with "Attribute".
        // For example [Fact] we normalize the name to 'FactAttribute' as it is actually defined.
        if (!attributeName.EndsWith("Attribute"))
        {
            attributeName += "Attribute";
        }

        var matches = TestFrameworkMetadata.Any(metadata => metadata.MatchesAttributeSyntacticName(attributeName));
        return matches;
    }
}
