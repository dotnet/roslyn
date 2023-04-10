// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

[Export, Shared]
internal sealed class TestFrameworkHelper
{
    private readonly ImmutableArray<ITestFrameworkMetadata> _testFrameworkMetadata;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TestFrameworkHelper([ImportMany] IEnumerable<ITestFrameworkMetadata> testFrameworks)
    {
        _testFrameworkMetadata = testFrameworks.ToImmutableArray();
    }

    /// <summary>
    /// Implements a simple method of finding potential test methods in a particular span.
    /// This is not 100% accurate and is not intended to be to reduce complexity (for example we do not consider inheritance).
    /// However it should cover the majority of cases.
    /// </summary>
    public async Task<ImmutableArray<MethodDeclarationSyntax>> GetPotentialTestMethodsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken);

        // Find the node represented by the span, then find any method declarations in the descendent nodes.
        // For example, if the range represents a class, we want to find all methods in that class.
        var node = root.FindNode(textSpan);
        var methodsInRange = node.DescendantNodesAndSelf(descendIntoTrivia: false).OfType<MethodDeclarationSyntax>();

        using var _ = ArrayBuilder<MethodDeclarationSyntax>.GetInstance(out var testMethods);
        foreach (var method in methodsInRange)
        {
            var isTestMethod = await IsTestMethodAsync(document, method, cancellationToken);
            if (isTestMethod)
            {
                testMethods.Add(method);
            }
        }

        return testMethods.ToImmutableArray();
    }

    private async Task<bool> IsTestMethodAsync(Document document, MethodDeclarationSyntax method, CancellationToken cancellationToken)
    {
        var attributes = method.AttributeLists.SelectMany(a => a.Attributes);
        foreach (var attribute in attributes)
        {
            var isTestAttribute = await IsTestAttributeAsync(document, attribute, cancellationToken);
            if (isTestAttribute)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> IsTestAttributeAsync(Document document, AttributeSyntax attribute, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken);
        var symbol = semanticModel.GetSymbolInfo(attribute, cancellationToken);
        if (symbol.Symbol is null)
        {
            return false;
        }

        var typeName = symbol.Symbol.ContainingType.ToNameDisplayString();
        var matches = _testFrameworkMetadata.Any(metadata => metadata.MatchesAttributeSymbolName(typeName));
        return matches;
    }

    private static async Task<string> GetAttributeName(ISyntaxFactsService syntaxFactsService, AttributeSyntax attribute, Document d)
    {
        var rightMostName = syntaxFactsService.GetRightmostNameOfExpression(attribute.Name);
        if (rightMostName == null)
        {
            return string.Empty;
        }

        var nameWithoutAttribute = GetNameWithoutAttribute(rightMostName);
        return nameWithoutAttribute;

        static string GetNameWithoutAttribute(string identifier)
        {
            // The attribute identifier could be 'Xyz' or 'XyzAttribute'.
            // If its 'XyzAttribute' normalize to just 'Xyz'.
            var lastIndexOfAttribute = identifier.LastIndexOf("Attribute", StringComparison.Ordinal);
            if (lastIndexOfAttribute != -1)
            {
                return identifier[..lastIndexOfAttribute];
            }
            else
            {
                return identifier;
            }
        }
    }
}
