// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    public enum CodeGenerationDestination
    {
        Unspecified,
        CompilationUnit,
        Namespace,
        ClassType,
        EnumType,
        InterfaceType,
        ModuleType,
        StructType,
    }
}