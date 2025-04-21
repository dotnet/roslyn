// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Commanding.Commands;

/// <summary>
/// Arguments for the Sort and Remove Unused Usings command being invoked.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class SortAndRemoveUnnecessaryImportsCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : EditorCommandArgs(textView, subjectBuffer)
{
}
