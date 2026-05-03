// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Structure;

internal static class XamlStructureTypes
{
    // Trivia
    public const string Comment = nameof(Comment);
    public const string Region = nameof(Region);

    // Top level declarations
    public const string Namespaces = nameof(Namespaces);
    public const string Type = nameof(Type);
    public const string Member = nameof(Member);
}
