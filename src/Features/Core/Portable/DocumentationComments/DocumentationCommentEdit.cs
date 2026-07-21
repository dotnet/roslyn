// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.DocumentationComments;

/// <summary>
/// A single edit produced by Copilot documentation generation: the span in the original text to replace
/// and the text to replace it with. This is the editor-agnostic counterpart of the editor's ProposedEdit,
/// so the generation core can be consumed both by the grey-text suggestion path and by the InlinePrompt
/// chip-accept path (which applies the edits directly to the buffer).
/// </summary>
internal readonly record struct DocumentationCommentEdit(TextSpan SpanToReplace, string ReplacementText);
