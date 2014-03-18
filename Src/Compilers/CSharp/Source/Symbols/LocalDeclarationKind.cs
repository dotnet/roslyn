// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal enum LocalDeclarationKind : byte
    {
        Variable,
        Constant,
        Fixed,
        Using,
        Catch,
        For,
        ForEach,
        CompilerGenerated,
        CompilerGeneratedLambdaDisplayClassLocal, // Handled differently in the StateMachineRewriter.
    }
}