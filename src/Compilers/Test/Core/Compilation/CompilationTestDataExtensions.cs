// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
using Microsoft.CodeAnalysis.Symbols;
using Cci = Microsoft.Cci;

namespace Roslyn.Test.Utilities
{
    internal static class CompilationTestDataExtensions
    {
        internal static void VerifyIL(
            this CompilationTestData.MethodData method,
            string expectedIL,
            [CallerLineNumber] int expectedValueSourceLine = 0,
            [CallerFilePath] string expectedValueSourcePath = null)
        {
            const string moduleNamePlaceholder = "{#Module#}";
            string actualIL = GetMethodIL(method);
            if (expectedIL.IndexOf(moduleNamePlaceholder) >= 0)
            {
                var module = method.Method.ContainingModule;
                var moduleName = Path.GetFileNameWithoutExtension(module.Name);
                expectedIL = expectedIL.Replace(moduleNamePlaceholder, moduleName);
            }

            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualIL, escapeQuotes: false, expectedValueSourcePath: expectedValueSourcePath, expectedValueSourceLine: expectedValueSourceLine);
        }

        internal static ImmutableArray<KeyValuePair<IMethodSymbolInternal, CompilationTestData.MethodData>> GetExplicitlyDeclaredMethods(this CompilationTestData data)
        {
            return data.Methods.Where(m => !m.Key.IsImplicitlyDeclared).ToImmutableArray();
        }

        private static bool TryGetMethodData(ImmutableDictionary<string, CompilationTestData.MethodData> map, string qualifiedMethodName, out CompilationTestData.MethodData methodData)
        {
            if (map.TryGetValue(qualifiedMethodName, out methodData))
            {
                return true;
            }

            // caller may not have specified parameter list, so try to match parameterless method
            if (map.TryGetValue(qualifiedMethodName + "()", out methodData))
            {
                return true;
            }

            // now try to match single method with any parameter list
            var keys = map.Keys.Where(k => k.StartsWith(qualifiedMethodName + "(", StringComparison.Ordinal));
            if (keys.Count() == 1)
            {
                methodData = map[keys.First()];
                return true;
            }
            else if (keys.Count() > 1)
            {
                throw new AmbiguousMatchException(
                    "Could not determine best match for method named: " + qualifiedMethodName + Environment.NewLine +
                    string.Join(Environment.NewLine, keys.Select(s => "    " + s)) + Environment.NewLine);
            }
            else
            {
                return false;
            }
        }

        internal static bool TryGetMethodData(this CompilationTestData data, string qualifiedMethodName, out CompilationTestData.MethodData methodData)
        {
            var map = data.GetMethodsByName();
            return TryGetMethodData(map, qualifiedMethodName, out methodData);
        }

        internal static CompilationTestData.MethodData GetMethodData(this CompilationTestData data, string qualifiedMethodName)
        {
            var map = data.GetMethodsByName();
            TryGetMethodData(data, qualifiedMethodName, out var methodData);

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
                lambdas: ImmutableArray<LambdaDebugInfo>.Empty,
                stateMachineStates: ImmutableArray<StateMachineStateDebugInfo>.Empty);
        }

        internal static Func<MethodDefinitionHandle, EditAndContinueMethodDebugInformation> EncDebugInfoProvider(this CompilationTestData.MethodData methodData)
        {
            return _ => methodData.GetEncDebugInfo();
        }
    }
}
