// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Roslyn.Test.Utilities
{
    public static class TestHelpers
    {
        public static IEnumerable<Type> GetAllTypesImplementingGivenInterface(Assembly assembly, Type interfaceType)
        {
            if (assembly == null || interfaceType == null || !interfaceType.IsInterface)
            {
                throw new ArgumentException("interfaceType is not an interface.", nameof(interfaceType));
            }

            return assembly.GetTypes().Where((t) =>
            {
                // simplest way to get types that implement mef type
                // we might need to actually check whether type export the interface type later
                if (t.IsAbstract)
                {
                    return false;
                }

                var candidate = t.GetInterface(interfaceType.ToString());
                return candidate != null && candidate.Equals(interfaceType);
            }).ToList();
        }

        public static IEnumerable<Type> GetAllTypesSubclassingType(Assembly assembly, Type type)
        {
            if (assembly == null || type == null)
            {
                throw new ArgumentException("Invalid arguments");
            }

            return (from t in assembly.GetTypes()
                    where !t.IsAbstract
                    where type.IsAssignableFrom(t)
                    select t).ToList();
        }

        public static IEnumerable<Type> GetAllTypesWithStaticFieldsImplementingType(Assembly assembly, Type type)
        {
            return assembly.GetTypes().Where(t =>
            {
                return t.GetFields(BindingFlags.Public | BindingFlags.Static).Any(f => type.IsAssignableFrom(f.FieldType));
            }).ToList();
        }

        public static string GetCultureInvariantString(object value)
        {
            if (value == null)
                return null;

            var valueType = value.GetType();
            if (valueType == typeof(string))
            {
                return value as string;
            }

            if (valueType == typeof(DateTime))
            {
                return ((DateTime)value).ToString("M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);
            }

            if (valueType == typeof(float))
            {
                return ((float)value).ToString(CultureInfo.InvariantCulture);
            }

            if (valueType == typeof(double))
            {
                return ((double)value).ToString(CultureInfo.InvariantCulture);
            }

            if (valueType == typeof(decimal))
            {
                return ((decimal)value).ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }

        public static TempFile CreateCSharpAnalyzerAssemblyWithTestAnalyzer(TempDirectory dir, string assemblyName)
        {
            var analyzerSource = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TestAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
    public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
}";

            var metadata = dir.CopyFile(typeof(System.Reflection.Metadata.MetadataReader).Assembly.Location);
            var immutable = dir.CopyFile(typeof(ImmutableArray).Assembly.Location);
            var analyzer = dir.CopyFile(typeof(DiagnosticAnalyzer).Assembly.Location);

            var analyzerCompilation = CSharpCompilation.Create(
                assemblyName,
                new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(analyzerSource) },
                new MetadataReference[]
                {
                    TestBase.SystemRuntimePP7Ref,
                    MetadataReference.CreateFromFile(immutable.Path),
                    MetadataReference.CreateFromFile(analyzer.Path)
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return dir.CreateFile(assemblyName + ".dll").WriteAllBytes(analyzerCompilation.EmitToArray());
        }
    }
}
