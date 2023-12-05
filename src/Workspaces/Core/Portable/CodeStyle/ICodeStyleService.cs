// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeStyle;

internal interface ICodeStyleService : ILanguageService
{
    IdeCodeStyleOptions DefaultOptions { get; }
    IdeCodeStyleOptions GetIdeCodeStyleOptions(IOptionsReader options, IdeCodeStyleOptions? fallbackOptions);
}
