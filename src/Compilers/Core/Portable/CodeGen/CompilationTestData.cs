// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.DiaSymReader;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            public readonly IMethodSymbolInternal Method;

            public MethodData(ILBuilder ilBuilder, IMethodSymbolInternal method)
            {
                this.ILBuilder = ilBuilder;
                this.Method = method;
            }
        }

        // The map is used for storing a list of methods and their associated IL.
        public readonly ConcurrentDictionary<IMethodSymbolInternal, MethodData> Methods = new ConcurrentDictionary<IMethodSymbolInternal, MethodData>();

        // The emitted module.
        public CommonPEModuleBuilder? Module;

        public Func<ISymWriterMetadataProvider, SymUnmanagedWriter>? SymWriterFactory;

        public ILBuilder GetIL(Func<IMethodSymbolInternal, bool> predicate)
        {
            return Methods.Single(p => predicate(p.Key)).Value.ILBuilder;
        }

        private ImmutableDictionary<string, MethodData>? _lazyMethodsByName;

        // Returns map indexed by name for those methods that have a unique name.
        public ImmutableDictionary<string, MethodData> GetMethodsByName()
        {
            if (_lazyMethodsByName == null)
            {
                var map = new Dictionary<string, MethodData>();
                foreach (var pair in Methods)
                {
                    var name = GetMethodName(pair.Key);
                    if (map.ContainsKey(name))
                    {
                        map[name] = default(MethodData);
                    }
                    else
                    {
                        map.Add(name, pair.Value);
                    }
                }
                var methodsByName = map.Where(p => p.Value.Method != null).ToImmutableDictionary();
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

        private static string GetMethodName(IMethodSymbolInternal methodSymbol)
        {
            IMethodSymbol iMethod = (IMethodSymbol)methodSymbol.GetISymbol();
            var format = (iMethod.MethodKind == MethodKind.UserDefinedOperator) ?
                _testDataOperatorKeyFormat :
                _testDataKeyFormat;
            return iMethod.ToDisplayString(format);
        }
    }
}
