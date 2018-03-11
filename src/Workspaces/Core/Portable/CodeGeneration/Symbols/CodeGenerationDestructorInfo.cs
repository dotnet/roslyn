// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationDestructorInfo
    {
        private static readonly ConditionalWeakTable<IMethodSymbol, CodeGenerationDestructorInfo> s_destructorToInfoMap =
            new ConditionalWeakTable<IMethodSymbol, CodeGenerationDestructorInfo>();

        private readonly string _typeName;
        private readonly ImmutableArray<SyntaxNode> _statements;

        private CodeGenerationDestructorInfo(
            string typeName,
            ImmutableArray<SyntaxNode> statements)
        {
            _typeName = typeName;
            _statements = statements;
        }

        public static void Attach(
            IMethodSymbol destructor,
            string typeName,
            ImmutableArray<SyntaxNode> statements)
        {
            var info = new CodeGenerationDestructorInfo(typeName, statements);
            s_destructorToInfoMap.Add(destructor, info);
        }

        private static CodeGenerationDestructorInfo GetInfo(IMethodSymbol method)
        {
            s_destructorToInfoMap.TryGetValue(method, out var info);
            return info;
        }

        public static ImmutableArray<SyntaxNode> GetStatements(IMethodSymbol destructor)
            => GetStatements(GetInfo(destructor));

        public static string GetTypeName(IMethodSymbol destructor)
            => GetTypeName(GetInfo(destructor), destructor);

        private static ImmutableArray<SyntaxNode> GetStatements(CodeGenerationDestructorInfo info)
            => info?._statements ?? default;

        private static string GetTypeName(CodeGenerationDestructorInfo info, IMethodSymbol constructor)
            => info == null ? constructor.ContainingType.Name : info._typeName;
    }
}
