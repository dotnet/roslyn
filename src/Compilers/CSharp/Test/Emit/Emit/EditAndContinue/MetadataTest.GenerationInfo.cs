// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    internal partial class MetadataTest
    {
        internal class GenerationInfo
        {
            private readonly string _description;
            private readonly List<MetadataReader> _readers;

            public readonly CSharpCompilation Compilation;
            public readonly MetadataReader MetadataReader;
            public readonly EmitBaseline Baseline;

            public GenerationInfo(string description, List<MetadataReader> readers, CSharpCompilation compilation, MetadataReader reader, EmitBaseline baseline)
            {
                _description = description;
                _readers = readers;

                Compilation = compilation;
                MetadataReader = reader;
                Baseline = baseline;
            }

            internal void VerifyTypeDefNames(params string[] expected)
            {
                var actual = _readers.GetStrings(MetadataReader.GetTypeDefNames());
                AssertEx.Equal(expected, actual, message: $"TypeDefs don't match in {_description}");
            }

            internal void VerifyMethodDefNames(params string[] expected)
            {
                var actual = _readers.GetStrings(MetadataReader.GetMethodDefNames());
                AssertEx.Equal(expected, actual, message: $"MemberRefs don't match in {_description}");
            }

            internal void VerifyMemberRefNames(params string[] expected)
            {
                var actual = _readers.GetStrings(MetadataReader.GetMemberRefNames());
                AssertEx.Equal(expected, actual, message: $"MemberRefs don't match in {_description}");
            }

            internal void VerifyTableSize(TableIndex table, int expected)
            {
                AssertEx.AreEqual(expected, MetadataReader.GetTableRowCount(table), message: $"{table} table size doesnt't match in {_description}");
            }

            internal void VerifyEncLog(IEnumerable<EditAndContinueLogEntry> expected)
            {
                AssertEx.Equal(expected, MetadataReader.GetEditAndContinueLogEntries(), itemInspector: EditAndContinueTestBase.EncLogRowToString, message: $"EncLog doesn't match in {_description}");
            }

            internal void VerifyEncMap(IEnumerable<EntityHandle> expected)
            {
                AssertEx.Equal(expected, MetadataReader.GetEditAndContinueMapEntries(), itemInspector: EditAndContinueTestBase.EncMapRowToString, message: $"EncMap doesn't match in {_description}");
            }
        }
    }
}
