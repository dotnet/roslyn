// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal static class VSTypeScriptCodeFixContextExtensions
    {
        public static bool IsBlocking(this CodeFixContext context)
            => ((ITypeScriptCodeFixContext)context).IsBlocking;
    }
}
