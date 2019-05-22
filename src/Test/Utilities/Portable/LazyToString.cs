// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
