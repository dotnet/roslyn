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

        private CodeGenerationPropertyInfo(
            bool isNew,
            bool isUnsafe)
        {
            this.isNew = isNew;
            this.isUnsafe = isUnsafe;
        }

        public static void Attach(
            IPropertySymbol property,
            bool isNew,
            bool isUnsafe)
        {
            var info = new CodeGenerationPropertyInfo(isNew, isUnsafe);
            propertyToInfoMap.Add(property, info);
        }

        private static CodeGenerationPropertyInfo GetInfo(IPropertySymbol property)
        {
            CodeGenerationPropertyInfo info;
            propertyToInfoMap.TryGetValue(property, out info);
            return info;
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