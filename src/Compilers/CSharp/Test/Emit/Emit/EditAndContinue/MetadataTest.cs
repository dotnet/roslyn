// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Test.Utilities;
using Xunit;

using static Microsoft.CodeAnalysis.CSharp.Test.Utilities.CSharpTestBase;
using static Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests.EditAndContinueTestBase;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    internal class MetadataTest
    {
        private readonly CSharpCompilationOptions? _options;
        private readonly TargetFramework _targetFramework;

        private readonly List<MetadataReader> _readers = new();
        private readonly List<GenerationInfo> _generations = new();

        public MetadataTest(CSharpCompilationOptions? options, TargetFramework? targetFramework)
        {
            _options = options;
            _targetFramework = targetFramework ?? TargetFramework.Standard;
        }

        internal MetadataTest AddGeneration(string source, SemanticEditDescription[]? edits = null, string[]? typeDefs = null, string[]? methodDefs = null, string[]? memberRefs = null, int? customAttributesTableSize = null, EditAndContinueLogEntry[]? encLog = null, EntityHandle[]? encMap = null)
        {
            _generations.Add(new GenerationInfo(source, edits, typeDefs, methodDefs, memberRefs, customAttributesTableSize, encLog, encMap));
            return this;
        }

        internal void Verify()
        {
            Assert.True(_generations.Count > 2, "Should have at least 2 generations (one baseline, one delta)");

            var firstGeneration = _generations[0];
            var prevCompilation = CreateCompilation(firstGeneration.Source, options: _options, targetFramework: _targetFramework);

            var disposables = new List<IDisposable>();

            var bytes = prevCompilation.EmitToArray();
            var md = ModuleMetadata.CreateFromImage(bytes);
            disposables.Add(md);

            try
            {
                VerifyCompilation(firstGeneration, md.MetadataReader, "initial compilation");

                var prevReader = md.MetadataReader;
                var prevBaseline = EmitBaseline.CreateInitialBaseline(md, EmptyLocalsProvider);

                int genId = 1;
                foreach (var generation in _generations.Skip(1))
                {
                    Debug.Assert(generation.Edits is not null);

                    var compilation = prevCompilation.WithSource(generation.Source);

                    var edits = GetSemanticEdits(generation.Edits, prevCompilation, compilation);

                    var diff1 = compilation.EmitDifference(prevBaseline, edits);

                    var genMd = diff1.GetMetadata();
                    disposables.Add(genMd);

                    EncValidation.VerifyModuleMvid(genId, prevReader, genMd.Reader);

                    VerifyCompilation(generation, genMd.Reader, $"generation {genId++}");

                    prevReader = genMd.Reader;
                    prevBaseline = diff1.NextGeneration;
                    prevCompilation = compilation;
                }
            }
            finally
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            }
        }

        private ImmutableArray<SemanticEdit> GetSemanticEdits(SemanticEditDescription[] edits, Compilation oldCompilation, Compilation newCompilation)
        {
            return ImmutableArray.CreateRange(edits.Select(e => new SemanticEdit(e.Kind, e.SymbolProvider(oldCompilation), e.SymbolProvider(newCompilation))));
        }

        private void VerifyCompilation(GenerationInfo generation, MetadataReader reader, string description)
        {
            _readers.Add(reader);

            if (generation.TypeDefs is not null)
            {
                var actual = _readers.GetStrings(reader.GetTypeDefNames());
                AssertEx.Equal(generation.TypeDefs, actual, message: $"TypeDefs don't match in {description}");
            }
            if (generation.MethodDefs is not null)
            {
                var actual = _readers.GetStrings(reader.GetMethodDefNames());
                AssertEx.Equal(generation.MethodDefs, actual, message: $"MethodDefs don't match in {description}");
            }
            if (generation.MemberRefs is not null)
            {
                var actual = _readers.GetStrings(reader.GetMemberRefNames());
                AssertEx.Equal(generation.MemberRefs, actual, message: $"MemberRefs don't match in {description}");
            }
            if (generation.CustomAttributesTableSize.HasValue)
            {
                AssertEx.AreEqual(generation.CustomAttributesTableSize.Value, reader.CustomAttributes.Count, message: $"CustomAttributes table size doesnt't match in {description}");
            }
            if (generation.EncLog is not null)
            {
                AssertEx.Equal(generation.EncLog, reader.GetEditAndContinueLogEntries(), itemInspector: EncLogRowToString, message: $"EncLog doesn't match in {description}");
            }
            if (generation.EncMap is not null)
            {
                AssertEx.Equal(generation.EncMap, reader.GetEditAndContinueMapEntries(), itemInspector: EncMapRowToString, message: $"EncMap doesn't match in {description}");
            }
        }

        private class GenerationInfo
        {
            public readonly string Source;
            public readonly SemanticEditDescription[]? Edits;
            public readonly string[]? TypeDefs;
            public readonly string[]? MethodDefs;
            public readonly string[]? MemberRefs;
            public readonly int? CustomAttributesTableSize;
            public readonly EditAndContinueLogEntry[]? EncLog;
            public readonly EntityHandle[]? EncMap;

            public GenerationInfo(string source, SemanticEditDescription[]? edits, string[]? typeDefs, string[]? methodDefs, string[]? memberRefs, int? customAttributesTableSize, EditAndContinueLogEntry[]? encLog, EntityHandle[]? encMap)
            {
                Source = source;
                Edits = edits;
                TypeDefs = typeDefs;
                MethodDefs = methodDefs;
                MemberRefs = memberRefs;
                CustomAttributesTableSize = customAttributesTableSize;
                EncLog = encLog;
                EncMap = encMap;
            }
        }
    }
}
