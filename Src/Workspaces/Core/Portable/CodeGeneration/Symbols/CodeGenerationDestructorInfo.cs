// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationDestructorInfo
    {
        private static readonly ConditionalWeakTable<IMethodSymbol, CodeGenerationDestructorInfo> destructorToInfoMap =
            new ConditionalWeakTable<IMethodSymbol, CodeGenerationDestructorInfo>();

        private readonly string typeName;
        private readonly IList<SyntaxNode> statements;

        private CodeGenerationDestructorInfo(
            string typeName,
            IList<SyntaxNode> statements)
        {
            this.typeName = typeName;
            this.statements = statements;
        }

        public static void Attach(
            IMethodSymbol destructor,
            string typeName,
            IList<SyntaxNode> statements)
        {
            var info = new CodeGenerationDestructorInfo(typeName, statements);
            destructorToInfoMap.Add(destructor, info);
        }

        private static CodeGenerationDestructorInfo GetInfo(IMethodSymbol method)
        {
            CodeGenerationDestructorInfo info;
            destructorToInfoMap.TryGetValue(method, out info);
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
            return info == null ? null : info.statements;
        }

        private static string GetTypeName(CodeGenerationDestructorInfo info, IMethodSymbol constructor)
        {
            return info == null ? constructor.ContainingType.Name : info.typeName;
        }
    }
}