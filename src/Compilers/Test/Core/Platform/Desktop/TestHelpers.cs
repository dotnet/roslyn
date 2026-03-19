// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET472

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Basic.Reference.Assemblies;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Test.Utilities
{
    public static class DesktopTestHelpers
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

            dir.CopyFile(typeof(System.Reflection.Metadata.MetadataReader).Assembly.Location);
            var immutable = dir.CopyFile(typeof(ImmutableArray).Assembly.Location);
            var analyzer = dir.CopyFile(typeof(DiagnosticAnalyzer).Assembly.Location);
            dir.CopyFile(typeof(Memory<>).Assembly.Location);
            dir.CopyFile(typeof(System.Runtime.CompilerServices.Unsafe).Assembly.Location);

            var analyzerCompilation = CSharpCompilation.Create(
                assemblyName,
                new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(SourceText.From(analyzerSource, encoding: null, SourceHashAlgorithms.Default)) },
                new MetadataReference[]
                {
                    NetStandard20.References.mscorlib,
                    NetStandard20.References.netstandard,
                    NetStandard20.References.SystemRuntime,
                    MetadataReference.CreateFromFile(immutable.Path),
                    MetadataReference.CreateFromFile(analyzer.Path)
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return dir.CreateFile(assemblyName + ".dll").WriteAllBytes(analyzerCompilation.EmitToArray());
        }

        public static string? GetMSBuildDirectory()
        {
            return MSBuildLocator.QueryVisualStudioInstances()
                .OrderByDescending(v => v.Version)
                .FirstOrDefault()?
                .MSBuildPath;
        }
    }
}

#endif
