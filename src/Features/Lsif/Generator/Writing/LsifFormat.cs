// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Lsif.Generator.Writing
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
