// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private Type _VBKindType;
        private readonly string _folder;

        public VBReflectionBasedKindProvider(string folder)
        {
            _folder = Path.GetFullPath(folder);
            GetKindTypes();
        }

        private void GetKindTypes()
        {
            if (_VBKindType == null)
            {
                var asm = Assembly.LoadFrom(Path.Combine(_folder, VB_DLL));
                _VBKindType = asm.GetType(VB_KIND_TYPE);
            }
        }

        private string GetKind(object o)
        {
            string kind = (string)o.GetType().GetProperty("Kind").GetValue(o, new object[] { });
            return Enum.Parse(_VBKindType, kind).ToString();
        }

        public string Kind(object node)
        {
            return GetKind(node);
        }
    }
}
