// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
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
}
