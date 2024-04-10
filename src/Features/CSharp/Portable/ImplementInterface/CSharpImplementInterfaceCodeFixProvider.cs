// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ImplementInterface;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ImplementInterface;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ImplementInterface), Shared]
[ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementAbstractClass)]
internal class CSharpImplementInterfaceCodeFixProvider : CodeFixProvider
{
    private readonly Func<TypeSyntax, bool> _interfaceName = n => n.Parent is BaseTypeSyntax && n.Parent.Parent is BaseListSyntax && ((BaseTypeSyntax)n.Parent).Type == n;

    private const string CS0535 = nameof(CS0535); // 'Program' does not implement interface member 'System.Collections.IEnumerable.GetEnumerator()'
    private const string CS0737 = nameof(CS0737); // 'Class' does not implement interface member 'IInterface.M()'. 'Class.M()' cannot implement an interface member because it is not public.
    private const string CS0738 = nameof(CS0738); // 'C' does not implement interface member 'I.Method1()'. 'B.Method1()' cannot implement 'I.Method1()' because it does not have the matching return type of 'void'.

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpImplementInterfaceCodeFixProvider()
    {
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [CS0535, CS0737, CS0738];

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var span = context.Span;
        var cancellationToken = context.CancellationToken;

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var token = root.FindToken(span.Start);
        if (!token.Span.IntersectsWith(span))
        {
            return;
        }

        var service = document.GetRequiredLanguageService<IImplementInterfaceService>();
        var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var actions = token.Parent.GetAncestorsOrThis<TypeSyntax>()
                                  .Where(_interfaceName)
                                  .Select(n => service.GetCodeActions(document, context.Options.GetImplementTypeGenerationOptions(document.Project.Services), model, n, cancellationToken))
                                  .FirstOrDefault(a => !a.IsEmpty);

        if (actions.IsDefaultOrEmpty)
            return;

        context.RegisterFixes(actions, context.Diagnostics);
    }

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;
}
