// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Roslyn.Utilities.Pdb
{
    internal static class SymUnmanagedReaderExtensions
    {
        private const int E_FAIL = unchecked((int)0x80004005);
        
        internal static ISymUnmanagedMethod GetBaselineMethod(this ISymUnmanagedReader reader, int methodToken)
        {
            ISymUnmanagedMethod method = null;
            int hr = reader.GetMethodByVersion(methodToken, 1, out method);
            if (hr == E_FAIL)
            {
                // method has no symbol info
                return null;
            }
            else if (hr != 0)
            {
                throw new ArgumentException(string.Format("Invalid method token '0x{0:x8}' (hresult = 0x{1:x8})", methodToken, hr), "methodToken");
            }

            Debug.Assert(method != null);
            return method;
        }

        /// <summary>
        /// Returns local names indexed by slot.
        /// </summary>
        internal static ImmutableArray<string> GetLocalNames(this ISymUnmanagedReader symReader, int methodToken, int ilOffset, bool isScopeEndInclusive)
        {
            if (symReader == null)
            {
                return ImmutableArray<string>.Empty;
            }
            
            var symMethod = symReader.GetBaselineMethod(methodToken);
            return symMethod.GetLocalVariableSlots(ilOffset, isScopeEndInclusive);
        }

        internal static ImmutableArray<string> GetLocalVariableSlots(this ISymUnmanagedMethod method)
        {
            return GetLocalVariableSlots(method, offset: -1, isScopeEndInclusive: false);
        }

        internal static ImmutableArray<string> GetLocalVariableSlots(this ISymUnmanagedMethod method, int offset, bool isScopeEndInclusive)
        {
            char[] nameBuffer = null;
            string[] result = null;
            int maxSlot = -1;

            ISymUnmanagedScope rootScope = method.GetRootScope();

            ISymUnmanagedVariable[] localsBuffer = null;

            ForEachLocalVariableRecursive(rootScope, offset, isScopeEndInclusive, ref localsBuffer, local =>
            {
                int nameLength;
                local.GetName(0, out nameLength, null);

                if (nameLength > 0)
                {
                    EnsureBufferSize(ref nameBuffer, nameLength);
                    local.GetName(nameLength, out nameLength, nameBuffer);

                    int slot;
                    local.GetAddressField1(out slot);

                    if (maxSlot < slot) 
                    {
                        maxSlot = slot;
                    }

                    EnsureBufferSize(ref result, slot + 1);

                    // nameLength is NUL-terminated
                    Debug.Assert(nameBuffer[nameLength - 1] == '\0');
                    result[slot] = new string(nameBuffer, 0, nameLength - 1);
                }
            });

            if (result == null)
            {
                return ImmutableArray.Create<string>();
            }

            return ImmutableArray.Create(result, 0, maxSlot + 1);
        }

        private static void ForEachLocalVariableRecursive(
            ISymUnmanagedScope scope,
            int offset,
            bool isScopeEndInclusive,
            ref ISymUnmanagedVariable[] localsBuffer,
            Action<ISymUnmanagedVariable> action)
        {
            Debug.Assert((offset < 0) || scope.IsInScope(offset, isScopeEndInclusive));

            // apply action on locals of the current scope:

            int localCount;
            scope.GetLocalCount(out localCount);

            if (localCount > 0)
            {
                EnsureBufferSize(ref localsBuffer, localCount);
                scope.GetLocals(localCount, out localCount, localsBuffer);

                for (int i = 0; i < localCount; i++)
                {
                    action(localsBuffer[i]);
                }
            }

            // recurse:

            int childCount;
            scope.GetChildren(0, out childCount, null);

            if (childCount > 0)
            {
                var children = new ISymUnmanagedScope[childCount];
                scope.GetChildren(childCount, out childCount, children);

                foreach (var child in children)
                {
                    if ((offset < 0) || child.IsInScope(offset, isScopeEndInclusive))
                    {
                        ForEachLocalVariableRecursive(child, offset, isScopeEndInclusive, ref localsBuffer, action);
                        if (offset >= 0)
                        {
                            return;
                        }
                    }
                }
            }
        }

        internal static bool IsInScope(this ISymUnmanagedScope scope, int offset, bool isEndInclusive)
        {
            int startOffset;
            scope.GetStartOffset(out startOffset);
            if (offset < startOffset)
            {
                return false;
            }

            int endOffset;
            scope.GetEndOffset(out endOffset);

            // In PDBs emitted by VB the end offset is inclusive, 
            // in PDBs emitted by C# the end offset is exclusive.
            return isEndInclusive ? offset <= endOffset : offset < endOffset;
        }

        private static void EnsureBufferSize<T>(ref T[] buffer, int minSize)
        {
            Debug.Assert(minSize != 0);

            if (buffer == null)
            {
                buffer = new T[Math.Max(8, minSize)];
            }

            if (minSize > buffer.Length)
            {
                Array.Resize(ref buffer, Math.Max((buffer.Length + 1) * 2, minSize));
            }
        }

        public static ISymUnmanagedScope GetRootScope(this ISymUnmanagedMethod method)
        {
            ISymUnmanagedScope scope;
            method.GetRootScope(out scope);
            return scope;
        }

        public static ImmutableArray<ISymUnmanagedScope> GetScopes(this ISymUnmanagedScope scope)
        {
            int numAvailable;
            scope.GetChildren(0, out numAvailable, null);
            if (numAvailable == 0)
            {
                return ImmutableArray<ISymUnmanagedScope>.Empty;
            }

            int numRead;
            var scopes = new ISymUnmanagedScope[numAvailable];
            scope.GetChildren(numAvailable, out numRead, scopes);
            if (numRead != numAvailable)
            {
                throw new InvalidOperationException(string.Format("Read only {0} of {1} child scopes.", numRead, numAvailable));
            }

            return ImmutableArray.Create(scopes);
        }

        public static ImmutableArray<ISymUnmanagedVariable> GetLocals(this ISymUnmanagedScope scope)
        {
            int numAvailable;
            scope.GetLocalCount(out numAvailable);
            if (numAvailable == 0)
            {
                return ImmutableArray<ISymUnmanagedVariable>.Empty;
            }

            int numRead;
            var locals = new ISymUnmanagedVariable[numAvailable];
            scope.GetLocals(numAvailable, out numRead, locals);
            if (numRead != numAvailable)
            {
                throw new InvalidOperationException(string.Format("Read only {0} of {1} locals.", numRead, numAvailable));
            }

            return ImmutableArray.Create(locals);
        }

        public static string GetName(this ISymUnmanagedVariable local)
        {
            int numAvailable;
            local.GetName(0, out numAvailable, null);
            if (numAvailable == 0)
            {
                return "";
            }

            var chars = new char[numAvailable];
            int numRead;
            local.GetName(numAvailable, out numRead, chars);
            if (numRead != numAvailable)
            {
                throw new InvalidOperationException(string.Format("Read only {0} of {1} name characters.", numRead, numAvailable));
            }

            Debug.Assert(chars[numAvailable - 1] == 0);
            return new string(chars, 0, numAvailable - 1);
        }

        public static ImmutableArray<ISymUnmanagedNamespace> GetNamespaces(this ISymUnmanagedScope scope)
        {
            int numNamespacesAvailable;
            scope.GetNamespaces(0, out numNamespacesAvailable, null);
            if (numNamespacesAvailable == 0)
            {
                return ImmutableArray<ISymUnmanagedNamespace>.Empty;
            }

            int numNamespacesRead;
            ISymUnmanagedNamespace[] namespaces = new ISymUnmanagedNamespace[numNamespacesAvailable];
            scope.GetNamespaces(numNamespacesAvailable, out numNamespacesRead, namespaces);
            if (numNamespacesRead != numNamespacesAvailable)
            {
                throw new InvalidOperationException(string.Format("Read only {0} of {1} namespaces.", numNamespacesRead, numNamespacesAvailable));
            }

            return ImmutableArray.Create(namespaces);
        }

        public static string GetName(this ISymUnmanagedNamespace @namespace)
        {
            int numAvailable;
            @namespace.GetName(0, out numAvailable, null);
            if (numAvailable == 0)
            {
                return "";
            }

            var chars = new char[numAvailable];
            int numRead;
            @namespace.GetName(numAvailable, out numRead, chars);
            if (numRead != numAvailable)
            {
                throw new InvalidOperationException(string.Format("Read only {0} of {1} name characters.", numRead, numAvailable));
            }

            Debug.Assert(chars[numAvailable - 1] == 0);
            return new string(chars, 0, numAvailable - 1);
        }

        public static int GetStartOffset(this ISymUnmanagedScope scope)
        {
            int startOffset;
            scope.GetStartOffset(out startOffset);
            return startOffset;
        }

        public static int GetEndOffset(this ISymUnmanagedScope scope)
        {
            int endOffset;
            scope.GetEndOffset(out endOffset);
            return endOffset;
        }

        internal static byte[] ToLocalSignature(this ISymUnmanagedVariable variable)
        {
            int n;
            variable.GetSignature(0, out n, null);
            var bytes = new byte[n];
            variable.GetSignature(n, out n, bytes);
            int slot;
            variable.GetAddressField1(out slot);
            return bytes;
        }
    }
}
