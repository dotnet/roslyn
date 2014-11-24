// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using CDI = Microsoft.Cci.CustomDebugInfoConstants;

namespace Roslyn.Utilities.Pdb
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
        public static void ReadGlobalHeader(byte[] bytes, ref int offset, out byte version, out byte count)
        {
            version = bytes[offset + 0];
            count = bytes[offset + 1];
            offset += CDI.CdiGlobalHeaderSize;
        }

        /// <summary>
        /// After the global header (see <see cref="ReadGlobalHeader"/> comes list of custom debug info record.
        /// Each record begins with a standard header.
        /// </summary>
        public static void ReadRecordHeader(byte[] bytes, ref int offset, out byte version, out CustomDebugInfoKind kind, out int size)
        {
            version = bytes[offset + 0];
            kind = (CustomDebugInfoKind)bytes[offset + 1];

            // two bytes of padding after kind
            size = BitConverter.ToInt32(bytes, offset + 4); 

            offset += CDI.CdiRecordHeaderSize;
        }

        internal static ImmutableArray<byte> TryGetCustomDebugInfoRecord(byte[] customDebugInfo, CustomDebugInfoKind recordKind)
        {
            if (customDebugInfo.Length < CDI.CdiGlobalHeaderSize)
            {
                return default(ImmutableArray<byte>);
            }

            int offset = 0;

            byte globalVersion;
            byte globalCount;
            ReadGlobalHeader(customDebugInfo, ref offset, out globalVersion, out globalCount);

            if (globalVersion != CDI.CdiVersion)
            {
                return default(ImmutableArray<byte>);
            }

            while (offset <= customDebugInfo.Length - CDI.CdiRecordHeaderSize)
            {
                byte version;
                CustomDebugInfoKind kind;
                int size;

                ReadRecordHeader(customDebugInfo, ref offset, out version, out kind, out size);
                if (size < CDI.CdiRecordHeaderSize)
                {
                    // invalid header
                    break;
                }

                int bodySize = size - CDI.CdiRecordHeaderSize;
                if (offset > customDebugInfo.Length - bodySize)
                {
                    // invalid header
                    break;
                }

                if (version != CDI.CdiVersion || kind != recordKind)
                {
                    offset += bodySize;
                    continue;
                }

                return ImmutableArray.Create(customDebugInfo, offset, bodySize);
            }

            return default(ImmutableArray<byte>);
        }

        /// <summary>
        /// For each namespace declaration enclosing a method (innermost-to-outermost), there is a count
        /// of the number of imports in that declaration.
        /// </summary>
        /// <remarks>
        /// There's always at least one entry (for the global namespace).
        /// </remarks>
        public static void ReadUsingRecord(byte[] bytes, ref int offset, int size, out ImmutableArray<short> counts)
        {
            int tempOffset = offset;

            var numCounts = BitConverter.ToInt16(bytes, tempOffset);
            tempOffset += 2;

            var builder = ArrayBuilder<short>.GetInstance(numCounts);
            for (int i = 0; i < numCounts; i++)
            {
                short count = BitConverter.ToInt16(bytes, tempOffset);
                tempOffset += 2;

                builder.Add(count);
            }

            counts = builder.ToImmutableAndFree();

            offset += size - CDI.CdiRecordHeaderSize;
            Debug.Assert(offset >= tempOffset);
        }

        /// <summary>
        /// This indicates that further information can be obtained by looking at the custom debug
        /// info of another method (specified by token).
        /// </summary>
        /// <remarks>
        /// Appears when multiple method would otherwise have identical using records (see <see cref="ReadUsingRecord"/>).
        /// </remarks>
        public static void ReadForwardRecord(byte[] bytes, ref int offset, int size, out int token)
        {
            int tempOffset = offset;

            token = BitConverter.ToInt32(bytes, tempOffset);

            offset += size - CDI.CdiRecordHeaderSize;
            Debug.Assert(offset >= tempOffset);
        }

        /// <summary>
        /// This indicates that further information can be obtained by looking at the custom debug
        /// info of another method (specified by token).
        /// </summary>
        /// <remarks>
        /// Appears when there are extern aliases and edit-and-continue is disabled.
        /// </remarks>
        public static void ReadForwardToModuleRecord(byte[] bytes, ref int offset, int size, out int token)
        {
            int tempOffset = offset;

            token = BitConverter.ToInt32(bytes, tempOffset);

            offset += size - CDI.CdiRecordHeaderSize;
            Debug.Assert(offset >= tempOffset);
        }

        /// <summary>
        /// Scopes of state machine hoisted local variables.
        /// </summary>
        public static void ReadStateMachineHoistedLocalScopesRecord(byte[] bytes, ref int offset, int size, out ImmutableArray<StateMachineHoistedLocalScope> scopes)
        {
            int tempOffset = offset;

            var bucketCount = BitConverter.ToInt32(bytes, tempOffset);
            tempOffset += 4;

            var builder = ArrayBuilder<StateMachineHoistedLocalScope>.GetInstance(bucketCount);
            for (int i = 0; i < bucketCount; i++)
            {
                int startOffset = BitConverter.ToInt32(bytes, tempOffset);
                tempOffset += 4;
                int endOffset = BitConverter.ToInt32(bytes, tempOffset);
                tempOffset += 4;

                builder.Add(new StateMachineHoistedLocalScope(startOffset, endOffset));
            }

            scopes = builder.ToImmutableAndFree();

            offset += size - CDI.CdiRecordHeaderSize;
            Debug.Assert(offset >= tempOffset);
        }

        /// <summary>
        /// Indicates that this method is the iterator state machine for the method named in the record.
        /// </summary>
        /// <remarks>
        /// Appears when are iterator methods.
        /// </remarks>
        public static void ReadForwardIteratorRecord(byte[] bytes, ref int offset, int size, out string name)
        {
            int tempOffset = offset;

            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;
            while (true)
            {
                char ch = BitConverter.ToChar(bytes, tempOffset);
                tempOffset += 2;

                if (ch == 0)
                {
                    break;
                }

                builder.Append(ch);
            }

            name = pooled.ToStringAndFree();

            offset += size - CDI.CdiRecordHeaderSize;
            Debug.Assert(offset >= tempOffset);
        }

        /// <summary>
        /// Does for locals what <see cref="System.Runtime.CompilerServices.DynamicAttribute"/> does for parameters, return types, and fields.
        /// In particular, indicates which occurences of <see cref="object"/> in the signature are really dynamic.
        /// </summary>
        /// <remarks>
        /// Appears when there are dynamic locals.
        /// </remarks>
        public static void ReadDynamicLocalsRecord(byte[] bytes, ref int offset, int size, out ImmutableArray<DynamicLocalBucket> buckets)
        {
            int tempOffset = offset;

            int bucketCount = BitConverter.ToInt32(bytes, tempOffset);
            tempOffset += 4;

            var builder = ArrayBuilder<DynamicLocalBucket>.GetInstance(bucketCount);
            for (int i = 0; i < bucketCount; i++)
            {
                const int FlagBytesCount = 64;

                ulong flags = 0UL;
                for (int j = 0; j < FlagBytesCount; j++)
                {
                    var flag = bytes[tempOffset + j] != 0;
                    if (flag)
                    {
                        flags |= 1UL << j;
                    }
                }

                tempOffset += FlagBytesCount;

                int flagCount = BitConverter.ToInt32(bytes, tempOffset);
                tempOffset += 4;

                int slotId = BitConverter.ToInt32(bytes, tempOffset);
                tempOffset += 4;

                const int NameBytesCount = 128;
                var pooled = PooledStringBuilder.GetInstance();
                var nameBuilder = pooled.Builder;
                for (int j = 0; j < NameBytesCount; j += 2)
                {
                    char ch = BitConverter.ToChar(bytes, tempOffset + j);
                    if (ch == 0)
                    {
                        break;
                    }

                    nameBuilder.Append(ch);
                }

                // The Identifier name takes 64 WCHAR no matter how big its actual length is.
                tempOffset += NameBytesCount; 
                var name = pooled.ToStringAndFree();

                var bucket = new DynamicLocalBucket(flagCount, flags, slotId, name);
                builder.Add(bucket);
            }

            buckets = builder.ToImmutableAndFree();

            offset += size - CDI.CdiRecordHeaderSize;
            Debug.Assert(offset >= tempOffset);
        }

        /// <summary>
        /// Returns the raw bytes of a record.
        /// </summary>
        public static void ReadRawRecordBody(byte[] bytes, ref int offset, int size, out ImmutableArray<byte> body)
        {
            int bodySize = size - CDI.CdiRecordHeaderSize;
            body = ImmutableArray.Create(bytes, offset, bodySize);
            offset += bodySize;
        }

        /// <summary>
        /// Skips past a record.
        /// </summary>
        public static void SkipRecord(byte[] bytes, ref int offset, int size)
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
            var bytes = reader.GetCustomDebugInfo(methodToken, methodVersion);
            if (bytes == null)
            {
                return default(ImmutableArray<ImmutableArray<string>>);
            }

            int offset = 0;

            byte globalVersion;
            byte unusedGlobalCount;
            ReadGlobalHeader(bytes, ref offset, out globalVersion, out unusedGlobalCount);
            CheckVersion(globalVersion, methodToken);

            while (offset < bytes.Length)
            {
                byte version;
                CustomDebugInfoKind kind;
                int size;
                ReadRecordHeader(bytes, ref offset, out version, out kind, out size);
                CheckVersion(version, methodToken);

                if (kind == CustomDebugInfoKind.UsingInfo)
                {
                    if (!groupSizes.IsDefault)
                    {
                        throw new InvalidOperationException(string.Format("Expected at most one Using record for method {0}", FormatMethodToken(methodToken)));
                    }

                    ReadUsingRecord(bytes, ref offset, size, out groupSizes);
                }
                else if (kind == CustomDebugInfoKind.ForwardInfo)
                {
                    if (!externAliasStrings.IsDefault)
                    {
                        throw new InvalidOperationException(string.Format("Did not expect both Forward and ForwardToModule records for method {0}", FormatMethodToken(methodToken)));
                    }

                    ReadForwardRecord(bytes, ref offset, size, out methodToken);

                    // Follow at most one forward link (as in FUNCBRECEE::ensureNamespaces).
                    // NOTE: Dev11 may produce chains of forward links (e.g. for System.Collections.Immutable).
                    if (!seenForward) 
                    {
                        seenForward = true;
                        goto RETRY;
                    }
                }
                else if (kind == CustomDebugInfoKind.ForwardToModuleInfo)
                {
                    if (!externAliasStrings.IsDefault)
                    {
                        throw new InvalidOperationException(string.Format("Expected at most one ForwardToModule record for method {0}", FormatMethodToken(methodToken)));
                    }

                    int moduleInfoMethodToken;
                    ReadForwardToModuleRecord(bytes, ref offset, size, out moduleInfoMethodToken);
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
                }
                else
                {
                    SkipRecord(bytes, ref offset, size);
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
                // TODO: remove this workaround for DevDiv #1060879.
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

        public static ImmutableSortedSet<int> GetCSharpInScopeHoistedLocalIndices(this ISymUnmanagedReader reader, int methodToken, int methodVersion, int ilOffset)
        {
            byte[] bytes = reader.GetCustomDebugInfo(methodToken, methodVersion);
            if (bytes == null)
            {
                return ImmutableSortedSet<int>.Empty;
            }

            int offset = 0;

            byte globalVersion;
            byte unusedGlobalCount;
            ReadGlobalHeader(bytes, ref offset, out globalVersion, out unusedGlobalCount);
            CheckVersion(globalVersion, methodToken);

            ImmutableArray<StateMachineHoistedLocalScope> scopes = default(ImmutableArray<StateMachineHoistedLocalScope>);

            while (offset < bytes.Length)
            {
                byte version;
                CustomDebugInfoKind kind;
                int size;
                ReadRecordHeader(bytes, ref offset, out version, out kind, out size);
                CheckVersion(version, methodToken);

                if (kind == CustomDebugInfoKind.StateMachineHoistedLocalScopes)
                {
                    ReadStateMachineHoistedLocalScopesRecord(bytes, ref offset, size, out scopes);
                    break;
                }
                else
                {
                    SkipRecord(bytes, ref offset, size);
                }
            }

            if (scopes.IsDefault)
            {
                return ImmutableSortedSet<int>.Empty;
            }

            ArrayBuilder<int> builder = ArrayBuilder<int>.GetInstance();
            for (int i = 0; i < scopes.Length; i++)
            {
                StateMachineHoistedLocalScope scope = scopes[i];

                // NB: scopes are end-inclusive.
                if (ilOffset >= scope.StartOffset && ilOffset <= scope.EndOffset)
                {
                    builder.Add(i);
                }
            }

            ImmutableSortedSet<int> result = builder.ToImmutableSortedSet();
            builder.Free();
            return result;
        }

        public static void GetCSharpDynamicLocalInfo(
            this ISymUnmanagedReader reader,
            int methodToken,
            int methodVersion,
            string firstLocalName,
            out ImmutableDictionary<int, ImmutableArray<bool>> dynamicLocalMap,
            out ImmutableDictionary<string, ImmutableArray<bool>> dynamicLocalConstantMap)
        {
            dynamicLocalMap = ImmutableDictionary<int, ImmutableArray<bool>>.Empty;
            dynamicLocalConstantMap = ImmutableDictionary<string, ImmutableArray<bool>>.Empty;

            byte[] bytes = reader.GetCustomDebugInfo(methodToken, methodVersion);
            if (bytes == null)
            {
                return;
            }

            int offset = 0;

            byte globalVersion;
            byte unusedGlobalCount;
            ReadGlobalHeader(bytes, ref offset, out globalVersion, out unusedGlobalCount);
            CheckVersion(globalVersion, methodToken);

            ImmutableArray<DynamicLocalBucket> buckets = default(ImmutableArray<DynamicLocalBucket>);

            while (offset < bytes.Length)
            {
                byte version;
                CustomDebugInfoKind kind;
                int size;
                ReadRecordHeader(bytes, ref offset, out version, out kind, out size);
                CheckVersion(version, methodToken);

                if (kind == CustomDebugInfoKind.DynamicLocals)
                {
                    ReadDynamicLocalsRecord(bytes, ref offset, size, out buckets);
                    break;
                }
                else
                {
                    SkipRecord(bytes, ref offset, size);
                }
            }

            if (buckets.IsDefault)
            {
                return;
            }

            ImmutableDictionary<int, ImmutableArray<bool>>.Builder localBuilder = null;
            ImmutableDictionary<string, ImmutableArray<bool>>.Builder constantBuilder = null;

            foreach (DynamicLocalBucket bucket in buckets)
            {
                int flagCount = bucket.FlagCount;
                ulong flags = bucket.Flags;
                ArrayBuilder<bool> dynamicBuilder = ArrayBuilder<bool>.GetInstance(flagCount);
                for (int i = 0; i < flagCount; i++)
                {
                    dynamicBuilder.Add((flags & (1u << i)) != 0);
                }

                var slot = bucket.SlotId;
                var name = bucket.Name;

                // All constants have slot 0, but none of them can have the same name
                // as the local that is actually in slot 0 (if there is one).
                if (slot == 0 && (firstLocalName == null || firstLocalName != name))
                {
                    constantBuilder = constantBuilder ?? ImmutableDictionary.CreateBuilder<string, ImmutableArray<bool>>();
                    constantBuilder.Add(name, dynamicBuilder.ToImmutableAndFree());
                }
                else
                {
                    localBuilder = localBuilder ?? ImmutableDictionary.CreateBuilder<int, ImmutableArray<bool>>();
                    localBuilder.Add(slot, dynamicBuilder.ToImmutableAndFree());
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

        private static void CheckVersion(byte globalVersion, int methodToken)
        {
            if (globalVersion != CDI.CdiVersion)
            {
                throw new InvalidOperationException(string.Format("Method {0}: Expected version {1}, but found version {2}.", FormatMethodToken(methodToken), CDI.CdiVersion, globalVersion));
            }
        }

        /// <summary>
        /// Get the (unprocessed) import strings for a given method.
        /// </summary>
        /// <remarks>
        /// Doesn't consider forwarding.
        /// 
        /// CONSIDER: Dev12 doesn't just check the root scope - it digs around to find the best
        /// match based on the IL offset and then walks up to the root scope (see PdbUtil::GetScopeFromOffset).
        /// However, it's not clear that this matters, since imports can't be scoped in VB.  This is probably
        /// just based on the way they were extracting locals and constants based on a specific scope.
        /// </remarks>
        private static ImmutableArray<string> GetImportStrings(this ISymUnmanagedMethod method)
        {
            if (method == null)
            {
                // TODO: remove this workaround for DevDiv #1060879.
                // If methodToken was updated (because the method we started with forwards to another method),
                // we have no way to know what version the new method is on.  If it's not the same as the
                // version we stared with (e.g. because it has been changed less frequently), then we may not
                // find a corresponding ISymUnmanagedMethod.
                // Note: The real fix is to not use CDI forwarding in EnC PDBs.
                return ImmutableArray<string>.Empty;
            }

            ISymUnmanagedScope rootScope = method.GetRootScope();
            if (rootScope == null)
            {
                Debug.Assert(false, "Expected a root scope.");
                return ImmutableArray<string>.Empty;
            }

            ImmutableArray<ISymUnmanagedScope> childScopes = rootScope.GetScopes();
            if (childScopes.Length == 0)
            {
                // It seems like there should always be at least one child scope, but we've
                // seen PDBs where that is not the case.
                return ImmutableArray<string>.Empty;
            }

            // As in NamespaceListWrapper::Init, we only consider namespaces in the first
            // child of the root scope.
            ISymUnmanagedScope firstChildScope = childScopes[0];

            ImmutableArray<ISymUnmanagedNamespace> namespaces = firstChildScope.GetNamespaces();
            if (namespaces.Length == 0)
            {
                // It seems like there should always be at least one namespace (i.e. the global
                // namespace), but we've seen PDBs where that is not the case.
                return ImmutableArray<string>.Empty;
            }

            ArrayBuilder<string> importsBuilder = ArrayBuilder<string>.GetInstance(namespaces.Length);
            foreach (ISymUnmanagedNamespace @namespace in namespaces)
            {
                importsBuilder.Add(@namespace.GetName());
            }

            return importsBuilder.ToImmutableAndFree();
        }

        public static bool IsCSharpExternAliasInfo(string import)
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
                    // but they were probably implementation details that do not affect roslyn.
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
                    externAlias = import.Substring(1);
                    alias = null;
                    target = null;
                    kind = ImportTargetKind.Assembly;
                    return true;

                case 'Z': // C# extern alias (module-level)
                    if (!TrySplit(import, 1, ' ', out externAlias, out target))
                    {
                        return false;
                    }

                    alias = null;
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
    }
}