// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ParameterSyntax
    {
        internal bool IsArgList
        {
            get
            {
                return this.Type == null && this.Identifier.ContextualKind() == SyntaxKind.ArgListKeyword;
            }
        }
    }
}
