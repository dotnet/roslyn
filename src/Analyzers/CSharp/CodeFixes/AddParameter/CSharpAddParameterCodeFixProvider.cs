// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.AddParameter;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.GenerateConstructor;
using Microsoft.CodeAnalysis.CSharp.InitializeParameter;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.AddParameter;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddParameter), Shared]
[ExtensionOrder(Before = PredefinedCodeFixProviderNames.GenerateConstructor)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpAddParameterCodeFixProvider() : AbstractAddParameterCodeFixProvider<
    ArgumentSyntax,
    AttributeArgumentSyntax,
    ArgumentListSyntax,
    AttributeArgumentListSyntax,
    ExpressionSyntax,
    InvocationExpressionSyntax,
    BaseObjectCreationExpressionSyntax>
{
    private const string CS1501 = nameof(CS1501); // error CS1501: No overload for method 'M' takes 1 arguments
    private const string CS1503 = nameof(CS1503); // error CS1503: Argument 1: cannot convert from 'double' to 'int'
    private const string CS1660 = nameof(CS1660); // error CS1660: Cannot convert lambda expression to type 'string[]' because it is not a delegate type
    private const string CS1729 = nameof(CS1729); // error CS1729: 'C' does not contain a constructor that takes n arguments
    private const string CS1739 = nameof(CS1739); // error CS1739: The best overload for 'M' does not have a parameter named 'x'

    private static readonly ImmutableArray<string> AddParameterFixableDiagnosticIds = [CS1501, CS1503, CS1660, CS1729, CS1739];

    public override ImmutableArray<string> FixableDiagnosticIds
        => AddParameterFixableDiagnosticIds;

    protected override ImmutableArray<string> TooManyArgumentsDiagnosticIds
        => GenerateConstructorDiagnosticIds.TooManyArgumentsDiagnosticIds;

    protected override ImmutableArray<string> CannotConvertDiagnosticIds
        => GenerateConstructorDiagnosticIds.CannotConvertDiagnosticIds;

    protected override ITypeSymbol GetArgumentType(SyntaxNode argumentNode, SemanticModel semanticModel, CancellationToken cancellationToken)
        => ((ArgumentSyntax)argumentNode).DetermineParameterType(semanticModel, cancellationToken);

    protected override RegisterFixData<ArgumentSyntax>? TryGetLanguageSpecificFixInfo(
        SemanticModel semanticModel,
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        if (node is ConstructorInitializerSyntax constructorInitializer)
        {
            var constructorDeclaration = constructorInitializer.GetRequiredParent();
            if (semanticModel.GetDeclaredSymbol(constructorDeclaration, cancellationToken) is IMethodSymbol constructorSymbol)
            {
                var type = constructorSymbol.ContainingType;
                if (constructorInitializer.IsKind(SyntaxKind.BaseConstructorInitializer))
                {
                    // Search for fixable constructors in the base class.
                    type = type?.BaseType;
                }

                if (type != null && type.IsFromSource())
                {
                    var methodCandidates = type.InstanceConstructors;
                    var arguments = constructorInitializer.ArgumentList.Arguments;
                    return new RegisterFixData<ArgumentSyntax>(arguments, methodCandidates, isConstructorInitializer: true);
                }
            }
        }

        return null;
    }
}
