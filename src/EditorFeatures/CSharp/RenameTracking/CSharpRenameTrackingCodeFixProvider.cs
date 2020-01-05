// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor.CSharp.RenameTracking
{
    // TODO: Remove the ExtensionOrder attributes once a better ordering mechanism is available

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RenameTracking), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddImport)]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.AddMissingReference)]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.FullyQualify)]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.FixIncorrectExitContinue)]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.GenerateConstructor)]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.GenerateEndConstruct)]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.GenerateEnumMember)]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.GenerateEvent)]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.GenerateVariable)]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.GenerateMethod)]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.GenerateType)]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.ImplementAbstractClass)]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.ImplementInterface)]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.MoveToTopOfFile)]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.RemoveUnnecessaryImports)]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.SimplifyNames)]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.SpellCheck)]
    internal sealed class CSharpRenameTrackingCodeFixProvider : AbstractRenameTrackingCodeFixProvider
    {
        [ImportingConstructor]
        public CSharpRenameTrackingCodeFixProvider(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            [ImportMany] IEnumerable<IRefactorNotifyService> refactorNotifyServices)
            : base(undoHistoryRegistry, refactorNotifyServices)
        {
        }
    }
}
