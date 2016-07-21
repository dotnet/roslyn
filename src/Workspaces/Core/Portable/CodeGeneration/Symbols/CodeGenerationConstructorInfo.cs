// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationConstructorInfo
    {
        private static readonly ConditionalWeakTable<IMethodSymbol, CodeGenerationConstructorInfo> s_constructorToInfoMap =
            new ConditionalWeakTable<IMethodSymbol, CodeGenerationConstructorInfo>();

        private readonly string _typeName;
        private readonly IList<SyntaxNode> _baseConstructorArguments;
        private readonly IList<SyntaxNode> _thisConstructorArguments;
        private readonly IList<SyntaxNode> _statements;

        private CodeGenerationConstructorInfo(
            string typeName,
            IList<SyntaxNode> statements,
            IList<SyntaxNode> baseConstructorArguments,
            IList<SyntaxNode> thisConstructorArguments)
        {
            _typeName = typeName;
            _statements = statements;
            _baseConstructorArguments = baseConstructorArguments;
            _thisConstructorArguments = thisConstructorArguments;
        }

        public static void Attach(
            IMethodSymbol constructor,
            string typeName,
            IList<SyntaxNode> statements,
            IList<SyntaxNode> baseConstructorArguments,
            IList<SyntaxNode> thisConstructorArguments)
        {
            var info = new CodeGenerationConstructorInfo(typeName, statements, baseConstructorArguments, thisConstructorArguments);
            s_constructorToInfoMap.Add(constructor, info);
        }

        private static CodeGenerationConstructorInfo GetInfo(IMethodSymbol method)
        {
            CodeGenerationConstructorInfo info;
            s_constructorToInfoMap.TryGetValue(method, out info);
            return info;
        }

        public static IList<SyntaxNode> GetThisConstructorArgumentsOpt(IMethodSymbol constructor)
        {
            return GetThisConstructorArgumentsOpt(GetInfo(constructor));
        }

        public static IList<SyntaxNode> GetBaseConstructorArgumentsOpt(IMethodSymbol constructor)
        {
            return GetBaseConstructorArgumentsOpt(GetInfo(constructor));
        }

        public static IList<SyntaxNode> GetStatements(IMethodSymbol constructor)
        {
            return GetStatements(GetInfo(constructor));
        }

        public static string GetTypeName(IMethodSymbol constructor)
        {
            return GetTypeName(GetInfo(constructor), constructor);
        }

        private static IList<SyntaxNode> GetThisConstructorArgumentsOpt(CodeGenerationConstructorInfo info)
        {
            return info == null ? null : info._thisConstructorArguments;
        }

        private static IList<SyntaxNode> GetBaseConstructorArgumentsOpt(CodeGenerationConstructorInfo info)
        {
            return info == null ? null : info._baseConstructorArguments;
        }

        private static IList<SyntaxNode> GetStatements(CodeGenerationConstructorInfo info)
        {
            return info == null ? null : info._statements;
        }

        private static string GetTypeName(CodeGenerationConstructorInfo info, IMethodSymbol constructor)
        {
            return info == null ? constructor.ContainingType.Name : info._typeName;
        }
    }
}
