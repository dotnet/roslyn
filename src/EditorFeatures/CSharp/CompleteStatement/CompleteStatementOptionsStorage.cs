// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement
{
    internal static class CompleteStatementOptionsStorage
    {
        public static readonly Option2<bool> AutomaticallyCompleteStatementOnSemicolon = new("csharp_complete_statement_on_semicolon", defaultValue: true);
    }
}
