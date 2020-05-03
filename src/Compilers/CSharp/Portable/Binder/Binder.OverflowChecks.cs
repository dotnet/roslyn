// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        protected enum OverflowChecks
        {
            /// <summary>
            /// Outside of <c>checked</c>, <c>unchecked</c> expression/block.
            /// </summary>
            Implicit,

            /// <summary>
            /// Within <c>unchecked</c> expression/block.
            /// </summary>
            Disabled,

            /// <summary>
            /// Within <c>checked</c> expression/block.
            /// </summary>
            Enabled
        }
    }
}
