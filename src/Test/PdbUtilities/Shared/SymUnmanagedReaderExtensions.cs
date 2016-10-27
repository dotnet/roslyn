// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader
{
    internal static class SymUnmanagedReaderExtensions
    {
        internal const int S_OK = 0x0;
        internal const int S_FALSE = 0x1;
        internal const int E_FAIL = unchecked((int)0x80004005);
        internal const int E_NOTIMPL = unchecked((int)0x80004001);

        private static readonly IntPtr s_ignoreIErrorInfo = new IntPtr(-1);

        /// <summary>
        /// Get the blob of binary custom debug info for a given method.
        /// </summary>
        public static byte[] GetCustomDebugInfoBytes(this ISymUnmanagedReader3 reader, int methodToken, int methodVersion)
        {
            try
            {
                return reader.GetCustomDebugInfo(methodToken, methodVersion);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Sometimes the debugger returns the HRESULT for ArgumentOutOfRangeException, rather than E_FAIL,
                // for methods without custom debug info (https://github.com/dotnet/roslyn/issues/4138).
                return null;
            }
        }

        internal static ImmutableArray<string> GetLocalVariableSlots(this ISymUnmanagedMethod method)
        {
            var builder = ImmutableArray.CreateBuilder<string>();
            ISymUnmanagedScope rootScope = method.GetRootScope();

            ForEachLocalVariableRecursive(rootScope, offset: -1, isScopeEndInclusive: false, action: local =>
            {
                int slot = local.GetSlot();
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
            Debug.Assert(offset < 0 || scope.IsInScope(offset, isScopeEndInclusive));

            // apply action on locals of the current scope:
            foreach (var local in scope.GetLocals())
            {
                action(local);
            }

            // recurse:
            foreach (var child in scope.GetChildren())
            {
                if (offset < 0 || child.IsInScope(offset, isScopeEndInclusive))
                {
                    ForEachLocalVariableRecursive(child, offset, isScopeEndInclusive, action);
                    if (offset >= 0)
                    {
                        return;
                    }
                }
            }
        }

        internal static bool IsInScope(this ISymUnmanagedScope scope, int offset, bool isEndInclusive)
        {
            int startOffset = scope.GetStartOffset();
            if (offset < startOffset)
            {
                return false;
            }

            int endOffset = scope.GetEndOffset();

            // In PDBs emitted by VB the end offset is inclusive, 
            // in PDBs emitted by C# the end offset is exclusive.
            return isEndInclusive ? offset <= endOffset : offset < endOffset;
        }

        internal static void ThrowExceptionForHR(int hr)
        {
            // E_FAIL indicates "no info".
            // E_NOTIMPL indicates a lack of ISymUnmanagedReader support (in a particular implementation).
            if (hr < 0 && hr != E_FAIL && hr != E_NOTIMPL)
            {
                Marshal.ThrowExceptionForHR(hr, s_ignoreIErrorInfo);
            }
        }
    }
}
