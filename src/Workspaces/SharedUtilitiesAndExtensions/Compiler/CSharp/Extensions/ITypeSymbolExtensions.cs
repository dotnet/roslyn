// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class ITypeSymbolExtensions
    {
        public static bool IsIntrinsicType(this ITypeSymbol typeSymbol)
        {
            switch (typeSymbol.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Byte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                // NOTE: VB treats System.DateTime as an intrinsic, while C# does not, see "predeftype.h"
                //case SpecialType.System_DateTime:
                case SpecialType.System_Decimal:
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryGetPrimaryConstructor(this INamedTypeSymbol typeSymbol, [NotNullWhen(true)] out IMethodSymbol? primaryConstructor)
        {
            if (typeSymbol.TypeKind is TypeKind.Class or TypeKind.Struct)
            {
                Debug.Assert(typeSymbol.GetParameters().IsDefaultOrEmpty, "If GetParameters extension handles record, we can remove the handling here.");

                // A bit hacky to determine the parameters of primary constructor associated with a given record.
                // Simplifying is tracked by: https://github.com/dotnet/roslyn/issues/53092.
                // Note: When the issue is handled, we can remove the logic here and handle things in GetParameters extension. BUT
                // if GetParameters extension method gets updated to handle records, we need to test EVERY usage
                // of the extension method and make sure the change is applicable to all these usages.

                primaryConstructor = typeSymbol.InstanceConstructors.FirstOrDefault(
                    c => c.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is RecordDeclarationSyntax or ClassDeclarationSyntax or StructDeclarationSyntax);
                return primaryConstructor is not null;
            }

            primaryConstructor = null;
            return false;
        }
    }
}
