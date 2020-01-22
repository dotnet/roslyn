// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames
{
    internal class TypeSyntaxSimplifierWalker : CSharpSyntaxWalker
    {
        private readonly CSharpSimplifyTypeNamesDiagnosticAnalyzer _analyzer;
        private readonly SemanticModel _semanticModel;
        private readonly OptionSet _optionSet;
        private readonly CancellationToken _cancellationToken;

        public List<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();

        public TypeSyntaxSimplifierWalker(CSharpSimplifyTypeNamesDiagnosticAnalyzer analyzer, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken)
            : base(SyntaxWalkerDepth.StructuredTrivia)
        {
            _analyzer = analyzer;
            _semanticModel = semanticModel;
            _optionSet = optionSet;
            _cancellationToken = cancellationToken;
        }

        public override void VisitQualifiedName(QualifiedNameSyntax node)
        {
            if (node.IsKind(SyntaxKind.QualifiedName) && TrySimplify(node))
            {
                // found a match. report it and stop processing.
                return;
            }

            // descend further.
            DefaultVisit(node);
        }

        public override void VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
        {
            if (node.IsKind(SyntaxKind.AliasQualifiedName) && TrySimplify(node))
            {
                // found a match. report it and stop processing.
                return;
            }

            // descend further.
            DefaultVisit(node);
        }

        public override void VisitGenericName(GenericNameSyntax node)
        {
            if (node.IsKind(SyntaxKind.GenericName) && TrySimplify(node))
            {
                // found a match. report it and stop processing.
                return;
            }

            // descend further.
            DefaultVisit(node);
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            // Always try to simplify identifiers with an 'Attribute' suffix.
            //
            // In other cases, don't bother looking at the right side of A.B or A::B. We will process those in
            // one of our other top level Visit methods (like VisitQualifiedName).
            var canTrySimplify = node.Identifier.ValueText!.EndsWith("Attribute", StringComparison.Ordinal)
                || !node.IsRightSideOfDotOrArrowOrColonColon();

            if (canTrySimplify && TrySimplify(node))
            {
                // found a match. report it and stop processing.
                return;
            }

            // descend further.
            DefaultVisit(node);
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression) && TrySimplify(node))
            {
                // found a match. report it and stop processing.
                return;
            }

            // descend further.
            DefaultVisit(node);
        }

        public override void VisitQualifiedCref(QualifiedCrefSyntax node)
        {
            // First, just try to simplify the top-most qualified-cref alone. If we're able to do
            // this, then there's no need to process it's container.  i.e.
            //
            // if we have <see cref="A.B.C"/> and we simplify that to <see cref="C"/> there's no
            // point looking at `A.B`.
            if (node.IsKind(SyntaxKind.QualifiedCref) && TrySimplify(node))
            {
                // found a match on the qualified cref itself. report it and keep processing.
            }
            else
            {
                // couldn't simplify the qualified cref itself.  descend into the container portion
                // as that might have portions that can be simplified.
                Visit(node.Container);
            }

            // unilaterally process the member portion of the qualified cref.  These may have things
            // like parameters that could be simplified.  i.e. if we have:
            //
            //      <see cref="A.B.C(X.Y)"/>
            //
            // We can simplify both the qualified portion to just `C` and we can simplify the
            // parameter to just `Y`.
            Visit(node.Member);
        }

        /// <summary>
        /// This is the root helper that all other TrySimplify methods in this type must call
        /// through once they think there is a good chance something is simplifiable.  It does the
        /// work of actually going through the real simplification system to validate that the
        /// simplification is legal and does not affect semantics.
        /// </summary>
        private bool TrySimplify(SyntaxNode node)
        {
            if (!_analyzer.TrySimplify(_semanticModel, node, out var diagnostic, _optionSet, _cancellationToken))
                return false;

            Diagnostics.Add(diagnostic);
            return true;
        }
    }
}
