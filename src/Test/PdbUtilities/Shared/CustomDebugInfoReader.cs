// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.DiaSymReader;
using CDI = Microsoft.Cci.CustomDebugInfoConstants;

#pragma warning disable RS0010 // Avoid using cref tags with a prefix

namespace Microsoft.CodeAnalysis
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
            offset += CDI.CdiGlobalHeaderSize;
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

            offset += CDI.CdiRecordHeaderSize;
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

            return default(ImmutableArray<byte>);
        }

        /// <remarks>
        /// Exposed for <see cref="T:Roslyn.Test.PdbUtilities.PdbToXmlConverter"/>.
        /// </remarks>
        /// <exception cref="InvalidOperationException"></exception>
        public static IEnumerable<CustomDebugInfoRecord> GetCustomDebugInfoRecords(byte[] customDebugInfo)
        {
            if (customDebugInfo.Length < CDI.CdiGlobalHeaderSize)
            {
                throw new InvalidOperationException("Invalid header.");
            }

            int offset = 0;

            byte globalVersion;
            byte globalCount;
            ReadGlobalHeader(customDebugInfo, ref offset, out globalVersion, out globalCount);

            if (globalVersion != CDI.CdiVersion)
            {
                yield break;
            }

            while (offset <= customDebugInfo.Length - CDI.CdiRecordHeaderSize)
            {
                byte version;
                CustomDebugInfoKind kind;
                int size;
                int alignmentSize;

                ReadRecordHeader(customDebugInfo, ref offset, out version, out kind, out size, out alignmentSize);
                if (size < CDI.CdiRecordHeaderSize)
                {
                    throw new InvalidOperationException("Invalid header.");
                }

                if (kind != CustomDebugInfoKind.EditAndContinueLambdaMap &&
                    kind != CustomDebugInfoKind.EditAndContinueLocalSlotMap)
                {
                    // ignore alignment for CDIs that don't support it
                    alignmentSize = 0;
                }

                int bodySize = size - CDI.CdiRecordHeaderSize;
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
            int offset = 0;
            var numCounts = ReadInt16(bytes, ref offset);

            var builder = ArrayBuilder<short>.GetInstance(numCounts);
            for (int i = 0; i < numCounts; i++)
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
            int offset = 0;
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
            int offset = 0;
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
            int offset = 0;

            var bucketCount = ReadInt32(bytes, ref offset);

            var builder = ArrayBuilder<StateMachineHoistedLocalScope>.GetInstance(bucketCount);
            for (int i = 0; i < bucketCount; i++)
            {
                int startOffset = ReadInt32(bytes, ref offset);
                int endOffset = ReadInt32(bytes, ref offset);

                builder.Add(new StateMachineHoistedLocalScope(startOffset, endOffset));
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Indicates that this method is the iterator state machine for the method named in the record.
        /// </summary>
        /// <remarks>
        /// Appears when are iterator methods.
        /// Exposed for <see cref="T:Roslyn.Test.PdbUtilities.PdbToXmlConverter"/>.
        /// </remarks>
        public static string DecodeForwardIteratorRecord(ImmutableArray<byte> bytes)
        {
            int offset = 0;

            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;
            while (true)
            {
                char ch = (char)ReadInt16(bytes, ref offset);
                if (ch == 0)
                {
                    break;
                }

                builder.Append(ch);
            }

            return pooled.ToStringAndFree();
        }

        /// <summary>
        /// Does for locals what <see cref="System.Runtime.CompilerServices.DynamicAttribute"/> does for parameters, return types, and fields.
        /// In particular, indicates which occurrences of <see cref="object"/> in the signature are really dynamic.
        /// </summary>
        /// <remarks>
        /// Appears when there are dynamic locals.
        /// Exposed for <see cref="T:Roslyn.Test.PdbUtilities.PdbToXmlConverter"/>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Bad data.</exception>
        public static ImmutableArray<DynamicLocalBucket> DecodeDynamicLocalsRecord(ImmutableArray<byte> bytes)
        {
            int offset = 0;
            int bucketCount = ReadInt32(bytes, ref offset);

            var builder = ArrayBuilder<DynamicLocalBucket>.GetInstance(bucketCount);
            for (int i = 0; i < bucketCount; i++)
            {
                const int FlagBytesCount = 64;

                ulong flags = 0UL;
                for (int j = 0; j < FlagBytesCount; j++)
                {
                    var flag = ReadByte(bytes, ref offset) != 0;
                    if (flag)
                    {
                        flags |= 1UL << j;
                    }
                }

                int flagCount = ReadInt32(bytes, ref offset);
                int slotId = ReadInt32(bytes, ref offset);

                const int NameBytesCount = 128;
                var pooled = PooledStringBuilder.GetInstance();
                var nameBuilder = pooled.Builder;

                int nameEnd = offset + NameBytesCount;
                while (offset < nameEnd)
                {
                    char ch = (char)ReadInt16(bytes, ref offset);
                    if (ch == 0)
                    {
                        // The Identifier name takes 64 WCHAR no matter how big its actual length is.
                        offset = nameEnd;
                        break;
                    }

                    nameBuilder.Append(ch);
                }

                var name = pooled.ToStringAndFree();

                var bucket = new DynamicLocalBucket(flagCount, flags, slotId, name);
                builder.Add(bucket);
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Returns the raw bytes of a record.
        /// </summary>
        private static void ReadRawRecordBody(byte[] bytes, ref int offset, int size, out ImmutableArray<byte> body)
        {
            int bodySize = size - CDI.CdiRecordHeaderSize;
            body = ImmutableArray.Create(bytes, offset, bodySize);
            offset += bodySize;
        }

        /// <summary>
        /// Skips past a record.
        /// </summary>
        private static void SkipRecord(byte[] bytes, ref int offset, int size)
        {
            offset += size - CDI.CdiRecordHeaderSize;
        }

        /// <summary>
        /// Get the import strings for a given method, following forward pointers as necessary.
        /// </summary>
        /// <returns>
        /// For each namespace enclosing the method, a list of import strings, innermost to outermost.
        /// There should always be at least one entry, for the global namespace.
        /// </returns>
        public static ImmutableArray<ImmutableArray<string>> GetCSharpGroupedImportStrings(this ISymUnmanagedReader reader, int methodToken, int methodVersion, out ImmutableArray<string> externAliasStrings)
        {
            externAliasStrings = default(ImmutableArray<string>);

            ImmutableArray<short> groupSizes = default(ImmutableArray<short>);
            bool seenForward = false;

        RETRY:
            byte[] bytes = reader.GetCustomDebugInfoBytes(methodToken, methodVersion);
            if (bytes == null)
            {
                return default(ImmutableArray<ImmutableArray<string>>);
            }

            foreach (var record in GetCustomDebugInfoRecords(bytes))
            {
                switch (record.Kind)
                {
                    case CustomDebugInfoKind.UsingInfo:
                        if (!groupSizes.IsDefault)
                        {
                            throw new InvalidOperationException(string.Format("Expected at most one Using record for method {0}", FormatMethodToken(methodToken)));
                        }

                        groupSizes = DecodeUsingRecord(record.Data);
                        break;

                    case CustomDebugInfoKind.ForwardInfo:
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

                    case CustomDebugInfoKind.ForwardToModuleInfo:
                        if (!externAliasStrings.IsDefault)
                        {
                            throw new InvalidOperationException(string.Format("Expected at most one ForwardToModule record for method {0}", FormatMethodToken(methodToken)));
                        }

                        int moduleInfoMethodToken = DecodeForwardToModuleRecord(record.Data);
                        ImmutableArray<string> allModuleInfoImportStrings = reader.GetMethodByVersion(moduleInfoMethodToken, methodVersion).GetImportStrings();
                        ArrayBuilder<string> externAliasBuilder = ArrayBuilder<string>.GetInstance();

                        foreach (string importString in allModuleInfoImportStrings)
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
                return default(ImmutableArray<ImmutableArray<string>>);
            }

            var method = reader.GetMethodByVersion(methodToken, methodVersion);
            if (method == null)
            {
                return default(ImmutableArray<ImmutableArray<string>>);
            }

            ImmutableArray<string> importStrings = method.GetImportStrings();
            int numImportStrings = importStrings.Length;

            ArrayBuilder<ImmutableArray<string>> resultBuilder = ArrayBuilder<ImmutableArray<string>>.GetInstance(groupSizes.Length);
            ArrayBuilder<string> groupBuilder = ArrayBuilder<string>.GetInstance();

            int pos = 0;

            foreach (short groupSize in groupSizes)
            {
                for (int i = 0; i < groupSize; i++, pos++)
                {
                    if (pos >= numImportStrings)
                    {
                        throw new InvalidOperationException(string.Format("Group size indicates more imports than there are import strings (method {0}).", FormatMethodToken(methodToken)));
                    }

                    string importString = importStrings[pos];
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
                for (; pos < numImportStrings; pos++)
                {
                    string importString = importStrings[pos];
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

                if (pos < numImportStrings)
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
        public static ImmutableArray<string> GetVisualBasicImportStrings(this ISymUnmanagedReader reader, int methodToken, int methodVersion)
        {
            ImmutableArray<string> importStrings = reader.GetMethodByVersion(methodToken, methodVersion).GetImportStrings();

            // Follow at most one forward link.
            if (importStrings.Length > 0)
            {
                // As in PdbUtil::GetRawNamespaceListCore, we consider only the first string when
                // checking for forwarding.
                string importString = importStrings[0];
                if (importString.Length >= 2 && importString[0] == '@')
                {
                    char ch1 = importString[1];
                    if ('0' <= ch1 && ch1 <= '9')
                    {
                        int tempMethodToken;
                        if (int.TryParse(importString.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out tempMethodToken))
                        {
                            return reader.GetMethodByVersion(tempMethodToken, methodVersion).GetImportStrings();
                        }
                    }
                }
            }

            return importStrings;
        }

        // TODO (https://github.com/dotnet/roslyn/issues/702): caller should depend on abstraction
        /// <exception cref="InvalidOperationException">Bad data.</exception>
        public static void GetCSharpDynamicLocalInfo(
            byte[] customDebugInfo,
            int methodToken,
            int methodVersion,
            ArrayBuilder<ISymUnmanagedScope> scopes,
            out ImmutableDictionary<int, ImmutableArray<bool>> dynamicLocalMap,
            out ImmutableDictionary<string, ImmutableArray<bool>> dynamicLocalConstantMap)
        {
            dynamicLocalMap = ImmutableDictionary<int, ImmutableArray<bool>>.Empty;
            dynamicLocalConstantMap = ImmutableDictionary<string, ImmutableArray<bool>>.Empty;

            var record = TryGetCustomDebugInfoRecord(customDebugInfo, CustomDebugInfoKind.DynamicLocals);
            if (record.IsDefault)
            {
                return;
            }

            ImmutableDictionary<int, ImmutableArray<bool>>.Builder localBuilder = null;
            ImmutableDictionary<string, ImmutableArray<bool>>.Builder constantBuilder = null;

            var buckets = RemoveAmbiguousLocals(DecodeDynamicLocalsRecord(record), scopes);
            foreach (var bucket in buckets)
            {
                var slot = bucket.SlotId;
                var flags = GetFlags(bucket);
                if (slot < 0)
                {
                    constantBuilder = constantBuilder ?? ImmutableDictionary.CreateBuilder<string, ImmutableArray<bool>>();
                    constantBuilder.Add(bucket.Name, flags);
                }
                else
                {
                    localBuilder = localBuilder ?? ImmutableDictionary.CreateBuilder<int, ImmutableArray<bool>>();
                    localBuilder.Add(slot, flags);
                }
            }

            if (localBuilder != null)
            {
                dynamicLocalMap = localBuilder.ToImmutable();
            }

            if (constantBuilder != null)
            {
                dynamicLocalConstantMap = constantBuilder.ToImmutable();
            }
        }

        /// <summary>
        /// If there dynamic locals or constants with SlotId == 0, check all locals and
        /// constants with SlotId == 0 for duplicate names and discard duplicates since we
        /// cannot determine which local or constant the dynamic info is associated with.
        /// </summary>
        private static ImmutableArray<DynamicLocalBucket> RemoveAmbiguousLocals(
            ImmutableArray<DynamicLocalBucket> locals,
            ArrayBuilder<ISymUnmanagedScope> scopes)
        {
            var localsAndConstants = PooledDictionary<string, object>.GetInstance();
            var firstLocal = GetFirstLocal(scopes);
            if (firstLocal != null)
            {
                localsAndConstants.Add(firstLocal.GetName(), firstLocal);
            }
            foreach (var scope in scopes)
            {
                foreach (var constant in scope.GetConstants())
                {
                    var name = constant.GetName();
                    localsAndConstants[name] = localsAndConstants.ContainsKey(name) ? null : constant;
                }
            }
            var builder = ArrayBuilder<DynamicLocalBucket>.GetInstance();
            foreach (var local in locals)
            {
                int slot = local.SlotId;
                var name = local.Name;
                if (slot == 0)
                {
                    object localOrConstant;
                    localsAndConstants.TryGetValue(name, out localOrConstant);
                    if (localOrConstant == null)
                    {
                        // Duplicate.
                        continue;
                    }
                    if (localOrConstant != firstLocal)
                    {
                        // Constant.
                        slot = -1;
                    }
                }
                builder.Add(new DynamicLocalBucket(local.FlagCount, local.Flags, slot, name));
            }
            var result = builder.ToImmutableAndFree();
            localsAndConstants.Free();
            return result;
        }

        private static ISymUnmanagedVariable GetFirstLocal(ArrayBuilder<ISymUnmanagedScope> scopes)
        {
            foreach (var scope in scopes)
            {
                foreach (var local in scope.GetLocals())
                {
                    if (local.GetSlot() == 0)
                    {
                        return local;
                    }
                }
            }
            return null;
        }

        private static ImmutableArray<bool> GetFlags(DynamicLocalBucket bucket)
        {
            int flagCount = bucket.FlagCount;
            ulong flags = bucket.Flags;
            var builder = ArrayBuilder<bool>.GetInstance(flagCount);
            for (int i = 0; i < flagCount; i++)
            {
                builder.Add((flags & (1u << i)) != 0);
            }
            return builder.ToImmutableAndFree();
        }

        private static void CheckVersion(byte globalVersion, int methodToken)
        {
            if (globalVersion != CDI.CdiVersion)
            {
                throw new InvalidOperationException(string.Format("Method {0}: Expected version {1}, but found version {2}.", FormatMethodToken(methodToken), CDI.CdiVersion, globalVersion));
            }
        }

        private static int ReadInt32(ImmutableArray<byte> bytes, ref int offset)
        {
            int i = offset;
            if (i + sizeof(int) > bytes.Length)
            {
                throw new InvalidOperationException("Read out of buffer.");
            }

            offset += sizeof(int);
            return bytes[i] | (bytes[i + 1] << 8) | (bytes[i + 2] << 16) | (bytes[i + 3] << 24);
        }

        private static short ReadInt16(ImmutableArray<byte> bytes, ref int offset)
        {
            int i = offset;
            if (i + sizeof(short) > bytes.Length)
            {
                throw new InvalidOperationException("Read out of buffer.");
            }

            offset += sizeof(short);
            return (short)(bytes[i] | (bytes[i + 1] << 8));
        }

        private static byte ReadByte(ImmutableArray<byte> bytes, ref int offset)
        {
            int i = offset;
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
        public static bool TryParseCSharpImportString(string import, out string alias, out string externAlias, out string target, out ImportTargetKind kind)
        {
            alias = null;
            externAlias = null;
            target = null;
            kind = default(ImportTargetKind);

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
        public static bool TryParseVisualBasicImportString(string import, out string alias, out string target, out ImportTargetKind kind, out ImportScope scope)
        {
            alias = null;
            target = null;
            kind = default(ImportTargetKind);
            scope = default(ImportScope);

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
                scope = ImportScope.Unspecified;
                return true;
            }

            int pos = 0;
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
                    scope = ImportScope.Unspecified;
                    return true;
                case '*': // VB default namespace
                    // see PEBuilder.cpp in vb\language\CodeGen
                    pos++;
                    alias = null;
                    target = import.Substring(pos);
                    kind = ImportTargetKind.DefaultNamespace;
                    scope = ImportScope.Unspecified;
                    return true;
                case '@': // VB cases other than default and current namespace
                    // see PEBuilder.cpp in vb\language\CodeGen
                    pos++;
                    if (pos >= import.Length)
                    {
                        return false;
                    }

                    scope = ImportScope.Unspecified;
                    switch (import[pos])
                    {
                        case 'F':
                            scope = ImportScope.File;
                            pos++;
                            break;
                        case 'P':
                            scope = ImportScope.Project;
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
                    scope = ImportScope.Unspecified;
                    return true;
            }
        }

        private static bool TrySplit(string input, int offset, char separator, out string before, out string after)
        {
            int separatorPos = input.IndexOf(separator, offset);

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
    }

    /// <remarks>
    /// Exposed for <see cref="T:Roslyn.Test.PdbUtilities.PdbToXmlConverter"/>.
    /// </remarks>
    internal struct CustomDebugInfoRecord
    {
        public readonly CustomDebugInfoKind Kind;
        public readonly byte Version;
        public readonly ImmutableArray<byte> Data;

        public CustomDebugInfoRecord(CustomDebugInfoKind kind, byte version, ImmutableArray<byte> data)
        {
            this.Kind = kind;
            this.Version = version;
            this.Data = data;
        }
    }

    internal enum ImportTargetKind
    {
        /// <summary>
        /// C# or VB namespace import.
        /// </summary>
        Namespace,

        /// <summary>
        /// C# or VB type import.
        /// </summary>
        Type,

        /// <summary>
        /// VB namespace or type alias target (not specified).
        /// </summary>
        NamespaceOrType,

        /// <summary>
        /// C# extern alias.
        /// </summary>
        Assembly,

        /// <summary>
        /// VB XML import.
        /// </summary>
        XmlNamespace,

        /// <summary>
        /// VB forwarding information (i.e. another method has the imports for this one).
        /// </summary>
        MethodToken,

        /// <summary>
        /// VB containing namespace (not an import).
        /// </summary>
        CurrentNamespace,

        /// <summary>
        /// VB root namespace (not an import).
        /// </summary>
        DefaultNamespace,

        /// <summary>
        /// A kind that is no longer used.
        /// </summary>
        Defunct,
    }

    internal enum ImportScope
    {
        Unspecified,
        File,
        Project,
    }

    internal struct StateMachineHoistedLocalScope
    {
        public readonly int StartOffset;
        public readonly int EndOffset;

        public StateMachineHoistedLocalScope(int startoffset, int endOffset)
        {
            this.StartOffset = startoffset;
            this.EndOffset = endOffset;
        }
    }

    internal struct DynamicLocalBucket
    {
        public readonly int FlagCount;
        public readonly ulong Flags;
        public readonly int SlotId;
        public readonly string Name;

        public DynamicLocalBucket(int flagCount, ulong flags, int slotId, string name)
        {
            this.FlagCount = flagCount;
            this.Flags = flags;
            this.SlotId = slotId;
            this.Name = name;
        }
    }

    /// <summary>
    /// The kinds of custom debug info that we know how to interpret.
    /// The values correspond to possible values of the "kind" byte
    /// in the record header.
    /// </summary>
    internal enum CustomDebugInfoKind : byte
    {
        UsingInfo = CDI.CdiKindUsingInfo,
        ForwardInfo = CDI.CdiKindForwardInfo,
        ForwardToModuleInfo = CDI.CdiKindForwardToModuleInfo,
        StateMachineHoistedLocalScopes = CDI.CdiKindStateMachineHoistedLocalScopes,
        ForwardIterator = CDI.CdiKindForwardIterator,
        DynamicLocals = CDI.CdiKindDynamicLocals,
        EditAndContinueLocalSlotMap = CDI.CdiKindEditAndContinueLocalSlotMap,
        EditAndContinueLambdaMap = CDI.CdiKindEditAndContinueLambdaMap,
    }
}
#pragma warning restore RS0010 // Avoid using cref tags with a prefix
