// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
{
    internal abstract partial class AbstractGenerateConstructorService<TService, TExpressionSyntax>
    {
        protected readonly struct Argument
        {
            public readonly RefKind RefKind;
            public readonly string Name;
            public readonly TExpressionSyntax Expression;

            public Argument(RefKind refKind, string name, TExpressionSyntax expression)
            {
                RefKind = refKind;
                Name = name ?? "";
                Expression = expression;
            }

            public bool IsNamed => !string.IsNullOrEmpty(Name);
        }
    }
}
