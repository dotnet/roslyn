// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationMethodInfo
    {
        private static readonly ConditionalWeakTable<IMethodSymbol, CodeGenerationMethodInfo> methodToInfoMap =
            new ConditionalWeakTable<IMethodSymbol, CodeGenerationMethodInfo>();

        private readonly bool isNew;
        private readonly bool isUnsafe;
        private readonly bool isPartial;
        private readonly bool isAsync;
        private readonly IList<SyntaxNode> statements;
        private readonly IList<SyntaxNode> handlesExpressions;

        private CodeGenerationMethodInfo(
            bool isNew,
            bool isUnsafe,
            bool isPartial,
            bool isAsync,
            IList<SyntaxNode> statements,
            IList<SyntaxNode> handlesExpressions)
        {
            this.isNew = isNew;
            this.isUnsafe = isUnsafe;
            this.isPartial = isPartial;
            this.isAsync = isAsync;
            this.statements = statements ?? SpecializedCollections.EmptyList<SyntaxNode>();
            this.handlesExpressions = handlesExpressions ?? SpecializedCollections.EmptyList<SyntaxNode>();
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
            methodToInfoMap.Add(method, info);
        }

        private static CodeGenerationMethodInfo GetInfo(IMethodSymbol method)
        {
            CodeGenerationMethodInfo info;
            methodToInfoMap.TryGetValue(method, out info);
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
                : info.statements;
        }

        private static IList<SyntaxNode> GetHandlesExpressions(CodeGenerationMethodInfo info)
        {
            return info == null
                ? SpecializedCollections.EmptyList<SyntaxNode>()
                : info.handlesExpressions;
        }

        private static bool GetIsNew(CodeGenerationMethodInfo info)
        {
            return info != null && info.isNew;
        }

        private static bool GetIsUnsafe(CodeGenerationMethodInfo info)
        {
            return info != null && info.isUnsafe;
        }

        private static bool GetIsPartial(CodeGenerationMethodInfo info)
        {
            return info != null && info.isPartial;
        }

        private static bool GetIsAsync(CodeGenerationMethodInfo info)
        {
            return info != null && info.isAsync;
        }
    }
}