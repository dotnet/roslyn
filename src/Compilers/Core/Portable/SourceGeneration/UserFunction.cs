﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal sealed class UserFunctionException : Exception
    {
        public new Exception InnerException => base.InnerException!;

        public UserFunctionException(Exception innerException)
            : base("User provided code threw an exception", innerException)
        {
        }
    }

    internal static class UserFunctionExtensions
    {
        internal static Func<TInput, CancellationToken, TOutput> WrapUserFunction<TInput, TOutput>(this Func<TInput, CancellationToken, TOutput> userFunction)
        {
            return (input, token) =>
            {
                try
                {
                    return userFunction(input, token);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, token))
                {
                    throw new UserFunctionException(e);
                }
            };
        }

        internal static Func<TInput, CancellationToken, ImmutableArray<TOutput>> WrapUserFunctionAsImmutableArray<TInput, TOutput>(this Func<TInput, CancellationToken, IEnumerable<TOutput>> userFunction)
        {
            return (input, token) => userFunction.WrapUserFunction()(input, token).ToImmutableArray();
        }

        internal static Action<TInput> WrapUserAction<TInput>(this Action<TInput> userAction)
        {
            return input =>
            {
                try
                {
                    userAction(input);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
                {
                    throw new UserFunctionException(e);
                }
            };
        }

        internal static Action<TInput1, TInput2> WrapUserAction<TInput1, TInput2>(this Action<TInput1, TInput2> userAction)
        {
            return (input1, input2) =>
            {
                try
                {
                    userAction(input1, input2);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
                {
                    throw new UserFunctionException(e);
                }
            };
        }
    }
}
