// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor;

internal abstract partial class AbstractGenerateConstructorService<TService, TExpressionSyntax>
{
    protected readonly struct Argument(RefKind refKind, string? name, TExpressionSyntax? expression)
    {
        public readonly RefKind RefKind = refKind;
        public readonly string Name = name ?? "";
        public readonly TExpressionSyntax? Expression = expression;

        public bool IsNamed => !string.IsNullOrEmpty(Name);
    }
}
