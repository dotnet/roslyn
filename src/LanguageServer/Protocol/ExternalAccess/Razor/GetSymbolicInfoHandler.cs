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

        var document = solution.GetDocument(request.Document);
        if (document is null)
        {
            return null;
        }

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxTree = semanticModel.SyntaxTree;
        var root = syntaxTree.GetRoot(cancellationToken);
        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        var generatedSpans = request.GeneratedDocumentRanges.Select(r => ProtocolConversions.RangeToTextSpan(r, sourceText));

        var classDeclarationNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classDeclarationNode is null)
        {
            return null;
        }

        var blockNode = classDeclarationNode.DescendantNodes().OfType<BlockSyntax>().FirstOrDefault();
        if (blockNode is null)
        {
            return null;
        }

        var dataFlowAnalysis = semanticModel.AnalyzeDataFlow(blockNode);
        var writtenInsideBlock = dataFlowAnalysis.WrittenInside.Select(symbol => symbol.Name);

        var identifiersInClass = classDeclarationNode.DescendantNodes().OfType<IdentifierNameSyntax>();
        var methodsInClass = classDeclarationNode.DescendantNodes().OfType<MethodDeclarationSyntax>();
        var fieldsInClass = classDeclarationNode.DescendantNodes().OfType<FieldDeclarationSyntax>();
        var propertiesInClass = classDeclarationNode.DescendantNodes().OfType<PropertyDeclarationSyntax>();

        var identifiersInRange = identifiersInClass.Where(i => generatedSpans.Any(s => s.Contains(i.Span)))
                                            .Select(i => semanticModel.GetSymbolInfo(i).Symbol?.Name)
                                            .Where(n => n != null).Select(n => n!);

        var methodsInRange = methodsInClass.Where(m => identifiersInRange.Contains(m.Identifier.Text));
        var fieldsInRange = fieldsInClass.Where(f => f.Declaration.Variables.Any(v => identifiersInRange.Contains(v.Identifier.Text)));
        var propertiesInRange = propertiesInClass.Where(p => identifiersInRange.Contains(p.Identifier.Text));

        var pooledMethods = PooledHashSet<MethodSymbolicInfo>.GetInstance();
        var pooledAttributes = PooledHashSet<AttributeSymbolicInfo>.GetInstance();

        foreach (var method in methodsInRange)
        {

            var parameterTypes = method.ParameterList.Parameters.Count > 0
                ? method.ParameterList.Parameters
                    .Where(p => p.Type != null)
                    .Select(p => p.Type!.GetFirstToken().Text)
                    .ToArray()
                : Array.Empty<string>();

            pooledMethods.Add(new MethodSymbolicInfo
            {
                Name = method.Identifier.Text,
                ReturnType = method.ReturnType.GetFirstToken().Text,
                ParameterTypes = parameterTypes
            });
        }

        var expressionsInClass = classDeclarationNode.DescendantNodes().OfType<ExpressionStatementSyntax>();
        var expressionIdentifiers = expressionsInClass.SelectMany(e => e.DescendantNodes().OfType<IdentifierNameSyntax>());
        var expressionIdentifierNames = expressionIdentifiers.Select(i => semanticModel.GetSymbolInfo(i).Symbol?.Name)
            .Where(n => n != null).Select(n => n!);

        foreach (var field in fieldsInRange)
        {
            foreach (var declaredVariable in field.Declaration.Variables)
            {
                ExtractAttributeInfo(declaredVariable, field.Declaration.Type, semanticModel, pooledAttributes, writtenInsideBlock, expressionIdentifierNames, cancellationToken);
            }
        }

        foreach (var property in propertiesInRange)
        {
            ExtractAttributeInfo(property, property.Type, semanticModel, pooledAttributes, writtenInsideBlock, expressionIdentifierNames, cancellationToken);
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

    private static void ExtractAttributeInfo(
        SyntaxNode node,
        TypeSyntax typeSyntax,
        SemanticModel semanticModel,
        PooledHashSet<AttributeSymbolicInfo> attributes,
        IEnumerable<string> writtenInsideBlock,
        IEnumerable<string> identifierSymbolNames,
        CancellationToken cancellationToken)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (typeSyntax is null)
        {
            throw new ArgumentNullException(nameof(typeSyntax));
        }

        if (semanticModel is null)
        {
            throw new ArgumentNullException(nameof(semanticModel));
        }

        var declarationInfo = semanticModel.GetDeclaredSymbol(node, cancellationToken);
        var typeInfo = semanticModel.GetTypeInfo(typeSyntax, cancellationToken);

        if (declarationInfo is null || typeInfo.Type is null)
        {
            return;
        }

        var isWrittenTo = writtenInsideBlock.Any(symbol => symbol == declarationInfo.Name);

        // Handle special case: attribute is string type or value type.
        // Attributes of these types are not added to the 'WrittenInside' property of a data flow analysis when written to or mutated.

        // Erring on the side of caution, assume they are written to if they are involved in some type of expression.

        // The 'isWrittenTo' property is not critical to functionality in current usage; it's only used in ExtractToComponent
        // to determine if a code attribute that has been promoted to a parameter in a component should include a comment warning.
        if (typeInfo.Type.ToDisplayString() == "string" || typeInfo.Type.IsValueType)
        {
            isWrittenTo = identifierSymbolNames.Contains(declarationInfo.Name);
        }

        attributes.Add(new AttributeSymbolicInfo
        {
            Name = declarationInfo.Name,
            Type = typeInfo.Type.ToDisplayString(),
            IsValueType = typeInfo.Type.IsValueType,
            IsWrittenTo = isWrittenTo
        });
    }
}
