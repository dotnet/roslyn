// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.AddAnonymousTypeMemberName;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.AddAnonymousTypeMemberName
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpAddAnonymousTypeMemberNameCodeFixProvider
        : AbstractAddAnonymousTypeMemberNameCodeFixProvider<
            ExpressionSyntax,
            AnonymousObjectCreationExpressionSyntax,
            AnonymousObjectMemberDeclaratorSyntax>
    {
        private const string CS0746 = nameof(CS0746); // Invalid anonymous type member declarator. Anonymous type members must be declared with a member assignment, simple name or member access.

        [ImportingConstructor]
        public CSharpAddAnonymousTypeMemberNameCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(CS0746);

        protected override bool HasName(AnonymousObjectMemberDeclaratorSyntax declarator)
            => declarator.NameEquals != null;

        protected override ExpressionSyntax GetExpression(AnonymousObjectMemberDeclaratorSyntax declarator)
            => declarator.Expression;

        protected override AnonymousObjectMemberDeclaratorSyntax WithName(AnonymousObjectMemberDeclaratorSyntax declarator, SyntaxToken name)
            => declarator.WithNameEquals(
                SyntaxFactory.NameEquals(
                    SyntaxFactory.IdentifierName(name)));

        protected override IEnumerable<string> GetAnonymousObjectMemberNames(AnonymousObjectCreationExpressionSyntax initializer)
            => initializer.Initializers.Where(i => i.NameEquals != null).Select(i => i.NameEquals.Name.Identifier.ValueText);
    }
}
