// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
