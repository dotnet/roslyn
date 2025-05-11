// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Utilities;

internal interface IParseOptionsChangingService : ILanguageService
{
    bool CanApplyChange(ParseOptions oldOptions, ParseOptions newOptions, string? maxSupportedLangVersion);

    void Apply(ParseOptions options, ProjectPropertyStorage storage);
}
