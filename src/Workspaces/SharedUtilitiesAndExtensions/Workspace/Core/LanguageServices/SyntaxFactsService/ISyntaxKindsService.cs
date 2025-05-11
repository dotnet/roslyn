// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageService;

/// <summary>
/// Provides a uniform view of SyntaxKinds over C# and VB for constructs they have
/// in common.
/// </summary>
internal partial interface ISyntaxKindsService : ISyntaxKinds, ILanguageService
{
}
