// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Tagger
{
    /// <remarks>
    /// These values appear in telemetry and should not be changed.
    /// </remarks>
    internal enum SyntacticClassificationMode
    {
        None = 0,
        /// <summary>Use TextMate for syntactic classification.</summary>
        TextMate = 1,
        /// <summary>Use the syntax-only for syntactic classification.</summary>
        SyntaxLsp = 2,
    }
}
