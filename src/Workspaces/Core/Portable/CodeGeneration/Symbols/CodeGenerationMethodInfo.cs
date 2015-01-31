// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationMethodInfo
    {
        private static readonly ConditionalWeakTable<IMethodSymbol, CodeGenerationMethodInfo> s_methodToInfoMap =
            new ConditionalWeakTable<IMethodSymbol, CodeGenerationMethodInfo>();

        private readonly bool _isNew;
        private readonly bool _isUnsafe;
        private readonly bool _isPartial;
        private readonly bool _isAsync;
        private readonly IList<SyntaxNode> _statements;
        private readonly IList<SyntaxNode> _handlesExpressions;

        private CodeGenerationMethodInfo(
            bool isNew,
            bool isUnsafe,
            bool isPartial,
            bool isAsync,
            IList<SyntaxNode> statements,
            IList<SyntaxNode> handlesExpressions)
        {
            _isNew = isNew;
            _isUnsafe = isUnsafe;
            _isPartial = isPartial;
            _isAsync = isAsync;
            _statements = statements ?? SpecializedCollections.EmptyList<SyntaxNode>();
            _handlesExpressions = handlesExpressions ?? SpecializedCollections.EmptyList<SyntaxNode>();
        }

        public static void Attach(
            IMethodSymbol method,
            bool isNew,
            bool isUnsafe,
            bool isPartial,
            bool isAsync,
            IList<SyntaxNode> statements,
            IList<SyntaxNode> handlesExpressions)
        {
            var info = new CodeGenerationMethodInfo(isNew, isUnsafe, isPartial, isAsync, statements, handlesExpressions);
            s_methodToInfoMap.Add(method, info);
        }

        private static CodeGenerationMethodInfo GetInfo(IMethodSymbol method)
        {
            CodeGenerationMethodInfo info;
            s_methodToInfoMap.TryGetValue(method, out info);
            return info;
        }

        public static IList<SyntaxNode> GetStatements(IMethodSymbol method)
        {
            return GetStatements(GetInfo(method));
        }

        public static IList<SyntaxNode> GetHandlesExpressions(IMethodSymbol method)
        {
            return GetHandlesExpressions(GetInfo(method));
        }

        public static bool GetIsNew(IMethodSymbol method)
        {
            return GetIsNew(GetInfo(method));
        }

        public static bool GetIsUnsafe(IMethodSymbol method)
        {
            return GetIsUnsafe(GetInfo(method));
        }

        public static bool GetIsPartial(IMethodSymbol method)
        {
            return GetIsPartial(GetInfo(method));
        }

        public static bool GetIsAsync(IMethodSymbol method)
        {
            return GetIsAsync(GetInfo(method));
        }

        private static IList<SyntaxNode> GetStatements(CodeGenerationMethodInfo info)
        {
            return info == null
                ? SpecializedCollections.EmptyList<SyntaxNode>()
                : info._statements;
        }

        private static IList<SyntaxNode> GetHandlesExpressions(CodeGenerationMethodInfo info)
        {
            return info == null
                ? SpecializedCollections.EmptyList<SyntaxNode>()
                : info._handlesExpressions;
        }

        private static bool GetIsNew(CodeGenerationMethodInfo info)
        {
            return info != null && info._isNew;
        }

        private static bool GetIsUnsafe(CodeGenerationMethodInfo info)
        {
            return info != null && info._isUnsafe;
        }

        private static bool GetIsPartial(CodeGenerationMethodInfo info)
        {
            return info != null && info._isPartial;
        }

        private static bool GetIsAsync(CodeGenerationMethodInfo info)
        {
            return info != null && info._isAsync;
        }
    }
}
