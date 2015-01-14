// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal enum CodeGenerationDestination
    {
        Unspecified = 0,
        CompilationUnit = 1,
        Namespace = 2,
        ClassType = 3,
        EnumType = 4,
        InterfaceType = 5,
        ModuleType = 6,
        StructType = 7,
    }
}
