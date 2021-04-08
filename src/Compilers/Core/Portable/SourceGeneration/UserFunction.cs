// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A Func based delegate that represents a user provided function
    /// that has been wrapped to convert exceptions to UserCodeExceptions
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="arg"></param>
    /// <returns></returns>
    internal delegate TResult UserFunc<in T, out TResult>(T arg);

    internal class UserFunctionException : Exception
    {
        public UserFunctionException(Exception innerException)
            : base("User provided code threw an exception", innerException)
        {
        }
    }

    internal static class UserFunctionExtensions
    {
        internal static UserFunc<TInput, TOutput> WrapUserFunction<TInput, TOutput>(this Func<TInput, TOutput> userFunction)
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
    }
}
