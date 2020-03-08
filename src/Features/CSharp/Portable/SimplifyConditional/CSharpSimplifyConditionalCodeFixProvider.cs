// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.SimplifyConditional;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyConditional
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpSimplifyConditionalCodeFixProvider :
        AbstractSimplifyConditionalCodeFixProvider
    {
        [ImportingConstructor]
        public CSharpSimplifyConditionalCodeFixProvider()
        {
        }
    }
}
