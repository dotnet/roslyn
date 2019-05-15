// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Indicates kind of a <see cref="TextDocument"/>
    /// </summary>
    /// <remarks>CONSIDER: Make this type public</remarks>
    internal enum TextDocumentKind
    {
        /// <summary>
        /// Indicates a regular source <see cref="CodeAnalysis.Document"/>
        /// </summary>
        Document,

        /// <summary>
        /// Indicates an <see cref="CodeAnalysis.AdditionalDocument"/>
        /// </summary>
        AdditionalDocument,

        /// <summary>
        /// Indicates an <see cref="CodeAnalysis.AnalyzerConfigDocument"/>
        /// </summary>
        AnalyzerConfigDocument
    }
}
