// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationDestructorInfo
    {
        private static readonly ConditionalWeakTable<IMethodSymbol, CodeGenerationDestructorInfo> s_destructorToInfoMap =
            new ConditionalWeakTable<IMethodSymbol, CodeGenerationDestructorInfo>();

        private readonly string _typeName;
        private readonly IList<SyntaxNode> _statements;

        private CodeGenerationDestructorInfo(
            string typeName,
            IList<SyntaxNode> statements)
        {
            _typeName = typeName;
            _statements = statements;
        }

        public static void Attach(
            IMethodSymbol destructor,
            string typeName,
            IList<SyntaxNode> statements)
        {
            var info = new CodeGenerationDestructorInfo(typeName, statements);
            s_destructorToInfoMap.Add(destructor, info);
        }

        private static CodeGenerationDestructorInfo GetInfo(IMethodSymbol method)
        {
            CodeGenerationDestructorInfo info;
            s_destructorToInfoMap.TryGetValue(method, out info);
            return info;
        }

        public static IList<SyntaxNode> GetStatements(IMethodSymbol destructor)
        {
            return GetStatements(GetInfo(destructor));
        }

        public static string GetTypeName(IMethodSymbol destructor)
        {
            return GetTypeName(GetInfo(destructor), destructor);
        }

        private static IList<SyntaxNode> GetStatements(CodeGenerationDestructorInfo info)
        {
            return info == null ? null : info._statements;
        }

        private static string GetTypeName(CodeGenerationDestructorInfo info, IMethodSymbol constructor)
        {
            return info == null ? constructor.ContainingType.Name : info._typeName;
        }
    }
}
