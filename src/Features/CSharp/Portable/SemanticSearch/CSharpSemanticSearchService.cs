// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SemanticSearch.CSharp;

[ExportLanguageService(typeof(ISemanticSearchService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpSemanticSearchService() : AbstractSemanticSearchService()
{
    protected override Compilation CreateCompilation(
        SourceText query,
        IEnumerable<MetadataReference> references,
        SolutionServices services,
        out SyntaxTree queryTree,
        CancellationToken cancellationToken)
    {
        var syntaxTreeFactory = services.GetRequiredLanguageService<ISyntaxTreeFactoryService>(LanguageNames.CSharp);

        var globalUsingsTree = syntaxTreeFactory.ParseSyntaxTree(
            filePath: null,
            CSharpSemanticSearchUtilities.ParseOptions,
            SemanticSearchUtilities.CreateSourceText(CSharpSemanticSearchUtilities.Configuration.GlobalUsings),
            cancellationToken);

        queryTree = syntaxTreeFactory.ParseSyntaxTree(
            filePath: SemanticSearchUtilities.QueryDocumentName,
            CSharpSemanticSearchUtilities.ParseOptions,
            query,
            cancellationToken);

        return CSharpCompilation.Create(
            assemblyName: SemanticSearchUtilities.QueryProjectName,
            [queryTree, globalUsingsTree],
            references,
            CSharpSemanticSearchUtilities.CompilationOptions);
    }

    protected override string MethodNotFoundMessage
        => string.Format(FeaturesResources.The_query_does_not_specify_0_1, CSharpFeaturesResources.local_function, SemanticSearchUtilities.FindMethodName);

    protected override IMethodSymbol? TryGetFindMethod(Compilation queryCompilation, SyntaxNode queryRoot, out TargetEntity targetEntity, out string? targetLanguage, out string? errorMessage, out string[]? errorMessageArgs)
    {
        errorMessage = null;
        errorMessageArgs = null;
        targetEntity = default;
        targetLanguage = null;

        var model = queryCompilation.GetSemanticModel(queryRoot.SyntaxTree);
        var compilationUnit = (CompilationUnitSyntax)queryRoot;

        foreach (var member in compilationUnit.Members)
        {
            if (member is GlobalStatementSyntax { Statement: LocalFunctionStatementSyntax { Identifier.Text: SemanticSearchUtilities.FindMethodName } localFunctionSyntax } &&
                (localFunctionSyntax.Body ?? (SyntaxNode?)localFunctionSyntax.ExpressionBody?.Expression) is { } body &&
                model.GetDeclaredSymbol(localFunctionSyntax) is IMethodSymbol localFunction)
            {
                if (localFunction is not { IsStatic: true, Arity: 0 })
                {
                    errorMessage = string.Format(FeaturesResources._0_1_must_be_static_and_non_generic, CSharpFeaturesResources.Local_function, SemanticSearchUtilities.FindMethodName);
                    return null;
                }

                var enumerableOfISymbol = queryCompilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
                Contract.ThrowIfNull(enumerableOfISymbol);

                if (localFunction is not { RefKind: RefKind.None, ReturnTypeCustomModifiers: [] } ||
                    !localFunction.ReturnType.Implements(enumerableOfISymbol))
                {
                    errorMessage = string.Format(
                        FeaturesResources._0_1_must_return_2,
                        CSharpFeaturesResources.Local_function,
                        SemanticSearchUtilities.FindMethodName,
                        enumerableOfISymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

                    return null;
                }

                var supportedParameterTypes = GetSupportedParameterTypes(queryCompilation);

                if (localFunction.Parameters is [{ Type: var paramType, RefKind: RefKind.None, RefCustomModifiers: [] }])
                {
                    foreach (var (supportedType, parameterKind) in supportedParameterTypes)
                    {
                        if (supportedType.Equals(paramType))
                        {
                            targetEntity = parameterKind;
                            return localFunction;
                        }

                        if (supportedType.Equals(paramType.BaseType) && GetLanguageFromNamespace(paramType) is { } language)
                        {
                            targetEntity = parameterKind;
                            targetLanguage = language;
                            return localFunction;
                        }
                    }
                }

                errorMessage = string.Format(
                    FeaturesResources._0_1_must_have_a_single_parameter_of_one_of_the_following_types_2,
                    CSharpFeaturesResources.Local_function,
                    SemanticSearchUtilities.FindMethodName,
                    string.Join(", ", supportedParameterTypes.Select(t => t.symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))));

                return null;
            }
        }

        return null;
    }

    private static string? GetLanguageFromNamespace(ITypeSymbol type)
        => type.ContainingNamespace.Name switch
        {
            "CSharp" => LanguageNames.CSharp,
            "VisualBasic" => LanguageNames.VisualBasic,
            _ => null
        };

    private IEnumerable<(ISymbol symbol, TargetEntity kind)> GetSupportedParameterTypes(Compilation compilation)
    {
        yield return (GetSymbol(nameof(Compilation)), TargetEntity.Compilation);
        yield return (GetSymbol(nameof(IAssemblySymbol)), TargetEntity.Assembly);
        yield return (GetSymbol(nameof(IModuleSymbol)), TargetEntity.Module);
        yield return (GetSymbol(nameof(INamespaceSymbol)), TargetEntity.Namespace);
        yield return (GetSymbol(nameof(INamespaceOrTypeSymbol)), TargetEntity.NamespaceOrType);
        yield return (GetSymbol(nameof(INamedTypeSymbol)), TargetEntity.NamedType);
        yield return (GetSymbol(nameof(IMethodSymbol)), TargetEntity.Method);
        yield return (GetSymbol(nameof(IFieldSymbol)), TargetEntity.Field);
        yield return (GetSymbol(nameof(IEventSymbol)), TargetEntity.Event);
        yield return (GetSymbol(nameof(IPropertySymbol)), TargetEntity.Property);

        ISymbol GetSymbol(string name)
        {
            var symbol = compilation.GetTypeByMetadataName($"Microsoft.CodeAnalysis.{name}");
            Contract.ThrowIfNull(symbol);
            return symbol;
        }
    }
}
#endif
