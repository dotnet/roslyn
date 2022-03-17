// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.Options;

internal interface IGlobalIdeOptionsProvider : ILanguageService
{
    SimplifierOptions GetSimplifierOptions(IGlobalOptionService globalOptions);
}
