// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.AddImport;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle;

internal interface ICodeStyleOptionsStorage : ILanguageService
{
    IdeCodeStyleOptions GetOptions(IGlobalOptionService globalOptions);
}

internal static class CodeStyleOptionsStorage
{
    public static IdeCodeStyleOptions GetCodeStyleOptions(this IGlobalOptionService globalOptions, HostLanguageServices languageServices)
        => languageServices.GetRequiredService<ICodeStyleOptionsStorage>().GetOptions(globalOptions);
}
