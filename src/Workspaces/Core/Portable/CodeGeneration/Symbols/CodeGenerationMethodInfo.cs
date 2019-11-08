// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;

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
        private readonly ImmutableArray<SyntaxNode> _statements;
        private readonly ImmutableArray<SyntaxNode> _handlesExpressions;

        private CodeGenerationMethodInfo(
            bool isNew,
            bool isUnsafe,
            bool isPartial,
            bool isAsync,
            ImmutableArray<SyntaxNode> statements,
            ImmutableArray<SyntaxNode> handlesExpressions)
        {
            _isNew = isNew;
            _isUnsafe = isUnsafe;
            _isPartial = isPartial;
            _isAsync = isAsync;
            _statements = statements.NullToEmpty();
            _handlesExpressions = handlesExpressions.NullToEmpty();
        }

        public static void Attach(
            IMethodSymbol method,
            bool isNew,
            bool isUnsafe,
            bool isPartial,
            bool isAsync,
            ImmutableArray<SyntaxNode> statements,
            ImmutableArray<SyntaxNode> handlesExpressions)
        {
            var info = new CodeGenerationMethodInfo(isNew, isUnsafe, isPartial, isAsync, statements, handlesExpressions);
            s_methodToInfoMap.Add(method, info);
        }

        private static CodeGenerationMethodInfo GetInfo(IMethodSymbol method)
        {
            s_methodToInfoMap.TryGetValue(method, out var info);
            return info;
        }

        public static ImmutableArray<SyntaxNode> GetStatements(IMethodSymbol method)
            => GetStatements(GetInfo(method));

        public static ImmutableArray<SyntaxNode> GetHandlesExpressions(IMethodSymbol method)
            => GetHandlesExpressions(GetInfo(method));

        public static bool GetIsNew(IMethodSymbol method)
            => GetIsNew(GetInfo(method));

        public static bool GetIsUnsafe(IMethodSymbol method)
            => GetIsUnsafe(GetInfo(method));

        public static bool GetIsPartial(IMethodSymbol method)
            => GetIsPartial(GetInfo(method));

        public static bool GetIsAsync(IMethodSymbol method)
            => GetIsAsync(GetInfo(method));

        private static ImmutableArray<SyntaxNode> GetStatements(CodeGenerationMethodInfo info)
            => info?._statements ?? ImmutableArray<SyntaxNode>.Empty;

        private static ImmutableArray<SyntaxNode> GetHandlesExpressions(CodeGenerationMethodInfo info)
            => info?._handlesExpressions ?? ImmutableArray<SyntaxNode>.Empty;

        private static bool GetIsNew(CodeGenerationMethodInfo info)
            => info is { _isNew: true };

        private static bool GetIsUnsafe(CodeGenerationMethodInfo info)
            => info != null && info._isUnsafe;

        private static bool GetIsPartial(CodeGenerationMethodInfo info)
            => info != null && info._isPartial;

        private static bool GetIsAsync(CodeGenerationMethodInfo info)
            => info != null && info._isAsync;
    }
}
