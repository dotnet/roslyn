// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class SpecialTypeExtensions
    {
        internal static QualifiedTypeSignature GetTypeSignature(this SpecialType type)
        {
            QualifiedTypeSignature signature;
            s_typeSignatures.TryGetValue(type, out signature);
            return signature;
        }

        private static readonly ImmutableDictionary<SpecialType, QualifiedTypeSignature> s_typeSignatures = GetTypeSignatures();

        private static ImmutableDictionary<SpecialType, QualifiedTypeSignature> GetTypeSignatures()
        {
            var systemNamespace = new QualifiedTypeSignature(null, "System");
            var builder = ImmutableDictionary.CreateBuilder<SpecialType, QualifiedTypeSignature>();
            builder.Add(SpecialType.System_Void, new QualifiedTypeSignature(systemNamespace, "Void"));
            builder.Add(SpecialType.System_Boolean, new QualifiedTypeSignature(systemNamespace, "Boolean"));
            builder.Add(SpecialType.System_Char, new QualifiedTypeSignature(systemNamespace, "Char"));
            builder.Add(SpecialType.System_SByte, new QualifiedTypeSignature(systemNamespace, "SByte"));
            builder.Add(SpecialType.System_Byte, new QualifiedTypeSignature(systemNamespace, "Byte"));
            builder.Add(SpecialType.System_Int16, new QualifiedTypeSignature(systemNamespace, "Int16"));
            builder.Add(SpecialType.System_UInt16, new QualifiedTypeSignature(systemNamespace, "UInt16"));
            builder.Add(SpecialType.System_Int32, new QualifiedTypeSignature(systemNamespace, "Int32"));
            builder.Add(SpecialType.System_UInt32, new QualifiedTypeSignature(systemNamespace, "UInt32"));
            builder.Add(SpecialType.System_Int64, new QualifiedTypeSignature(systemNamespace, "Int64"));
            builder.Add(SpecialType.System_UInt64, new QualifiedTypeSignature(systemNamespace, "UInt64"));
            builder.Add(SpecialType.System_Single, new QualifiedTypeSignature(systemNamespace, "Single"));
            builder.Add(SpecialType.System_Double, new QualifiedTypeSignature(systemNamespace, "Double"));
            builder.Add(SpecialType.System_String, new QualifiedTypeSignature(systemNamespace, "String"));
            builder.Add(SpecialType.System_Object, new QualifiedTypeSignature(systemNamespace, "Object"));
            builder.Add(SpecialType.System_Decimal, new QualifiedTypeSignature(systemNamespace, "Decimal"));
            builder.Add(SpecialType.System_IntPtr, new QualifiedTypeSignature(systemNamespace, "IntPtr"));
            builder.Add(SpecialType.System_UIntPtr, new QualifiedTypeSignature(systemNamespace, "UIntPtr"));
            builder.Add(SpecialType.System_TypedReference, new QualifiedTypeSignature(systemNamespace, "TypedReference"));
            builder.Add(SpecialType.System_Nullable_T, new QualifiedTypeSignature(systemNamespace, "Nullable"));
            builder.Add(SpecialType.System_DateTime, new QualifiedTypeSignature(systemNamespace, "DateTime"));
            return builder.ToImmutable();
        }
    }
}
