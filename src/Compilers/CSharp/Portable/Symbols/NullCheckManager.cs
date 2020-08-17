// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Manages anonymous types created in owning compilation. All requests for 
    /// anonymous type symbols go via the instance of this class.
    /// </summary>
    internal sealed class NullCheckManager
    {
        internal NullCheckManager(CSharpCompilation compilation)
        {
            Debug.Assert(compilation != null);
            this.Compilation = compilation;
        }

        /// <summary> 
        /// Current compilation
        /// </summary>
        public CSharpCompilation Compilation { get; }

        /// <summary>
        /// Given anonymous type descriptor provided constructs an anonymous type symbol.
        /// </summary>
        public MethodSymbol GetNullCheckMethod(PrivateImplementationDetails privateImplType)
        {
            if (privateImplType.GetMethod(PrivateImplementationDetails.ParameterNullCheckFunctionName) is MethodSymbol nullCheckSymbol)
            {
                return nullCheckSymbol;
            }

            MethodSymbol method = new NullCheckSymbol(Compilation, privateImplType);
            if (!privateImplType.TryAddSynthesizedMethod(method))
            {
                method = (MethodSymbol)privateImplType.GetMethod(PrivateImplementationDetails.ParameterNullCheckFunctionName);
                Debug.Assert(method is object);
            }

            return method;
        }

        private sealed class NullCheckSymbol : SynthesizedGlobalMethodSymbol
        {
            internal NullCheckSymbol(CSharpCompilation compilation, PrivateImplementationDetails privateImplType)
            : base(compilation.SourceModule, privateImplType, compilation.GetSpecialType(SpecialType.System_Void), PrivateImplementationDetails.ParameterNullCheckFunctionName)
            {
                this.SetParameters(ImmutableArray.Create<ParameterSymbol>(SynthesizedParameterSymbol.Create(this,
                    TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_String)), 0, RefKind.None, "s")));
            }

            internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
            {
                SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);

                try
                {
                    ParameterSymbol parameter = Parameters[0];

                    BoundObjectCreationExpression ex = F.New(F.WellKnownMethod(WellKnownMember.System_ArgumentNullException__ctorString),
                        F.Convert(F.SpecialType(SpecialType.System_String), F.Parameter(parameter)));
                    BoundThrowStatement throwArgNullStatement = F.Throw(ex);

                    F.CloseMethod(F.Block(throwArgNullStatement, F.Return()));
                }
                catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
                {
                    diagnostics.Add(ex.Diagnostic);
                    F.CloseMethod(F.ThrowNull());
                }
            }
        }
    }
}
