// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    /// <summary>
    /// Exception indicates a text span in the syntax tree is renamed by using two different <see cref="LocationRenameContext"/>.
    /// </summary>
    internal class LocationRenameContextOverlappingException : Exception
    {
        public TextSpan TextSpan { get; }
        public DocumentId DocumentId { get; }
        public LocationRenameContext FirstLocationRenameContext { get; }
        public LocationRenameContext SecondLocationRenameContext { get; }
        public override string Message => $"{TextSpan} of document: {DocumentId} is renamed by using {FirstLocationRenameContext} and {SecondLocationRenameContext}.";

        public LocationRenameContextOverlappingException(
            TextSpan textSpan,
            DocumentId documentId,
            LocationRenameContext firstLocationRenameContext,
            LocationRenameContext secondLocationRenameContext)
        {
            TextSpan = textSpan;
            DocumentId = documentId;
            FirstLocationRenameContext = firstLocationRenameContext;
            SecondLocationRenameContext = secondLocationRenameContext;
        }
    }
}
