// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.Completion.FileSystem;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.InteractiveWindow;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Completion.FileSystem
{
    [ExportCompletionProvider(
        nameof(LoadDirectiveCompletionProvider),
        LanguageNames.CSharp,
        // Using TextViewRole here is a temporary work-around to prevent this component from being loaded in
        // regular C# contexts.  We will need to remove this and implement a new "CSharp Script" Content type
        // in order to fix #load completion in .csx files (https://github.com/dotnet/roslyn/issues/5325).
        Roles = new[] { PredefinedInteractiveTextViewRoles.InteractiveTextViewRole })]
    [ExtensionOrder(After = nameof(FunctionPointerUnmanagedCallingConventionCompletionProvider))]
    [Shared]
    internal sealed class LoadDirectiveCompletionProvider : AbstractLoadDirectiveCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LoadDirectiveCompletionProvider()
        {
        }

        protected override bool TryGetStringLiteralToken(SyntaxTree tree, int position, out SyntaxToken stringLiteral, CancellationToken cancellationToken)
            => DirectiveCompletionProviderUtilities.TryGetStringLiteralToken(tree, position, SyntaxKind.LoadDirectiveTrivia, out stringLiteral, cancellationToken);
    }
}
