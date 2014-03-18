// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class VBReflectionBasedKindProvider : ISyntaxNodeKindProvider
    {
        private const string VB_DLL = "Microsoft.CodeAnalysis.VisualBasic.dll";
        private const string VB_KIND_TYPE = "Roslyn.Compilers.VisualBasic.SyntaxKind";
        private Type m_VBKindType = null;
        private readonly string m_Folder = null;

        public VBReflectionBasedKindProvider(string folder)
        {
            m_Folder = Path.GetFullPath(folder);
            GetKindTypes();
        }

        private void GetKindTypes()
        {
            if (m_VBKindType == null)
            {
                var asm = Assembly.LoadFrom(Path.Combine(m_Folder, VB_DLL));
                m_VBKindType = asm.GetType(VB_KIND_TYPE);
            }
        }

        private string GetKind(object o)
        {
            string kind = (string)o.GetType().GetProperty("Kind").GetValue(o, new object[] { });
            return Enum.Parse(m_VBKindType, kind).ToString();
        }

        public string Kind(object node)
        {
            return GetKind(node);
        }
    }
}