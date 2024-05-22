// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.FileHeaders;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.FileHeaders;

/// <summary>
/// Implements a code fix for file header diagnostics.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.FileHeader)]
[Shared]
internal class CSharpFileHeaderCodeFixProvider : AbstractFileHeaderCodeFixProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpFileHeaderCodeFixProvider()
    {
    }

    protected override AbstractFileHeaderHelper FileHeaderHelper => CSharpFileHeaderHelper.Instance;
}
