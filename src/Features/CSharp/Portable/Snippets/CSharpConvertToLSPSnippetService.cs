// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Snippets;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportLanguageService(typeof(ConvertToLSPSnippetService), LanguageNames.CSharp), Shared]
    internal class CSharpConvertToLSPSnippetService : ConvertToLSPSnippetService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpConvertToLSPSnippetService()
        {
        }
    }
}
