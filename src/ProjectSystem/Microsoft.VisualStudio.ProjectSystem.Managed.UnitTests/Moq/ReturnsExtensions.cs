// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft;
using Moq.Language;
using Moq.Language.Flow;

namespace Moq
{
    internal static class ReturnsExtensions
    {
        public static IReturnsResult<TMock> ReturnsAsync<TMock>(this IReturns<TMock, Task> mock, Action action) where TMock : class
        {
            return mock.Returns(() => { action(); return Task.CompletedTask; });
        }

        public static IReturnsThrows<TMock, TReturn> Returns<TMock, TReturn, TOut, TResult>(this IReturns<TMock, TReturn> valueFunction, FuncWithOut<TOut, TResult> action)
              where TMock : class
        {
            return Returns(valueFunction, (object)action);
        }

        public static IReturnsThrows<TMock, TReturn> Returns<TMock, TReturn, T1, TOut, TResult>(this IReturns<TMock, TReturn> valueFunction, FuncWithOut<T1, TOut, TResult> action)
            where TMock : class
        {
            return Returns(valueFunction, (object)action);
        }

        private static IReturnsThrows<TMock, TReturn> Returns<TMock, TReturn>(IReturns<TMock, TReturn> valueFunction, object action)
            where TMock : class
        {
            valueFunction.GetType()
                         .InvokeMember("SetCallbackWithArguments", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public |  BindingFlags.Instance, (Binder)null, valueFunction, new[] { action });

            return (IReturnsThrows<TMock, TReturn>)valueFunction;
        }
    }
}
