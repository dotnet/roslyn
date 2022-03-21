﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Test.Utilities;
using static Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests.EditAndContinueTestBase;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    internal partial class EditAndContinueTest
    {
        internal sealed class GenerationVerifier
        {
            private readonly int _ordinal;
            private readonly MetadataReader _metadataReader;
            private readonly IEnumerable<MetadataReader> _readers;

            public GenerationVerifier(int ordinal, MetadataReader metadataReader, IEnumerable<MetadataReader> readers)
            {
                _ordinal = ordinal;
                _metadataReader = metadataReader;
                _readers = readers;
            }

            private string GetAssertMessage(string message)
            {
                var ordinalDescription = _ordinal == 0 ? "initial baseline" : $"generation {_ordinal}";
                return $"Failure in {ordinalDescription}: {message}";
            }

            internal void VerifyTypeDefNames(params string[] expected)
            {
                var actual = _readers.GetStrings(_metadataReader.GetTypeDefNames());
                AssertEx.Equal(expected, actual, message: GetAssertMessage("TypeDefs don't match"));
            }

            internal void VerifyMethodDefNames(params string[] expected)
            {
                var actual = _readers.GetStrings(_metadataReader.GetMethodDefNames());
                AssertEx.Equal(expected, actual, message: GetAssertMessage("MethodDefs don't match"));
            }

            internal void VerifyMemberRefNames(params string[] expected)
            {
                var actual = _readers.GetStrings(_metadataReader.GetMemberRefNames());
                AssertEx.Equal(expected, actual, message: GetAssertMessage("MemberRefs don't match"));
            }

            internal void VerifyFieldDefNames(params string[] expected)
            {
                var actual = _readers.GetStrings(_metadataReader.GetFieldDefNames());
                AssertEx.Equal(expected, actual, message: GetAssertMessage("FieldDefs don't match"));
            }

            internal void VerifyPropertyDefNames(params string[] expected)
            {
                var actual = _readers.GetStrings(_metadataReader.GetPropertyDefNames());
                AssertEx.Equal(expected, actual, message: GetAssertMessage("PropertyDefs don't match"));
            }

            internal void VerifyTableSize(TableIndex table, int expected)
            {
                AssertEx.AreEqual(expected, _metadataReader.GetTableRowCount(table), message: GetAssertMessage($"{table} table size doesnt't match"));
            }

            internal void VerifyEncLog(IEnumerable<EditAndContinueLogEntry> expected)
            {
                AssertEx.Equal(expected, _metadataReader.GetEditAndContinueLogEntries(), itemInspector: EncLogRowToString, message: GetAssertMessage("EncLog doesn't match"));
            }

            internal void VerifyEncMap(IEnumerable<EntityHandle> expected)
            {
                AssertEx.Equal(expected, _metadataReader.GetEditAndContinueMapEntries(), itemInspector: EncMapRowToString, message: GetAssertMessage("EncMap doesn't match"));
            }

            internal void VerifyEncLogDefinitions(IEnumerable<EditAndContinueLogEntry> expected)
            {
                AssertEx.Equal(expected, _metadataReader.GetEditAndContinueLogEntries().Where(e => IsDefinition(e.Handle.Kind)), itemInspector: EncLogRowToString, message: GetAssertMessage("EncLog definitions don't match"));
            }

            internal void VerifyEncMapDefinitions(IEnumerable<EntityHandle> expected)
            {
                AssertEx.Equal(expected, _metadataReader.GetEditAndContinueMapEntries().Where(e => IsDefinition(e.Kind)), itemInspector: EncMapRowToString, message: GetAssertMessage("EncMap definitions don't match"));
            }

            internal void VerifyCustomAttributes(IEnumerable<CustomAttributeRow> expected)
            {
                AssertEx.Equal(expected, _metadataReader.GetCustomAttributeRows(), itemInspector: AttributeRowToString);
            }
        }
    }
}
