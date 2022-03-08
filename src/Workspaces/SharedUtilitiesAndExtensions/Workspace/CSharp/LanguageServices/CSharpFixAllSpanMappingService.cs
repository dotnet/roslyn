// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes
{
    [ExportLanguageService(typeof(IFixAllSpanMappingService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpFixAllSpanMappingService : AbstractFixAllSpanMappingService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpFixAllSpanMappingService()
        {
        }
    }
}
