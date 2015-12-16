// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        internal T InvokeFormatter<T>(MethodId method, Func<IDkmClrFormatter, T> f)
        {
            return _formatters.Invoke(method, f);
        }

        internal T InvokeResultProvider<T>(MethodId method, Func<IDkmClrResultProvider, T> f)
        {
            return _resultProviders.Invoke(method, f);
        }

        private sealed class Dispatcher<TInterface>
        {
            private readonly ImmutableArray<TInterface> _implementations;
            private readonly ArrayBuilder<MethodId> _calls;

            internal Dispatcher(ImmutableArray<TInterface> items)
            {
                _implementations = items;
                _calls = new ArrayBuilder<MethodId>();
            }

            internal TResult Invoke<TResult>(MethodId method, Func<TInterface, TResult> f)
            {
                // If the last n - 1 calls are to the same method,
                // call the n-th implementation.
                int n = _calls.Count;
                int index = 0;
                while ((n - index > 0) && (_calls[n - index - 1] == method))
                {
                    index++;
                }
                if (index == _implementations.Length)
                {
                    throw new InvalidOperationException();
                }
                var item = _implementations[index];
                _calls.Push(method);
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
