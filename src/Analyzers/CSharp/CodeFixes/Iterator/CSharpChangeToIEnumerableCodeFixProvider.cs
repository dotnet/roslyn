// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Iterator;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.Iterator;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ChangeReturnType), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal class CSharpChangeToIEnumerableCodeFixProvider() : AbstractIteratorCodeFixProvider
{
    /// <summary>
    /// CS1624: The body of 'x' cannot be an iterator block because 'y' is not an iterator interface type
    /// </summary>
    private const string CS1624 = nameof(CS1624);

    public override ImmutableArray<string> FixableDiagnosticIds => [CS1624];

    protected override async Task<CodeAction?> GetCodeFixAsync(SyntaxNode root, SyntaxNode node, Document document, Diagnostic diagnostics, CancellationToken cancellationToken)
    {
        var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var methodSymbol = model.GetDeclaredSymbol(node, cancellationToken) as IMethodSymbol;
        // IMethod symbol can either be a regular method or an accessor
        if (methodSymbol?.ReturnType == null || methodSymbol.ReturnsVoid)
        {
            return null;
        }

        var type = methodSymbol.ReturnType;
        if (!TryGetIEnumerableSymbols(model, out var ienumerableSymbol, out var ienumerableGenericSymbol))
        {
            return null;
        }

        if (type.InheritsFromOrEquals(ienumerableSymbol, includeInterfaces: true))
        {
            var arity = type.GetArity();
            if (arity == 1)
            {
                var typeArg = type.GetTypeArguments().First();
                ienumerableGenericSymbol = ienumerableGenericSymbol.Construct(typeArg);
            }
            else if (arity == 0 && type is IArrayTypeSymbol arrayType)
            {
                ienumerableGenericSymbol = ienumerableGenericSymbol.Construct(arrayType.ElementType);
            }
            else
            {
                return null;
            }
        }
        else
        {
            ienumerableGenericSymbol = ienumerableGenericSymbol.Construct(type);
        }

        var newReturnType = ienumerableGenericSymbol.GenerateTypeSyntax();
        Document? newDocument = null;
        var newMethodDeclarationSyntax = (node as MethodDeclarationSyntax)?.WithReturnType(newReturnType);
        if (newMethodDeclarationSyntax != null)
        {
            newDocument = document.WithSyntaxRoot(root.ReplaceNode(node, newMethodDeclarationSyntax));
        }

        var newOperator = (node as OperatorDeclarationSyntax)?.WithReturnType(newReturnType);
        if (newOperator != null)
        {
            newDocument = document.WithSyntaxRoot(root.ReplaceNode(node, newOperator));
        }

        var oldAccessor = node.Parent?.Parent as PropertyDeclarationSyntax;
        if (oldAccessor != null)
        {
            newDocument = document.WithSyntaxRoot(root.ReplaceNode(oldAccessor, oldAccessor.WithType(newReturnType)));
        }

        var oldIndexer = node.Parent?.Parent as IndexerDeclarationSyntax;
        if (oldIndexer != null)
        {
            newDocument = document.WithSyntaxRoot(root.ReplaceNode(oldIndexer, oldIndexer.WithType(newReturnType)));
        }

        if (newDocument == null)
        {
            return null;
        }

        var title = string.Format(CSharpCodeFixesResources.Change_return_type_from_0_to_1,
            type.ToMinimalDisplayString(model, node.SpanStart),
            ienumerableGenericSymbol.ToMinimalDisplayString(model, node.SpanStart));

        return CodeAction.Create(title, _ => Task.FromResult(newDocument), title);
    }

    private static bool TryGetIEnumerableSymbols(
        SemanticModel model,
        [NotNullWhen(true)] out INamedTypeSymbol? ienumerableSymbol,
        [NotNullWhen(true)] out INamedTypeSymbol? ienumerableGenericSymbol)
    {
        ienumerableSymbol = model.Compilation.GetTypeByMetadataName(typeof(IEnumerable).FullName!);
        ienumerableGenericSymbol = model.Compilation.GetTypeByMetadataName(typeof(IEnumerable<>).FullName!);

        return ienumerableGenericSymbol != null && ienumerableSymbol != null;
    }
}
