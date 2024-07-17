// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseImplicitObjectCreation;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class CSharpUseImplicitObjectCreationDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public CSharpUseImplicitObjectCreationDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseImplicitObjectCreationDiagnosticId,
               EnforceOnBuildValues.UseImplicitObjectCreation,
               CSharpCodeStyleOptions.ImplicitObjectCreationWhenTypeIsApparent,
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_new), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.new_expression_can_be_simplified), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.ObjectCreationExpression);

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var cancellationToken = context.CancellationToken;
        var semanticModel = context.SemanticModel;
        var syntaxTree = context.Node.SyntaxTree;

        // Not available prior to C# 9.
        if (syntaxTree.Options.LanguageVersion() < LanguageVersion.CSharp9)
            return;

        var styleOption = context.GetCSharpAnalyzerOptions().ImplicitObjectCreationWhenTypeIsApparent;
        if (!styleOption.Value || ShouldSkipAnalysis(context, styleOption.Notification))
        {
            // Bail immediately if the user has disabled this feature.
            return;
        }

        var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
        if (!Analyze(semanticModel, context.GetCSharpAnalyzerOptions().GetSimplifierOptions(), objectCreation, cancellationToken))
            return;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            objectCreation.Type.GetLocation(),
            styleOption.Notification,
            context.Options,
            ImmutableArray.Create(objectCreation.GetLocation()),
            properties: null));
    }

    public static bool Analyze(
        SemanticModel semanticModel,
        CSharpSimplifierOptions simplifierOptions,
        ObjectCreationExpressionSyntax objectCreation,
        CancellationToken cancellationToken)
    {
        // type is apparent if we the object creation location is closely tied (spatially) to the explicit type.  Specifically:
        //
        // 1. Variable declarations.    i.e. `List<int> list = new ...`.  Note: we will suppress ourselves if this
        //    is a field and the 'var' preferences would lead to preferring this as `var list = ...`
        // 2. Expression-bodied constructs with an explicit return type.  i.e. `List<int> Prop => new ...` or
        //    `List<int> GetValue(...) => ...` The latter doesn't necessarily have the object creation spatially next to
        //    the type.  However, the type is always in a very easy to ascertain location in C#, so it is treated as
        //    apparent. 
        // 3. Collection-like constructs where the type of the collection is itself explicit.  For example: `new
        //    List<C> { new() }` or `new C[] { new() }`.

        TypeSyntax? typeNode;

        if (objectCreation.Parent is EqualsValueClauseSyntax
            {
                Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Type.IsVar: false } variableDeclaration }
            })
        {
            typeNode = variableDeclaration.Type;

            var helper = CSharpUseImplicitTypeHelper.Instance;
            if (helper.ShouldAnalyzeVariableDeclaration(variableDeclaration, cancellationToken))
            {
                // this is a case where the user would prefer 'var'.  don't offer to use an implicit object here.
                if (helper.AnalyzeTypeName(typeNode, semanticModel, simplifierOptions, cancellationToken).IsStylePreferred)
                    return false;
            }
        }
        else if (objectCreation.Parent.IsKind(SyntaxKind.ArrowExpressionClause))
        {
            typeNode = objectCreation.Parent.Parent switch
            {
                LocalFunctionStatementSyntax localFunction => localFunction.ReturnType,
                MethodDeclarationSyntax method => method.ReturnType,
                ConversionOperatorDeclarationSyntax conversion => conversion.Type,
                OperatorDeclarationSyntax op => op.ReturnType,
                BasePropertyDeclarationSyntax property => property.Type,
                AccessorDeclarationSyntax(SyntaxKind.GetAccessorDeclaration) { Parent: AccessorListSyntax { Parent: BasePropertyDeclarationSyntax baseProperty } } => baseProperty.Type,
                _ => null,
            };
        }
        else if (objectCreation.Parent is InitializerExpressionSyntax { Parent: ObjectCreationExpressionSyntax { Type: var collectionType } })
        {
            typeNode = collectionType switch
            {
                GenericNameSyntax { TypeArgumentList.Arguments: [{ } typeArgument] } => typeArgument,
                QualifiedNameSyntax { Right: GenericNameSyntax { TypeArgumentList.Arguments: [{ } typeArgument] } } => typeArgument,
                _ => null,
            };
        }
        else if (objectCreation.Parent is InitializerExpressionSyntax(kind: SyntaxKind.ArrayInitializerExpression)
        {
            Parent: EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax arrayVariableDeclaration } }
        })
        {
            typeNode = arrayVariableDeclaration.Type is ArrayTypeSyntax arrayType ? arrayType.ElementType : null;
        }
        else if (objectCreation.Parent is InitializerExpressionSyntax { Parent: ArrayCreationExpressionSyntax { Type: var arrayCreationType } })
        {
            typeNode = arrayCreationType is ArrayTypeSyntax arrayType ? arrayType.ElementType : null;
        }
        else
        {
            // more cases can be added here if we discover more cases we think the type is readily apparent from context.
            return false;
        }

        if (typeNode == null)
            return false;

        // Only offer if the type being constructed is the exact same as the type being assigned into.  We don't
        // want to change semantics by trying to instantiate something else.
        var leftType = semanticModel.GetTypeInfo(typeNode, cancellationToken).Type;
        var rightType = semanticModel.GetTypeInfo(objectCreation, cancellationToken).Type;

        if (leftType is null || rightType is null)
            return false;

        if (leftType.IsErrorType() || rightType.IsErrorType())
            return false;

        // `new T?()` cannot be simplified to `new()`.  Even if the contextual type is `T?`, `new()` will be
        // interpetted as `new T()` which is a change in semantics.
        if (rightType.IsNullable())
            return false;

        // The default SymbolEquivalenceComparer will ignore tuple name differences, which is advantageous here
        if (!SymbolEquivalenceComparer.Instance.Equals(leftType, rightType))
            return false;

        return true;
    }
}
