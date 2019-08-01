// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis
{
    public class AnalyzerConfigCodingConventionsContext : ICodingConventionContext, ICodingConventionsSnapshot
    {
        private static readonly Func<object, string, string?> TryGetAnalyzerConfigValue;

        private readonly object _analyzerConfigOptions;

        static AnalyzerConfigCodingConventionsContext()
        {
            TryGetAnalyzerConfigValue = CreateTryGetAnalyzerConfigValueAccessor();

            static Func<object, string, string?> CreateTryGetAnalyzerConfigValueAccessor()
            {
                var analyzerConfigOptions = typeof(AnalyzerOptions).GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions", throwOnError: false, ignoreCase: false);
                var method = analyzerConfigOptions?.GetRuntimeMethod("TryGetValue", new[] { typeof(string), typeof(string).MakeByRefType() });
                if (method is null)
                {
                    return (_1, _2) => null;
                }

                var instance = Expression.Parameter(typeof(object), "instance");
                var key = Expression.Parameter(typeof(string), "key");
                var value = Expression.Variable(typeof(string), "value");
                var accessor = Expression.Lambda<Func<object, string, string>>(
                    Expression.Block(
                        typeof(string),
                        new[] { value },
                        Expression.Call(
                            Expression.Convert(instance, analyzerConfigOptions),
                            method,
                            key,
                            value),
                        value),
                    instance,
                    key);
                return accessor.Compile();
            }
        }

        public AnalyzerConfigCodingConventionsContext(object analyzerConfigOptions)
        {
            _analyzerConfigOptions = analyzerConfigOptions;
        }

        public ICodingConventionsSnapshot CurrentConventions => this;

        IUniversalCodingConventions ICodingConventionsSnapshot.UniversalConventions => throw new NotSupportedException();
        IReadOnlyDictionary<string, object> ICodingConventionsSnapshot.AllRawConventions => throw new NotSupportedException();
        int ICodingConventionsSnapshot.Version => 0;

        event CodingConventionsChangedAsyncEventHandler ICodingConventionContext.CodingConventionsChangedAsync
        {
            add { }
            remove { }
        }

        public void Dispose()
        {
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task WriteConventionValueAsync(string conventionName, string conventionValue, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            throw new NotSupportedException();
        }

        bool ICodingConventionsSnapshot.TryGetConventionValue<T>(string conventionName, [MaybeNullWhen(returnValue: false)] out T conventionValue)
        {
            if (typeof(T) != typeof(string))
            {
                conventionValue = default!;
                return false;
            }

            conventionValue = (T)(object?)TryGetAnalyzerConfigValue(_analyzerConfigOptions, conventionName)!;
            return conventionValue is object;
        }
    }
}
