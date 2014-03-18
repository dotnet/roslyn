// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Roslyn.Utilities.Pdb
{
    [Guid("A09E53B2-2A57-4cca-8F63-B84F7C35D4AA")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISymUnmanagedReader2
    {
        void __GetDocument(/*...*/);
        void __GetDocuments(/*...*/);
        void __GetUserEntryPoint(/*...*/);
        void __GetMethod(/*...*/);
        void __GetMethodByVersion(/*...*/);
        void __GetVariables(/*...*/);
        void __GetGlobalVariables(/*...*/);
        void __GetMethodFromDocumentPosition(/*...*/);
        void __GetSymAttribute(/*...*/);
        void __GetNamespaces(/*...*/);
        void __Initialize(/*...*/);
        void __UpdateSymbolStore(/*...*/);
        void __ReplaceSymbolStore(/*...*/);
        void __GetSymbolStoreFileName(/*...*/);
        void __GetMethodsFromDocumentPosition(/*...*/);
        void __GetDocumentVersion(/*...*/);
        void __GetMethodVersion(/*...*/);

        /// <summary>
        /// Get a symbol reader method given a method token and an E&C
        /// version number. Version numbers start at 1 and are incremented
        /// each time the method is changed due to an E&C operation.
        /// </summary>
        void __GetMethodByVersionPreRemap(/*[in] mdMethodDef token,
                                           [in] int version,
                                           [out, retval] ISymUnmanagedMethod** pRetVal*/);
        /// <summary>
        /// Gets a custom attribute based upon its name. Not to be
        /// confused with Metadata custom attributes, these attributes are
        /// held in the symbol store.
        /// </summary>
        void __GetSymAttributePreRemap(/*[in] mdToken parent,
                                        [in] WCHAR* name,
                                        [in] ULONG32 cBuffer,
                                        [out] ULONG32* pcBuffer,
                                        [out, size_is(cBuffer),
                                        length_is(*pcBuffer)] BYTE buffer[]*/);

        /// <summary>
        /// Gets every method that has line information in the provided Document.  
        /// </summary>
        void GetMethodsInDocument(
            ISymUnmanagedDocument document,
            int cMethod,
            out int pcMethod,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]ISymUnmanagedMethod[] pRetVal);
    }
}
