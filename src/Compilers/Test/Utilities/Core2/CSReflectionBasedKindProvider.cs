// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class CSReflectionBasedKindProvider : ISyntaxNodeKindProvider
    {
        private const string CS_DLL = "Microsoft.CodeAnalysis.CSharp.dll";
        private const string CS_KIND_TYPE = "Roslyn.Compilers.CSharp.SyntaxKind";
        private Type m_CSKindType = null;
        private readonly string m_Folder = null;

        public CSReflectionBasedKindProvider(string folder)
        {
            m_Folder = Path.GetFullPath(folder);
            GetKindTypes();
        }

        private void GetKindTypes()
        {
            if (m_CSKindType == null)
            {
                var asm = Assembly.LoadFrom(Path.Combine(m_Folder, CS_DLL));
                m_CSKindType = asm.GetType(CS_KIND_TYPE);
            }
        }

        private string GetKind(object o)
        {
            string kind = (string)o.GetType().GetProperty("Kind").GetValue(o, new object[] { });
            return Enum.Parse(m_CSKindType, kind).ToString();
        }

        public string Kind(object node)
        {
            return GetKind(node);
        }
    }
}