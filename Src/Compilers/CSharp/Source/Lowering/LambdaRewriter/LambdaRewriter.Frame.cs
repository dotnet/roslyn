// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class LambdaRewriter
    {
        /// <summary>
        /// A class that represents the set of variables in a scope that have been
        /// captured by lambdas within that scope.
        /// </summary>
        private sealed class LambdaFrame : SynthesizedContainer
        {
            private readonly MethodSymbol constructor;

            internal LambdaFrame(MethodSymbol topLevelMethod, TypeCompilationState compilationState)
                : base(topLevelMethod, GeneratedNames.MakeAnonymousDisplayClassName(compilationState.GenerateTempNumber()), TypeKind.Class)
            {
                this.constructor = new SynthesizedInstanceConstructor(this);
            }

            internal override MethodSymbol Constructor
            {
                get { return this.constructor; }
            }
        }

        /// <summary>
        /// A field of a frame class that represents a variable that has been captured in a lambda.
        /// </summary>
        private sealed class LambdaCapturedVariable : SynthesizedCapturedVariable
        {
            internal LambdaCapturedVariable(SynthesizedContainer frame, Symbol captured)
                : base(frame, captured, GetType(frame, captured))
            {
            }

            private static TypeSymbol GetType(SynthesizedContainer frame, Symbol captured)
            {
                var local = captured as LocalSymbol;
                if ((object)local != null)
                {
                    // if we're capturing a generic frame pointer, construct it with the new frame's type parameters
                    var lambdaFrame = local.Type.OriginalDefinition as LambdaFrame;
                    if ((object)lambdaFrame != null)
                    {
                        return lambdaFrame.ConstructIfGeneric(frame.TypeArgumentsNoUseSiteDiagnostics);
                    }
                }
                return frame.TypeMap.SubstituteType((object)local != null ? local.Type : ((ParameterSymbol)captured).Type);
            }

            internal override int IteratorLocalIndex
            {
                get { throw ExceptionUtilities.Unreachable; }
            }
        }

        /// <summary>
        /// A local variable used to store a reference to the frame objects in which captured
        /// local variables have become fields.
        /// </summary>
        private sealed class LambdaFrameLocalSymbol : SynthesizedLocal
        {
            internal LambdaFrameLocalSymbol(MethodSymbol containingMethod, TypeSymbol type, TypeCompilationState compilationState)
                : base(containingMethod, type, GeneratedNames.MakeLambdaDisplayClassLocalName(compilationState.GenerateTempNumber()), declarationKind: LocalDeclarationKind.CompilerGeneratedLambdaDisplayClassLocal)
            {
            }
        }

        /// <summary>
        /// A method that results from the translation of a single lambda expression.
        /// </summary>
        private sealed class SynthesizedLambdaMethod : SynthesizedMethodBaseSymbol
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
                    return this.BaseMethod.ParameterCount;
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
                    return this.BaseMethod.Parameters;
                }
            }

            internal override bool GenerateDebugInfo
            {
                get { return true; }
            }
        }
    }
}
