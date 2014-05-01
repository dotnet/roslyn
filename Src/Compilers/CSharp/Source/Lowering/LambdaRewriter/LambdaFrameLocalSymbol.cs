// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A local variable used to store a reference to the frame objects in which captured
    /// local variables have become fields.
    /// </summary>
    internal sealed class LambdaFrameLocalSymbol : SynthesizedLocal
    {
        internal LambdaFrameLocalSymbol(MethodSymbol containingMethod, TypeSymbol type, TypeCompilationState compilationState)
            : base(containingMethod, type, GeneratedNames.MakeLambdaDisplayClassLocalName(compilationState.GenerateTempNumber()), declarationKind: LocalDeclarationKind.CompilerGeneratedLambdaDisplayClassLocal)
        {
        }
    }
}
