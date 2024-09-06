// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Additional to predefined <see cref="FoldingRangeKind"/> folding range kinds.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#foldingRangeKind">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal static class VSFoldingRangeKind
    {
        /// <summary>
        /// Implementation folding range.
        /// </summary>
        /// <remarks>
        /// Implementation ranges are the blocks of code following a method/function definition. 
        /// They are used for commands such as the Visual Studio Collapse to Definition command, 
        /// which hides the implementation ranges and leaves only method definitions exposed.
        /// </remarks>
        public static FoldingRangeKind Implementation = new("implementation");
    }
}
