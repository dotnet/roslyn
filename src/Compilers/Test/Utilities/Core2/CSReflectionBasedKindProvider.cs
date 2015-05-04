// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private Type _CSKindType;
        private readonly string _folder;

        public CSReflectionBasedKindProvider(string folder)
        {
            _folder = Path.GetFullPath(folder);
            GetKindTypes();
        }

        private void GetKindTypes()
        {
            if (_CSKindType == null)
            {
                var asm = Assembly.LoadFrom(Path.Combine(_folder, CS_DLL));
                _CSKindType = asm.GetType(CS_KIND_TYPE);
            }
        }

        private string GetKind(object o)
        {
            string kind = (string)o.GetType().GetProperty("Kind").GetValue(o, new object[] { });
            return Enum.Parse(_CSKindType, kind).ToString();
        }

        public string Kind(object node)
        {
            return GetKind(node);
        }
    }
}
