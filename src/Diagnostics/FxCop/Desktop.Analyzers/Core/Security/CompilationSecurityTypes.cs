// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Desktop.Analyzers.Common
{
    public class CompilationSecurityTypes
    {
        public INamedTypeSymbol HandleProcessCorruptedStateExceptionsAttribute { get; private set; }
        public INamedTypeSymbol SystemObject { get; private set; }
        public INamedTypeSymbol SystemException { get; private set; }
        public INamedTypeSymbol SystemSystemException { get; private set; }

        public CompilationSecurityTypes(Compilation compilation)
        {
            this.HandleProcessCorruptedStateExceptionsAttribute = 
                SecurityTypes.HandleProcessCorruptedStateExceptionsAttribute(compilation);
            this.SystemObject = SecurityTypes.SystemObject(compilation);
            this.SystemException = SecurityTypes.SystemException(compilation);
            this.SystemSystemException = SecurityTypes.SystemSystemException(compilation);
        }
    }
}
