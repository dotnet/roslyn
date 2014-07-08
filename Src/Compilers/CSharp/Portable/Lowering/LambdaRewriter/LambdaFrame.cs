// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A class that represents the set of variables in a scope that have been
    /// captured by lambdas within that scope.
    /// </summary>
    internal sealed class LambdaFrame : SynthesizedContainer, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly MethodSymbol topLevelMethod;
        private readonly MethodSymbol constructor;

        internal LambdaFrame(MethodSymbol topLevelMethod, TypeCompilationState compilationState)
            : base(GeneratedNames.MakeLambdaDisplayClassName(compilationState.GenerateTempNumber()), topLevelMethod)
        {
            this.topLevelMethod = topLevelMethod;
            this.constructor = new LambdaFrameConstructor(this);
        }

        public override TypeKind TypeKind
        {
            get { return TypeKind.Class; }
        }

        internal override MethodSymbol Constructor
        {
            get { return this.constructor; }
        }

        public override Symbol ContainingSymbol
        {
            get { return topLevelMethod.ContainingSymbol; }
        }

        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
        {
            get
            {
                // the lambda method contains user code from the lambda:
                return true;
            }
        }

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return topLevelMethod; }
        }
    }
}
