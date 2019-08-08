// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis
{
    internal class AnalyzerConfigCodingConventionsManager : ICodingConventionsManager
    {
        private static readonly Func<AnalyzerOptions, object?> GetAnalyzerConfigOptionsProvider;
        private static readonly Func<object, SyntaxTree, object> GetAnalyzerConfigOptions;

        private readonly SyntaxTree _tree;
        private readonly AnalyzerOptions _options;
        private readonly ICodingConventionsManager? _codingConventionsManager;

        static AnalyzerConfigCodingConventionsManager()
        {
            GetAnalyzerConfigOptionsProvider = CreateAnalyzerConfigOptionsProviderAccessor();
            GetAnalyzerConfigOptions = CreateAnalyzerConfigOptionsAccessor();

            static Func<AnalyzerOptions, object?> CreateAnalyzerConfigOptionsProviderAccessor()
            {
                var property = typeof(AnalyzerOptions).GetTypeInfo().GetDeclaredProperty("AnalyzerConfigOptionsProvider");
                if (property is null)
                {
                    return _ => null;
                }

                var options = Expression.Parameter(typeof(AnalyzerOptions), "options");
                var accessor = Expression.Lambda<Func<AnalyzerOptions, object>>(
                    Expression.Call(options, property.GetMethod),
                    options);
                return accessor.Compile();
            }

            static Func<object, SyntaxTree, object> CreateAnalyzerConfigOptionsAccessor()
            {
                var analyzerConfigOptionsProvider = typeof(AnalyzerOptions).GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptionsProvider", throwOnError: false, ignoreCase: false);
                var method = analyzerConfigOptionsProvider?.GetRuntimeMethod("GetOptions", new[] { typeof(SyntaxTree) });
                if (method is null)
                {
                    return (_1, _2) => throw new NotImplementedException();
                }

                var provider = Expression.Parameter(typeof(object), "provider");
                var tree = Expression.Parameter(typeof(SyntaxTree), "tree");
                var accessor = Expression.Lambda<Func<object, SyntaxTree, object>>(
                    Expression.Call(
                        Expression.Convert(provider, analyzerConfigOptionsProvider),
                        method,
                        tree),
                    provider,
                    tree);
                return accessor.Compile();
            }
        }

        public AnalyzerConfigCodingConventionsManager(SyntaxTree tree, AnalyzerOptions options)
        {
            _tree = tree;
            _options = options;
            if (GetAnalyzerConfigOptionsProvider(options) is null)
            {
                _codingConventionsManager = CodingConventionsManagerFactory.CreateCodingConventionsManager();
            }
        }

        public Task<ICodingConventionContext> GetConventionContextAsync(string filePathContext, CancellationToken cancellationToken)
        {
            if (_codingConventionsManager is object)
            {
                return _codingConventionsManager.GetConventionContextAsync(filePathContext, cancellationToken);
            }

            var analyzerConfigOptionsProvider = GetAnalyzerConfigOptionsProvider(_options);
            var analyzerConfigOptions = GetAnalyzerConfigOptions(analyzerConfigOptionsProvider!, _tree);
            return Task.FromResult<ICodingConventionContext>(new AnalyzerConfigCodingConventionsContext(analyzerConfigOptions));
        }
    }
}
