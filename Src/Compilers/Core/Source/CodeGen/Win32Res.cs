// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Cci = Microsoft.Cci;
using DWORD = System.UInt32;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal class Win32Resource : Cci.IWin32Resource
    {
        private readonly byte[] data;
        private readonly DWORD codePage;
        private readonly DWORD languageId;
        private readonly int id;
        private readonly string name;
        private readonly int typeId;
        private readonly string typeName;

        internal Win32Resource(
            byte[] data,
            DWORD codePage,
            DWORD languageId,
            int id,
            string name,
            int typeId,
            string typeName)
        {
            this.data = data;
            this.codePage = codePage;
            this.languageId = languageId;
            this.id = id;
            this.name = name;
            this.typeId = typeId;
            this.typeName = typeName;
        }

        public string TypeName
        {
            get { return typeName; }
        }

        public int TypeId
        {
            get { return typeId; }
        }

        public string Name
        {
            get { return name; }
        }

        public int Id
        {
            get { return id; }
        }

        public DWORD LanguageId
        {
            get { return languageId; }
        }

        public DWORD CodePage
        {
            get { return codePage; }
        }

        public IEnumerable<byte> Data
        {
            get { return data; }
        }
    }
}