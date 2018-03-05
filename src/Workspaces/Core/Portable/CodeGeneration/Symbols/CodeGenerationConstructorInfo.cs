// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationConstructorInfo
    {
        private static readonly ConditionalWeakTable<IMethodSymbol, CodeGenerationConstructorInfo> s_constructorToInfoMap =
            new ConditionalWeakTable<IMethodSymbol, CodeGenerationConstructorInfo>();

        private readonly string _typeName;
        private readonly ImmutableArray<SyntaxNode> _baseConstructorArguments;
        private readonly ImmutableArray<SyntaxNode> _thisConstructorArguments;
        private readonly ImmutableArray<SyntaxNode> _statements;

        private CodeGenerationConstructorInfo(
            string typeName,
            ImmutableArray<SyntaxNode> statements,
            ImmutableArray<SyntaxNode> baseConstructorArguments,
            ImmutableArray<SyntaxNode> thisConstructorArguments)
        {
            _typeName = typeName;
            _statements = statements;
            _baseConstructorArguments = baseConstructorArguments;
            _thisConstructorArguments = thisConstructorArguments;
        }

        public static void Attach(
            IMethodSymbol constructor,
            string typeName,
            ImmutableArray<SyntaxNode> statements,
            ImmutableArray<SyntaxNode> baseConstructorArguments,
            ImmutableArray<SyntaxNode> thisConstructorArguments)
        {
            var info = new CodeGenerationConstructorInfo(typeName, statements, baseConstructorArguments, thisConstructorArguments);
            s_constructorToInfoMap.Add(constructor, info);
        }

        private static CodeGenerationConstructorInfo GetInfo(IMethodSymbol method)
        {
            s_constructorToInfoMap.TryGetValue(method, out var info);
            return info;
        }

        public static ImmutableArray<SyntaxNode> GetThisConstructorArgumentsOpt(IMethodSymbol constructor)
            => GetThisConstructorArgumentsOpt(GetInfo(constructor));

        public static ImmutableArray<SyntaxNode> GetBaseConstructorArgumentsOpt(IMethodSymbol constructor)
            => GetBaseConstructorArgumentsOpt(GetInfo(constructor));

        public static ImmutableArray<SyntaxNode> GetStatements(IMethodSymbol constructor)
            => GetStatements(GetInfo(constructor));

        public static string GetTypeName(IMethodSymbol constructor)
            => GetTypeName(GetInfo(constructor), constructor);

        private static ImmutableArray<SyntaxNode> GetThisConstructorArgumentsOpt(CodeGenerationConstructorInfo info)
            => info?._thisConstructorArguments ?? default;

        private static ImmutableArray<SyntaxNode> GetBaseConstructorArgumentsOpt(CodeGenerationConstructorInfo info)
            => info?._baseConstructorArguments ?? default;

        private static ImmutableArray<SyntaxNode> GetStatements(CodeGenerationConstructorInfo info)
            => info?._statements ?? default;

        private static string GetTypeName(CodeGenerationConstructorInfo info, IMethodSymbol constructor)
            => info == null ? constructor.ContainingType.Name : info._typeName;
    }
}
