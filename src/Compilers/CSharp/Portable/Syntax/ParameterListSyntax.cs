// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ParameterListSyntax
    {
        internal int ParameterCount
        {
            get
            {
                int count = 0;
                foreach (ParameterSyntax parameter in this.Parameters)
                {
                    // __arglist does not affect the parameter count.
                    if (!parameter.IsArgList)
                    {
                        count++;
                    }
                }
                return count;
            }
        }
    }
}
