// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    /// <summary>
    /// Contains values suitable for populating System.Runtime.CompilerServices.InterceptsLocationAttribute for a given call.
    /// </summary>
    [RequiresPreviewFeatures]
    public struct InterceptableLocation
    {
        internal InterceptableLocation(string filePath, int lineNumber, int characterNumber)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
            CharacterNumber = characterNumber;
        }

        public string FilePath { get; }

        /// <summary>
        /// The 1-indexed line number.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// The 1-indexed character number.
        /// </summary>
        public int CharacterNumber { get; }
    }
}
