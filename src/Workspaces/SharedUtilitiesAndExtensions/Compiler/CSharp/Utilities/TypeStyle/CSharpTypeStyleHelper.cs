// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Utilities
{
    internal readonly struct TypeStyleResult
    {
        private readonly CSharpTypeStyleHelper _helper;
        private readonly TypeSyntax _typeName;
        private readonly SemanticModel _semanticModel;
        private readonly CSharpSimplifierOptions _options;
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
        public readonly NotificationOption2 Notification;

        public TypeStyleResult(CSharpTypeStyleHelper helper, TypeSyntax typeName, SemanticModel semanticModel, CSharpSimplifierOptions options, bool isStylePreferred, NotificationOption2 notificationOption, CancellationToken cancellationToken) : this()
        {
            _helper = helper;
            _typeName = typeName;
            _semanticModel = semanticModel;
            _options = options;
            _cancellationToken = cancellationToken;

            IsStylePreferred = isStylePreferred;
            Notification = notificationOption;
        }

        public bool CanConvert()
            => _helper.TryAnalyzeVariableDeclaration(_typeName, _semanticModel, _options, _cancellationToken);
    }

    internal abstract partial class CSharpTypeStyleHelper
    {
        protected abstract bool IsStylePreferred(in State state);

        public virtual TypeStyleResult AnalyzeTypeName(
            TypeSyntax typeName, SemanticModel semanticModel,
            CSharpSimplifierOptions options, CancellationToken cancellationToken)
        {
            if (typeName?.FirstAncestorOrSelf<SyntaxNode>(a => a.Kind() is SyntaxKind.DeclarationExpression or SyntaxKind.VariableDeclaration or SyntaxKind.ForEachStatement) is not { } declaration)
            {
                return default;
            }

            var state = new State(
                declaration, semanticModel, options, cancellationToken);
            var isStylePreferred = this.IsStylePreferred(in state);
            var notificationOption = state.GetDiagnosticSeverityPreference();

            return new TypeStyleResult(
                this, typeName, semanticModel, options, isStylePreferred, notificationOption, cancellationToken);
        }

        internal abstract bool TryAnalyzeVariableDeclaration(
            TypeSyntax typeName, SemanticModel semanticModel, CSharpSimplifierOptions options, CancellationToken cancellationToken);

        protected abstract bool AssignmentSupportsStylePreference(
            SyntaxToken identifier, TypeSyntax typeName, ExpressionSyntax initializer, SemanticModel semanticModel, CSharpSimplifierOptions options, CancellationToken cancellationToken);

        internal TypeSyntax? FindAnalyzableType(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            Debug.Assert(node.Kind() is SyntaxKind.VariableDeclaration or SyntaxKind.ForEachStatement or SyntaxKind.DeclarationExpression);

            return node switch
            {
                VariableDeclarationSyntax variableDeclaration => ShouldAnalyzeVariableDeclaration(variableDeclaration, cancellationToken)
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

        public virtual bool ShouldAnalyzeVariableDeclaration(VariableDeclarationSyntax variableDeclaration, CancellationToken cancellationToken)
        {
            // implicit type is applicable only for local variables and
            // such declarations cannot have multiple declarators and
            // must have an initializer.
            var isSupportedParentKind = variableDeclaration.Parent is (kind:
                SyntaxKind.LocalDeclarationStatement or
                SyntaxKind.ForStatement or
                SyntaxKind.UsingStatement);

            return isSupportedParentKind &&
                variableDeclaration.Variables is [{ Initializer: not null }];
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
