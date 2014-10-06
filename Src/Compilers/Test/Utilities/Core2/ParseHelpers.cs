// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private const string VB_SYNTAX_TREE_TYPE = "Roslyn.Compilers.VisualBasic.VBSyntaxTree";
        private const string CS_OPTIONS_TYPE = "Roslyn.Compilers.CSharp.ParseOptions";
        private const string VB_OPTIONS_TYPE = "Roslyn.Compilers.VisualBasic.ParseOptions";
        private const string SYNTAX_TREE_PARSE_METHOD = "ParseCompilationUnit";
        private const string CS_LANG_VERSION_OPTION_TYPE = "Roslyn.Compilers.CSharp.LanguageVersion";
        private const string CODE_KIND_OPTION = "Roslyn.Compilers.SourceCodeKind";
#if false
        private Type m_CSParserType = null;
        private Type m_VBParserType = null;
#endif
        private Type m_CSSyntaxTreeType = null;
        private Type m_VBSyntaxTreeType = null;
        private object m_CSOptions = null;
        private object m_VBOptions = null;
        private readonly string m_CSFileName = "Default.cs";
        private readonly string m_VBFileName = "Default.vb";
        private object m_CodeKind = null;
        public SyntaxTree ParseCSTree(string code, string folder)
        {
            if (m_CSSyntaxTreeType == null)
            {
                var asm = Assembly.LoadFrom(Path.Combine(folder, CS_PARSER_DLL));
                m_CSSyntaxTreeType = asm.GetType(CS_SYNTAX_TREE_TYPE);
                var csLangVersionOption = Enum.Parse(asm.GetType(CS_LANG_VERSION_OPTION_TYPE), "CSharp4");
                m_CodeKind = Enum.Parse(asm.GetType(CODE_KIND_OPTION), "Regular");
                m_CSOptions = Activator.CreateInstance(asm.GetType(CS_OPTIONS_TYPE), csLangVersionOption, null, false, m_CodeKind);
            }

            SyntaxTree syntaxTree = (SyntaxTree)m_CSSyntaxTreeType.InvokeMember(SYNTAX_TREE_PARSE_METHOD, BindingFlags.InvokeMethod, null, null, new[]
        {
        code, m_CSFileName, m_CSOptions
        }

            );
            return syntaxTree;
        }

        public SyntaxTree ParseVBTree(string code, string folder)
        {
            if (m_VBSyntaxTreeType == null)
            {
                var asm = Assembly.LoadFrom(Path.Combine(folder, VB_PARSER_DLL));
                m_VBSyntaxTreeType = asm.GetType(VB_SYNTAX_TREE_TYPE);
                m_CodeKind = Enum.Parse(asm.GetType(CODE_KIND_OPTION), "Regular");
                m_VBOptions = Activator.CreateInstance(asm.GetType(VB_OPTIONS_TYPE), null, false, m_CodeKind);
            }

            SyntaxTree syntaxTree = (SyntaxTree)m_VBSyntaxTreeType.InvokeMember(SYNTAX_TREE_PARSE_METHOD, BindingFlags.InvokeMethod, null, null, new[]
        {
        code, m_VBFileName, m_VBOptions
        }

            );
            return syntaxTree;
        }
    }
}