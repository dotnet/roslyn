﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    internal sealed class CustomDebugInfoWriter
    {
        private int _methodTokenWithModuleInfo;
        private IMethodBody _methodBodyWithModuleInfo;

        private int _previousMethodTokenWithUsingInfo;
        private IMethodBody _previousMethodBodyWithUsingInfo;

        private readonly PdbWriter _pdbWriter;

        public CustomDebugInfoWriter(PdbWriter pdbWriter)
        {
            Debug.Assert(pdbWriter != null);
            _pdbWriter = pdbWriter;
        }

        /// <summary>
        /// Returns true if the namespace scope for this method should be forwarded to another method.
        /// Returns non-null <paramref name="forwardToMethod"/> if the forwarding should be done directly via UsingNamespace,
        /// null if the forwarding is done via custom debug info.
        /// </summary>
        public bool ShouldForwardNamespaceScopes(EmitContext context, IMethodBody methodBody, int methodToken, out IMethodDefinition forwardToMethod)
        {
            if (ShouldForwardToPreviousMethodWithUsingInfo(context, methodBody))
            {
                // SerializeNamespaceScopeMetadata will do the actual forwarding in case this is a CSharp method.
                // VB on the other hand adds a "@methodtoken" to the scopes instead.
                if (context.Module.GenerateVisualBasicStylePdb)
                {
                    forwardToMethod = _previousMethodBodyWithUsingInfo.MethodDefinition;
                }
                else
                {
                    forwardToMethod = null;
                }

                return true;
            }

            _previousMethodBodyWithUsingInfo = methodBody;
            _previousMethodTokenWithUsingInfo = methodToken;
            forwardToMethod = null;
            return false;
        }

        public byte[] SerializeMethodDebugInfo(EmitContext context, IMethodBody methodBody, int methodToken, bool isEncDelta, bool suppressNewCustomDebugInfo, out bool emitExternNamespaces)
        {
            emitExternNamespaces = false;

            // CONSIDER: this may not be the same "first" method as in Dev10, but
            // it shouldn't matter since all methods will still forward to a method
            // containing the appropriate information.
            if (_methodBodyWithModuleInfo == null) //UNDONE: || edit-and-continue
            {
                // This module level information could go on every method (and does in
                // the edit-and-continue case), but - as an optimization - we'll just
                // put it on the first method we happen to encounter and then put a
                // reference to the first method's token in every other method (so they
                // can find the information).
                if (context.Module.GetAssemblyReferenceAliases(context).Any())
                {
                    _methodTokenWithModuleInfo = methodToken;
                    _methodBodyWithModuleInfo = methodBody;
                    emitExternNamespaces = true;
                }
            }

            var customDebugInfo = ArrayBuilder<PooledBlobBuilder>.GetInstance();

            SerializeIteratorClassMetadata(methodBody, customDebugInfo);

            // NOTE: This is an attempt to match Dev10's apparent behavior.  For iterator methods (i.e. the method
            // that appears in source, not the synthesized ones), Dev10 only emits the ForwardIterator and IteratorLocal
            // custom debug info (e.g. there will be no information about the usings that were in scope).
            // NOTE: There seems to be an unusual behavior in ISymUnmanagedWriter where, if all the methods in a type are
            // iterator methods, no custom debug info is emitted for any method.  Adding a single non-iterator
            // method causes the custom debug info to be produced for all methods (including the iterator methods).
            // Since we are making the same ISymUnmanagedWriter calls as Dev10, we see the same behavior (i.e. this
            // is not a regression).
            if (methodBody.StateMachineTypeName == null)
            {
                SerializeNamespaceScopeMetadata(context, methodBody, customDebugInfo);
                SerializeStateMachineLocalScopes(methodBody, customDebugInfo);
            }

            if (!suppressNewCustomDebugInfo)
            {
                SerializeDynamicLocalInfo(methodBody, customDebugInfo);
                SerializeTupleElementNames(methodBody, customDebugInfo);

                // delta doesn't need this information - we use information recorded by previous generation emit
                if (!isEncDelta)
                {
                    var encMethodInfo = MetadataWriter.GetEncMethodDebugInfo(methodBody);
                    SerializeCustomDebugInformation(encMethodInfo, customDebugInfo);
                }
            }

            byte[] result = SerializeCustomDebugMetadata(customDebugInfo);

            foreach(var builder in customDebugInfo)
            {
                builder.Free();
            }

            customDebugInfo.Free();

            return result;
        }

        // internal for testing
        internal static void SerializeCustomDebugInformation(EditAndContinueMethodDebugInformation debugInfo, ArrayBuilder<PooledBlobBuilder> customDebugInfo)
        {
            // PERF: note that we pass debugInfo as explicit parameter
            //       that is intentional to avoid capturing debugInfo as that 
            //       would result in a lot of delegate allocations here that are otherwise can be avoided.
            if (!debugInfo.LocalSlots.IsDefaultOrEmpty)
            {
                customDebugInfo.Add(
                    SerializeRecord(
                        CustomDebugInfoKind.EditAndContinueLocalSlotMap, 
                        debugInfo,
                        (info, builder) => info.SerializeLocalSlots(builder)));
            }

            if (!debugInfo.Lambdas.IsDefaultOrEmpty)
            {
                customDebugInfo.Add(
                    SerializeRecord(
                        CustomDebugInfoKind.EditAndContinueLambdaMap,
                        debugInfo,
                        (info, builder) => info.SerializeLambdaMap(builder)));
            }
        }

        private static PooledBlobBuilder SerializeRecord<T>(
            CustomDebugInfoKind kind,
            T debugInfo,
            Action<T, BlobBuilder> recordSerializer)
        {
            var cmw = PooledBlobBuilder.GetInstance();
            cmw.WriteByte(CustomDebugInfoConstants.Version);
            cmw.WriteByte((byte)kind);
            cmw.WriteByte(0);

            // alignment size and length (will be patched)
            var alignmentSizeAndLengthWriter = new BlobWriter(cmw.ReserveBytes(sizeof(byte) + sizeof(uint)));

            recordSerializer(debugInfo, cmw);

            int length = cmw.Count;
            int alignedLength = 4 * ((length + 3) / 4);
            byte alignmentSize = (byte)(alignedLength - length);
            cmw.WriteBytes(0, alignmentSize);

            // fill in alignment size and length:
            alignmentSizeAndLengthWriter.WriteByte(alignmentSize);
            alignmentSizeAndLengthWriter.WriteUInt32((uint)alignedLength);

            return cmw;
        }

        private static void SerializeIteratorClassMetadata(IMethodBody methodBody, ArrayBuilder<PooledBlobBuilder> customDebugInfo)
        {
            SerializeReferenceToIteratorClass(methodBody.StateMachineTypeName, customDebugInfo);
        }

        private static void SerializeReferenceToIteratorClass(string iteratorClassName, ArrayBuilder<PooledBlobBuilder> customDebugInfo)
        {
            if (iteratorClassName == null) return;
            var cmw = PooledBlobBuilder.GetInstance();
            cmw.WriteByte(CustomDebugInfoConstants.Version);
            cmw.WriteByte((byte)CustomDebugInfoKind.ForwardIterator);
            cmw.Align(4);
            uint length = 10 + (uint)iteratorClassName.Length * 2;
            if ((length & 3) != 0) length += 4 - (length & 3);
            cmw.WriteUInt32(length);
            WriteUtf16String(cmw, iteratorClassName);
            cmw.Align(4);
            Debug.Assert(cmw.Count == length);
            customDebugInfo.Add(cmw);
        }

        private static void SerializeStateMachineLocalScopes(IMethodBody methodBody, ArrayBuilder<PooledBlobBuilder> customDebugInfo)
        {
            var scopes = methodBody.StateMachineHoistedLocalScopes;
            if (scopes.IsDefaultOrEmpty)
            {
                return;
            }

            uint numberOfScopes = (uint)scopes.Length;
            var cmw = PooledBlobBuilder.GetInstance();
            cmw.WriteByte(CustomDebugInfoConstants.Version);
            cmw.WriteByte((byte)CustomDebugInfoKind.StateMachineHoistedLocalScopes);
            cmw.Align(4);
            cmw.WriteUInt32(12 + numberOfScopes * 8);
            cmw.WriteUInt32(numberOfScopes);
            foreach (var scope in scopes)
            {
                if (scope.IsDefault)
                {
                    cmw.WriteUInt32(0);
                    cmw.WriteUInt32(0);
                }
                else
                {
                    // Dev12 C# emits end-inclusive range
                    cmw.WriteUInt32((uint)scope.StartOffset);
                    cmw.WriteUInt32((uint)scope.EndOffset - 1);
                }
            }

            customDebugInfo.Add(cmw);
        }

        private static ArrayBuilder<T> GetLocalInfoToSerialize<T>(
            IMethodBody methodBody,
            Func<ILocalDefinition, bool> filter,
            Func<LocalScope, ILocalDefinition, T> getInfo)
        {
            ArrayBuilder<T> builder = null;

            foreach (var local in methodBody.LocalVariables)
            {
                Debug.Assert(local.SlotIndex >= 0);
                if (filter(local))
                {
                    if (builder == null)
                    {
                        builder = ArrayBuilder<T>.GetInstance();
                    }
                    builder.Add(getInfo(default(LocalScope), local));
                }
            }

            foreach (var currentScope in methodBody.LocalScopes)
            {
                foreach (var localConstant in currentScope.Constants)
                {
                    Debug.Assert(localConstant.SlotIndex < 0);
                    if (filter(localConstant))
                    {
                        if (builder == null)
                        {
                            builder = ArrayBuilder<T>.GetInstance();
                        }
                        builder.Add(getInfo(currentScope, localConstant));
                    }
                }
            }

            return builder;
        }

        private static void SerializeDynamicLocalInfo(IMethodBody methodBody, ArrayBuilder<PooledBlobBuilder> customDebugInfo)
        {
            if (!methodBody.HasDynamicLocalVariables)
            {
                return; //There are no dynamic locals
            }

            const int dynamicAttributeSize = 64;
            const int identifierSize = 64;

            var dynamicLocals = GetLocalInfoToSerialize(
                methodBody,
                local =>
                {
                    var dynamicTransformFlags = local.DynamicTransformFlags;
                    return !dynamicTransformFlags.IsEmpty &&
                        dynamicTransformFlags.Length <= dynamicAttributeSize &&
                        local.Name.Length < identifierSize;
                },
                (scope, local) => local);
            if (dynamicLocals == null)
            {
                return;
            }

            const int blobSize = dynamicAttributeSize + 4 + 4 + identifierSize * 2;//DynamicAttribute: 64, DynamicAttributeLength: 4, SlotIndex: 4, IdentifierName: 128
            var cmw = PooledBlobBuilder.GetInstance();
            cmw.WriteByte(CustomDebugInfoConstants.Version);
            cmw.WriteByte((byte)CustomDebugInfoKind.DynamicLocals);
            cmw.Align(4);
            // size = Version,Kind + size + cBuckets + (dynamicCount * sizeOf(Local Blob))
            cmw.WriteUInt32(4 + 4 + 4 + (uint)dynamicLocals.Count * blobSize);//Size of the Dynamic Block
            cmw.WriteUInt32((uint)dynamicLocals.Count);

            foreach (ILocalDefinition local in dynamicLocals)
            {
                var dynamicTransformFlags = local.DynamicTransformFlags;
                byte[] flag = new byte[dynamicAttributeSize];
                for (int k = 0; k < dynamicTransformFlags.Length; k++)
                {
                    if ((bool)dynamicTransformFlags[k].Value)
                    {
                        flag[k] = 1;
                    }
                }
                cmw.WriteBytes(flag); //Written Flag
                cmw.WriteUInt32((uint)dynamicTransformFlags.Length); //Written Length

                var localIndex = local.SlotIndex;
                cmw.WriteUInt32((localIndex < 0) ? 0u : (uint)localIndex);

                char[] localName = new char[identifierSize];
                local.Name.CopyTo(0, localName, 0, local.Name.Length);
                cmw.WriteUTF16(localName);
            }

            dynamicLocals.Free();
            customDebugInfo.Add(cmw);
        }

        private struct LocalAndScope
        {
            internal readonly ILocalDefinition Local;
            internal readonly LocalScope Scope;

            internal LocalAndScope(ILocalDefinition local, LocalScope scope)
            {
                Local = local;
                Scope = scope;
            }
        }

        private static void SerializeTupleElementNames(IMethodBody methodBody, ArrayBuilder<PooledBlobBuilder> customDebugInfo)
        {
            var builder = GetLocalInfoToSerialize(
                methodBody,
                local => !local.TupleElementNames.IsEmpty,
                (scope, local) => new LocalAndScope(local, scope));
            if (builder == null)
            {
                return;
            }

            customDebugInfo.Add(
                SerializeRecord(
                    CustomDebugInfoKind.TupleElementNames,
                    builder,
                    SerializeTupleElementNames));
            builder.Free();
        }

        private static void SerializeTupleElementNames(ArrayBuilder<LocalAndScope> locals, BlobBuilder cmw)
        {
            cmw.WriteInt32(locals.Count);
            foreach (var localAndScope in locals)
            {
                var local = localAndScope.Local;
                var scope = localAndScope.Scope;
                var tupleElementNames = local.TupleElementNames;
                cmw.WriteInt32(tupleElementNames.Length);
                foreach (var tupleElementName in tupleElementNames)
                {
                    WriteUtf8String(cmw, (string)tupleElementName.Value ?? string.Empty);
                }
                cmw.WriteInt32(local.SlotIndex);
                cmw.WriteInt32(scope.StartOffset);
                cmw.WriteInt32(scope.EndOffset);
                WriteUtf8String(cmw, local.Name);
            }
        }

        // internal for testing
        internal static byte[] SerializeCustomDebugMetadata(ArrayBuilder<PooledBlobBuilder> recordWriters)
        {
            if (recordWriters.Count == 0)
            {
                return null;
            }

            int records = 0;
            foreach(var rec in recordWriters)
            {
                records += rec.Count;
            }

            var result = new byte[
                sizeof(byte) +                  // version
                sizeof(byte) +                  // record count
                sizeof(ushort) +                // padding
                records                         // records
            ];

            var cmw = new BlobWriter(result);
            cmw.WriteByte(CustomDebugInfoConstants.Version);
            cmw.WriteByte((byte)recordWriters.Count); // count
            cmw.WriteInt16(0);
            foreach (var recordWriter in recordWriters)
            {
                cmw.WriteBytes(recordWriter);
            }

            return result;
        }

        private void SerializeNamespaceScopeMetadata(EmitContext context, IMethodBody methodBody, ArrayBuilder<PooledBlobBuilder> customDebugInfo)
        {
            if (context.Module.GenerateVisualBasicStylePdb)
            {
                return;
            }

            if (ShouldForwardToPreviousMethodWithUsingInfo(context, methodBody))
            {
                Debug.Assert(!ReferenceEquals(_previousMethodBodyWithUsingInfo, methodBody));
                SerializeReferenceToPreviousMethodWithUsingInfo(customDebugInfo);
                return;
            }

            List<ushort> usingCounts = new List<ushort>();
            var cmw = PooledBlobBuilder.GetInstance();
            for (IImportScope scope = methodBody.ImportScope; scope != null; scope = scope.Parent)
            {
                usingCounts.Add((ushort)scope.GetUsedNamespaces().Length);
            }

            // ACASEY: This originally wrote (uint)12, (ushort)1, (ushort)0 in the
            // case where usingCounts was empty, but I'm not sure why.
            if (usingCounts.Count > 0)
            {
                uint streamLength;
                cmw.WriteByte(CustomDebugInfoConstants.Version);
                cmw.WriteByte((byte)CustomDebugInfoKind.UsingInfo);
                cmw.Align(4);

                cmw.WriteUInt32(streamLength = BitArithmeticUtilities.Align((uint)usingCounts.Count * 2 + 10, 4));
                cmw.WriteUInt16((ushort)usingCounts.Count);
                foreach (ushort uc in usingCounts)
                {
                    cmw.WriteUInt16(uc);
                }

                cmw.Align(4);
                Debug.Assert(streamLength == cmw.Count);
                customDebugInfo.Add(cmw);
            }

            if (_methodBodyWithModuleInfo != null && !ReferenceEquals(_methodBodyWithModuleInfo, methodBody))
            {
                SerializeReferenceToMethodWithModuleInfo(customDebugInfo);
            }
        }

        private bool ShouldForwardToPreviousMethodWithUsingInfo(EmitContext context, IMethodBody methodBody)
        {
            if (_previousMethodBodyWithUsingInfo == null ||
                ReferenceEquals(_previousMethodBodyWithUsingInfo, methodBody))
            {
                return false;
            }

            // VB includes method namespace in namespace scopes:
            if (context.Module.GenerateVisualBasicStylePdb)
            {
                if (_pdbWriter.GetOrCreateSerializedNamespaceName(_previousMethodBodyWithUsingInfo.MethodDefinition.ContainingNamespace) !=
                    _pdbWriter.GetOrCreateSerializedNamespaceName(methodBody.MethodDefinition.ContainingNamespace))
                {
                    return false;
                }
            }

            var previousScopes = _previousMethodBodyWithUsingInfo.ImportScope;

            // methods share the same import scope (common case for methods declared in the same file)
            if (methodBody.ImportScope == previousScopes)
            {
                return true;
            }

            // If methods are in different files they don't share the same scopes,
            // but the imports might be the same nevertheless.
            // Note: not comparing project-level imports since those are the same for all method bodies.
            var s1 = methodBody.ImportScope;
            var s2 = previousScopes;
            while (s1 != null && s2 != null)
            {
                if (!s1.GetUsedNamespaces().SequenceEqual(s2.GetUsedNamespaces()))
                {
                    return false;
                }

                s1 = s1.Parent;
                s2 = s2.Parent;
            }

            return s1 == s2;
        }

        private void SerializeReferenceToMethodWithModuleInfo(ArrayBuilder<PooledBlobBuilder> customDebugInfo)
        {
            var cmw = PooledBlobBuilder.GetInstance();
            cmw.WriteByte(CustomDebugInfoConstants.Version);
            cmw.WriteByte((byte)CustomDebugInfoKind.ForwardToModuleInfo);
            cmw.Align(4);
            cmw.WriteUInt32(12);
            cmw.WriteUInt32((uint)_methodTokenWithModuleInfo);
            customDebugInfo.Add(cmw);
        }

        private void SerializeReferenceToPreviousMethodWithUsingInfo(ArrayBuilder<PooledBlobBuilder> customDebugInfo)
        {
            var cmw = PooledBlobBuilder.GetInstance(12);
            cmw.WriteByte(CustomDebugInfoConstants.Version);
            cmw.WriteByte((byte)CustomDebugInfoKind.ForwardInfo);
            cmw.Align(4);
            cmw.WriteUInt32(12);
            cmw.WriteUInt32((uint)_previousMethodTokenWithUsingInfo);
            customDebugInfo.Add(cmw);
        }

        /// <summary>
        /// Write string as UTF8 with null terminator.
        /// </summary>
        private static void WriteUtf8String(BlobBuilder cmw, string str)
        {
            cmw.WriteUTF8(str);
            cmw.WriteByte(0);
        }

        /// <summary>
        /// Write string as UTF16 with null terminator.
        /// </summary>
        private static void WriteUtf16String(BlobBuilder cmw, string str)
        {
            cmw.WriteUTF16(str);
            cmw.WriteUInt16(0);
        }
    }
}
