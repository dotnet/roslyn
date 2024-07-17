// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.GenerateMember;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.GenerateConstructor;

/// <summary>
/// This <see cref="CodeFixProvider"/> gives users a way to generate constructors for an existing
/// type when a user tries to 'new' up an instance of that type with a set of parameter that does
/// not match any existing constructor.  i.e. it is the equivalent of 'Generate-Method' but for
/// constructors.  Parameters for the constructor will be picked in a manner similar to Generate-
/// Method.  However, this type will also attempt to hook up those parameters to existing fields
/// and properties, or pass them to a this/base constructor if available.
/// 
/// Importantly, this type is not responsible for generating constructors for a type based on 
/// the user selecting some fields/properties of that type.  Nor is it responsible for generating
/// derived class constructors for all unmatched base class constructors in a type hierarchy.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.GenerateConstructor), Shared]
[ExtensionOrder(After = PredefinedCodeFixProviderNames.FullyQualify)]
internal class GenerateConstructorCodeFixProvider : AbstractGenerateMemberCodeFixProvider
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public GenerateConstructorCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds => GenerateConstructorDiagnosticIds.AllDiagnosticIds;

    protected override Task<ImmutableArray<CodeAction>> GetCodeActionsAsync(
        Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var service = document.GetLanguageService<IGenerateConstructorService>();
        return service.GenerateConstructorAsync(document, node, cancellationToken);
    }

    protected override bool IsCandidate(SyntaxNode node, SyntaxToken token, Diagnostic diagnostic)
    {
        return node is BaseObjectCreationExpressionSyntax or
               ConstructorInitializerSyntax or
               AttributeSyntax;
    }

    protected override SyntaxNode GetTargetNode(SyntaxNode node)
    {
        switch (node)
        {
            case ObjectCreationExpressionSyntax objectCreationNode:
                return objectCreationNode.Type.GetRightmostName();
            case AttributeSyntax attributeNode:
                return attributeNode.Name;
        }

        return node;
    }
}
