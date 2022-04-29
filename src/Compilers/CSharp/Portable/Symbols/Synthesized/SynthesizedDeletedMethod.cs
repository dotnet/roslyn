// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a method that has been deleted in an Edit and Continue session, and whose body
    /// throws a System.MissingMethodException.
    /// </summary>
    internal sealed class SynthesizedDeletedMethod : MethodSymbol, ISynthesizedDeletedMethod
    {
        private readonly MethodSymbol _methodSymbol;
        private readonly NamedTypeSymbol _containingTypeSymbol;

        internal SynthesizedDeletedMethod(MethodSymbol methodSymbol, NamedTypeSymbol containingTypeSymbol)
            : base()
        {
            _methodSymbol = methodSymbol;
            _containingTypeSymbol = containingTypeSymbol;
        }

        // MethodSymbol properties that delegate through _containingTypeSymbol

        internal override ModuleSymbol ContainingModule => _containingTypeSymbol.ContainingModule;

        public override Symbol ContainingSymbol => _containingTypeSymbol;

        public override AssemblySymbol ContainingAssembly => _containingTypeSymbol.ContainingAssembly;

        public override NamespaceSymbol ContainingNamespace => _containingTypeSymbol.ContainingNamespace;

        public override bool IsImplicitlyDeclared => true;

        internal override bool SynthesizesLoweredBoundBody => true;

        // MethodSymbol properties that delegate through _methodSymbol

        public override string Name => _methodSymbol.Name;

        public override MethodKind MethodKind => _methodSymbol.MethodKind;

        public override int Arity => _methodSymbol.Arity;

        public override bool IsExtensionMethod => _methodSymbol.IsExtensionMethod;

        public override bool HidesBaseMethodsByName => _methodSymbol.HidesBaseMethodsByName;

        public override bool IsVararg => _methodSymbol.IsVararg;

        public override bool ReturnsVoid => _methodSymbol.ReturnsVoid;

        public override bool IsAsync => _methodSymbol.IsAsync;

        public override RefKind RefKind => _methodSymbol.RefKind;

        public override TypeWithAnnotations ReturnTypeWithAnnotations => _methodSymbol.ReturnTypeWithAnnotations;

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => _methodSymbol.ReturnTypeFlowAnalysisAnnotations;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => _methodSymbol.ReturnNotNullIfParameterNotNull;

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => _methodSymbol.FlowAnalysisAnnotations;

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => _methodSymbol.TypeArgumentsWithAnnotations;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => _methodSymbol.TypeParameters;

        public override ImmutableArray<ParameterSymbol> Parameters => _methodSymbol.Parameters;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => _methodSymbol.ExplicitInterfaceImplementations;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => _methodSymbol.RefCustomModifiers;

        public override Symbol AssociatedSymbol => _methodSymbol.AssociatedSymbol;

        public override bool AreLocalsZeroed => _methodSymbol.AreLocalsZeroed;

        public override ImmutableArray<Location> Locations => _methodSymbol.Locations;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => _methodSymbol.DeclaringSyntaxReferences;

        public override Accessibility DeclaredAccessibility => _methodSymbol.DeclaredAccessibility;

        public override bool IsStatic => _methodSymbol.IsStatic;

        public override bool IsVirtual => _methodSymbol.IsVirtual;

        public override bool IsOverride => _methodSymbol.IsOverride;

        public override bool IsAbstract => _methodSymbol.IsAbstract;

        public override bool IsSealed => _methodSymbol.IsSealed;

        public override bool IsExtern => _methodSymbol.IsExtern;

        internal override bool HasSpecialName => _methodSymbol.HasSpecialName;

        internal override MethodImplAttributes ImplementationAttributes => _methodSymbol.ImplementationAttributes;

        internal override bool HasDeclarativeSecurity => _methodSymbol.HasDeclarativeSecurity;

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation => _methodSymbol.ReturnValueMarshallingInformation;

        internal override bool RequiresSecurityObject => _methodSymbol.RequiresSecurityObject;

        internal override bool IsDeclaredReadOnly => _methodSymbol.IsDeclaredReadOnly;

        internal override bool IsInitOnly => _methodSymbol.IsInitOnly;

        internal override CallingConvention CallingConvention => _methodSymbol.CallingConvention;

        internal override bool GenerateDebugInfo => _methodSymbol.GenerateDebugInfo;

        internal override ObsoleteAttributeData ObsoleteAttributeData => _methodSymbol.ObsoleteAttributeData;

        // Interesting MethodSymbol methods

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentFunction = this;

            try
            {
                var className = _methodSymbol.ContainingType.Name;
                var methodName = _methodSymbol.Name;

                //throw new MissingMethodException(className, methodName);

                //TODO: var body = F.Throw(F.New(F.WellKnownMethod(WellKnownMember.System_MissingMethodException__ctorStringString), ImmutableArray.Create<BoundExpression>(F.Parameter(className), F.Parameter(methodName))));

                // NOTE: we created this block in its most-lowered form, so analysis is unnecessary
                F.CloseMethod(F.ThrowNull());
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }

        // MethodSymbol methods that delegate to _methodSymbol

        public override DllImportData? GetDllImportData()
        {
            return _methodSymbol.GetDllImportData();
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            return _methodSymbol.CalculateLocalSyntaxOffset(localPosition, localTree);
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return _methodSymbol.GetAppliedConditionalSymbols();
        }

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation()
        {
            return _methodSymbol.GetSecurityInformation();
        }

        internal override UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete)
        {
            return _methodSymbol.GetUnmanagedCallersOnlyAttributeData(forceComplete);
        }

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return _methodSymbol.IsMetadataNewSlot(ignoreInterfaceImplementationChanges);
        }

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return _methodSymbol.IsMetadataVirtual(ignoreInterfaceImplementationChanges);
        }

        internal override bool IsNullableAnalysisEnabled()
        {
            return _methodSymbol.IsNullableAnalysisEnabled();
        }
    }
}
