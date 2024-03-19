// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeGeneration;

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
