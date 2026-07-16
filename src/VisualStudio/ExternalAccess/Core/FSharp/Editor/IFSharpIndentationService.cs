// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;

/// <summary>
/// An indentation result represents where the indent should be placed.  It conveys this through
/// a pair of values.  A position in the existing document where the indent should be relative,
/// and the number of columns after that the indent should be placed at.  
/// 
/// This pairing provides flexibility to the implementor to compute the indentation results in
/// a variety of ways.  For example, one implementation may wish to express indentation of a 
/// newline as being four columns past the start of the first token on a previous line.  Another
/// may wish to simply express the indentation as an absolute amount from the start of the 
/// current line.  With this tuple, both forms can be expressed, and the implementor does not
/// have to convert from one to the other.
/// </summary>
internal readonly struct FSharpIndentationResult
{
    /// <summary>
    /// The base position in the document that the indent should be relative to.  This position
    /// can occur on any line (including the current line, or a previous line).
    /// </summary>
    public int BasePosition { get; }

    /// <summary>
    /// The number of columns the indent should be at relative to the BasePosition's column.
    /// </summary>
    public int Offset { get; }

    public FSharpIndentationResult(int basePosition, int offset)
    {
        BasePosition = basePosition;
        Offset = offset;
    }
}

[Obsolete("Use IFSharpIndentationService instead")]
internal interface IFSharpSynchronousIndentationService
{
    /// <summary>
    /// Determines the desired indentation of a given line.  May return <see langword="null"/> if the
    /// <paramref name="document"/> does not want any sort of automatic indentation.  May also return
    /// <see langword="null"/> if the line in question is not blank and thus indentation should
    /// be deferred to the formatting command handler to handle.
    /// </summary>
    FSharpIndentationResult? GetDesiredIndentation(Document document, int lineNumber, CancellationToken cancellationToken);
}

internal interface IFSharpIndentationService
{
    /// <summary>
    /// Determines the desired indentation of a given line.  May return <see langword="null"/> if the
    /// no automatic indentation is desired.  May also return <see langword="null"/> if the line in
    /// question is not blank and thus indentation should be deferred to the formatting command handler to handle.
    /// </summary>
    FSharpIndentationResult? GetDesiredIndentation(HostLanguageServices services, SourceText text, DocumentId documentId, string path, int lineNumber, FSharpIndentationOptions options);
}

internal readonly record struct FSharpIndentationOptions(int TabSize, FormattingOptions.IndentStyle IndentStyle);
