// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Cci;
using DWORD = System.UInt32;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal class Win32Resource : IWin32Resource
    {
        internal Win32Resource(
            byte[] data,
            DWORD codePage,
            DWORD languageId,
            int id,
            string name,
            int typeId,
            string typeName)
        {
            Data = data;
            CodePage = codePage;
            LanguageId = languageId;
            Id = id;
            Name = name;
            TypeId = typeId;
            TypeName = typeName;
        }

        public string TypeName { get; }

        public int TypeId { get; }

        public string Name { get; }

        public int Id { get; }

        public DWORD LanguageId { get; }

        public DWORD CodePage { get; }

        public IEnumerable<byte> Data { get; }
    }
}
