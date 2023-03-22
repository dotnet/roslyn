// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Classification;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SpellCheck;

namespace Microsoft.CodeAnalysis.CSharp.SpellCheck
{
    [ExportLanguageService(typeof(ISpellCheckSpanService), LanguageNames.CSharp), Shared]
    internal class CSharpSpellCheckSpanService : AbstractSpellCheckSpanService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSpellCheckSpanService()
        {
        }
    }
}
