// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.RawStringLiteral
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(nameof(RawStringLiteralCommandHandler))]
    [Order(After = nameof(SplitStringLiteralCommandHandler))]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal partial class RawStringLiteralCommandHandler(
        ITextUndoHistoryRegistry undoHistoryRegistry,
        IGlobalOptionService globalOptions,
        IEditorOperationsFactoryService editorOperationsFactoryService,
        EditorOptionsService editorOptionsService,
        IIndentationManagerService indentationManager)
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry = undoHistoryRegistry;
        private readonly IGlobalOptionService _globalOptions = globalOptions;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService = editorOperationsFactoryService;
        private readonly EditorOptionsService _editorOptionsService = editorOptionsService;
        private readonly IIndentationManagerService _indentationManager = indentationManager;

        public string DisplayName => CSharpEditorResources.Split_raw_string;
    }
}
