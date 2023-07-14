// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

internal abstract class InstanceConstructorDeclarationBody(ConstructorDeclarationSyntax constructor) : MemberBody
{
    public ConstructorDeclarationSyntax Constructor
        => constructor;

    public SyntaxNode Body
        => (SyntaxNode?)constructor.Body ?? constructor.ExpressionBody?.Expression!;

    /// <summary>
    /// Active statement node for the implicit or explicit constructor initializer.
    /// Implicit initializer: [|public C()|] { }
    /// Explicit initializer: public C() : [|base(expr)|] { }
    /// </summary>
    public abstract SyntaxNode InitializerActiveStatement { get; }

    public sealed override SyntaxNode EncompassingAncestor
        => Constructor;

    public sealed override SyntaxTree SyntaxTree
        => constructor.SyntaxTree;

    public override OneOrMany<SyntaxNode> RootNodes
        => OneOrMany.Create<SyntaxNode>(Constructor);

    public sealed override SyntaxNode FindStatementAndPartner(TextSpan span, MemberBody? partnerDeclarationBody, out SyntaxNode? partnerStatement, out int statementPart)
    {
        var partnerCtorBody = (InstanceConstructorDeclarationBody?)partnerDeclarationBody;

        if (span.Start == InitializerActiveStatement.SpanStart)
        {
            statementPart = AbstractEditAndContinueAnalyzer.DefaultStatementPart;
            partnerStatement = partnerCtorBody?.InitializerActiveStatement;
            return InitializerActiveStatement;
        }

        if (Constructor.Initializer?.Span.Contains(span.Start) == true)
        {
            // Partner body does not have any non-trivial changes and thus the initializer is also present.
            Debug.Assert(partnerCtorBody == null || partnerCtorBody?.Constructor.Initializer != null);

            return CSharpEditAndContinueAnalyzer.FindStatementAndPartner(
                span,
                body: Constructor.Initializer,
                partnerBody: partnerCtorBody?.Constructor.Initializer,
                out partnerStatement,
                out statementPart);
        }

        return CSharpEditAndContinueAnalyzer.FindStatementAndPartner(
                span,
                body: Body,
                partnerBody: partnerCtorBody?.Body,
                out partnerStatement,
                out statementPart);
    }

    public sealed override StateMachineInfo GetStateMachineInfo()
        => new(IsAsync: false, IsIterator: false, HasSuspensionPoints: false);

    public sealed override Match<SyntaxNode> ComputeMatch(DeclarationBody newBody, IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>>? knownMatches)
        => SyntaxComparer.Statement.ComputeMatch(Constructor, ((InstanceConstructorDeclarationBody)newBody).Constructor, knownMatches);

    public sealed override bool TryMatchActiveStatement(DeclarationBody newBody, SyntaxNode oldStatement, int statementPart, [NotNullWhen(true)] out SyntaxNode? newStatement)
    {
        var newCtorBody = (InstanceConstructorDeclarationBody)newBody;

        if (oldStatement is (kind: SyntaxKind.ThisConstructorInitializer or SyntaxKind.BaseConstructorInitializer or SyntaxKind.ConstructorDeclaration))
        {
            newStatement = newCtorBody.Constructor.Initializer ?? (SyntaxNode)newCtorBody.Constructor;
            return true;
        }

        return CSharpEditAndContinueAnalyzer.TryMatchActiveStatement(Body, newCtorBody.Body, oldStatement, out newStatement);
    }
}
