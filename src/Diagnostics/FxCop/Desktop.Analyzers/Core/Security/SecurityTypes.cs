// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Desktop.Analyzers.Common
{
    public static class SecurityTypes
    {
        public static INamedTypeSymbol HandleProcessCorruptedStateExceptionsAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(
                "System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptionsAttribute");
        }

        public static INamedTypeSymbol SystemObject(Compilation compilation)
        {
            return compilation.GetSpecialType(SpecialType.System_Object);
        }

        public static INamedTypeSymbol SystemException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Exception");
        }

        public static INamedTypeSymbol SystemSystemException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.SystemException");
        }

    }
}
