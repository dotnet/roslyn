// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Throws a System.ArgumentNullException if 'argument' is null.
    /// </summary>
    internal sealed class SynthesizedThrowIfNullMethod : SynthesizedGlobalMethodSymbol
    {
        internal MethodSymbol ThrowMethod { get; }
        internal SynthesizedThrowIfNullMethod(SourceModuleSymbol containingModule, PrivateImplementationDetails privateImplType, MethodSymbol throwMethod, TypeSymbol returnType, TypeSymbol argumentParamType, TypeSymbol paramNameParamType)
            : base(containingModule, privateImplType, returnType, PrivateImplementationDetails.SynthesizedThrowIfNullFunctionName)
        {
            ThrowMethod = throwMethod;

            this.SetParameters(ImmutableArray.Create<ParameterSymbol>(
                SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(argumentParamType), ordinal: 0, RefKind.None, "argument"),
                SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(paramNameParamType), ordinal: 1, RefKind.None, "paramName")));
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentFunction = this;

            try
            {
                ParameterSymbol argument = this.Parameters[0];
                ParameterSymbol paramName = this.Parameters[1];

                //if (argument is null)
                //{
                //    Throw(paramName);
                //}

                var body = F.Block(
                        ImmutableArray<LocalSymbol>.Empty,
                        F.If(
                            F.Binary(BinaryOperatorKind.ObjectEqual, F.SpecialType(SpecialType.System_Boolean),
                                F.Parameter(argument),
                                F.Null(argument.Type)),
                            F.ExpressionStatement(F.Call(receiver: null, ThrowMethod, F.Parameter(paramName)))),
                        F.Return());

                // NOTE: we created this block in its most-lowered form, so analysis is unnecessary
                F.CloseMethod(body);
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }
    }
}
