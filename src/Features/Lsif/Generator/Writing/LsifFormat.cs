// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Writing
{
    internal enum LsifFormat
    {
        /// <summary>
        /// Line format, where each line is a JSON object.
        /// </summary>
        Line,

        /// <summary>
        /// JSON format, where the entire output is a single JSON array.
        /// </summary>
        Json
    }
}
