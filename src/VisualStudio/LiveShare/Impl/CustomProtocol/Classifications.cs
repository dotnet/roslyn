// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol
{
    // TODO - Move this to LSP extensions or elsewhere.
    // Temporary internal copy so we can implement these custom extensions
    // until these types are available elsewhere.
    internal class ClassificationParams
    {
        /// <summary>
        /// The document for which classification is requested.
        /// </summary>
        public TextDocumentIdentifier TextDocument { get; set; }

        /// <summary>
        /// The range for which classification is requested.
        /// </summary>
        public Range Range { get; set; }
    }

    internal class ClassificationSpan
    {
        /// <summary>
        /// The range being classified.
        /// </summary>
        public Range Range { get; set; }

        /// <summary>
        /// The classification of the span.
        /// </summary>
        public string Classification { get; set; }
    }
}
