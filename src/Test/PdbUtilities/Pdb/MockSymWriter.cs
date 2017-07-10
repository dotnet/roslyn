// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Cci;

namespace Roslyn.Test.Utilities
{
    internal class MockSymUnmanagedWriter : ISymUnmanagedWriter5
    {
        public virtual void Abort()
        {
            throw new NotImplementedException();
        }

        public virtual void Close()
        {
            throw new NotImplementedException();
        }

        public void CloseMapTokensToSourceSpans()
        {
            throw new NotImplementedException();
        }

        public virtual void CloseMethod()
        {
            throw new NotImplementedException();
        }

        public virtual void CloseNamespace()
        {
            throw new NotImplementedException();
        }

        public virtual void CloseScope(uint endOffset)
        {
            throw new NotImplementedException();
        }

        public void Commit()
        {
            throw new NotImplementedException();
        }

        public virtual void DefineConstant(string name, object value, uint sig, IntPtr signature)
        {
            throw new NotImplementedException();
        }

        public virtual void DefineConstant2(string name, VariantStructure value, uint sigToken)
        {
            throw new NotImplementedException();
        }

        public virtual ISymUnmanagedDocumentWriter DefineDocument(string url, ref Guid language, ref Guid languageVendor, ref Guid documentType)
        {
            throw new NotImplementedException();
        }

        public virtual void DefineField(uint parent, string name, uint attributes, uint sig, IntPtr signature, uint addrKind, uint addr1, uint addr2, uint addr3)
        {
            throw new NotImplementedException();
        }

        public virtual void DefineGlobalVariable(string name, uint attributes, uint sig, IntPtr signature, uint addrKind, uint addr1, uint addr2, uint addr3)
        {
            throw new NotImplementedException();
        }

        public virtual void DefineGlobalVariable2(string name, uint attributes, uint sigToken, uint addrKind, uint addr1, uint addr2, uint addr3)
        {
            throw new NotImplementedException();
        }

        public virtual void DefineLocalVariable(string name, uint attributes, uint sig, IntPtr signature, uint addrKind, uint addr1, uint addr2, uint startOffset, uint endOffset)
        {
            throw new NotImplementedException();
        }

        public virtual void DefineLocalVariable2(string name, uint attributes, uint sigToken, uint addrKind, uint addr1, uint addr2, uint addr3, uint startOffset, uint endOffset)
        {
            throw new NotImplementedException();
        }

        public virtual void DefineParameter(string name, uint attributes, uint sequence, uint addrKind, uint addr1, uint addr2, uint addr3)
        {
            throw new NotImplementedException();
        }

        public virtual void DefineSequencePoints(ISymUnmanagedDocumentWriter document, uint count, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]uint[] offsets, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]uint[] lines, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]uint[] columns, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]uint[] endLines, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]uint[] endColumns)
        {
            throw new NotImplementedException();
        }

        public virtual void GetDebugInfo(ref ImageDebugDirectory ptrIDD, uint dataCount, out uint dataCountPtr, IntPtr data)
        {
            throw new NotImplementedException();
        }

        public void GetDebugInfoWithPadding(ref ImageDebugDirectory debugDirectory, uint dataCount, out uint dataCountPtr, IntPtr data)
        {
            throw new NotImplementedException();
        }

        public virtual void Initialize([MarshalAs(UnmanagedType.IUnknown)]object emitter, string filename, [MarshalAs(UnmanagedType.IUnknown)]object ptrIStream, bool fullBuild)
        {
            throw new NotImplementedException();
        }

        public virtual void Initialize2([MarshalAs(UnmanagedType.IUnknown)]object emitter, string tempfilename, [MarshalAs(UnmanagedType.IUnknown)]object ptrIStream, bool fullBuild, string finalfilename)
        {
            throw new NotImplementedException();
        }

        public void MapTokenToSourceSpan(uint token, ISymUnmanagedDocumentWriter document, uint startLine, uint startColumn, uint endLine, uint endColumn)
        {
            throw new NotImplementedException();
        }

        public void OpenMapTokensToSourceSpans()
        {
            throw new NotImplementedException();
        }

        public virtual void OpenMethod(uint method)
        {
            throw new NotImplementedException();
        }

        public void OpenMethod2(uint methodToken, int sectionIndex, int offsetRelativeOffset)
        {
            throw new NotImplementedException();
        }

        public virtual void OpenNamespace(string name)
        {
            throw new NotImplementedException();
        }

        public virtual uint OpenScope(uint startOffset)
        {
            throw new NotImplementedException();
        }

        public virtual void RemapToken(uint oldToken, uint newToken)
        {
            throw new NotImplementedException();
        }

        public virtual void SetMethodSourceRange(ISymUnmanagedDocumentWriter startDoc, uint startLine, uint startColumn, object endDoc, uint endLine, uint endColumn)
        {
            throw new NotImplementedException();
        }

        public virtual void SetScopeRange(uint scopeID, uint startOffset, uint endOffset)
        {
            throw new NotImplementedException();
        }

        public virtual void SetSymAttribute(uint parent, string name, uint data, IntPtr signature)
        {
            throw new NotImplementedException();
        }

        public virtual void SetUserEntryPoint(uint entryMethod)
        {
            throw new NotImplementedException();
        }

        public virtual void UsingNamespace(string fullName)
        {
            throw new NotImplementedException();
        }
    }
}
