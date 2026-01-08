// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

#pragma warning disable CA1200 // Avoid using cref tags with a prefix

namespace Microsoft.CodeAnalysis.Debugging
{
    /// <summary>
    /// A collection of utility method for consuming custom debug info from a PDB.
    /// </summary>
    /// <remarks>
    /// This is not a public API, so we're just going to let bad offsets fail on their own.
    /// </remarks>
    internal static class CustomDebugInfoReader
    {
        /// <summary>
        /// This is the first header in the custom debug info blob.
        /// </summary>
        private static void ReadGlobalHeader(byte[] bytes, ref int offset, out byte version, out byte count)
        {
            version = bytes[offset + 0];
            count = bytes[offset + 1];
            offset += CustomDebugInfoConstants.GlobalHeaderSize;
        }

        /// <summary>
        /// After the global header (see <see cref="ReadGlobalHeader"/> comes list of custom debug info record.
        /// Each record begins with a standard header.
        /// </summary>
        private static void ReadRecordHeader(byte[] bytes, ref int offset, out byte version, out CustomDebugInfoKind kind, out int size, out int alignmentSize)
        {
            version = bytes[offset + 0];
            kind = (CustomDebugInfoKind)bytes[offset + 1];
            alignmentSize = bytes[offset + 3];

            // two bytes of padding after kind
            size = BitConverter.ToInt32(bytes, offset + 4);

            offset += CustomDebugInfoConstants.RecordHeaderSize;
        }

        /// <exception cref="InvalidOperationException"></exception>
        public static ImmutableArray<byte> TryGetCustomDebugInfoRecord(byte[] customDebugInfo, CustomDebugInfoKind recordKind)
        {
            foreach (var record in GetCustomDebugInfoRecords(customDebugInfo))
            {
                if (record.Kind == recordKind)
                {
                    return record.Data;
                }
            }

            return default;
        }

        /// <remarks>
        /// Exposed for <see cref="T:Roslyn.Test.PdbUtilities.PdbToXmlConverter"/>.
        /// </remarks>
        /// <exception cref="InvalidOperationException"></exception>
        public static IEnumerable<CustomDebugInfoRecord> GetCustomDebugInfoRecords(byte[] customDebugInfo)
        {
            if (customDebugInfo.Length < CustomDebugInfoConstants.GlobalHeaderSize)
            {
                throw new InvalidOperationException("Invalid header.");
            }

            var offset = 0;
            ReadGlobalHeader(customDebugInfo, ref offset, out var globalVersion, out _);

            if (globalVersion != CustomDebugInfoConstants.Version)
            {
                yield break;
            }

            while (offset <= customDebugInfo.Length - CustomDebugInfoConstants.RecordHeaderSize)
            {
                ReadRecordHeader(customDebugInfo, ref offset, out var version, out var kind, out var size, out var alignmentSize);
                if (size < CustomDebugInfoConstants.RecordHeaderSize)
                {
                    throw new InvalidOperationException("Invalid header.");
                }

                switch (kind)
                {
                    case CustomDebugInfoKind.EditAndContinueLambdaMap:
                    case CustomDebugInfoKind.EditAndContinueLocalSlotMap:
                    case CustomDebugInfoKind.TupleElementNames:
                        break;
                    default:
                        // ignore alignment for CDIs that don't support it
                        alignmentSize = 0;
                        break;
                }

                var bodySize = size - CustomDebugInfoConstants.RecordHeaderSize;
                if (offset > customDebugInfo.Length - bodySize || alignmentSize > 3 || alignmentSize > bodySize)
                {
                    throw new InvalidOperationException("Invalid header.");
                }

                yield return new CustomDebugInfoRecord(kind, version, ImmutableArray.Create(customDebugInfo, offset, bodySize - alignmentSize));
                offset += bodySize;
            }
        }

        /// <summary>
        /// For each namespace declaration enclosing a method (innermost-to-outermost), there is a count
        /// of the number of imports in that declaration.
        /// </summary>
        /// <remarks>
        /// There's always at least one entry (for the global namespace).
        /// Exposed for <see cref="T:Roslyn.Test.PdbUtilities.PdbToXmlConverter"/>.
        /// </remarks>
        public static ImmutableArray<short> DecodeUsingRecord(ImmutableArray<byte> bytes)
        {
            var offset = 0;
            var numCounts = ReadInt16(bytes, ref offset);

            var builder = ArrayBuilder<short>.GetInstance(numCounts);
            for (var i = 0; i < numCounts; i++)
            {
                builder.Add(ReadInt16(bytes, ref offset));
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// This indicates that further information can be obtained by looking at the custom debug
        /// info of another method (specified by token).
        /// </summary>
        /// <remarks>
        /// Appears when multiple method would otherwise have identical using records (see <see cref="DecodeUsingRecord"/>).
        /// Exposed for <see cref="T:Roslyn.Test.PdbUtilities.PdbToXmlConverter"/>.
        /// </remarks>
        public static int DecodeForwardRecord(ImmutableArray<byte> bytes)
        {
            var offset = 0;
            return ReadInt32(bytes, ref offset);
        }

        /// <summary>
        /// This indicates that further information can be obtained by looking at the custom debug
        /// info of another method (specified by token).
        /// </summary>
        /// <remarks>
        /// Appears when there are extern aliases and edit-and-continue is disabled.
        /// Exposed for <see cref="T:Roslyn.Test.PdbUtilities.PdbToXmlConverter"/>.
        /// </remarks>
        public static int DecodeForwardToModuleRecord(ImmutableArray<byte> bytes)
        {
            var offset = 0;
            return ReadInt32(bytes, ref offset);
        }

        /// <summary>
        /// Scopes of state machine hoisted local variables.
        /// </summary>
        /// <remarks>
        /// Exposed for <see cref="T:Roslyn.Test.PdbUtilities.PdbToXmlConverter"/>.
        /// </remarks>
        public static ImmutableArray<StateMachineHoistedLocalScope> DecodeStateMachineHoistedLocalScopesRecord(ImmutableArray<byte> bytes)
        {
            var offset = 0;

            var bucketCount = ReadInt32(bytes, ref offset);

            var builder = ArrayBuilder<StateMachineHoistedLocalScope>.GetInstance(bucketCount);
            for (var i = 0; i < bucketCount; i++)
            {
                var startOffset = ReadInt32(bytes, ref offset);
                var endOffset = ReadInt32(bytes, ref offset);

                // The range is stored as end-inclusive.
                // The case [0,0] is ambiguous in Windows PDBs.
                // It means either a user defined local with range [0, 1) or a synthesized local.
                // It is unlikely that a user local scope spans just 1B from the start of the method. 
                // Assume therefore that [0,0] means a synthesized local.
                if (startOffset != 0 || endOffset != 0)
                {
                    endOffset++;
                }

                builder.Add(new StateMachineHoistedLocalScope(startOffset, endOffset));
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Indicates that this method is the iterator state machine for the method named in the record.
        /// </summary>
        /// <remarks>
        /// Appears on kick-off methods of a state machine.
        /// Exposed for <see cref="T:Roslyn.Test.PdbUtilities.PdbToXmlConverter"/>.
        /// 
        /// Encodes NULL-terminated UTF16 name of the state machine type.
        /// The ending NULL character might not be present if the PDB was generated by an older compiler.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Bad data.</exception>
        public static string DecodeForwardIteratorRecord(ImmutableArray<byte> bytes)
        {
            var offset = 0;

            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;
            while (offset < bytes.Length)
            {
                var ch = (char)ReadInt16(bytes, ref offset);
                if (ch == 0)
                {
                    break;
                }

                builder.Append(ch);
            }

            return pooled.ToStringAndFree();
        }

        /// <summary>
        /// Does for locals what System.Runtime.CompilerServices.DynamicAttribute does for parameters, return types, and fields.
        /// In particular, indicates which occurrences of <see cref="object"/> in the signature are really dynamic.
        /// </summary>
        /// <remarks>
        /// Appears when there are dynamic locals.
        /// Exposed for <see cref="T:Roslyn.Test.PdbUtilities.PdbToXmlConverter"/>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Bad data.</exception>
        public static ImmutableArray<DynamicLocalInfo> DecodeDynamicLocalsRecord(ImmutableArray<byte> bytes)
        {
            const int FlagBytesCount = 64;

            var flagsBuilder = ArrayBuilder<bool>.GetInstance(FlagBytesCount);
            var pooledNameBuilder = PooledStringBuilder.GetInstance();
            var nameBuilder = pooledNameBuilder.Builder;

            var offset = 0;
            var bucketCount = ReadInt32(bytes, ref offset);
            var builder = ArrayBuilder<DynamicLocalInfo>.GetInstance(bucketCount);

            for (var i = 0; i < bucketCount; i++)
            {
                Debug.Assert(flagsBuilder.Count == 0);
                Debug.Assert(nameBuilder.Length == 0);

                for (var j = 0; j < FlagBytesCount; j++)
                {
                    flagsBuilder.Add(ReadByte(bytes, ref offset) != 0);
                }

                var flagCount = ReadInt32(bytes, ref offset);
                if (flagCount < flagsBuilder.Count)
                {
                    flagsBuilder.Count = flagCount;
                }

                var slotId = ReadInt32(bytes, ref offset);

                const int NameBytesCount = 128;
                var nameEnd = offset + NameBytesCount;
                while (offset < nameEnd)
                {
                    var ch = (char)ReadInt16(bytes, ref offset);
                    if (ch == 0)
                    {
                        // The Identifier name takes 64 WCHAR no matter how big its actual length is.
                        offset = nameEnd;
                        break;
                    }

                    nameBuilder.Append(ch);
                }

                builder.Add(new DynamicLocalInfo(flagsBuilder.ToImmutable(), slotId, nameBuilder.ToString()));

                flagsBuilder.Clear();
                nameBuilder.Clear();
            }

            flagsBuilder.Free();
            pooledNameBuilder.Free();
            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Tuple element names for locals.
        /// </summary>
        public static ImmutableArray<TupleElementNamesInfo> DecodeTupleElementNamesRecord(ImmutableArray<byte> bytes)
        {
            var offset = 0;
            var n = ReadInt32(bytes, ref offset);
            var builder = ArrayBuilder<TupleElementNamesInfo>.GetInstance(n);
            for (var i = 0; i < n; i++)
            {
                builder.Add(DecodeTupleElementNamesInfo(bytes, ref offset));
            }

            return builder.ToImmutableAndFree();
        }

        private static TupleElementNamesInfo DecodeTupleElementNamesInfo(ImmutableArray<byte> bytes, ref int offset)
        {
            var n = ReadInt32(bytes, ref offset);
            var builder = ArrayBuilder<string?>.GetInstance(n);
            for (var i = 0; i < n; i++)
            {
                var value = ReadUtf8String(bytes, ref offset);
                builder.Add(string.IsNullOrEmpty(value) ? null : value);
            }

            var slotIndex = ReadInt32(bytes, ref offset);
            var scopeStart = ReadInt32(bytes, ref offset);
            var scopeEnd = ReadInt32(bytes, ref offset);
            var localName = ReadUtf8String(bytes, ref offset);
            return new TupleElementNamesInfo(builder.ToImmutableAndFree(), slotIndex, localName, scopeStart, scopeEnd);
        }

        /// <summary>
        /// Get the import strings for a given method, following forward pointers as necessary.
        /// </summary>
        /// <returns>
        /// For each namespace enclosing the method, a list of import strings, innermost to outermost.
        /// There should always be at least one entry, for the global namespace.
        /// </returns>
        public static ImmutableArray<ImmutableArray<string>> GetCSharpGroupedImportStrings<TArg>(
            int methodToken,
            TArg arg,
            Func<int, TArg, byte[]?> getMethodCustomDebugInfo,
            Func<int, TArg, ImmutableArray<string>> getMethodImportStrings,
            out ImmutableArray<string> externAliasStrings)
        {
            externAliasStrings = default;

            ImmutableArray<short> groupSizes = default;
            var seenForward = false;

RETRY:
            var bytes = getMethodCustomDebugInfo(methodToken, arg);
            if (bytes == null)
            {
                return default;
            }

            foreach (var record in GetCustomDebugInfoRecords(bytes))
            {
                switch (record.Kind)
                {
                    case CustomDebugInfoKind.UsingGroups:
                        if (!groupSizes.IsDefault)
                        {
                            throw new InvalidOperationException(string.Format("Expected at most one Using record for method {0}", FormatMethodToken(methodToken)));
                        }

                        groupSizes = DecodeUsingRecord(record.Data);
                        break;

                    case CustomDebugInfoKind.ForwardMethodInfo:
                        if (!externAliasStrings.IsDefault)
                        {
                            throw new InvalidOperationException(string.Format("Did not expect both Forward and ForwardToModule records for method {0}", FormatMethodToken(methodToken)));
                        }

                        methodToken = DecodeForwardRecord(record.Data);

                        // Follow at most one forward link (as in FUNCBRECEE::ensureNamespaces).
                        // NOTE: Dev11 may produce chains of forward links (e.g. for System.Collections.Immutable).
                        if (!seenForward)
                        {
                            seenForward = true;
                            goto RETRY;
                        }

                        break;

                    case CustomDebugInfoKind.ForwardModuleInfo:
                        if (!externAliasStrings.IsDefault)
                        {
                            throw new InvalidOperationException(string.Format("Expected at most one ForwardToModule record for method {0}", FormatMethodToken(methodToken)));
                        }

                        var moduleInfoMethodToken = DecodeForwardToModuleRecord(record.Data);

                        var allModuleInfoImportStrings = getMethodImportStrings(moduleInfoMethodToken, arg);
                        Debug.Assert(!allModuleInfoImportStrings.IsDefault);

                        var externAliasBuilder = ArrayBuilder<string>.GetInstance();

                        foreach (var importString in allModuleInfoImportStrings)
                        {
                            if (IsCSharpExternAliasInfo(importString))
                            {
                                externAliasBuilder.Add(importString);
                            }
                        }

                        externAliasStrings = externAliasBuilder.ToImmutableAndFree();
                        break;
                }
            }

            if (groupSizes.IsDefault)
            {
                // This can happen in malformed PDBs (e.g. chains of forwards).
                return default;
            }

            var importStrings = getMethodImportStrings(methodToken, arg);
            Debug.Assert(!importStrings.IsDefault);

            var resultBuilder = ArrayBuilder<ImmutableArray<string>>.GetInstance(groupSizes.Length);
            var groupBuilder = ArrayBuilder<string>.GetInstance();
            var pos = 0;

            foreach (var groupSize in groupSizes)
            {
                for (var i = 0; i < groupSize; i++, pos++)
                {
                    if (pos >= importStrings.Length)
                    {
                        throw new InvalidOperationException(string.Format("Group size indicates more imports than there are import strings (method {0}).", FormatMethodToken(methodToken)));
                    }

                    var importString = importStrings[pos];
                    if (IsCSharpExternAliasInfo(importString))
                    {
                        throw new InvalidOperationException(string.Format("Encountered extern alias info before all import strings were consumed (method {0}).", FormatMethodToken(methodToken)));
                    }

                    groupBuilder.Add(importString);
                }

                resultBuilder.Add(groupBuilder.ToImmutable());
                groupBuilder.Clear();
            }

            if (externAliasStrings.IsDefault)
            {
                Debug.Assert(groupBuilder.Count == 0);

                // Extern alias detail strings (prefix "Z") are not included in the group counts.
                for (; pos < importStrings.Length; pos++)
                {
                    var importString = importStrings[pos];
                    if (!IsCSharpExternAliasInfo(importString))
                    {
                        throw new InvalidOperationException(string.Format("Expected only extern alias info strings after consuming the indicated number of imports (method {0}).", FormatMethodToken(methodToken)));
                    }

                    groupBuilder.Add(importString);
                }

                externAliasStrings = groupBuilder.ToImmutableAndFree();
            }
            else
            {
                groupBuilder.Free();

                if (pos < importStrings.Length)
                {
                    throw new InvalidOperationException(string.Format("Group size indicates fewer imports than there are import strings (method {0}).", FormatMethodToken(methodToken)));
                }
            }

            return resultBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Get the import strings for a given method, following forward pointers as necessary.
        /// </summary>
        /// <returns>
        /// A list of import strings.  There should always be at least one entry, for the global namespace.
        /// </returns>
        public static ImmutableArray<string> GetVisualBasicImportStrings<TArg>(
            int methodToken,
            TArg arg,
            Func<int, TArg, ImmutableArray<string>> getMethodImportStrings)
        {
            var importStrings = getMethodImportStrings(methodToken, arg);
            Debug.Assert(!importStrings.IsDefault);

            if (importStrings.IsEmpty)
            {
                return ImmutableArray<string>.Empty;
            }

            // Follow at most one forward link.
            // As in PdbUtil::GetRawNamespaceListCore, we consider only the first string when
            // checking for forwarding.
            var importString = importStrings[0];
            if (importString.Length >= 2 && importString[0] == '@')
            {
                var ch1 = importString[1];
                if (ch1 is >= '0' and <= '9')
                {
                    if (int.TryParse(importString.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out var tempMethodToken))
                    {
                        importStrings = getMethodImportStrings(tempMethodToken, arg);
                        Debug.Assert(!importStrings.IsDefault);
                    }
                }
            }

            return importStrings;
        }

        private static int ReadInt32(ImmutableArray<byte> bytes, ref int offset)
        {
            var i = offset;
            if (i + sizeof(int) > bytes.Length)
            {
                throw new InvalidOperationException("Read out of buffer.");
            }

            offset += sizeof(int);
            return bytes[i] | (bytes[i + 1] << 8) | (bytes[i + 2] << 16) | (bytes[i + 3] << 24);
        }

        private static short ReadInt16(ImmutableArray<byte> bytes, ref int offset)
        {
            var i = offset;
            if (i + sizeof(short) > bytes.Length)
            {
                throw new InvalidOperationException("Read out of buffer.");
            }

            offset += sizeof(short);
            return (short)(bytes[i] | (bytes[i + 1] << 8));
        }

        private static byte ReadByte(ImmutableArray<byte> bytes, ref int offset)
        {
            var i = offset;
            if (i + sizeof(byte) > bytes.Length)
            {
                throw new InvalidOperationException("Read out of buffer.");
            }

            offset += sizeof(byte);
            return bytes[i];
        }

        private static bool IsCSharpExternAliasInfo(string import)
        {
            return import.Length > 0 && import[0] == 'Z';
        }

        /// <summary>
        /// Parse a string representing a C# using (or extern alias) directive.
        /// </summary>
        /// <remarks>
        /// <![CDATA[
        /// For C#:
        ///  "USystem" -> <namespace name="System" />
        ///  "AS USystem" -> <alias name="S" target="System" kind="namespace" />
        ///  "AC TSystem.Console" -> <alias name="C" target="System.Console" kind="type" />
        ///  "AS ESystem alias" -> <alias name="S" qualifier="alias" target="System" kind="type" />
        ///  "XOldLib" -> <extern alias="OldLib" />
        ///  "ZOldLib assembly" -> <externinfo name="OldLib" assembly="assembly" />
        ///  "ESystem alias" -> <namespace qualifier="alias" name="System" />
        ///  "TSystem.Math" -> <type name="System.Math" />
        /// ]]>
        /// </remarks>
        public static bool TryParseCSharpImportString(string import, out string? alias, out string? externAlias, out string? target, out ImportTargetKind kind)
        {
            alias = null;
            externAlias = null;
            target = null;
            kind = default;

            if (string.IsNullOrEmpty(import))
            {
                return false;
            }

            switch (import[0])
            {
                case 'U': // C# (namespace) using
                    alias = null;
                    externAlias = null;
                    target = import.Substring(1);
                    kind = ImportTargetKind.Namespace;
                    return true;

                case 'E': // C# (namespace) using
                    // NOTE: Dev12 has related cases "I" and "O" in EMITTER::ComputeDebugNamespace,
                    // but they were probably implementation details that do not affect Roslyn.
                    if (!TrySplit(import, 1, ' ', out target, out externAlias))
                    {
                        return false;
                    }

                    alias = null;
                    kind = ImportTargetKind.Namespace;
                    return true;

                case 'T': // C# (type) using
                    alias = null;
                    externAlias = null;
                    target = import.Substring(1);
                    kind = ImportTargetKind.Type;
                    return true;

                case 'A': // C# type or namespace alias
                    if (!TrySplit(import, 1, ' ', out alias, out target))
                    {
                        return false;
                    }

                    switch (target[0])
                    {
                        case 'U':
                            kind = ImportTargetKind.Namespace;
                            target = target.Substring(1);
                            externAlias = null;
                            return true;

                        case 'T':
                            kind = ImportTargetKind.Type;
                            target = target.Substring(1);
                            externAlias = null;
                            return true;

                        case 'E':
                            kind = ImportTargetKind.Namespace; // Never happens for types.
                            if (!TrySplit(target, 1, ' ', out target, out externAlias))
                            {
                                return false;
                            }

                            return true;

                        default:
                            return false;
                    }

                case 'X': // C# extern alias (in file)
                    externAlias = null;
                    alias = import.Substring(1); // For consistency with the portable format, store it in alias, rather than externAlias.
                    target = null;
                    kind = ImportTargetKind.Assembly;
                    return true;

                case 'Z': // C# extern alias (module-level)
                    // For consistency with the portable format, store it in alias, rather than externAlias.
                    if (!TrySplit(import, 1, ' ', out alias, out target))
                    {
                        return false;
                    }

                    externAlias = null;
                    kind = ImportTargetKind.Assembly;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Parse a string representing a VB import statement.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="import"/> is null.</exception>
        /// <exception cref="ArgumentException">Format of <paramref name="import"/> is not valid.</exception>
        public static bool TryParseVisualBasicImportString(string import, out string? alias, out string? target, out ImportTargetKind kind, out VBImportScopeKind scope)
        {
            alias = null;
            target = null;
            kind = default;
            scope = default;

            if (import == null)
            {
                return false;
            }

            // VB current namespace
            if (import.Length == 0)
            {
                alias = null;
                target = import;
                kind = ImportTargetKind.CurrentNamespace;
                scope = VBImportScopeKind.Unspecified;
                return true;
            }

            var pos = 0;
            switch (import[pos])
            {
                case '&':
                // Indicates the presence of embedded PIA types from a given assembly.  No longer required (as of Roslyn).
                case '$':
                case '#':
                    // From ProcedureContext::LoadImportsAndDefaultNamespaceNormal:
                    //   "Module Imports and extension types are no longer needed since we are not doing custom name lookup"
                    alias = null;
                    target = import;
                    kind = ImportTargetKind.Defunct;
                    scope = VBImportScopeKind.Unspecified;
                    return true;
                case '*': // VB default namespace
                    // see PEBuilder.cpp in vb\language\CodeGen
                    pos++;
                    alias = null;
                    target = import.Substring(pos);
                    kind = ImportTargetKind.DefaultNamespace;
                    scope = VBImportScopeKind.Unspecified;
                    return true;
                case '@': // VB cases other than default and current namespace
                    // see PEBuilder.cpp in vb\language\CodeGen
                    pos++;
                    if (pos >= import.Length)
                    {
                        return false;
                    }

                    scope = VBImportScopeKind.Unspecified;
                    switch (import[pos])
                    {
                        case 'F':
                            scope = VBImportScopeKind.File;
                            pos++;
                            break;
                        case 'P':
                            scope = VBImportScopeKind.Project;
                            pos++;
                            break;
                    }

                    if (pos >= import.Length)
                    {
                        return false;
                    }

                    switch (import[pos])
                    {
                        case 'A':
                            pos++;

                            if (import[pos] != ':')
                            {
                                return false;
                            }

                            pos++;

                            if (!TrySplit(import, pos, '=', out alias, out target))
                            {
                                return false;
                            }

                            kind = ImportTargetKind.NamespaceOrType;
                            return true;

                        case 'X':
                            pos++;

                            if (import[pos] != ':')
                            {
                                return false;
                            }

                            pos++;

                            if (!TrySplit(import, pos, '=', out alias, out target))
                            {
                                return false;
                            }

                            kind = ImportTargetKind.XmlNamespace;
                            return true;

                        case 'T':
                            pos++;

                            if (import[pos] != ':')
                            {
                                return false;
                            }

                            pos++;

                            alias = null;
                            target = import.Substring(pos);
                            kind = ImportTargetKind.Type;
                            return true;

                        case ':':
                            pos++;
                            alias = null;
                            target = import.Substring(pos);
                            kind = ImportTargetKind.Namespace;
                            return true;

                        default:
                            alias = null;
                            target = import.Substring(pos);
                            kind = ImportTargetKind.MethodToken;
                            return true;
                    }

                default:
                    // VB current namespace
                    alias = null;
                    target = import;
                    kind = ImportTargetKind.CurrentNamespace;
                    scope = VBImportScopeKind.Unspecified;
                    return true;
            }
        }

        private static bool TrySplit(string input, int offset, char separator, [NotNullWhen(true)] out string? before, [NotNullWhen(true)] out string? after)
        {
            var separatorPos = input.IndexOf(separator, offset);

            // Allow zero-length before for the global namespace (empty string).
            // Allow zero-length after for an XML alias in VB ("@PX:=").  Not sure what it means.
            if (offset <= separatorPos && separatorPos < input.Length)
            {
                before = input.Substring(offset, separatorPos - offset);
                after = separatorPos + 1 == input.Length
                    ? ""
                    : input.Substring(separatorPos + 1);
                return true;
            }

            before = null;
            after = null;
            return false;
        }

        private static string FormatMethodToken(int methodToken)
        {
            return string.Format("0x{0:x8}", methodToken);
        }

        /// <summary>
        /// Read UTF-8 string with null terminator.
        /// </summary>
        private static string ReadUtf8String(ImmutableArray<byte> bytes, ref int offset)
        {
            var builder = ArrayBuilder<byte>.GetInstance();
            while (true)
            {
                var b = ReadByte(bytes, ref offset);
                if (b == 0)
                {
                    break;
                }

                builder.Add(b);
            }

            var block = builder.ToArrayAndFree();
            return Encoding.UTF8.GetString(block, 0, block.Length);
        }

        /// <summary>
        /// Returns <see cref="CustomDebugInformation"/> of the specified kind associated with the given handle if there is a single such entry.
        /// </summary>
        /// <exception cref="BadImageFormatException">The PDB is malformed or there are multiple entries of the given <paramref name="kind"/>.</exception>
        public static bool TryGetCustomDebugInformation(this MetadataReader reader, EntityHandle handle, Guid kind, out CustomDebugInformation customDebugInfo)
        {
            var foundAny = false;
            customDebugInfo = default;
            foreach (var infoHandle in reader.GetCustomDebugInformation(handle))
            {
                var info = reader.GetCustomDebugInformation(infoHandle);
                var id = reader.GetGuid(info.Kind);
                if (id == kind)
                {
                    if (foundAny)
                    {
                        throw new BadImageFormatException();
                    }

                    customDebugInfo = info;
                    foundAny = true;
                }
            }

            return foundAny;
        }

        /// <summary>
        /// Reads compilation options custom debug information.
        /// https://github.com/dotnet/runtime/blob/ef5b188467e37b28c952ea9f2fd423422365f90a/docs/design/specs/PortablePdb-Metadata.md#compilation-options-c-and-vb-compilers
        /// </summary>
        /// <exception cref="BadImageFormatException">The PDB is malformed.</exception>
        public static ImmutableDictionary<string, string> GetCompilationOptions(this MetadataReader pdbReader)
        {
            if (!pdbReader.TryGetCustomDebugInformation(EntityHandle.ModuleDefinition, PortableCustomDebugInfoKinds.CompilationOptions, out var customDebugInformation))
            {
                return ImmutableDictionary<string, string>.Empty;
            }

            var result = PooledDictionary<string, string>.GetInstance();
            try
            {
                var blobReader = pdbReader.GetBlobReader(customDebugInformation.Value);

                var name = ReadNullTerminatedString(ref blobReader) ?? throw new BadImageFormatException();
                var value = ReadNullTerminatedString(ref blobReader) ?? throw new BadImageFormatException();

                // There shall be no two entries with the same name in the list.
                if (result.ContainsKey(name))
                {
                    throw new BadImageFormatException();
                }

                // The spec allows an empty name.
                result.Add(name, value);

                static string? ReadNullTerminatedString(ref BlobReader reader)
                {
                    var nullIndex = reader.IndexOf(0);
                    if (nullIndex == -1)
                    {
                        return null;
                    }

                    var value = reader.ReadUTF8(nullIndex);
                    reader.ReadByte();
                    return value;
                }

                return result.ToImmutableDictionary();
            }
            finally
            {
                result.Free();
            }
        }
    }
}
