// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpImplementNotImplementedExceptionDiagnosticAnalyzer()
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(
        IDEDiagnosticIds.CopilotImplementNotImplementedExceptionDiagnosticId,
        EnforceOnBuildValues.CopilotImplementNotImplementedException,
        option: null,
        new LocalizableResourceString(
            nameof(CSharpAnalyzersResources.Implement_with_Copilot), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
        configurable: false)
{
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            var notImplementedExceptionType = context.Compilation.GetTypeByMetadataName(typeof(NotImplementedException).FullName!);
            if (notImplementedExceptionType != null)
            {
                // Register action for all throw operations
                context.RegisterOperationAction(context => AnalyzeThrow(context, notImplementedExceptionType), OperationKind.Throw);

                // Register action for all member declarations
                using var _ = SharedPools.Default<HashSet<SyntaxNode>>().GetPooledObject(out var reportedMembers);
                context.RegisterSyntaxNodeAction(context => AnalyzeMethod(context, notImplementedExceptionType, reportedMembers),
                    SyntaxKind.MethodDeclaration,
                    SyntaxKind.ConstructorDeclaration,
                    SyntaxKind.DestructorDeclaration,
                    SyntaxKind.PropertyDeclaration,
                    SyntaxKind.EventDeclaration,
                    SyntaxKind.IndexerDeclaration,
                    SyntaxKind.OperatorDeclaration,
                    SyntaxKind.ConversionOperatorDeclaration);
            }
        });
    }

    private void AnalyzeThrow(OperationAnalysisContext context, INamedTypeSymbol notImplementedExceptionType)
    {
        var throwOperation = (IThrowOperation)context.Operation;
        if (throwOperation is
            {
                Exception: IConversionOperation
                {
                    Operand: IObjectCreationOperation
                    {
                        Constructor.ContainingType: INamedTypeSymbol constructedType,
                    },
                },
                Syntax: ThrowExpressionSyntax or ThrowStatementSyntax,
            } &&
            notImplementedExceptionType.Equals(constructedType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptor,
                throwOperation.Syntax.GetLocation()));
        }
    }

    private void AnalyzeMethod(SyntaxNodeAnalysisContext context, INamedTypeSymbol notImplementedExceptionType, HashSet<SyntaxNode> reportedMembers)
    {
        if (context.Node is not MemberDeclarationSyntax memberDeclaration || reportedMembers.Contains(memberDeclaration))
            return;

        var nameToken = GetMemberNameToken(memberDeclaration);
        if (nameToken == default)
            return;

        var semanticModel = context.SemanticModel;
        var throwNodes = memberDeclaration.DescendantNodes()
            .Where(n => n is ThrowStatementSyntax || n is ThrowExpressionSyntax);

        foreach (var throwNode in throwNodes)
        {
            ExpressionSyntax? expression = null;
            if (throwNode is ThrowStatementSyntax throwStatement)
                expression = throwStatement.Expression;
            else if (throwNode is ThrowExpressionSyntax throwExpression)
                expression = throwExpression.Expression;

            if (expression is ObjectCreationExpressionSyntax objectCreation)
            {
                var typeInfo = semanticModel.GetTypeInfo(objectCreation);
                if (typeInfo.Type != null && notImplementedExceptionType.Equals(typeInfo.Type))
                {
                    reportedMembers.Add(memberDeclaration);
                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptor,
                        nameToken.GetLocation()));

                    // Report only once for each member
                    break;
                }
            }
        }
    }

    private static SyntaxToken GetMemberNameToken(MemberDeclarationSyntax memberDeclaration)
    {
        return memberDeclaration switch
        {
            MethodDeclarationSyntax method => method.Identifier,
            ConstructorDeclarationSyntax constructor => constructor.Identifier,
            DestructorDeclarationSyntax destructor => destructor.Identifier,
            PropertyDeclarationSyntax property => property.Identifier,
            EventDeclarationSyntax @event => @event.Identifier,
            IndexerDeclarationSyntax indexer => indexer.ThisKeyword,
            OperatorDeclarationSyntax @operator => @operator.OperatorToken,
            ConversionOperatorDeclarationSyntax conversion => conversion.Type.GetFirstToken(),
            _ => default
        };
    }
}
