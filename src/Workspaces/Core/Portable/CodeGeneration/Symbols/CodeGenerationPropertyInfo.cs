// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationPropertyInfo
    {
        private static readonly ConditionalWeakTable<IPropertySymbol, CodeGenerationPropertyInfo> s_propertyToInfoMap =
            new ConditionalWeakTable<IPropertySymbol, CodeGenerationPropertyInfo>();

        private readonly bool _isNew;
        private readonly bool _isUnsafe;
        private readonly SyntaxNode _initializer;

        private CodeGenerationPropertyInfo(
            bool isNew,
            bool isUnsafe,
            SyntaxNode initializer)
        {
            _isNew = isNew;
            _isUnsafe = isUnsafe;
            _initializer = initializer;
        }

        public static void Attach(
            IPropertySymbol property,
            bool isNew,
            bool isUnsafe,
            SyntaxNode initializer)
        {
            var info = new CodeGenerationPropertyInfo(isNew, isUnsafe, initializer);
            s_propertyToInfoMap.Add(property, info);
        }

        private static CodeGenerationPropertyInfo GetInfo(IPropertySymbol property)
        {
            s_propertyToInfoMap.TryGetValue(property, out var info);
            return info;
        }

        public static SyntaxNode GetInitializer(CodeGenerationPropertyInfo info)
        {
            return info == null ? null : info._initializer;
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
            return info is { _isNew: true };
        }

        private static bool GetIsUnsafe(CodeGenerationPropertyInfo info)
        {
            return info != null && info._isUnsafe;
        }
    }
}
