// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using ILAssembler = Microsoft.DotNet.ILAssembler;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class IlasmUtilities
    {
        private static string GetArchitecture()
        {
#if NET8_0_OR_GREATER
            return RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
#else
            return "x64";
#endif
        }

        public static readonly string Architecture = GetArchitecture();

        private static string GetAssemblyPath()
            => Path.Combine(TempRoot.Root, $"_{Guid.NewGuid()}.dll");

        internal static (ImmutableArray<byte> Assembly, ImmutableArray<byte> Pdb) Assemble(string declarations, bool appendDefaultHeader, bool includePdb, bool autoInherit)
            => Assemble(declarations, appendDefaultHeader, includePdb, autoInherit, default, useMarkup: true, GetAssemblyPath());

        internal static (ImmutableArray<byte> Assembly, ImmutableArray<byte> Pdb) Assemble(string declarations, ImmutableArray<ILAssembler.Diagnostic> expectedDiagnostics, bool appendDefaultHeader, bool includePdb, bool autoInherit)
            => Assemble(declarations, appendDefaultHeader, includePdb, autoInherit, expectedDiagnostics, useMarkup: false, GetAssemblyPath());

        private static (ImmutableArray<byte> Assembly, ImmutableArray<byte> Pdb) Assemble(string declarations, bool appendDefaultHeader, bool includePdb, bool autoInherit, ImmutableArray<ILAssembler.Diagnostic> expectedDiagnostics, bool useMarkup, string assemblyPath)
        {
            if (declarations == null)
                throw new ArgumentNullException(nameof(declarations));

            var sourceFileName = Path.GetFileNameWithoutExtension(assemblyPath);
            string completeIL;
            if (appendDefaultHeader)
            {
                const string corLibName = "mscorlib";
                const string corLibVersion = "4:0:0:0";
                const string corLibKey = "B7 7A 5C 56 19 34 E0 89";

                completeIL =
$@".assembly '{sourceFileName}' {{}}

.assembly extern {corLibName}
{{
  .publickeytoken = ({corLibKey})
  .ver {corLibVersion}
}}

{declarations}";
            }
            else
            {
                completeIL = declarations.Replace("<<GeneratedFileName>>", sourceFileName);
            }

            IDictionary<string, ImmutableArray<TextSpan>> expectedSpans = null;
            if (useMarkup)
            {
                MarkupTestFile.GetSpans(completeIL, out completeIL, out expectedSpans, treatPositionIndicatorsAsCode: true);
                Assert.DoesNotContain(string.Empty, expectedSpans.Keys);
            }

            var emitPdb = includePdb && !MonoHelpers.IsRunningOnMono();
            var options = new ILAssembler.Options
            {
                Dll = true,
                NoAutoInherit = !autoInherit,
                ErrorTolerant = useMarkup ? expectedSpans.Count > 0 : !expectedDiagnostics.IsDefaultOrEmpty,
                OutputFileName = Path.GetFileName(assemblyPath),
                Pdb = emitPdb,
                PdbFilePath = emitPdb ? Path.ChangeExtension(assemblyPath, "pdb") : null,
            };

            var sourceText = new ILAssembler.SourceText(completeIL, Path.ChangeExtension(assemblyPath, "il"));
            var (diagnostics, result) = new ILAssembler.DocumentCompiler().Compile(
                sourceText,
                path => new ILAssembler.SourceText(File.ReadAllText(path), path),
                path => File.Exists(path) ? File.ReadAllBytes(path) : null,
                options);

            if (useMarkup)
            {
                Assert.All(diagnostics, diagnostic => Assert.Equal(sourceText.Path, diagnostic.Location.Source.Path));
                var expected = expectedSpans.SelectMany(pair => pair.Value.Select(span => (Id: pair.Key, span.Start, span.Length)))
                    .OrderBy(item => item.Start).ThenBy(item => item.Id).ThenBy(item => item.Length);
                var actual = diagnostics.Select(diagnostic => (diagnostic.Id, diagnostic.Location.Span.Start, diagnostic.Location.Span.Length))
                    .OrderBy(item => item.Start).ThenBy(item => item.Id).ThenBy(item => item.Length);
                Assert.Equal(expected, actual);
            }
            else
            {
                Assert.Equal(expectedDiagnostics.IsDefault ? ImmutableArray<ILAssembler.Diagnostic>.Empty : expectedDiagnostics, diagnostics,
                    new DiagnosticComparer(declarations, sourceText, appendDefaultHeader, sourceFileName));
            }

            if (result == null)
            {
                var diagnosticText = string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Location}: {diagnostic.Severity} {diagnostic.Id}: {diagnostic.Message}"));
                throw new ArgumentException(
                    "The provided IL cannot be compiled." + Environment.NewLine +
                    (diagnosticText.Length > 0 ? diagnosticText : "No compilation result was produced."),
                    nameof(declarations));
            }

            var imageBuilder = new BlobBuilder();
            result.Serialize(imageBuilder);
            if (emitPdb && !result.PortablePdb.HasValue)
            {
                throw new InvalidOperationException("The IL assembler did not produce the requested PDB.");
            }

            return (imageBuilder.ToImmutableArray(), emitPdb ? result.PortablePdb.Value : default);
        }

        private sealed class DiagnosticComparer : IEqualityComparer<ILAssembler.Diagnostic>
        {
            private readonly string _declarations;
            private readonly ILAssembler.SourceText _sourceText;
            private readonly bool _appendDefaultHeader;
            private readonly string _sourceFileName;

            public DiagnosticComparer(string declarations, ILAssembler.SourceText sourceText, bool appendDefaultHeader, string sourceFileName)
            {
                _declarations = declarations;
                _sourceText = sourceText;
                _appendDefaultHeader = appendDefaultHeader;
                _sourceFileName = sourceFileName;
            }

            public bool Equals(ILAssembler.Diagnostic x, ILAssembler.Diagnostic y)
            {
                if (x.Id != y.Id || x.Severity != y.Severity || x.Message != y.Message)
                    return false;

                if (x.Location.Source.Text != _declarations)
                    return x.Location.Source.Path == y.Location.Source.Path && x.Location.Source.Text == y.Location.Source.Text && x.Location.Span == y.Location.Span;

                var span = x.Location.Span;
                if (span.Start < 0 || span.Length < 0 || span.Start > _declarations.Length - span.Length)
                    return false;

                var before = _declarations.Substring(0, span.Start);
                var within = _declarations.Substring(span.Start, span.Length);
                var prefixLength = 0;
                if (_appendDefaultHeader)
                {
                    prefixLength = _sourceText.Text.Length - _declarations.Length;
                }
                else
                {
                    before = before.Replace("<<GeneratedFileName>>", _sourceFileName);
                    within = within.Replace("<<GeneratedFileName>>", _sourceFileName);
                }

                return y.Location.Source.Path == _sourceText.Path &&
                    y.Location.Span.Start == prefixLength + before.Length && y.Location.Span.Length == within.Length;
            }

            public int GetHashCode(ILAssembler.Diagnostic diagnostic)
                => diagnostic.Id.GetHashCode();
        }
    }
}
