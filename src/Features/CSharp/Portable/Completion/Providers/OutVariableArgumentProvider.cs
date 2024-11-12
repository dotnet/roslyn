// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportArgumentProvider(nameof(OutVariableArgumentProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(ContextVariableArgumentProvider))]
[Shared]
internal sealed class OutVariableArgumentProvider : ArgumentProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public OutVariableArgumentProvider()
    {
    }

    public override Task ProvideArgumentAsync(ArgumentContext context)
    {
        if (context.PreviousValue is not null)
        {
            // This argument provider does not attempt to replace arguments already in code.
            return Task.CompletedTask;
        }

        if (context.Parameter.RefKind != RefKind.Out)
        {
            // This argument provider only considers 'out' parameters.
            return Task.CompletedTask;
        }

        // Since tihs provider runs after ContextVariableArgumentProvider, we know there is no suitable target in
        // the current context. Instead, offer to declare a new variable.
        var name = context.Parameter.Name;
        if (SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None
            || SyntaxFacts.GetContextualKeywordKind(name) != SyntaxKind.None)
        {
            name = "@" + name;
        }

        var syntax = Argument(
            nameColon: null,
            refKindKeyword: OutKeyword,
            DeclarationExpression(
                type: IdentifierName("var"),
                designation: SingleVariableDesignation(Identifier(
                    [],
                    contextualKind: SyntaxKind.None,
                    text: name,
                    valueText: context.Parameter.Name,
                    []))));

        context.DefaultValue = syntax.NormalizeWhitespace().ToFullString();
        return Task.CompletedTask;
    }
}
