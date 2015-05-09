// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class ParseHelpers
    {
        private const string CS_PARSER_DLL = "Microsoft.CodeAnalysis.CSharp.dll";
        private const string VB_PARSER_DLL = "Microsoft.CodeAnalysis.VisualBasic.dll";
        private const string CS_SYNTAX_TREE_TYPE = "Roslyn.Compilers.CSharp.CSharpSyntaxTree";
        private const string VB_SYNTAX_TREE_TYPE = "Roslyn.Compilers.VisualBasic.VisualBasicSyntaxTree";
        private const string CS_OPTIONS_TYPE = "Roslyn.Compilers.CSharp.ParseOptions";
        private const string VB_OPTIONS_TYPE = "Roslyn.Compilers.VisualBasic.ParseOptions";
        private const string SYNTAX_TREE_PARSE_METHOD = "ParseCompilationUnit";
        private const string CS_LANG_VERSION_OPTION_TYPE = "Roslyn.Compilers.CSharp.LanguageVersion";
        private const string CODE_KIND_OPTION = "Roslyn.Compilers.SourceCodeKind";
#if false
        private Type m_CSParserType = null;
        private Type m_VBParserType = null;
#endif
        private Type _CSSyntaxTreeType;
        private Type _visualBasicSyntaxTreeType;
        private object _CSOptions;
        private object _VBOptions;
        private readonly string _CSFileName = "Default.cs";
        private readonly string _VBFileName = "Default.vb";
        private object _codeKind;
        public SyntaxTree ParseCSTree(string code, string folder)
        {
            if (_CSSyntaxTreeType == null)
            {
                var asm = Assembly.LoadFrom(Path.Combine(folder, CS_PARSER_DLL));
                _CSSyntaxTreeType = asm.GetType(CS_SYNTAX_TREE_TYPE);
                var csLangVersionOption = Enum.Parse(asm.GetType(CS_LANG_VERSION_OPTION_TYPE), "CSharp4");
                _codeKind = Enum.Parse(asm.GetType(CODE_KIND_OPTION), "Regular");
                _CSOptions = Activator.CreateInstance(asm.GetType(CS_OPTIONS_TYPE), csLangVersionOption, null, false, _codeKind);
            }

            SyntaxTree syntaxTree = (SyntaxTree)_CSSyntaxTreeType.InvokeMember(SYNTAX_TREE_PARSE_METHOD, BindingFlags.InvokeMethod, null, null, new[]
        {
        code, _CSFileName, _CSOptions
        }

            );
            return syntaxTree;
        }

        public SyntaxTree ParseVBTree(string code, string folder)
        {
            if (_visualBasicSyntaxTreeType == null)
            {
                var asm = Assembly.LoadFrom(Path.Combine(folder, VB_PARSER_DLL));
                _visualBasicSyntaxTreeType = asm.GetType(VB_SYNTAX_TREE_TYPE);
                _codeKind = Enum.Parse(asm.GetType(CODE_KIND_OPTION), "Regular");
                _VBOptions = Activator.CreateInstance(asm.GetType(VB_OPTIONS_TYPE), null, false, _codeKind);
            }

            SyntaxTree syntaxTree = (SyntaxTree)_visualBasicSyntaxTreeType.InvokeMember(SYNTAX_TREE_PARSE_METHOD, BindingFlags.InvokeMethod, null, null, new[]
        {
        code, _VBFileName, _VBOptions
        }

            );
            return syntaxTree;
        }
    }
}
