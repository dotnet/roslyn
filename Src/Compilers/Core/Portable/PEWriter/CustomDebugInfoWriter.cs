// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    internal sealed class CustomDebugInfoWriter
    {
        private uint methodTokenWithModuleInfo;
        private IMethodBody methodBodyWithModuleInfo;

        private uint previousMethodTokenWithUsingInfo;
        private IMethodBody previousMethodBodyWithUsingInfo;

        public CustomDebugInfoWriter()
        {
        }

        /// <summary>
        /// Returns true if the namespace scope for this method should be forwarded to another method.
        /// Returns non-null <paramref name="forwardToMethod"/> if the forwarding should be done directly via UsingNamespace,
        /// null if the forwarding is done via custom debug info.
        /// </summary>
        public bool ShouldForwardNamespaceScopes(IMethodBody methodBody, uint methodToken, out IMethodDefinition forwardToMethod)
        {
            if (ShouldForwardToPreviousMethodWithUsingInfo(methodBody) || methodBody.NamespaceScopes.IsEmpty)
            {
                // SerializeNamespaceScopeMetadata will do the actual forwarding in case this is a CSharp method.
                // VB on the other hand adds a "@methodtoken" to the scopes instead.
                if (methodBody.CustomDebugInfoKind == CustomDebugInfoKind.VisualBasicStyle)
                {
                    forwardToMethod = this.previousMethodBodyWithUsingInfo.MethodDefinition;
                }
                else
                {
                    forwardToMethod = null;
                }

                return true;
            }

            this.previousMethodBodyWithUsingInfo = methodBody;
            this.previousMethodTokenWithUsingInfo = methodToken;
            forwardToMethod = null;
            return false;
        }

        public byte[] SerializeMethodDebugInfo(IModule module, IMethodBody methodBody, uint methodToken, out bool emitExternNamespaces)
        {
            emitExternNamespaces = false;

            // CONSIDER: this may not be the same "first" method as in Dev10, but
            // it shouldn't matter since all methods will still forward to a method
            // containing the appropriate information.
            if (this.methodBodyWithModuleInfo == null) //UNDONE: || edit-and-continue
            {
                // This module level information could go on every method (and does in
                // the edit-and-continue case), but - as an optimization - we'll just
                // put it on the first method we happen to encounter and then put a
                // reference to the first method's token in every other method (so they
                // can find the information).
                if (module.ExternNamespaces.Any())
                {
                    this.methodTokenWithModuleInfo = methodToken;
                    this.methodBodyWithModuleInfo = methodBody;
                    emitExternNamespaces = true;
                }
            }

            var customDebugInfo = ArrayBuilder<MemoryStream>.GetInstance();

            SerializeIteratorClassMetadata(methodBody, customDebugInfo);

            // NOTE: This is an attempt to match Dev10's apparent behavior.  For iterator methods (i.e. the method
            // that appears in source, not the synthesized ones), Dev10 only emits the ForwardIterator and IteratorLocal
            // custom debug info (e.g. there will be no information about the usings that were in scope).
            // NOTE: There seems to be an unusual behavior in ISymUnmanagedWriter where, if all the methods in a type are
            // iterator methods, no custom debug info is emitted for any method.  Adding a single non-iterator
            // method causes the custom debug info to be produced for all methods (including the iterator methods).
            // Since we are making the same ISymUnmanagedWriter calls as Dev10, we see the same behavior (i.e. this
            // is not a regression).
            if (methodBody.IteratorClassName == null)
            {
                SerializeNamespaceScopeMetadata(methodBody, customDebugInfo);
                SerializeIteratorLocalScopes(methodBody, customDebugInfo);
            }

            SerializeDynamicLocalInfo(methodBody, customDebugInfo);

            byte[] result;
            if (methodBody.CustomDebugInfoKind == CustomDebugInfoKind.CSharpStyle)
            {
                result = SerializeCustomDebugMetadata(customDebugInfo);
            }
            else
            {
                result = null;
            }

            customDebugInfo.Free();
            return result;
        }

        private static void SerializeIteratorClassMetadata(IMethodBody methodBody, ArrayBuilder<MemoryStream> customDebugInfo)
        {
            SerializeReferenceToIteratorClass(methodBody.IteratorClassName, customDebugInfo);
        }

        private static void SerializeReferenceToIteratorClass(string iteratorClassName, ArrayBuilder<MemoryStream> customDebugInfo)
        {
            if (iteratorClassName == null) return;
            MemoryStream customMetadata = new MemoryStream();
            BinaryWriter cmw = new BinaryWriter(customMetadata, true);
            cmw.WriteByte(4); // version
            cmw.WriteByte(4); // kind: ForwardIterator
            cmw.Align(4);
            uint length = 10 + (uint)iteratorClassName.Length * 2;
            if ((length & 3) != 0) length += 4 - (length & 3);
            cmw.WriteUint(length);
            cmw.WriteString(iteratorClassName, true);
            cmw.Align(4);
            Debug.Assert(customMetadata.Position == length);
            customDebugInfo.Add(customMetadata);
        }

        private static void SerializeIteratorLocalScopes(IMethodBody methodBody, ArrayBuilder<MemoryStream> customDebugInfo)
        {
            ImmutableArray<LocalScope> scopes = methodBody.IteratorScopes;
            uint numberOfScopes = (uint)scopes.Length;
            if (numberOfScopes == 0)
            {
                return;
            }

            MemoryStream customMetadata = new MemoryStream();
            BinaryWriter cmw = new BinaryWriter(customMetadata);
            cmw.WriteByte(4); // version
            cmw.WriteByte(3); // kind: IteratorLocals
            cmw.Align(4);
            cmw.WriteUint(12 + numberOfScopes * 8);
            cmw.WriteUint(numberOfScopes);
            foreach (var scope in scopes)
            {
                cmw.WriteUint(scope.Offset);
                cmw.WriteUint(scope.Offset + scope.Length);
            }

            customDebugInfo.Add(customMetadata);
        }

        private static void SerializeDynamicLocalInfo(IMethodBody methodBody, ArrayBuilder<MemoryStream> customDebugInfo)
        {
            if (!methodBody.HasDynamicLocalVariables)
            {
                return; //There are no dynamic locals
            }

            var dynamicLocals = ArrayBuilder<ILocalDefinition>.GetInstance();

            foreach (ILocalDefinition local in methodBody.LocalVariables)
            {
                if (local.IsDynamic)
                {
                    dynamicLocals.Add(local);
                }
            }

            int dynamicVariableCount = dynamicLocals.Count;

            foreach (var currentScope in methodBody.LocalScopes)
            {
                foreach (var localConstant in currentScope.Constants)
                {
                    if (localConstant.IsDynamic)
                    {
                        dynamicLocals.Add(localConstant);
                    }
                }
            }

            Debug.Assert(dynamicLocals.Any()); // There must be atleast one dynamic local if this point is reached

            const int blobSize = 200;//DynamicAttribute - 64, DynamicAttributeLength - 4, SlotIndex -4, IdentifierName - 128
            MemoryStream customMetadata = new MemoryStream();
            BinaryWriter cmw = new BinaryWriter(customMetadata, true);
            cmw.WriteByte(4);//Version
            cmw.WriteByte(5);//Kind : Dynamic Locals
            cmw.Align(4);
            // size = Version,Kind + size + cBuckets + (dynamicCount * sizeOf(Local Blob))
            cmw.WriteUint(4 + 4 + 4 + (uint)dynamicLocals.Count * blobSize);//Size of the Dynamic Block
            cmw.WriteUint((uint)dynamicLocals.Count);

            int localIndex = 0;
            foreach (ILocalDefinition local in dynamicLocals)
            {
                if (local.Name.Length > 63)//Ignore and push empty information
                {
                    cmw.WriteBytes(0, blobSize);
                    continue;
                }

                var dynamicTransformFlags = local.DynamicTransformFlags;
                if (!dynamicTransformFlags.IsDefault && dynamicTransformFlags.Length <= 64)
                {
                    byte[] flag = new byte[64];
                    for (int k = 0; k < dynamicTransformFlags.Length; k++)
                    {
                        if ((bool)dynamicTransformFlags[k].Value)
                        {
                            flag[k] = (byte)1;
                        }
                    }
                    cmw.WriteBytes(flag); //Written Flag
                    cmw.WriteUint((uint)dynamicTransformFlags.Length); //Written Length
                }
                else
                {
                    cmw.WriteBytes(0, 68); //Empty flag array and size.
                }

                if (localIndex < dynamicVariableCount)
                {
                    // Dynamic variable
                    cmw.WriteUint((uint)local.SlotIndex);
                }
                else
                {
                    // Dynamic constant
                    cmw.WriteUint(0);
                }

                char[] localName = new char[64];
                local.Name.CopyTo(0, localName, 0, local.Name.Length);
                cmw.WriteChars(localName);

                localIndex++;
            }

            dynamicLocals.Free();
            customDebugInfo.Add(customMetadata);
        }

        private static byte[] SerializeCustomDebugMetadata(ArrayBuilder<MemoryStream> customDebugInfo)
        {
            if (customDebugInfo.Count == 0)
            {
                return null;
            }

            MemoryStream customMetadata = MemoryStream.GetInstance();
            BinaryWriter cmw = new BinaryWriter(customMetadata);
            cmw.WriteByte(4); // version
            cmw.WriteByte((byte)customDebugInfo.Count); // count
            cmw.Align(4);
            foreach (MemoryStream ms in customDebugInfo)
            {
                ms.WriteTo(customMetadata);
            }

            var result = customMetadata.ToArray();
            customMetadata.Free();
            return result;
        }

        private void SerializeNamespaceScopeMetadata(IMethodBody methodBody, ArrayBuilder<MemoryStream> customDebugInfo)
        {
            if (ShouldForwardToPreviousMethodWithUsingInfo(methodBody))
            {
                Debug.Assert(!ReferenceEquals(this.previousMethodBodyWithUsingInfo, methodBody));
                SerializeReferenceToPreviousMethodWithUsingInfo(customDebugInfo);
                return;
            }

            MemoryStream customMetadata = new MemoryStream();
            List<ushort> usingCounts = new List<ushort>();
            BinaryWriter cmw = new BinaryWriter(customMetadata);
            foreach (NamespaceScope namespaceScope in methodBody.NamespaceScopes)
            {
                usingCounts.Add((ushort)namespaceScope.UsedNamespaces.Length);
            }

            // ACASEY: This originally wrote (uint)12, (ushort)1, (ushort)0 in the
            // case where usingCounts was empty, but I'm not sure why.
            if (usingCounts.Count > 0)
            {
                uint streamLength = 0;
                cmw.WriteByte(4); // version
                cmw.WriteByte(0); // kind: UsingInfo
                cmw.Align(4);

                cmw.WriteUint(streamLength = BitArithmeticUtilities.Align((uint)usingCounts.Count * 2 + 10, 4));
                cmw.WriteUshort((ushort)usingCounts.Count);
                foreach (ushort uc in usingCounts)
                {
                    cmw.WriteUshort(uc);
                }

                cmw.Align(4);
                Debug.Assert(streamLength == customMetadata.Length);
                customDebugInfo.Add(customMetadata);
            }

            if (this.methodBodyWithModuleInfo != null && !ReferenceEquals(this.methodBodyWithModuleInfo, methodBody))
            {
                SerializeReferenceToMethodWithModuleInfo(customDebugInfo);
            }
        }

        private bool ShouldForwardToPreviousMethodWithUsingInfo(IMethodBody methodBody)
        {
            if (this.previousMethodBodyWithUsingInfo ==  null || ReferenceEquals(this.previousMethodBodyWithUsingInfo, methodBody))
            {
                return false;
            }

            // CONSIDER: is there a more efficient way to check if the scopes are the same?
            // CONSIDER: might want to cache the list of scopes.
            var previousScopes = this.previousMethodBodyWithUsingInfo.NamespaceScopes;
            return methodBody.NamespaceScopes.SequenceEqual(previousScopes, NamespaceScopeComparer.Instance);
        }

        private void SerializeReferenceToMethodWithModuleInfo(ArrayBuilder<MemoryStream> customDebugInfo)
        {
            MemoryStream customMetadata = new MemoryStream(12);
            BinaryWriter cmw = new BinaryWriter(customMetadata);
            cmw.WriteByte(4); // version
            cmw.WriteByte(2); // kind: ForwardToModuleInfo
            cmw.Align(4);
            cmw.WriteUint(12);
            cmw.WriteUint(this.methodTokenWithModuleInfo);
            customDebugInfo.Add(customMetadata);
        }

        private void SerializeReferenceToPreviousMethodWithUsingInfo(ArrayBuilder<MemoryStream> customDebugInfo)
        {
            MemoryStream customMetadata = new MemoryStream(12);
            BinaryWriter cmw = new BinaryWriter(customMetadata);
            cmw.WriteByte(4); // version
            cmw.WriteByte(1); // kind: ForwardInfo
            cmw.Align(4);
            cmw.WriteUint(12);
            cmw.WriteUint(this.previousMethodTokenWithUsingInfo);
            customDebugInfo.Add(customMetadata);
        }

        private class NamespaceScopeComparer : IEqualityComparer<NamespaceScope>
        {
            public static readonly IEqualityComparer<NamespaceScope> Instance = new NamespaceScopeComparer();

            public bool Equals(NamespaceScope x, NamespaceScope y)
            {
                Debug.Assert(x != null);
                Debug.Assert(y != null);
                return x.UsedNamespaces.SequenceEqual(y.UsedNamespaces, UsedNamespaceOrTypeComparer.Instance);
            }

            public int GetHashCode(NamespaceScope obj)
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private class UsedNamespaceOrTypeComparer : IEqualityComparer<UsedNamespaceOrType>
        {
            public static readonly IEqualityComparer<UsedNamespaceOrType> Instance = new UsedNamespaceOrTypeComparer();

            public bool Equals(UsedNamespaceOrType x, UsedNamespaceOrType y)
            {
                Debug.Assert(x != null);
                Debug.Assert(y != null);
                return x.Kind == y.Kind &&
                    x.Alias == y.Alias &&
                    x.TargetName == y.TargetName &&
                    x.ExternAlias == y.ExternAlias &&
                    x.ProjectLevel == y.ProjectLevel;
            }

            public int GetHashCode(UsedNamespaceOrType obj)
            {
                Debug.Assert(false);
                return 0;
            }
        }
    }
}
