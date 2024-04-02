// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.AddAnonymousTypeMemberName;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.AddAnonymousTypeMemberName;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddAnonymousTypeMemberName), Shared]
internal class CSharpAddAnonymousTypeMemberNameCodeFixProvider
    : AbstractAddAnonymousTypeMemberNameCodeFixProvider<
        ExpressionSyntax,
        AnonymousObjectCreationExpressionSyntax,
        AnonymousObjectMemberDeclaratorSyntax>
{
    private const string CS0746 = nameof(CS0746); // Invalid anonymous type member declarator. Anonymous type members must be declared with a member assignment, simple name or member access.

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpAddAnonymousTypeMemberNameCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [CS0746];

    protected override bool HasName(AnonymousObjectMemberDeclaratorSyntax declarator)
        => declarator.NameEquals != null;

    protected override ExpressionSyntax GetExpression(AnonymousObjectMemberDeclaratorSyntax declarator)
        => declarator.Expression;

    protected override AnonymousObjectMemberDeclaratorSyntax WithName(AnonymousObjectMemberDeclaratorSyntax declarator, SyntaxToken name)
        => declarator.WithNameEquals(
            SyntaxFactory.NameEquals(
                SyntaxFactory.IdentifierName(name)));

    protected override IEnumerable<string> GetAnonymousObjectMemberNames(AnonymousObjectCreationExpressionSyntax initializer)
        => initializer.Initializers.Where(i => i.NameEquals != null).Select(i => i.NameEquals!.Name.Identifier.ValueText);
}
