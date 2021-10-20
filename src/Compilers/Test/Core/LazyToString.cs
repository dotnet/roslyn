// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Roslyn.Test.Utilities
{
    internal sealed class LazyToString
    {
        private readonly Func<object> _evaluator;

        public LazyToString(Func<object> evaluator)
            => _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));

        public override string ToString()
            => _evaluator().ToString();
    }
}
