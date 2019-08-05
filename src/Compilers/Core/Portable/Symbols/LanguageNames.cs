// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
