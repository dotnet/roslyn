using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class AsyncRewriter
    {
        /// <summary>
        /// The class that represents a translated async method.
        /// </summary>
        sealed internal class AsyncStruct : SynthesizedContainer
        {
            private readonly MethodSymbol constructor;
            private readonly ReadOnlyArray<NamedTypeSymbol> interfaces;

            public AsyncStruct(MethodSymbol method, TypeCompilationState compilationState)
                : base(method, GeneratedNames.MakeIteratorOrAsyncDisplayClassName(method.Name, compilationState.GenerateTempNumber()), TypeKind.Struct)
            {
                this.interfaces = ReadOnlyArray<NamedTypeSymbol>.CreateFrom(
                    compilationState.EmitModule.Compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine));

                this.constructor = new SynthesizedInstanceConstructor(this);
            }

            internal override MethodSymbol Constructor
            {
                get { return constructor; }
            }

            public override ReadOnlyArray<NamedTypeSymbol> Interfaces
            {
                get { return interfaces; }
            }
        }
    }
}