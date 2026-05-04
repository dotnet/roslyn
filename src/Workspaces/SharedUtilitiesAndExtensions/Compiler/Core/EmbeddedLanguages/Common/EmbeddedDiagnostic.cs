// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

/// <summary>
/// Represents an error in a embedded language snippet.  The error contains the message to show 
/// a user as well as the span of the error.  This span is in actual user character coordinates.
/// For example, if the user has the string "...\\p{0}..." then the span of the error would be 
/// for the range of characters for '\\p{0}' (even though the regex engine would only see the \\ 
/// translated as a virtual char to the single \ character.
/// </summary>
internal readonly record struct EmbeddedDiagnostic(string Message, TextSpan Span);
