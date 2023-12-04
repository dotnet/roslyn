// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class CompilationUnitSyntaxExtensions
    {
        public static bool IsTopLevelProgram(this CompilationUnitSyntax compilationUnit)
            // Only need to check first member as having any other member type before a global statement is not legal.
            => compilationUnit.Members is [GlobalStatementSyntax, ..];
    }
}
