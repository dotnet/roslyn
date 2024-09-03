// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Transforms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;

[ExportCSharpVisualBasicStatelessLspService(typeof(GetSymbolicInfoHandler)), Shared]
[Method(GetSymbolicInfoMethodName)]
internal sealed class GetSymbolicInfoHandler : ILspServiceDocumentRequestHandler<GetSymbolicInfoParams, MemberSymbolicInfo?>
{
    public const string GetSymbolicInfoMethodName = "roslyn/getSymbolicInfo";
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public GetSymbolicInfoHandler(IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(GetSymbolicInfoParams request) => request.Document;

    public async Task<MemberSymbolicInfo?> HandleRequestAsync(GetSymbolicInfoParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var solution = context.Solution;
        if (solution is null)
        {
            return null;
        }

        var document = context.Document;
        if (document is null)
        {
            return null;
        }

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxTree = semanticModel.SyntaxTree;
        var root = syntaxTree.GetRoot(cancellationToken);
        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        var generatedSpans = request.GeneratedDocumentRanges.Select(r => ProtocolConversions.RangeToTextSpan(r, sourceText));

        // First, get the class declaration for the component (implements Microsoft.AspNetCore.Components.ComponentBase). There might be a better way to get the type.
        var componentBaseSymbol = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.ComponentBase");
        if (componentBaseSymbol is null)
        {
            return null;
        }

        var classDeclarationNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(classSymbol => InheritsFromComponentBase(componentBaseSymbol, classSymbol, semanticModel));
        if (classDeclarationNode is null)
        {
            return null;
        }

        // Get the block syntax directly inside method BuildRenderTree(RenderTreeBuilder builder). This will be the most ancestral (first) block syntax.
        var blockNode = classDeclarationNode.DescendantNodes().OfType<BlockSyntax>().FirstOrDefault();
        if (blockNode is null)
        {
            return null;
        }

        var dataFlowAnalysis = semanticModel.AnalyzeDataFlow(blockNode);
        var writtenInsideSymbols = dataFlowAnalysis.WrittenInside;

        // Using the generated spans as a criterion to traverse through the tree generally returns incomplete results.
        // Instead, we get all of the identifiers, methods, fields, and properties in the class.
        // Then we get the identifiers that are within the generated spans.
        // We then find the methods, fields, and properties that correspond to the identifiers within the generated spans.
        var identifiersBuilder = ArrayBuilder<IdentifierNameSyntax>.GetInstance();
        var methodsBuilder = ArrayBuilder<MethodDeclarationSyntax>.GetInstance();
        var fieldsBuilder = ArrayBuilder<FieldDeclarationSyntax>.GetInstance();
        var propertiesBuilder = ArrayBuilder<PropertyDeclarationSyntax>.GetInstance();
        foreach (var node in classDeclarationNode.DescendantNodes())
        {
            switch (node.Kind())
            {
                case SyntaxKind.IdentifierName:
                    identifiersBuilder.Add((IdentifierNameSyntax)node);
                    break;
                case SyntaxKind.MethodDeclaration:
                    methodsBuilder.Add((MethodDeclarationSyntax)node);
                    break;
                case SyntaxKind.FieldDeclaration:
                    fieldsBuilder.Add((FieldDeclarationSyntax)node);
                    break;
                case SyntaxKind.PropertyDeclaration:
                    propertiesBuilder.Add((PropertyDeclarationSyntax)node);
                    break;
            }
        }

        var identifiersInClass = identifiersBuilder.ToImmutableAndFree();
        var methodsInClass = methodsBuilder.ToImmutableAndFree();
        var fieldsInClass = fieldsBuilder.ToImmutableAndFree();
        var propertiesInClass = propertiesBuilder.ToImmutableAndFree();

        var identifiersInRange = identifiersInClass.Where(identifier => generatedSpans.Any(span => span.Contains(identifier.Span)))
                    .Select(identifier => new IdentifierAndSymbol
                    {
                        Identifier = identifier,
                        Symbol = semanticModel.GetSymbolInfo(identifier).Symbol
                    })
                    .Where(x => x.Symbol != null);

        var methodsInRange = methodsInClass.Where(method => identifiersInRange
                                    .Any(identifier => SymbolEqualityComparer.Default.Equals(identifier.Symbol, semanticModel.GetDeclaredSymbol(method))));

        var fieldsInRange = fieldsInClass.Where(field => field.Declaration.Variables
                                            .Any(variable => identifiersInRange
                                                .Any(identifier => SymbolEqualityComparer.Default.Equals(identifier.Symbol, semanticModel.GetDeclaredSymbol(variable)))));

        var propertiesInRange = propertiesInClass.Where(property => identifiersInRange
                                        .Any(identifier => SymbolEqualityComparer.Default.Equals(identifier.Symbol, semanticModel.GetDeclaredSymbol(property))));

        // Now, we iterate through the methods, fields, and properties in the range and extract the necessary information.
        var pooledMethods = PooledHashSet<MethodSymbolicInfo>.GetInstance();
        var pooledAttributes = PooledHashSet<AttributeSymbolicInfo>.GetInstance();
        foreach (var method in methodsInRange)
        {
            var parameterTypes = method.ParameterList.Parameters.Count > 0
                ? method.ParameterList.Parameters
                    .Where(p => p.Type != null)
                    .Select(p => GetFullTypeName(p.Type!, semanticModel))
                    .ToArray()
                : Array.Empty<string>();

            pooledMethods.Add(new MethodSymbolicInfo
            {
                Name = method.Identifier.Text,
                ReturnType = GetFullTypeName(method.ReturnType!, semanticModel),
                ParameterTypes = parameterTypes
            });
        }

        var expressionIdentifiersInRange = identifiersInRange.Where(i => i.Identifier.Ancestors().OfType<ExpressionStatementSyntax>().Any());
        foreach (var field in fieldsInRange)
        {
            foreach (var declaredVariable in field.Declaration.Variables)
            {
                ExtractAttributeInfo(declaredVariable, field.Declaration.Type, semanticModel, pooledAttributes, writtenInsideSymbols, expressionIdentifiersInRange, cancellationToken);
            }
        }

        foreach (var property in propertiesInRange)
        {
            ExtractAttributeInfo(property, property.Type, semanticModel, pooledAttributes, writtenInsideSymbols, expressionIdentifiersInRange, cancellationToken);
        }

        var result = new MemberSymbolicInfo
        {
            Methods = pooledMethods.ToArray(),
            Attributes = pooledAttributes.ToArray()
        };

        pooledMethods.Free();
        pooledAttributes.Free();

        return result;
    }

    private static bool InheritsFromComponentBase(ITypeSymbol componentBaseSymbol, ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
    {
        if (componentBaseSymbol is null)
        {
            return false;
        }

        var baseTypes = classDeclaration.BaseList?.Types;
        if (baseTypes is null)
        {
            return false;
        }

        foreach (var baseTypeSyntax in baseTypes)
        {
            var classTypeSymbol = semanticModel.GetTypeInfo(baseTypeSyntax.Type).Type;
            if (classTypeSymbol is not null && InheritsFrom(classTypeSymbol, componentBaseSymbol))
            {
                return true;
            }
        }

        return false;
    }

    private static bool InheritsFrom(ITypeSymbol derivedType, ITypeSymbol baseType)
    {
        var currentType = derivedType;
        while (currentType is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(currentType, baseType))
                return true;
            currentType = currentType.BaseType;
        }
        return false;
    }

    private static void ExtractAttributeInfo(
        SyntaxNode node,
        TypeSyntax typeSyntax,
        SemanticModel semanticModel,
        PooledHashSet<AttributeSymbolicInfo> attributes,
        ImmutableArray<ISymbol> writtenInsideBlockSymbols,
        IEnumerable<IdentifierAndSymbol> identifiersInExpressions,
        CancellationToken cancellationToken)
    {
        var declarationInfo = semanticModel.GetDeclaredSymbol(node, cancellationToken);
        var typeSymbol = semanticModel.GetTypeInfo(typeSyntax, cancellationToken).Type;

        if (declarationInfo is null || typeSymbol is null)
        {
            return;
        }

        var isWrittenTo = writtenInsideBlockSymbols.Any(symbol => SymbolEqualityComparer.Default.Equals(symbol, declarationInfo));

        // Handle special case: attribute is string type or value type.
        // Attributes of these types are not added to the 'WrittenInside' property of a data flow analysis when written to or mutated.

        // Erring on the side of caution, assume they are written to if they are involved in some type of expression.

        // The 'isWrittenTo' property is not critical to functionality in current usage; it's only used in ExtractToComponent
        // to determine if a code attribute that has been promoted to a parameter in a component should include a comment warning.
        if (typeSymbol.SpecialType == SpecialType.System_String || typeSymbol.IsValueType)
        {
            isWrittenTo = identifiersInExpressions.Any(symbol => SymbolEqualityComparer.Default.Equals(symbol.Symbol, declarationInfo));
        }

        attributes.Add(new AttributeSymbolicInfo
        {
            Name = declarationInfo.Name,
            Type = GetFullTypeName(typeSyntax, semanticModel),
            IsValueType = typeSymbol.IsValueType,
            IsWrittenTo = isWrittenTo
        });
    }

    private static string GetFullTypeName(TypeSyntax type, SemanticModel semanticModel)
    {
        var symbol = semanticModel.GetSymbolInfo(type).Symbol as ITypeSymbol;
        if (symbol is not null)
        {
            return FormatType(symbol);
        }

        // Fallback to string if we can't get the symbol. Ideally this should never happen.
        return type.ToString();
    }

    private static string FormatType(ITypeSymbol typeSymbol)
    {
        // Check if the symbol is a named type symbol (e.g., List<T>)
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            // Get the base name of the type, e.g., "List"
            var typeName = namedTypeSymbol.Name;

            // If there are type arguments, format them recursively
            if (namedTypeSymbol.TypeArguments.Length > 0)
            {
                var typeArguments = string.Join(", ", namedTypeSymbol.TypeArguments.Select(FormatType));
                return $"{typeName}<{typeArguments}>"; // Returning a formatted string seems hacky so might need to be revisited. 
            }

            // If no type arguments, just return the type name
            return typeName;
        }

        // Fallback for non-named types
        return typeSymbol.ToDisplayString();
    }

    internal sealed record IdentifierAndSymbol
    {
        public required IdentifierNameSyntax Identifier { get; init; }
        public ISymbol? Symbol { get; init; }
    }
}
