// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationFieldInfo
    {
        private static readonly ConditionalWeakTable<IFieldSymbol, CodeGenerationFieldInfo> s_fieldToInfoMap =
            new ConditionalWeakTable<IFieldSymbol, CodeGenerationFieldInfo>();

        private readonly bool _isUnsafe;
        private readonly bool _isWithEvents;
        private readonly SyntaxNode _initializer;

        private CodeGenerationFieldInfo(
            bool isUnsafe,
            bool isWithEvents,
            SyntaxNode initializer)
        {
            _isUnsafe = isUnsafe;
            _isWithEvents = isWithEvents;
            _initializer = initializer;
        }

        public static void Attach(
            IFieldSymbol field,
            bool isUnsafe,
            bool isWithEvents,
            SyntaxNode initializer)
        {
            var info = new CodeGenerationFieldInfo(isUnsafe, isWithEvents, initializer);
            s_fieldToInfoMap.Add(field, info);
        }

        private static CodeGenerationFieldInfo GetInfo(IFieldSymbol field)
        {
            s_fieldToInfoMap.TryGetValue(field, out var info);
            return info;
        }

        private static bool GetIsUnsafe(CodeGenerationFieldInfo info)
        {
            return info is { _isUnsafe: true };
        }

        public static bool GetIsUnsafe(IFieldSymbol field)
        {
            return GetIsUnsafe(GetInfo(field));
        }

        private static bool GetIsWithEvents(CodeGenerationFieldInfo info)
        {
            return info != null && info._isWithEvents;
        }

        public static bool GetIsWithEvents(IFieldSymbol field)
        {
            return GetIsWithEvents(GetInfo(field));
        }

        private static SyntaxNode GetInitializer(CodeGenerationFieldInfo info)
        {
            return info == null ? null : info._initializer;
        }

        public static SyntaxNode GetInitializer(IFieldSymbol field)
        {
            return GetInitializer(GetInfo(field));
        }
    }
}
