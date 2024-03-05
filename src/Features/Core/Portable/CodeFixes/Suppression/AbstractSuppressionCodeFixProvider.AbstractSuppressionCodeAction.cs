// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression;

internal partial class AbstractSuppressionCodeFixProvider
{
    internal abstract class AbstractSuppressionCodeAction : NestedSuppressionCodeAction
    {
        private readonly AbstractSuppressionCodeFixProvider _fixer;

        protected AbstractSuppressionCodeAction(AbstractSuppressionCodeFixProvider fixer, string title)
            : base(title)
        {
            _fixer = fixer;
        }

        protected AbstractSuppressionCodeFixProvider Fixer => _fixer;
    }
}
