// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static partial class EnumConversions
    {
        internal static TypeKind ToTypeKind(this DeclarationKind kind)
        {
            switch (kind)
            {
                case DeclarationKind.Class:
                case DeclarationKind.Script:
                case DeclarationKind.ImplicitClass:
                case DeclarationKind.SimpleProgram:
                case DeclarationKind.Record:
                    return TypeKind.Class;

                case DeclarationKind.Submission:
                    return TypeKind.Submission;

                case DeclarationKind.Delegate:
                    return TypeKind.Delegate;

                case DeclarationKind.Enum:
                    return TypeKind.Enum;

                case DeclarationKind.Interface:
                    return TypeKind.Interface;

                case DeclarationKind.Struct:
                    return TypeKind.Struct;

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }
    }
}
