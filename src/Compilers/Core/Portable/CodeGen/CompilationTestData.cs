// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal sealed class CompilationTestData
    {
        internal struct MethodData
        {
            public readonly ILBuilder ILBuilder;
            public readonly IMethodSymbol Method;

            public MethodData(ILBuilder ilBuilder, IMethodSymbol method)
            {
                this.ILBuilder = ilBuilder;
                this.Method = method;
            }
        }

        // The map is used for storing a list of methods and their associated IL.
        public readonly ConcurrentDictionary<IMethodSymbol, MethodData> Methods = new ConcurrentDictionary<IMethodSymbol, MethodData>();

        // The emitted module.
        public Cci.IModule Module;

        public Func<object> SymWriterFactory;

        public ILBuilder GetIL(Func<IMethodSymbol, bool> predicate)
        {
            return Methods.Single(p => predicate(p.Key)).Value.ILBuilder;
        }

        private ImmutableDictionary<string, MethodData> _lazyMethodsByName;

        public ImmutableDictionary<string, MethodData> GetMethodsByName()
        {
            if (_lazyMethodsByName == null)
            {
                var methodsByName = Methods.ToImmutableDictionary(p => GetMethodName(p.Key), p => p.Value);
                Interlocked.CompareExchange(ref _lazyMethodsByName, methodsByName, null);
            }
            return _lazyMethodsByName;
        }

        private static readonly SymbolDisplayFormat _testDataKeyFormat = new SymbolDisplayFormat(
            compilerInternalOptions: SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames,
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
            memberOptions:
                SymbolDisplayMemberOptions.IncludeParameters |
                SymbolDisplayMemberOptions.IncludeContainingType |
                SymbolDisplayMemberOptions.IncludeExplicitInterface,
            parameterOptions:
                SymbolDisplayParameterOptions.IncludeParamsRefOut |
                SymbolDisplayParameterOptions.IncludeExtensionThis |
                SymbolDisplayParameterOptions.IncludeType,
            // Not showing the name is important because we visit parameters to display their
            // types.  If we visited their types directly, we wouldn't get ref/out/params.
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays |
                SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName);

        private static readonly SymbolDisplayFormat _testDataOperatorKeyFormat = new SymbolDisplayFormat(
             _testDataKeyFormat.CompilerInternalOptions,
             _testDataKeyFormat.GlobalNamespaceStyle,
             _testDataKeyFormat.TypeQualificationStyle,
             _testDataKeyFormat.GenericsOptions,
             _testDataKeyFormat.MemberOptions | SymbolDisplayMemberOptions.IncludeType,
             _testDataKeyFormat.ParameterOptions,
             _testDataKeyFormat.DelegateStyle,
             _testDataKeyFormat.ExtensionMethodStyle,
             _testDataKeyFormat.PropertyStyle,
             _testDataKeyFormat.LocalOptions,
             _testDataKeyFormat.KindOptions,
             _testDataKeyFormat.MiscellaneousOptions);

        private static string GetMethodName(IMethodSymbol methodSymbol)
        {
            var format = (methodSymbol.MethodKind == MethodKind.UserDefinedOperator) ?
                _testDataOperatorKeyFormat :
                _testDataKeyFormat;
            return methodSymbol.ToDisplayString(format);
        }
    }
}
