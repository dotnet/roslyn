﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.SymReaderInterop
{
    internal struct AsyncStepInfo : IEquatable<AsyncStepInfo>
    {
        public readonly int YieldOffset;
        public readonly int ResumeOffset;
        public readonly int ResumeMethod;

        public AsyncStepInfo(int yieldOffset, int resumeOffset, int resumeMethod)
        {
            this.YieldOffset = yieldOffset;
            this.ResumeOffset = resumeOffset;
            this.ResumeMethod = resumeMethod;
        }

        public override bool Equals(object obj)
        {
            return obj is AsyncStepInfo && Equals((AsyncStepInfo)obj);
        }

        public bool Equals(AsyncStepInfo other)
        {
            return YieldOffset == other.YieldOffset
                && ResumeMethod == other.ResumeMethod
                && ResumeOffset == other.ResumeOffset;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(YieldOffset, Hash.Combine(ResumeMethod, ResumeOffset));
        }
    }

    internal static class SymUnmanagedReaderExtensions
    {
        internal const int S_OK = 0x0;
        internal const int S_FALSE = 0x1;
        internal const int E_FAIL = unchecked((int)0x80004005);
        internal const int E_NOTIMPL = unchecked((int)0x80004001);

        private static readonly IntPtr s_ignoreIErrorInfo = new IntPtr(-1);

        // The name of the attribute containing the byte array of custom debug info.
        // MSCUSTOMDEBUGINFO in Dev10.
        private const string CdiAttributeName = "MD2";

        #region Interop Helpers

        private delegate int CountGetter<TEntity>(TEntity entity, out int count);
        private delegate int ItemsGetter<TEntity, TItem>(TEntity entity, int bufferLength, out int count, TItem[] buffer);
        private delegate int ItemsGetter<TEntity, TArg1, TItem>(TEntity entity, TArg1 arg1, int bufferLength, out int count, TItem[] buffer);
        private delegate int ItemsGetter<TEntity, TArg1, TArg2, TItem>(TEntity entity, TArg1 arg1, TArg2 arg2, int bufferLength, out int count, TItem[] buffer);

        private static ImmutableArray<T> ToImmutableOrEmpty<T>(T[] items)
        {
            return (items == null) ? ImmutableArray<T>.Empty : ImmutableArray.CreateRange<T>(items);
        }

        private static string ToString(char[] buffer)
        {
            Debug.Assert(buffer[buffer.Length - 1] == 0);
            return new string(buffer, 0, buffer.Length - 1);
        }

        private static void ValidateItems(int actualCount, int bufferLength)
        {
            if (actualCount != bufferLength)
            {
                throw new InvalidOperationException(string.Format("Read only {0} of {1} items.", actualCount, bufferLength));
            }
        }

        private static TItem[] GetItems<TEntity, TItem>(TEntity entity, CountGetter<TEntity> countGetter, ItemsGetter<TEntity, TItem> itemsGetter)
        {
            int count;
            int hr = countGetter(entity, out count);
            ThrowExceptionForHR(hr);
            if (count == 0)
            {
                return null;
            }

            var result = new TItem[count];
            hr = itemsGetter(entity, count, out count, result);
            ThrowExceptionForHR(hr);
            ValidateItems(count, result.Length);
            return result;
        }

        private static TItem[] GetItems<TEntity, TItem>(TEntity entity, ItemsGetter<TEntity, TItem> getter)
        {
            int count;
            int hr = getter(entity, 0, out count, null);
            ThrowExceptionForHR(hr);
            if (count == 0)
            {
                return null;
            }

            var result = new TItem[count];
            hr = getter(entity, count, out count, result);
            ThrowExceptionForHR(hr);
            ValidateItems(count, result.Length);
            return result;
        }

        private static TItem[] GetItems<TEntity, TArg1, TItem>(TEntity entity, TArg1 arg1, ItemsGetter<TEntity, TArg1, TItem> getter)
        {
            int count;
            int hr = getter(entity, arg1, 0, out count, null);
            ThrowExceptionForHR(hr);
            if (count == 0)
            {
                return null;
            }

            var result = new TItem[count];
            hr = getter(entity, arg1, count, out count, result);
            ThrowExceptionForHR(hr);
            ValidateItems(count, result.Length);
            return result;
        }

        private static TItem[] GetItems<TEntity, TArg1, TArg2, TItem>(TEntity entity, TArg1 arg1, TArg2 arg2, ItemsGetter<TEntity, TArg1, TArg2, TItem> getter)
        {
            int count;
            int hr = getter(entity, arg1, arg2, 0, out count, null);
            ThrowExceptionForHR(hr);
            if (count == 0)
            {
                return null;
            }

            var result = new TItem[count];
            hr = getter(entity, arg1, arg2, count, out count, result);
            ThrowExceptionForHR(hr);
            ValidateItems(count, result.Length);
            return result;
        }

        #endregion

        public static ISymUnmanagedReader CreateReader(Stream pdbStream, object metadataImporter)
        {
            Guid corSymReaderSxS = new Guid("0A3976C5-4529-4ef8-B0B0-42EED37082CD");
            var reader = (ISymUnmanagedReader)Activator.CreateInstance(Type.GetTypeFromCLSID(corSymReaderSxS));
            int hr = reader.Initialize(metadataImporter, null, null, new ComStreamWrapper(pdbStream));
            ThrowExceptionForHR(hr);
            return reader;
        }

        /// <summary>
        /// Get the blob of binary custom debug info for a given method.
        /// TODO: consume <paramref name="methodVersion"/> (DevDiv #1068138).
        /// </summary>
        public static byte[] GetCustomDebugInfoBytes(this ISymUnmanagedReader reader, int methodToken, int methodVersion)
        {
            return GetItems(reader, new SymbolToken(methodToken), CdiAttributeName,
                (ISymUnmanagedReader a, SymbolToken b, string c, int d, out int e, byte[] f) => a.GetSymAttribute(b, c, d, out e, f));
        }

        public static int GetUserEntryPoint(this ISymUnmanagedReader symReader)
        {
            SymbolToken entryPoint;
            int hr = symReader.GetUserEntryPoint(out entryPoint);
            if (hr == E_FAIL)
            {
                // Not all assemblies have entry points
                // dlls for example...
                return 0;
            }

            ThrowExceptionForHR(hr);
            return entryPoint.GetToken();
        }

        public static ImmutableArray<ISymUnmanagedDocument> GetDocuments(this ISymUnmanagedReader reader)
        {
            return ToImmutableOrEmpty(GetItems(reader,
                (ISymUnmanagedReader a, int b, out int c, ISymUnmanagedDocument[] d) => a.GetDocuments(b, out c, d)));
        }

        public static int GetToken(this ISymUnmanagedMethod method)
        {
            SymbolToken token;
            int hr = method.GetToken(out token);
            ThrowExceptionForHR(hr);
            return token.GetToken();
        }

        public static ImmutableArray<ISymUnmanagedDocument> GetDocumentsForMethod(this ISymUnmanagedMethod method)
        {
            return ToImmutableOrEmpty(GetItems((ISymENCUnmanagedMethod)method,
                (ISymENCUnmanagedMethod a, out int b) => a.GetDocumentsForMethodCount(out b),
                (ISymENCUnmanagedMethod a, int b, out int c, ISymUnmanagedDocument[] d) => a.GetDocumentsForMethod(b, out c, d)));
        }

        public static void GetSourceExtentInDocument(this ISymUnmanagedMethod method, ISymUnmanagedDocument document, out int startLine, out int endLine)
        {
            var symMethodEnc = (ISymENCUnmanagedMethod)method;
            int hr = symMethodEnc.GetSourceExtentInDocument(document, out startLine, out endLine);
            ThrowExceptionForHR(hr);
        }

        public static ImmutableArray<ISymUnmanagedMethod> GetMethodsInDocument(this ISymUnmanagedReader reader, ISymUnmanagedDocument symDocument)
        {
            return ToImmutableOrEmpty(GetItems((ISymUnmanagedReader2)reader, symDocument,
                (ISymUnmanagedReader2 a, ISymUnmanagedDocument b, int c, out int d, ISymUnmanagedMethod[] e) =>
                {
                    uint ud;
                    var result = a.GetMethodsInDocument(b, (uint)c, out ud, e);
                    d = (int)ud;
                    return result;
                }));
        }

        public static ISymUnmanagedMethod GetMethod(this ISymUnmanagedReader reader, int methodToken)
        {
            return GetMethodByVersion(reader, methodToken, methodVersion: 1);
        }

        public static ISymUnmanagedMethod GetMethodByVersion(this ISymUnmanagedReader reader, int methodToken, int methodVersion)
        {
            ISymUnmanagedMethod method = null;
            int hr = reader.GetMethodByVersion(new SymbolToken(methodToken), methodVersion, out method);
            ThrowExceptionForHR(hr);

            if (hr < 0)
            {
                // method has no symbol info
                return null;
            }

            Debug.Assert(method != null);
            return method;
        }

        internal static string GetName(this ISymUnmanagedDocument document)
        {
            return ToString(GetItems(document,
                (ISymUnmanagedDocument a, int b, out int c, char[] d) => a.GetURL(b, out c, d)));
        }

        public static ImmutableArray<byte> GetCheckSum(this ISymUnmanagedDocument document)
        {
            return ToImmutableOrEmpty(GetItems(document,
                (ISymUnmanagedDocument a, int b, out int c, byte[] d) => a.GetCheckSum(b, out c, d)));
        }

        public static Guid GetLanguage(this ISymUnmanagedDocument document)
        {
            Guid result = default(Guid);
            int hr = document.GetLanguage(ref result);
            ThrowExceptionForHR(hr);
            return result;
        }

        public static Guid GetLanguageVendor(this ISymUnmanagedDocument document)
        {
            Guid result = default(Guid);
            int hr = document.GetLanguageVendor(ref result);
            ThrowExceptionForHR(hr);
            return result;
        }

        public static Guid GetDocumentType(this ISymUnmanagedDocument document)
        {
            Guid result = default(Guid);
            int hr = document.GetDocumentType(ref result);
            ThrowExceptionForHR(hr);
            return result;
        }

        public static Guid GetHashAlgorithm(this ISymUnmanagedDocument document)
        {
            Guid result = default(Guid);
            int hr = document.GetCheckSumAlgorithmId(ref result);
            ThrowExceptionForHR(hr);
            return result;
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

        public static ImmutableArray<ISymUnmanagedNamespace> GetNamespaces(this ISymUnmanagedScope scope)
        {
            return ToImmutableOrEmpty(GetItems(scope,
                (ISymUnmanagedScope a, int b, out int c, ISymUnmanagedNamespace[] d) => a.GetNamespaces(b, out c, d)));
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
            return ToImmutableOrEmpty(GetScopesInternal(scope));
        }

        private static ISymUnmanagedScope[] GetScopesInternal(ISymUnmanagedScope scope)
        {
            return GetItems(scope,
                (ISymUnmanagedScope a, int b, out int c, ISymUnmanagedScope[] d) => a.GetChildren(b, out c, d));
        }

        public static ImmutableArray<ISymUnmanagedVariable> GetLocals(this ISymUnmanagedScope scope)
        {
            return ToImmutableOrEmpty(GetLocalsInternal(scope));
        }

        private static ISymUnmanagedVariable[] GetLocalsInternal(ISymUnmanagedScope scope)
        {
            return GetItems(scope,
                (ISymUnmanagedScope a, out int b) => a.GetLocalCount(out b),
                (ISymUnmanagedScope a, int b, out int c, ISymUnmanagedVariable[] d) => a.GetLocals(b, out c, d));
        }

        public static ImmutableArray<ISymUnmanagedConstant> GetConstants(this ISymUnmanagedScope scope)
        {
            return ToImmutableOrEmpty(GetConstantsInternal((ISymUnmanagedScope2)scope));
        }

        private static ISymUnmanagedConstant[] GetConstantsInternal(ISymUnmanagedScope2 scope)
        {
            return GetItems(scope,
               (ISymUnmanagedScope2 a, int b, out int c, ISymUnmanagedConstant[] d) => a.GetConstants(b, out c, d));
        }

        internal static int GetSlot(this ISymUnmanagedVariable local)
        {
            int result;
            int hr = local.GetAddressField1(out result);
            ThrowExceptionForHR(hr);
            return result;
        }

        internal static int GetAttributes(this ISymUnmanagedVariable local)
        {
            int result;
            int hr = local.GetAttributes(out result);
            ThrowExceptionForHR(hr);
            return result;
        }

        public static string GetName(this ISymUnmanagedVariable local)
        {
            return ToString(GetItems(local,
                (ISymUnmanagedVariable a, int b, out int c, char[] d) => a.GetName(b, out c, d)));
        }

        public static string GetName(this ISymUnmanagedConstant constant)
        {
            return ToString(GetItems(constant,
                (ISymUnmanagedConstant a, int b, out int c, char[] d) => a.GetName(b, out c, d)));
        }

        public static object GetValue(this ISymUnmanagedConstant constant)
        {
            object value;
            int hr = constant.GetValue(out value);
            ThrowExceptionForHR(hr);
            return value;
        }

        public static ImmutableArray<byte> GetSignature(this ISymUnmanagedConstant constant)
        {
            return ToImmutableOrEmpty(GetItems(constant,
                (ISymUnmanagedConstant a, int b, out int c, byte[] d) => a.GetSignature(b, out c, d)));
        }

        public static string GetName(this ISymUnmanagedNamespace @namespace)
        {
            return ToString(GetItems(@namespace,
                (ISymUnmanagedNamespace a, int b, out int c, char[] d) => a.GetName(b, out c, d)));
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

        internal static void ThrowExceptionForHR(int hr)
        {
            // E_FAIL indicates "no info".
            // E_NOTIMPL indicates a lack of ISymUnmanagedReader support (in a particular implementation).
            if (hr < 0 && hr != E_FAIL && hr != E_NOTIMPL)
            {
                Marshal.ThrowExceptionForHR(hr, s_ignoreIErrorInfo);
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
        internal static ImmutableArray<string> GetImportStrings(this ISymUnmanagedMethod method)
        {
            if (method == null)
            {
                // In rare circumstances (only bad PDBs?) GetMethodByVersion can return null.
                // If there's no debug info for the method, then no import strings are available.
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

            return ImmutableArray.CreateRange(namespaces, n => n.GetName());
        }

        public static ISymUnmanagedAsyncMethod AsAsync(this ISymUnmanagedMethod method)
        {
            var asyncMethod = method as ISymUnmanagedAsyncMethod;
            if (asyncMethod == null)
            {
                return null;
            }

            bool isAsyncMethod;
            int hr = asyncMethod.IsAsyncMethod(out isAsyncMethod);
            ThrowExceptionForHR(hr);
            if (!isAsyncMethod)
            {
                return null;
            }

            return asyncMethod;
        }

        public static int GetCatchHandlerILOffset(this ISymUnmanagedAsyncMethod asyncMethod)
        {
            bool hasCatchHandler;
            int hr = asyncMethod.HasCatchHandlerILOffset(out hasCatchHandler);
            ThrowExceptionForHR(hr);
            if (!hasCatchHandler)
            {
                return -1;
            }

            uint result;
            hr = asyncMethod.GetCatchHandlerILOffset(out result);
            ThrowExceptionForHR(hr);
            return (int)result;
        }

        public static int GetKickoffMethod(this ISymUnmanagedAsyncMethod asyncMethod)
        {
            uint result;
            int hr = asyncMethod.GetKickoffMethod(out result);
            ThrowExceptionForHR(hr);
            return (int)result;
        }

        public static IEnumerable<AsyncStepInfo> GetAsyncStepInfos(this ISymUnmanagedAsyncMethod asyncMethod)
        {
            uint count;
            int hr = asyncMethod.GetAsyncStepInfoCount(out count);
            ThrowExceptionForHR(hr);
            if (count == 0)
            {
                yield break;
            }

            var yieldOffsets = new uint[count];
            var breakpointOffsets = new uint[count];
            var breakpointMethods = new uint[count];
            hr = asyncMethod.GetAsyncStepInfo(count, out count, yieldOffsets, breakpointOffsets, breakpointMethods);
            ThrowExceptionForHR(hr);
            ValidateItems((int)count, yieldOffsets.Length);
            ValidateItems((int)count, breakpointOffsets.Length);
            ValidateItems((int)count, breakpointMethods.Length);

            for (int i = 0; i < count; i++)
            {
                yield return new AsyncStepInfo((int)yieldOffsets[i], (int)breakpointOffsets[i], (int)breakpointMethods[i]);
            }
        }
    }
}
