// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class SynthesizedInstanceConstructor : SynthesizedInstanceMethodSymbol
    {
        private readonly NamedTypeSymbol _containingType;

        internal SynthesizedInstanceConstructor(NamedTypeSymbol containingType)
        {
            Debug.Assert((object)containingType != null);
            _containingType = containingType;
        }

        //
        // Consider overriding when implementing a synthesized subclass.
        //

        internal override bool GenerateDebugInfo
        {
            get { return true; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return ImmutableArray<ParameterSymbol>.Empty; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return ContainingType.IsAbstract ? Accessibility.Protected : Accessibility.Public; }
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return false;
            }
        }

        #region Sealed

        public sealed override Symbol ContainingSymbol
        {
            get { return _containingType; }
        }

        public sealed override NamedTypeSymbol ContainingType
        {
            get
            {
                return _containingType;
            }
        }

        public sealed override string Name
        {
            get { return WellKnownMemberNames.InstanceConstructorName; }
        }

        internal sealed override bool HasSpecialName
        {
            get { return true; }
        }

        internal sealed override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get
            {
                if (_containingType.IsComImport)
                {
                    Debug.Assert(_containingType.TypeKind == TypeKind.Class);
                    return System.Reflection.MethodImplAttributes.Runtime | System.Reflection.MethodImplAttributes.InternalCall;
                }

                if (_containingType.TypeKind == TypeKind.Delegate)
                {
                    return System.Reflection.MethodImplAttributes.Runtime;
                }

                return default(System.Reflection.MethodImplAttributes);
            }
        }

        internal sealed override bool RequiresSecurityObject
        {
            get { return false; }
        }

        public sealed override DllImportData GetDllImportData()
        {
            return null;
        }

        internal sealed override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get { return null; }
        }

        internal sealed override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        internal sealed override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        public sealed override bool IsVararg
        {
            get { return false; }
        }

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return ImmutableArray<TypeParameterSymbol>.Empty; }
        }

        internal sealed override LexicalSortKey GetLexicalSortKey()
        {
            //For the sake of matching the metadata output of the native compiler, make synthesized constructors appear last in the metadata.
            //This is not critical, but it makes it easier on tools that are comparing metadata.
            return LexicalSortKey.SynthesizedCtor;
        }

        public sealed override ImmutableArray<Location> Locations
        {
            get { return ContainingType.Locations; }
        }

        public override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        public sealed override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get { return TypeWithAnnotations.Create(ContainingAssembly.GetSpecialType(SpecialType.System_Void)); }
        }

        public sealed override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public sealed override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        public sealed override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get { return ImmutableArray<TypeWithAnnotations>.Empty; }
        }

        public sealed override Symbol AssociatedSymbol
        {
            get { return null; }
        }

        public sealed override int Arity
        {
            get { return 0; }
        }

        public sealed override bool ReturnsVoid
        {
            get { return true; }
        }

        public sealed override MethodKind MethodKind
        {
            get { return MethodKind.Constructor; }
        }

        public sealed override bool IsExtern
        {
            get
            {
                // Synthesized constructors of ComImport type are extern
                NamedTypeSymbol containingType = this.ContainingType;
                return containingType is object { IsComImport: true };
            }
        }

        public sealed override bool IsSealed
        {
            get { return false; }
        }

        public sealed override bool IsAbstract
        {
            get { return false; }
        }

        public sealed override bool IsOverride
        {
            get { return false; }
        }

        public sealed override bool IsVirtual
        {
            get { return false; }
        }

        public sealed override bool IsStatic
        {
            get { return false; }
        }

        public sealed override bool IsAsync
        {
            get { return false; }
        }

        public sealed override bool HidesBaseMethodsByName
        {
            get { return false; }
        }

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        public sealed override bool IsExtensionMethod
        {
            get { return false; }
        }

        internal sealed override Cci.CallingConvention CallingConvention
        {
            get { return Cci.CallingConvention.HasThis; }
        }

        internal sealed override bool IsExplicitInterfaceImplementation
        {
            get { return false; }
        }

        public sealed override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray<MethodSymbol>.Empty; }
        }

        internal sealed override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            var containingType = (SourceMemberContainerTypeSymbol)this.ContainingType;
            return containingType.CalculateSyntaxOffsetInSynthesizedConstructor(localPosition, localTree, isStatic: false);
        }

        internal sealed override DiagnosticInfo GetUseSiteDiagnostic()
        {
            return ReturnTypeWithAnnotations.Type.GetUseSiteDiagnostic();
        }
        #endregion

        protected void GenerateMethodBodyCore(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var factory = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            factory.CurrentFunction = this;
            if (ContainingType.BaseTypeNoUseSiteDiagnostics is MissingMetadataTypeSymbol)
            {
                // System_Attribute was not found or was inaccessible
                factory.CloseMethod(factory.Block());
                return;
            }

            var baseConstructorCall = MethodCompiler.GenerateBaseParameterlessConstructorInitializer(this, diagnostics);
            if (baseConstructorCall == null)
            {
                // Attribute..ctor was not found or was inaccessible
                factory.CloseMethod(factory.Block());
                return;
            }

            var statements = ArrayBuilder<BoundStatement>.GetInstance();
            statements.Add(factory.ExpressionStatement(baseConstructorCall));
            GenerateMethodBodyStatements(factory, statements, diagnostics);
            statements.Add(factory.Return());

            var block = factory.Block(statements.ToImmutableAndFree());

            factory.CloseMethod(block);
        }

        protected virtual void GenerateMethodBodyStatements(SyntheticBoundNodeFactory factory, ArrayBuilder<BoundStatement> statements, DiagnosticBag diagnostics)
        {
            // overridden in a derived class to add extra statements to the body of the generated constructor
        }

    }
}
