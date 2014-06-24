// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A method that results from the translation of a single lambda expression.
    /// </summary>
    internal sealed class SynthesizedLambdaMethod : SynthesizedMethodBaseSymbol
    {
        internal SynthesizedLambdaMethod(NamedTypeSymbol containingType, MethodSymbol topLevelMethod, BoundLambda node, bool isStatic, TypeCompilationState compilationState)
            : base(containingType,
                    node.Symbol,
                    null,
                    node.SyntaxTree.GetReference(node.Body.Syntax),
                    node.Syntax.GetLocation(),
                    GeneratedNames.MakeLambdaMethodName(topLevelMethod.Name, compilationState.GenerateTempNumber()),
                    (containingType is LambdaFrame ? DeclarationModifiers.Internal : DeclarationModifiers.Private)
                        | (isStatic ? DeclarationModifiers.Static : 0)
                        | (node.Symbol.IsAsync ? DeclarationModifiers.Async : 0))
        {
            TypeMap typeMap;
            ImmutableArray<TypeParameterSymbol> typeParameters;
            LambdaFrame lambdaFrame;

            if (!topLevelMethod.IsGenericMethod)
            {
                typeMap = TypeMap.Empty;
                typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
            }
            else if ((object)(lambdaFrame = this.ContainingType as LambdaFrame) != null)
            {
                typeMap = lambdaFrame.TypeMap;
                typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
            }
            else
            {
                typeMap = TypeMap.Empty.WithAlphaRename(topLevelMethod, this, out typeParameters);
            }

            AssignTypeMapAndTypeParameters(typeMap, typeParameters);
        }

        internal override int ParameterCount
        {
            // TODO: keep this in sync with BaseMethodParameters { get; }
            get
            {
                var cnt = this.BaseMethod.ParameterCount;
                if (this.IsStatic)
                {
                    // account for dummy "this", see BaseMethodParameters for explanation.
                    cnt++;
                }

                return cnt;
            }
        }

        protected override ImmutableArray<ParameterSymbol> BaseMethodParameters
        {
            get
            {
                // The lambda symbol might have declared no parameters in the case
                //
                // D d = delegate {};
                //
                // but there still might be parameters that need to be generated for the
                // synthetic method. If there are no lambda parameters, try the delegate 
                // parameters instead. 
                // 
                // UNDONE: In the native compiler in this scenario we make up new names for
                // UNDONE: synthetic parameters; in this implementation we use the parameter
                // UNDONE: names from the delegate. Does it really matter?
                var parameters = this.BaseMethod.Parameters;
                if (this.IsStatic)
                {
                    // add dummy "this"
                    // delegate Invoke works better when methods have "this"
                    // since the argument list does not need to be left-shifted
                    // only "this" argument needs to be substututed from the delegate instance to
                    // the enclosed receiver, which will be "null" in our case and will not be used by the method
                    parameters = parameters.Insert(0, new SynthesizedParameterSymbol(this, this.DeclaringCompilation.GetSpecialType(SpecialType.System_Object), 0, RefKind.None));
                }

                return parameters;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get { return true; }
        }

        internal override bool IsExpressionBodied
        {
            get { return false; }
        }
    }
}
