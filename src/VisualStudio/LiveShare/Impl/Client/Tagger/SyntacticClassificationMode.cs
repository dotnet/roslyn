// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
