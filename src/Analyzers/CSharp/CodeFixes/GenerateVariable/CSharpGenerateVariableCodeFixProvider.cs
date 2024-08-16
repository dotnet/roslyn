// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.GenerateMember;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateVariable;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.GenerateVariable;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.GenerateVariable), Shared]
[ExtensionOrder(After = PredefinedCodeFixProviderNames.GenerateMethod)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpGenerateVariableCodeFixProvider() : AbstractGenerateMemberCodeFixProvider
{
    private const string CS1061 = nameof(CS1061); // error CS1061: 'C' does not contain a definition for 'Goo' and no extension method 'Goo' accepting a first argument of type 'C' could be found
    private const string CS0103 = nameof(CS0103); // error CS0103: The name 'Goo' does not exist in the current context
    private const string CS0117 = nameof(CS0117); // error CS0117: 'TestNs.Program' does not contain a definition for 'blah'
    private const string CS0539 = nameof(CS0539); // error CS0539: 'Class.SomeProp' in explicit interface declaration is not a member of interface
    private const string CS0246 = nameof(CS0246); // error CS0246: The type or namespace name 'Version' could not be found
    private const string CS0120 = nameof(CS0120); // error CS0120: An object reference is required for the non-static field, method, or property 'A'
    private const string CS0118 = nameof(CS0118); // error CS0118: 'C' is a type but is used like a variable

    public override ImmutableArray<string> FixableDiagnosticIds
        => [CS1061, CS0103, CS0117, CS0539, CS0246, CS0120, CS0118];

    protected override bool IsCandidate(SyntaxNode node, SyntaxToken token, Diagnostic diagnostic)
        => node is SimpleNameSyntax or PropertyDeclarationSyntax or MemberBindingExpressionSyntax;

    protected override SyntaxNode GetTargetNode(SyntaxNode node)
    {
        if (node.IsKind(SyntaxKind.MemberBindingExpression))
        {
            var nameNode = node.ChildNodes().FirstOrDefault(n => n.IsKind(SyntaxKind.IdentifierName));
            if (nameNode != null)
            {
                return nameNode;
            }
        }

        return base.GetTargetNode(node);
    }

    protected override Task<ImmutableArray<CodeAction>> GetCodeActionsAsync(
        Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var service = document.GetLanguageService<IGenerateVariableService>();
        return service.GenerateVariableAsync(document, node, cancellationToken);
    }
}
