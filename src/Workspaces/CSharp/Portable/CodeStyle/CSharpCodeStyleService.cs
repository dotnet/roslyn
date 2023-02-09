// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle;

[ExportLanguageService(typeof(ICodeStyleService), LanguageNames.CSharp), Shared]
internal sealed class CSharpCodeStyleService : ICodeStyleService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpCodeStyleService()
    {
    }

    public IdeCodeStyleOptions DefaultOptions
        => CSharpIdeCodeStyleOptions.Default;

    public IdeCodeStyleOptions GetIdeCodeStyleOptions(IOptionsReader options, IdeCodeStyleOptions? fallbackOptions)
        => new CSharpIdeCodeStyleOptions(options, (CSharpIdeCodeStyleOptions?)fallbackOptions);
}
