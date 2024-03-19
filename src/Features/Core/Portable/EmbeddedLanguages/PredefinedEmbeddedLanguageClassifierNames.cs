// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

internal static class PredefinedEmbeddedLanguageNames
{
    public const string Regex = nameof(Regex);

    public const string Json = nameof(Json);

    public const string CSharpTest = $"{LanguageNames.CSharp}-Test";
}
