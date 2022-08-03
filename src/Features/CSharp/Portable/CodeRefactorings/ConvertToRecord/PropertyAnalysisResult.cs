// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertToRecord
{
    internal class PropertyAnalysisResult
    {

        private static bool ShouldConvertProperty(MemberDeclarationSyntax member, INamedTypeSymbol containingType)
        {
            if (member is not PropertyDeclarationSyntax
                {
                    Initializer: null,
                    ExpressionBody: null,
                } property)
            {
                return false;
            }

            var propAccessibility = CSharpAccessibilityFacts.Instance.GetAccessibility(member);

            // more restrictive than internal (protected, private, private protected, or unspecified (private by default))
            if (propAccessibility < Accessibility.Internal)
            {
                return false;
            }

            // no accessors declared
            if (property.AccessorList == null)
            {
                return false;
            }

            // When we convert to primary constructor parameters, the auto-generated properties have default behavior
            // in this iteration, we will only move if it would not change the default behavior.
            // - No accessors can have any explicit implementation or modifiers
            //   - This is because it would indicate complex functionality or explicit hiding which is not default
            // - class records and readonly struct records must have:
            //   - public get accessor (with no explicit impl)
            //   - optionally a public init accessor (again w/ no impl)
            //     - note: if this is not provided the user can still initialize the property in the constructor,
            //             so it's like init but without the user ability to initialize outside the constructor
            // - for non-readonly structs, we must have:
            //   - public get accessor (with no explicit impl)
            //   - public set accessor (with no explicit impl)
            var accessors = property.AccessorList.Accessors;
            if (accessors.Any(a => a.Body != null || a.ExpressionBody != null) ||
                !accessors.Any(a => a.Kind() == SyntaxKind.GetAccessorDeclaration && a.Modifiers.IsEmpty()))
            {
                return false;
            }

            if (containingType.TypeKind == TypeKind.Struct && !containingType.IsReadOnly)
            {
                if (!accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)))
                {
                    return false;
                }
            }
            else
            {
                // either we are a class or readonly struct, we only want to convert init setters, not normal ones
                if (accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
