// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression;

internal abstract partial class AbstractSuppressionCodeFixProvider
{
    internal abstract class AbstractSuppressionCodeAction : NestedSuppressionCodeAction
    {
        protected AbstractSuppressionCodeAction(AbstractSuppressionCodeFixProvider fixer, string title)
            : base(title)
        {
            Fixer = fixer;
        }

        protected AbstractSuppressionCodeFixProvider Fixer { get; }
    }
}
