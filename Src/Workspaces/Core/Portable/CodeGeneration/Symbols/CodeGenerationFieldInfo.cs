// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationFieldInfo
    {
        private static readonly ConditionalWeakTable<IFieldSymbol, CodeGenerationFieldInfo> fieldToInfoMap =
            new ConditionalWeakTable<IFieldSymbol, CodeGenerationFieldInfo>();

        private readonly bool isUnsafe;
        private readonly bool isWithEvents;
        private readonly SyntaxNode initializer;

        private CodeGenerationFieldInfo(
            bool isUnsafe,
            bool isWithEvents,
            SyntaxNode initializer)
        {
            this.isUnsafe = isUnsafe;
            this.isWithEvents = isWithEvents;
            this.initializer = initializer;
        }

        public static void Attach(
            IFieldSymbol field,
            bool isUnsafe,
            bool isWithEvents,
            SyntaxNode initializer)
        {
            var info = new CodeGenerationFieldInfo(isUnsafe, isWithEvents, initializer);
            fieldToInfoMap.Add(field, info);
        }

        private static CodeGenerationFieldInfo GetInfo(IFieldSymbol field)
        {
            CodeGenerationFieldInfo info;
            fieldToInfoMap.TryGetValue(field, out info);
            return info;
        }

        private static bool GetIsUnsafe(CodeGenerationFieldInfo info)
        {
            return info != null && info.isUnsafe;
        }

        public static bool GetIsUnsafe(IFieldSymbol field)
        {
            return GetIsUnsafe(GetInfo(field));
        }

        private static bool GetIsWithEvents(CodeGenerationFieldInfo info)
        {
            return info != null && info.isWithEvents;
        }

        public static bool GetIsWithEvents(IFieldSymbol field)
        {
            return GetIsWithEvents(GetInfo(field));
        }

        private static SyntaxNode GetInitializer(CodeGenerationFieldInfo info)
        {
            return info == null ? null : info.initializer;
        }

        public static SyntaxNode GetInitializer(IFieldSymbol field)
        {
            return GetInitializer(GetInfo(field));
        }
    }
}
