// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class RecordLambdaBindingBinder : Binder
    {
        private readonly Dictionary<SyntaxNode, int> _lambdaBindingCounts;

        internal RecordLambdaBindingBinder(Binder next, Dictionary<SyntaxNode, int> lambdaBindingCounts) :
            base(next, next.Flags)
        {
            _lambdaBindingCounts = lambdaBindingCounts;
        }

        internal override void RecordLambdaBinding(SyntaxNode syntax)
        {
            if (_lambdaBindingCounts.TryGetValue(syntax, out int count))
            {
                _lambdaBindingCounts[syntax] = ++count;
            }
            else
            {
                _lambdaBindingCounts.Add(syntax, 1);
            }
        }

        internal override Binder? GetBinder(SyntaxNode node)
        {
            Debug.Assert(Next is { });

            var binder = Next.GetBinder(node);
            if (binder is { })
            {
                binder = new RecordLambdaBindingBinder(binder, _lambdaBindingCounts);
            }

            return binder;
        }
    }
}
