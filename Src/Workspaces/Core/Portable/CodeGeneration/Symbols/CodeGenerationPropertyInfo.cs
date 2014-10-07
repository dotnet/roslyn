// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationPropertyInfo
    {
        private static readonly ConditionalWeakTable<IPropertySymbol, CodeGenerationPropertyInfo> propertyToInfoMap =
            new ConditionalWeakTable<IPropertySymbol, CodeGenerationPropertyInfo>();

        private readonly bool isNew;
        private readonly bool isUnsafe;
        private readonly SyntaxNode initializer;

        private CodeGenerationPropertyInfo(
            bool isNew,
            bool isUnsafe,
            SyntaxNode initializer)
        {
            this.isNew = isNew;
            this.isUnsafe = isUnsafe;
            this.initializer = initializer;
        }

        public static void Attach(
            IPropertySymbol property,
            bool isNew,
            bool isUnsafe,
            SyntaxNode initializer)
        {
            var info = new CodeGenerationPropertyInfo(isNew, isUnsafe, initializer);
            propertyToInfoMap.Add(property, info);
        }

        private static CodeGenerationPropertyInfo GetInfo(IPropertySymbol property)
        {
            CodeGenerationPropertyInfo info;
            propertyToInfoMap.TryGetValue(property, out info);
            return info;
        }

        public static SyntaxNode GetInitializer(CodeGenerationPropertyInfo info)
        {
            return info == null ? null : info.initializer;
        }

        public static SyntaxNode GetInitializer(IPropertySymbol property)
        {
            return GetInitializer(GetInfo(property));
        }

        public static bool GetIsNew(IPropertySymbol property)
        {
            return GetIsNew(GetInfo(property));
        }

        public static bool GetIsUnsafe(IPropertySymbol property)
        {
            return GetIsUnsafe(GetInfo(property));
        }

        private static bool GetIsNew(CodeGenerationPropertyInfo info)
        {
            return info != null && info.isNew;
        }

        private static bool GetIsUnsafe(CodeGenerationPropertyInfo info)
        {
            return info != null && info.isUnsafe;
        }
    }
}