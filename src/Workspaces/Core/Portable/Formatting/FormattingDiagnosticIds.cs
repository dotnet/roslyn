// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Formatting
{
    internal static class FormattingDiagnosticIds
    {
        /// <summary>
        /// This is the ID reported for formatting diagnostics.
        /// </summary>
        public const string FormattingDiagnosticId = "IDE0055";

        /// <summary>
        /// This spacial diagnostic can be suppressed via <c>#pragma</c> to prevent the formatter from making changes to
        /// code formatting within the span where the diagnostic is suppressed.
        /// </summary>
        public const string FormatDocumentControlDiagnosticId = "format";
    }
}
