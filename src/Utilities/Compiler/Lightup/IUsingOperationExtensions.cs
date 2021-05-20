// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using System;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.Lightup
{
    internal static class IUsingOperationExtensions
    {
        private static readonly Func<IUsingOperation, bool> s_isAsynchronous
            = LightupHelpers.CreateOperationPropertyAccessor<IUsingOperation, bool>(typeof(IUsingOperation), nameof(IsAsynchronous), fallbackResult: false);

        public static bool IsAsynchronous(this IUsingOperation usingOperation)
            => s_isAsynchronous(usingOperation);
    }
}

#endif
