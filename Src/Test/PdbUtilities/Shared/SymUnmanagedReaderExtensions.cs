// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.SymReaderInterop
{
    internal static class SymUnmanagedReaderExtensions
    {
        internal const int S_OK = 0x0;
        internal const int S_FALSE = 0x1;
        internal const int E_FAIL = unchecked((int)0x80004005);

        private static readonly IntPtr IgnoreIErrorInfo = new IntPtr(-1);

        // The name of the attribute containing the byte array of custom debug info.
        // MSCUSTOMDEBUGINFO in Dev10.
        private const string CdiAttributeName = "MD2"; 

        /// <summary>
        /// Get the blob of binary custom debug info for a given method.
        /// TODO: consume <paramref name="methodVersion"/> (DevDiv #1068138).
        /// </summary>
        internal static byte[] GetCustomDebugInfo(this ISymUnmanagedReader symReader, int methodToken, int methodVersion)
        {
            SymbolToken symbolToken = new SymbolToken(methodToken);

            int bytesAvailable;
            int hr = symReader.GetSymAttribute(symbolToken, CdiAttributeName, 0, out bytesAvailable, buffer: null);
            ThrowExceptionForHR(hr);

            if (bytesAvailable <= 0)
            {
                return null;
            }

            var buffer = new byte[bytesAvailable];
            int bytesRead;
            hr = symReader.GetSymAttribute(symbolToken, CdiAttributeName, bytesAvailable, out bytesRead, buffer);
            ThrowExceptionForHR(hr);

            if (bytesAvailable != bytesRead)
            {
                return null;
            }

            return buffer;
        }

        internal static ISymUnmanagedMethod GetBaselineMethod(this ISymUnmanagedReader reader, int methodToken)
        {
            return reader.GetMethodByVersion(methodToken, methodVersion: 1);
        }

        internal static ISymUnmanagedMethod GetMethodByVersion(this ISymUnmanagedReader reader, int methodToken, int methodVersion)
        {
            ISymUnmanagedMethod method = null;
            int hr = reader.GetMethodByVersion(new SymbolToken(methodToken), methodVersion, out method);
            if (hr == E_FAIL)
            {
                // method has no symbol info
                return null;
            }
            else if (hr != 0)
            {
                throw new ArgumentException(string.Format("Invalid method token '0x{0:x8}' or version '{1}' (hresult = 0x{2:x8})", methodToken, methodVersion, hr), "methodToken");
            }

            Debug.Assert(method != null);
            return method;
        }

        internal static ImmutableArray<string> GetLocalVariableSlots(this ISymUnmanagedMethod method)
        {
            var builder = ImmutableArray.CreateBuilder<string>();
            ISymUnmanagedScope rootScope = method.GetRootScope();

            ForEachLocalVariableRecursive(rootScope, offset: -1, isScopeEndInclusive: false, action: local =>
            {
                var slot = local.GetSlot();
                while (builder.Count <= slot)
                {
                    builder.Add(null);
                }

                var name = local.GetName();
                builder[slot] = name;
            });

            return builder.ToImmutable();
        }

        private static void ForEachLocalVariableRecursive(
            ISymUnmanagedScope scope,
            int offset,
            bool isScopeEndInclusive,
            Action<ISymUnmanagedVariable> action)
        {
            Debug.Assert((offset < 0) || scope.IsInScope(offset, isScopeEndInclusive));

            // apply action on locals of the current scope:
            var locals = GetLocalsInternal(scope);
            if (locals != null)
            {
                foreach (var local in locals)
                {
                    action(local);
                }
            }

            // recurse:
            var children = GetScopesInternal(scope);
            if (children != null)
            {
                foreach (var child in children)
                {
                    if ((offset < 0) || child.IsInScope(offset, isScopeEndInclusive))
                    {
                        ForEachLocalVariableRecursive(child, offset, isScopeEndInclusive, action);
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
            int hr = scope.GetStartOffset(out startOffset);
            ThrowExceptionForHR(hr);
            if (offset < startOffset)
            {
                return false;
            }

            int endOffset;
            hr = scope.GetEndOffset(out endOffset);
            ThrowExceptionForHR(hr);

            // In PDBs emitted by VB the end offset is inclusive, 
            // in PDBs emitted by C# the end offset is exclusive.
            return isEndInclusive ? offset <= endOffset : offset < endOffset;
        }

        public static ISymUnmanagedScope GetRootScope(this ISymUnmanagedMethod method)
        {
            ISymUnmanagedScope scope;
            int hr = method.GetRootScope(out scope);
            ThrowExceptionForHR(hr);
            return scope;
        }

        public static ImmutableArray<ISymUnmanagedScope> GetScopes(this ISymUnmanagedScope scope)
        {
            var scopes = GetScopesInternal(scope);
            return (scopes == null) ? ImmutableArray<ISymUnmanagedScope>.Empty : ImmutableArray.Create(scopes);
        }

        private static ISymUnmanagedScope[] GetScopesInternal(ISymUnmanagedScope scope)
        {
            int numAvailable;
            int hr = scope.GetChildren(0, out numAvailable, null);
            ThrowExceptionForHR(hr);
            if (numAvailable == 0)
            {
                return null;
            }

            int numRead;
            var scopes = new ISymUnmanagedScope[numAvailable];
            hr = scope.GetChildren(numAvailable, out numRead, scopes);
            ThrowExceptionForHR(hr);
            if (numRead != numAvailable)
            {
                throw new InvalidOperationException(string.Format("Read only {0} of {1} child scopes.", numRead, numAvailable));
            }

            return scopes;
        }

        public static ImmutableArray<ISymUnmanagedVariable> GetLocals(this ISymUnmanagedScope scope)
        {
            var locals = GetLocalsInternal(scope);
            return (locals == null) ? ImmutableArray<ISymUnmanagedVariable>.Empty : ImmutableArray.Create(locals);
        }

        private static ISymUnmanagedVariable[] GetLocalsInternal(ISymUnmanagedScope scope)
        {
            int numAvailable;
            int hr = scope.GetLocalCount(out numAvailable);
            ThrowExceptionForHR(hr);
            if (numAvailable == 0)
            {
                return null;
            }

            int numRead;
            var locals = new ISymUnmanagedVariable[numAvailable];
            hr = scope.GetLocals(numAvailable, out numRead, locals);
            ThrowExceptionForHR(hr);
            if (numRead != numAvailable)
            {
                throw new InvalidOperationException(string.Format("Read only {0} of {1} locals.", numRead, numAvailable));
            }

            return locals;
        }

        internal static int GetSlot(this ISymUnmanagedVariable local)
        {
            int slot;
            int hr = local.GetAddressField1(out slot);
            ThrowExceptionForHR(hr);
            return slot;
        }

        public static string GetName(this ISymUnmanagedVariable local)
        {
            int numAvailable;
            int hr = local.GetName(0, out numAvailable, null);
            ThrowExceptionForHR(hr);
            if (numAvailable == 0)
            {
                return "";
            }

            var chars = new char[numAvailable];
            int numRead;
            hr = local.GetName(numAvailable, out numRead, chars);
            ThrowExceptionForHR(hr);
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
            int hr = scope.GetNamespaces(0, out numNamespacesAvailable, null);
            ThrowExceptionForHR(hr);
            if (numNamespacesAvailable == 0)
            {
                return ImmutableArray<ISymUnmanagedNamespace>.Empty;
            }

            int numNamespacesRead;
            ISymUnmanagedNamespace[] namespaces = new ISymUnmanagedNamespace[numNamespacesAvailable];
            hr = scope.GetNamespaces(numNamespacesAvailable, out numNamespacesRead, namespaces);
            ThrowExceptionForHR(hr);
            if (numNamespacesRead != numNamespacesAvailable)
            {
                throw new InvalidOperationException(string.Format("Read only {0} of {1} namespaces.", numNamespacesRead, numNamespacesAvailable));
            }

            return ImmutableArray.Create(namespaces);
        }

        public static string GetName(this ISymUnmanagedNamespace @namespace)
        {
            int numAvailable;
            int hr = @namespace.GetName(0, out numAvailable, null);
            ThrowExceptionForHR(hr);
            if (numAvailable == 0)
            {
                return "";
            }

            var chars = new char[numAvailable];
            int numRead;
            hr = @namespace.GetName(numAvailable, out numRead, chars);
            ThrowExceptionForHR(hr);
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
            int hr = scope.GetStartOffset(out startOffset);
            ThrowExceptionForHR(hr);
            return startOffset;
        }

        public static int GetEndOffset(this ISymUnmanagedScope scope)
        {
            int endOffset;
            int hr = scope.GetEndOffset(out endOffset);
            ThrowExceptionForHR(hr);
            return endOffset;
        }

        internal static byte[] ToLocalSignature(this ISymUnmanagedVariable variable)
        {
            int n;
            int hr = variable.GetSignature(0, out n, null);
            ThrowExceptionForHR(hr);
            var bytes = new byte[n];
            hr = variable.GetSignature(n, out n, bytes);
            ThrowExceptionForHR(hr);
            return bytes;
        }

        internal static void ThrowExceptionForHR(int hr)
        {
            // E_FAIL indicates "no info".
            if (hr != E_FAIL)
            {
                Marshal.ThrowExceptionForHR(hr, IgnoreIErrorInfo);
            }
        }
    }
}
