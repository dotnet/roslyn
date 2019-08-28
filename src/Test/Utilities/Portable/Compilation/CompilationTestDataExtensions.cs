// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Cci = Microsoft.Cci;

namespace Roslyn.Test.Utilities
{
    internal static class CompilationTestDataExtensions
    {
        internal static void VerifyIL(
            this CompilationTestData.MethodData method,
            string expectedIL,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            const string moduleNamePlaceholder = "{#Module#}";
            string actualIL = GetMethodIL(method);
            if (expectedIL.IndexOf(moduleNamePlaceholder) >= 0)
            {
                var module = method.Method.ContainingModule;
                var moduleName = Path.GetFileNameWithoutExtension(module.Name);
                expectedIL = expectedIL.Replace(moduleNamePlaceholder, moduleName);
            }

            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualIL, escapeQuotes: true, expectedValueSourcePath: expectedValueSourcePath, expectedValueSourceLine: expectedValueSourceLine);
        }

        internal static ImmutableArray<KeyValuePair<IMethodSymbol, CompilationTestData.MethodData>> GetExplicitlyDeclaredMethods(this CompilationTestData data)
        {
            return data.Methods.Where(m => !m.Key.IsImplicitlyDeclared).ToImmutableArray();
        }

        internal static CompilationTestData.MethodData GetMethodData(this CompilationTestData data, string qualifiedMethodName)
        {
            var map = data.GetMethodsByName();

            if (!map.TryGetValue(qualifiedMethodName, out var methodData))
            {
                // caller may not have specified parameter list, so try to match parameterless method
                if (!map.TryGetValue(qualifiedMethodName + "()", out methodData))
                {
                    // now try to match single method with any parameter list
                    var keys = map.Keys.Where(k => k.StartsWith(qualifiedMethodName + "(", StringComparison.Ordinal));
                    if (keys.Count() == 1)
                    {
                        methodData = map[keys.First()];
                    }
                    else if (keys.Count() > 1)
                    {
                        throw new AmbiguousMatchException(
                            "Could not determine best match for method named: " + qualifiedMethodName + Environment.NewLine +
                            string.Join(Environment.NewLine, keys.Select(s => "    " + s)) + Environment.NewLine);
                    }
                }
            }

            if (methodData.ILBuilder == null)
            {
                throw new KeyNotFoundException("Could not find ILBuilder matching method '" + qualifiedMethodName + "'. Existing methods:\r\n" + string.Join("\r\n", map.Keys));
            }

            return methodData;
        }

        internal static string GetMethodIL(this CompilationTestData.MethodData method)
        {
            return ILBuilderVisualizer.ILBuilderToString(method.ILBuilder);
        }

        internal static EditAndContinueMethodDebugInformation GetEncDebugInfo(this CompilationTestData.MethodData methodData)
        {
            // TODO:
            return new EditAndContinueMethodDebugInformation(
                0,
                Cci.MetadataWriter.GetLocalSlotDebugInfos(methodData.ILBuilder.LocalSlotManager.LocalsInOrder()),
                closures: ImmutableArray<ClosureDebugInfo>.Empty,
                lambdas: ImmutableArray<LambdaDebugInfo>.Empty);
        }

        internal static Func<MethodDefinitionHandle, EditAndContinueMethodDebugInformation> EncDebugInfoProvider(this CompilationTestData.MethodData methodData)
        {
            return _ => methodData.GetEncDebugInfo();
        }
    }
}
