// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A class that provides constants for common language names.
    /// </summary>
    public static class LanguageNames
    {
        /// <summary>
        /// The common name used for the C# language.
        /// </summary>
        public const string CSharp = "C#";

        /// <summary>
        /// The common name used for the Visual Basic language.
        /// </summary>
        public const string VisualBasic = "Visual Basic";

        /// <summary>
        /// The common name used for the F# language.
        /// </summary>
        /// <remarks>
        /// F# is not a supported compile target for the Roslyn compiler.
        /// </remarks>
        public const string FSharp = "F#";
    }
}
