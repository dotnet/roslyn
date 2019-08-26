// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Utilities
{
    internal struct TypeStyleResult
    {
        private readonly CSharpTypeStyleHelper _helper;
        private readonly TypeSyntax _typeName;
        private readonly SemanticModel _semanticModel;
        private readonly OptionSet _optionSet;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Whether or not converting would transition the code to the style the user prefers. i.e. if the user likes
        /// <c>var</c> for everything, and you have <c>int i = 0</c> then <see cref="IsStylePreferred"/> will be
        /// <see langword="true"/>. However, if the user likes <c>var</c> for everything and you have <c>var i = 0</c>,
        /// then it's still possible to convert that, it would just be <see langword="false"/> for
        /// <see cref="IsStylePreferred"/> because it goes against the user's preferences.
        /// </summary>
        /// <remarks>
        /// <para>In general, most features should only convert the type if <see cref="IsStylePreferred"/> is
        /// <see langword="true"/>. The one exception is the refactoring, which is explicitly there to still let people
        /// convert things quickly, even if it's going against their stated style.</para>
        /// </remarks>
        public readonly bool IsStylePreferred;
        public readonly ReportDiagnostic Severity;

        public TypeStyleResult(CSharpTypeStyleHelper helper, TypeSyntax typeName, SemanticModel semanticModel, OptionSet optionSet, bool isStylePreferred, ReportDiagnostic severity, CancellationToken cancellationToken) : this()
        {
            _helper = helper;
            _typeName = typeName;
            _semanticModel = semanticModel;
            _optionSet = optionSet;
            _cancellationToken = cancellationToken;

            IsStylePreferred = isStylePreferred;
            Severity = severity;
        }

        public bool CanConvert()
            => _helper.TryAnalyzeVariableDeclaration(_typeName, _semanticModel, _optionSet, _cancellationToken);
    }

    internal abstract partial class CSharpTypeStyleHelper
    {
        protected abstract bool IsStylePreferred(
            SemanticModel semanticModel, OptionSet optionSet, State state, CancellationToken cancellationToken);

        public virtual TypeStyleResult AnalyzeTypeName(
            TypeSyntax typeName, SemanticModel semanticModel,
            OptionSet optionSet, CancellationToken cancellationToken)
        {
            var declaration = typeName?.FirstAncestorOrSelf<SyntaxNode>(
                a => a.IsKind(SyntaxKind.DeclarationExpression, SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement));

            if (declaration == null)
            {
                return default;
            }

            var state = State.Generate(
                declaration, semanticModel, optionSet, cancellationToken);
            var isStylePreferred = this.IsStylePreferred(semanticModel, optionSet, state, cancellationToken);
            var severity = state.GetDiagnosticSeverityPreference();

            return new TypeStyleResult(
                this, typeName, semanticModel, optionSet, isStylePreferred, severity, cancellationToken);
        }

        internal abstract bool TryAnalyzeVariableDeclaration(
            TypeSyntax typeName, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken);

        protected abstract bool AssignmentSupportsStylePreference(SyntaxToken identifier, TypeSyntax typeName, ExpressionSyntax initializer, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken);

        internal TypeSyntax FindAnalyzableType(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            Debug.Assert(node.IsKind(SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement, SyntaxKind.DeclarationExpression));

            return node switch
            {
                VariableDeclarationSyntax variableDeclaration => ShouldAnalyzeVariableDeclaration(variableDeclaration, semanticModel, cancellationToken)
                    ? variableDeclaration.Type
                    : null,
                ForEachStatementSyntax forEachStatement => ShouldAnalyzeForEachStatement(forEachStatement, semanticModel, cancellationToken)
                    ? forEachStatement.Type
                    : null,
                DeclarationExpressionSyntax declarationExpression => ShouldAnalyzeDeclarationExpression(declarationExpression, semanticModel, cancellationToken)
                    ? declarationExpression.Type
                    : null,
                _ => null,
            };
        }

        protected virtual bool ShouldAnalyzeVariableDeclaration(VariableDeclarationSyntax variableDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // implicit type is applicable only for local variables and
            // such declarations cannot have multiple declarators and
            // must have an initializer.
            var isSupportedParentKind = variableDeclaration.IsParentKind(
                SyntaxKind.LocalDeclarationStatement,
                SyntaxKind.ForStatement,
                SyntaxKind.UsingStatement);

            return isSupportedParentKind &&
                variableDeclaration.Variables.Count == 1 &&
                variableDeclaration.Variables.Single().Initializer.IsKind(SyntaxKind.EqualsValueClause);
        }

        protected virtual bool ShouldAnalyzeForEachStatement(ForEachStatementSyntax forEachStatement, SemanticModel semanticModel, CancellationToken cancellationToken)
            => true;

        protected virtual bool ShouldAnalyzeDeclarationExpression(DeclarationExpressionSyntax declaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // Ensure that deconstruction assignment or foreach variable statement have a non-null deconstruct method.
            DeconstructionInfo? deconstructionInfoOpt = null;
            switch (declaration.Parent)
            {
                case AssignmentExpressionSyntax assignmentExpression:
                    if (assignmentExpression.IsDeconstruction())
                    {
                        deconstructionInfoOpt = semanticModel.GetDeconstructionInfo(assignmentExpression);
                    }

                    break;

                case ForEachVariableStatementSyntax forEachVariableStatement:
                    deconstructionInfoOpt = semanticModel.GetDeconstructionInfo(forEachVariableStatement);
                    break;
            }

            return !deconstructionInfoOpt.HasValue || !deconstructionInfoOpt.Value.Nested.IsEmpty || deconstructionInfoOpt.Value.Method != null;
        }
    }
}
