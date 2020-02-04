// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    [ExportLanguageService(typeof(SyntaxGenerator), LanguageNames.CSharp), Shared]
    internal partial class CSharpSyntaxGenerator : SyntaxGenerator
    {
        [ImportingConstructor]
        public CSharpSyntaxGenerator()
        {
        }
    }
}
