// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;

namespace Microsoft.CodeAnalysis.Editor;

/// <summary>
/// Command handler names
/// </summary>
internal static class PredefinedCommandHandlerNames
{
    /// <summary>
    /// Command handler name for Automatic Pair Completion
    /// </summary>
    /// <remarks></remarks>
    public const string AutomaticCompletion = "Automatic Pair Completion Command Handler";

    /// <summary>
    /// Command handler name for Automatic Line Ender
    /// </summary>
    /// <remarks></remarks>
    public const string AutomaticLineEnder = "Automatic Line Ender Command Handler";

    /// <summary>
    /// Command handler name for Change Signature.
    /// </summary>
    public const string ChangeSignature = "Change Signature";

    /// <summary>
    /// Command handler name for Class View.
    /// </summary>
    public const string ClassView = "Class View";

    /// <summary>
    /// Command handler name for Comment Selection.
    /// </summary>
    /// <remarks></remarks>
    public const string CommentSelection = "Comment Selection Command Handler";

    /// <summary>
    /// Command handler name for Commit.
    /// </summary>
    /// <remarks></remarks>
    public const string Commit = "Commit Command Handler";

    /// <summary>
    /// Command handler name for Documentation Comments.
    /// </summary>
    public const string DocumentationComments = "Documentation Comments Command Handler";

    /// <summary>
    /// Command handler name for Encapsulate Field.
    /// </summary>
    public const string EncapsulateField = nameof(EncapsulateField);

    /// <summary>
    /// Command handler name for End Construct.
    /// </summary>
    public const string EndConstruct = "End Construct Command Handler";

    /// <summary>
    /// Command handler name for Event Hookup.
    /// </summary>
    public const string EventHookup = "Event Hookup Command Handler";

    /// <summary>
    /// Command handler name for Extract Interface
    /// </summary>
    public const string ExtractInterface = "Extract Interface Command Handler";

    /// <summary>
    /// Command handler name for Extract Method
    /// </summary>
    public const string ExtractMethod = "Extract Method Command Handler";

    /// <summary>
    /// Command handler name for Find References.
    /// </summary>
    public const string FindReferences = "Find References Command Handler";

    /// <summary>
    /// Command handler name for Format Document.
    /// </summary>
    public const string FormatDocument = "Format Document Command Handler";

    /// <summary>
    /// Command handler name for Go to Base.
    /// </summary>
    public const string GoToBase = "Go To Base Command Handler";

    /// <summary>
    /// Command handler name for Go to Definition.
    /// </summary>
    public const string GoToDefinition = "Go To Definition Command Handler";

    /// <summary>
    /// Command handler name for Go to Implementation.
    /// </summary>
    public const string GoToImplementation = "Go To Implementation Command Handler";

    /// <summary>
    /// Command handler name for Go to Adjacent Member.
    /// </summary>
    public const string GoToAdjacentMember = "Go To Adjacent Member Command Handler";

    /// <summary>
    /// Command handler name for Indent.
    /// </summary>
    public const string Indent = "Indent Command Handler";

    /// <summary>
    /// Command handler name for Navigate to Highlighted Reference.
    /// </summary>
    public const string NavigateToHighlightedReference = "Navigate to Highlighted Reference Command Handler";

    /// <summary>
    /// Command handler name for Organize Document.
    /// </summary>
    public const string OrganizeDocument = "Organize Document Command Handler";

    /// <summary>
    /// Command handler name for Quick Info.
    /// </summary>
    public const string QuickInfo = "Quick Info Command Handler";

    /// <summary>
    /// Command handler name for Rename.
    /// </summary>
    public const string Rename = "Rename Command Handler";

    /// <summary>
    /// Command handler name for Rename Tracking cancellation.
    /// </summary>
    public const string RenameTrackingCancellation = "Rename Tracking Cancellation Command Handler";

    /// <summary>
    /// Command handler name for a Signature Help command handler executing before <see cref="PredefinedCompletionNames.CompletionCommandHandler"/>.
    /// </summary>
    public const string SignatureHelpBeforeCompletion = "Signature Help Before Completion Command Handler";

    /// <summary>
    /// Command handler name for a Signature Help command handler executing after <see cref="PredefinedCompletionNames.CompletionCommandHandler"/>.
    /// </summary>
    public const string SignatureHelpAfterCompletion = "Signature Help After Completion Command Handler";

    /// <summary>
    /// Command handler name for String Copy Paste.
    /// </summary>
    public const string StringCopyPaste = "String Copy Paste";

    /// <summary>
    /// Command handler name for Toggle Block Comments.
    /// </summary>
    /// <remarks></remarks>
    public const string ToggleBlockComment = "Toggle Block Comment Command Handler";

    /// <summary>
    /// Command handler name for Toggle Line Comments.
    /// </summary>
    /// <remarks></remarks>
    public const string ToggleLineComment = "Toggle Line Comment Command Handler";

    /// <summary>
    /// Command handler name for Paste Content in Interactive Format. 
    /// </summary>
    public const string InteractivePaste = "Interactive Paste Command Handler";

    /// <summary>
    /// Command handler name for Paste in Paste Tracking.
    /// </summary>
    public const string PasteTrackingPaste = "Paste Tracking Paste Command Handler";

    /// <summary>
    /// Command handler name for Paste in Add Imports.
    /// </summary>
    public const string AddImportsPaste = "Add Imports Paste Command Handler";

    /// <summary>
    /// Command handler name for Edit and Continue file save handler.
    /// </summary>
    public const string EditAndContinueFileSave = "Edit and Continue Save File Handler";

    /// <summary>
    /// Command handler name for showing the Value Tracking tool window.
    /// </summary>
    public const string ShowValueTracking = "Show Value Tracking";

    /// <summary>
    /// Command handler name for showing the Callstack Explorer tool window.
    /// </summary>
    public const string ShowCallstackExplorer = "Show Callstack Explorer";

    /// <summary>
    /// Command handler name for LSP Go To Definition.
    /// This name is agreed upon externally and cannot be changed.
    /// </summary>
    public const string LspGoToDefinition = "LSP GoToDefinitionCommandHandler";

    /// <summary>
    /// Command handler name for LSP Find References.
    /// This name is agreed upon externally and cannot be changed.
    /// </summary>
    public const string LspFindReferences = "LSP FindReferenceCommandHandler";

    /// <summary>
    /// Command handler name for LSP Go To Implementation.
    /// This name is agreed upon externally and cannot be changed.
    /// </summary>
    public const string LspGoToImplementation = "LSP GoToImplementationCommandHandler";
}
