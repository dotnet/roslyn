// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;

namespace Microsoft.CodeAnalysis.CSharp.InlineHints
{
    /// <summary>
    /// The service to locate all positions where inline hints should be placed.
    /// </summary>
    [ExportLanguageService(typeof(IInlineHintsService), LanguageNames.CSharp), Shared]
    internal class CSharpInlineHintsService : AbstractInlineHintsService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpInlineHintsService()
        {
        }
    }
}
