// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll
#endregion

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    internal enum MethodId
    {
        GetValueString,
        HasUnderlyingString,
        GetUnderlyingString,
        GetResult,
        GetChildren,
        GetItems,
        GetTypeName,
        GetClrValue,
    }

    public class DkmInspectionSession
    {
        private readonly Dispatcher<IDkmClrFormatter> _formatters;
        private readonly Dispatcher<IDkmClrResultProvider> _resultProviders;

        internal DkmInspectionSession(ImmutableArray<IDkmClrFormatter> formatters, ImmutableArray<IDkmClrResultProvider> resultProviders)
        {
            _formatters = new Dispatcher<IDkmClrFormatter>(formatters);
            _resultProviders = new Dispatcher<IDkmClrResultProvider>(resultProviders);
        }

        internal T InvokeFormatter<T>(object instance, MethodId method, Func<IDkmClrFormatter, T> f)
        {
            return _formatters.Invoke(instance, method, f);
        }

        internal T InvokeResultProvider<T>(object instance, MethodId method, Func<IDkmClrResultProvider, T> f)
        {
            return _resultProviders.Invoke(instance, method, f);
        }

        private sealed class Dispatcher<TInterface>
        {
            private readonly struct InstanceAndMethod
            {
                internal InstanceAndMethod(object instance, MethodId method)
                {
                    Instance = instance;
                    Method = method;
                }
                internal readonly object Instance;
                internal readonly MethodId Method;
                internal bool Equals(InstanceAndMethod other)
                {
                    return Instance == other.Instance && Method == other.Method;
                }
            }

            private readonly ImmutableArray<TInterface> _implementations;
            private readonly ArrayBuilder<InstanceAndMethod> _calls;

            internal Dispatcher(ImmutableArray<TInterface> items)
            {
                _implementations = items;
                _calls = new ArrayBuilder<InstanceAndMethod>();
            }

            internal TResult Invoke<TResult>(object instance, MethodId method, Func<TInterface, TResult> f)
            {
                // If the last n - 1 calls are to the same method,
                // call the n-th implementation.
                var instanceAndMethod = new InstanceAndMethod(instance, method);
                int n = _calls.Count;
                int index = 0;
                while ((n - index > 0) && _calls[n - index - 1].Equals(instanceAndMethod))
                {
                    index++;
                }
                if (index == _implementations.Length)
                {
                    throw new InvalidOperationException();
                }
                var item = _implementations[index];
                _calls.Push(instanceAndMethod);
                try
                {
                    return f(item);
                }
                finally
                {
                    _calls.Pop();
                }
            }
        }
    }
}
