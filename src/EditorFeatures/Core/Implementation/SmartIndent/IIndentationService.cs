// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor
{
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
    [Obsolete("Use Microsoft.CodeAnalysis.Indentation.IndentationResult instead.")]
    internal struct IndentationResult
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

        public IndentationResult(int basePosition, int offset) : this()
        {
            this.BasePosition = basePosition;
            this.Offset = offset;
        }

        public static implicit operator Indentation.IndentationResult(IndentationResult result)
            => new Indentation.IndentationResult(result.BasePosition, result.Offset);
    }

    // Removal of this interface tracked with https://github.com/dotnet/roslyn/issues/35872
    [Obsolete("Use Microsoft.CodeAnalysis.Indentation.IIndentationService instead.")]
    internal interface IIndentationService : ILanguageService
    {
        Task<IndentationResult?> GetDesiredIndentation(Document document, int lineNumber, CancellationToken cancellationToken);
    }

    // Removal of this interface tracked with https://github.com/dotnet/roslyn/issues/35872
    [Obsolete("Use Microsoft.CodeAnalysis.Indentation.IIndentationService instead.")]
    internal interface ISynchronousIndentationService : ILanguageService
    {
        /// <summary>
        /// Determines the desired indentation of a given line.  May return <see langword="null"/> if the
        /// <paramref name="document"/> does not want any sort of automatic indentation.  May also return
        /// <see langword="null"/> if the line in question is not blank and thus indentation should
        /// be deferred to the formatting command handler to handle.
        /// </summary>
        IndentationResult? GetDesiredIndentation(Document document, int lineNumber, CancellationToken cancellationToken);
    }
}
