// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal sealed class UserFunctionException : Exception
    {
        public UserFunctionException(Exception innerException)
            : base("User provided code threw an exception", innerException)
        {
        }
    }

    internal static class UserFunctionExtensions
    {
        internal static Func<TInput, TOutput> WrapUserFunction<TInput, TOutput>(this Func<TInput, TOutput> userFunction)
        {
            return (input) =>
            {
                try
                {
                    return userFunction(input);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
                {
                    throw new UserFunctionException(e);
                }
            };
        }

        internal static Action<TInput1, TInput2> WrapUserAction<TInput1, TInput2>(this Action<TInput1, TInput2> userFunction)
        {
            return (input1, input2) =>
            {
                try
                {
                    userFunction(input1, input2);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
                {
                    throw new UserFunctionException(e);
                }
            };
        }
    }
}
