// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Structure
{
    internal static class OmniSharpBlockTypes
    {
        // Basic types.
        public static string Nonstructural => BlockTypes.Nonstructural;

        // Trivstatic 
        public static string Comment => BlockTypes.Comment;
        public static string PreprocessorRegion => BlockTypes.PreprocessorRegion;

        // Top static declarations.
        public static string Imports => BlockTypes.Imports;
        public static string Namespace => BlockTypes.Namespace;
        public static string Type => BlockTypes.Type;
        public static string Member => BlockTypes.Member;

        // Statstatic  and expressions.
        public static string Statement => BlockTypes.Statement;
        public static string Conditional => BlockTypes.Conditional;
        public static string Loop => BlockTypes.Loop;

        public static string Expression => BlockTypes.Expression;
    }
}
