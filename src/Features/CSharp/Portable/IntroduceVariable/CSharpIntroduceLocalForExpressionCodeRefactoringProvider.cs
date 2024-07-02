// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.IntroduceVariable;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.IntroduceLocalForExpression), Shared]
internal class CSharpIntroduceLocalForExpressionCodeRefactoringProvider :
    AbstractIntroduceLocalForExpressionCodeRefactoringProvider<
        ExpressionSyntax,
        StatementSyntax,
        ExpressionStatementSyntax,
        LocalDeclarationStatementSyntax>
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpIntroduceLocalForExpressionCodeRefactoringProvider()
    {
    }

    protected override bool IsValid(ExpressionStatementSyntax expressionStatement, TextSpan span)
    {
        // Expression is likely too simple to want to offer to generate a local for.
        // This leads to too many false cases where this is offered.
        if (span.IsEmpty &&
            expressionStatement.SemicolonToken.IsMissing &&
            expressionStatement.Expression.IsKind(SyntaxKind.IdentifierName))
        {
            return false;
        }

        // We don't want to offer new local for an assignmentExpression `a = 42` -> `int newA = a = 42`
        return expressionStatement.Expression is not AssignmentExpressionSyntax;
    }

    protected override LocalDeclarationStatementSyntax FixupLocalDeclaration(
        ExpressionStatementSyntax expressionStatement, LocalDeclarationStatementSyntax localDeclaration)
    {
        // If there wasn't a semicolon before, ensure the trailing trivia of the expression
        // becomes the trailing trivia of a new semicolon that we add.
        var semicolonToken = expressionStatement.SemicolonToken;
        if (expressionStatement.SemicolonToken.IsMissing && localDeclaration is { Declaration.Variables: [{ Initializer.Value: { } value }, ..] })
        {
            var expression = expressionStatement.Expression;
            localDeclaration = localDeclaration.ReplaceNode(value, expression.WithoutLeadingTrivia());
            semicolonToken = SemicolonToken.WithTrailingTrivia(expression.GetTrailingTrivia());
        }

        return localDeclaration.WithSemicolonToken(semicolonToken);
    }

    protected override ExpressionStatementSyntax FixupDeconstruction(
        ExpressionStatementSyntax expressionStatement, ExpressionStatementSyntax deconstruction)
    {
        // If there wasn't a semicolon before, ensure the trailing trivia of the expression
        // becomes the trailing trivia of a new semicolon that we add.
        var semicolonToken = expressionStatement.SemicolonToken;
        if (expressionStatement.SemicolonToken.IsMissing && deconstruction is { Expression: AssignmentExpressionSyntax binary })
        {
            var expression = expressionStatement.Expression;
            deconstruction = deconstruction.ReplaceNode(binary.Right, expression.WithoutLeadingTrivia());
            semicolonToken = SemicolonToken.WithTrailingTrivia(expression.GetTrailingTrivia());
        }

        return deconstruction.WithSemicolonToken(semicolonToken);
    }

    protected override async Task<ExpressionStatementSyntax> CreateTupleDeconstructionAsync(
        Document document,
        CodeActionOptionsProvider optionsProvider,
        INamedTypeSymbol tupleType,
        ExpressionSyntax expression,
        CancellationToken cancellationToken)
    {
        var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
        var simplifierOptions = (CSharpSimplifierOptions)await document.GetSimplifierOptionsAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var tupleUnderlyingType = tupleType.TupleUnderlyingType ?? tupleType;

        // Generate the names for the locals.  For Tuples that have user provided names, keep that name.
        // Otherwise, generate a reasonable local name for the type of the field, using our helpers.
        var localTypesAndDesignations = tupleType.TupleElements.SelectAsArray((field, index, _) =>
            {
                var name = field.Name.ToCamelCase();
                if (field.Name == tupleUnderlyingType.TupleElements[index].Name)
                    name = field.Type.GetLocalName(fallback: null) ?? name;

                var uniqueName = semanticFacts.GenerateUniqueLocalName(semanticModel, expression, container: null, name, cancellationToken);
                var designation = SingleVariableDesignation(uniqueName);
                return (type: field.Type, designation: (VariableDesignationSyntax)designation);
            }, arg: /*unused*/false);

        return ExpressionStatement(
            AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateDeclarationExpression(),
                expression));

        ExpressionSyntax CreateDeclarationExpression()
        {
            if (CanUseVar())
            {
                return DeclarationExpression(
                    IdentifierName("var"),
                    ParenthesizedVariableDesignation(
                        [.. localTypesAndDesignations.Select(n => n.designation)]));
            }
            else
            {
                // otherwise, emit as `(T1 x, T2 y, T3 z) = ...`.  Note, the 'T's will get simplified to 'var' if that matches the user's preference.
                return TupleExpression([.. localTypesAndDesignations.Select(t =>
                    Argument(DeclarationExpression(t.type.GenerateTypeSyntax(), t.designation)))]);
            }

            bool CanUseVar()
            {
                // check the user's 'var' preference.  If it holds for this tuple type and expr, then emit as:
                // `var (x, y, z) = ...`.
                var varPreference = simplifierOptions.GetUseVarPreference();

                // If the user likes 'var' for intrinsics, and all the elements would be intrinsic.  Then use
                var isIntrinsic = tupleType.TupleElements.All(f => f.Type?.SpecialType != SpecialType.None);
                if (isIntrinsic)
                    return varPreference.HasFlag(UseVarPreference.ForBuiltInTypes);

                // now see if the type is apparent using the existing helper.
                var isApparent = TypeStyleHelper.IsTypeApparentInAssignmentExpression(varPreference, expression, semanticModel, tupleType, cancellationToken);
                if (isApparent)
                    return varPreference.HasFlag(UseVarPreference.WhenTypeIsApparent);

                // Finally, use 'var' if the user wants that for non-intrinsic, non-apparent cases.
                return varPreference.HasFlag(UseVarPreference.Elsewhere);
            }
        }
    }
}
