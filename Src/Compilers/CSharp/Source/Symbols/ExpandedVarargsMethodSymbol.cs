using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Roslyn.Compilers.Common;
using Roslyn.Utilities;

namespace Roslyn.Compilers.CSharp
{
    partial class MethodSymbol
    {
        private ConcurrentHashSet<ExpandedVarargsMethodSymbol> expandedSymbols;

        internal MethodSymbol GetExpandedSymbol(BoundArgListOperator argList)
        {
            if (expandedSymbols == null)
            {
                Interlocked.CompareExchange(ref expandedSymbols, new ConcurrentHashSet<ExpandedVarargsMethodSymbol>(), null);
            }

            ExpandedVarargsMethodSymbol newX = new ExpandedVarargsMethodSymbol(this, argList);

            // There are races here where we could end up with two outstanding copies of "the same"
            // expanded symbol; we really don't care. The worst that will happen is we emit a duplicate
            // entry in the method ref table.
            foreach(ExpandedVarargsMethodSymbol x in expandedSymbols)
            {
                if (x == newX)
                {
                    return x;
                }
            }
            expandedSymbols.Add(newX);
            return newX;
        }

        private sealed class ExpandedVarargsMethodSymbol : MethodSymbol
        {
            public readonly MethodSymbol underlyingMethod;
            public readonly ReadOnlyArray<ParameterSymbol> extraParameters;

            public ExpandedVarargsMethodSymbol(MethodSymbol underlyingMethod, BoundArgListOperator argList)
            {
                this.underlyingMethod = underlyingMethod;

                ArrayBuilder<ParameterSymbol> builder = ArrayBuilder<ParameterSymbol>.GetInstance();
                for (int i = 0; i < argList.Arguments.Count; ++i)
                {
                    Debug.Assert(argList.Arguments[i].Type != null);

                    builder.Add(new SynthesizedParameterSymbol(
                        container: this, 
                        type: argList.Arguments[i].Type,
                        ordinal: i + underlyingMethod.ParameterCount,
                        refKind: argList.ArgumentRefKindsOpt.IsNullOrEmpty ? RefKind.None : argList.ArgumentRefKindsOpt[i],
                        name: "")); // these fake parameters are never accessed by name.
                }

                this.extraParameters = builder.ToReadOnlyAndFree();
            }

            internal override ReadOnlyArray<ParameterSymbol> ExtraParameters
            {
                get
                {
                    return this.extraParameters;
                }
            }

            public sealed override bool Equals(object _other)
            {
                if ((object)this == _other) return true;

                ExpandedVarargsMethodSymbol other = _other as ExpandedVarargsMethodSymbol;
                if ((object)other == null) return false;
                if (this.underlyingMethod != other.underlyingMethod || this.extraParameters.Count != other.extraParameters.Count)
                {
                    return false;
                }

                int count = this.extraParameters.Count;
                for (int i = 0; i < count; ++i)
                {
                    var thisParameter = this.extraParameters[i];
                    var otherParameter = other.extraParameters[i];
                    if (thisParameter.RefKind != otherParameter.RefKind || thisParameter.Type != otherParameter.Type)
                    {
                        return false;
                    }
                }

                return true;
            }

            public override int GetHashCode()
            {
                int key = this.underlyingMethod.GetHashCode();
                int count = this.extraParameters.Count;
                for (int i = 0; i < count; ++i)
                {
                    key = Utilities.Hash.Combine((int)this.extraParameters[i].RefKind, key);
                    key = Utilities.Hash.Combine(this.extraParameters[i].Type.GetHashCode(), key);
                }
                return key;
            }

            public override string Name
            {
                get
                {
                    return underlyingMethod.Name;
                }
            }

            #region Abstract method overrides

            internal override bool GenerateDebugInfo
            {
                get { return underlyingMethod.GenerateDebugInfo; }
            }

            internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
            {
                return underlyingMethod.IsMetadataNewSlot(ignoreInterfaceImplementationChanges);
            }

            internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
            {
                return underlyingMethod.IsMetadataVirtual(ignoreInterfaceImplementationChanges);
            }

            internal override bool IsMetadataFinal()
            {
                return underlyingMethod.IsMetadataFinal();
            }

            public override MethodKind MethodKind
            {
                get { return underlyingMethod.MethodKind; }
            }

            public override int Arity
            {
                get
                {
                    Debug.Assert(underlyingMethod.Arity == 0);
                    return underlyingMethod.Arity;
                }
            }

            internal override bool HasSpecialName
            {
                get { return underlyingMethod.HasSpecialName; }
            }

            internal override System.Reflection.MethodImplAttributes ImplementationAttributes
            {
                get { return default(System.Reflection.MethodImplAttributes); }
            }

            public override bool IsExtensionMethod
            {
                get { return underlyingMethod.IsExtensionMethod; }
            }

            public override bool HidesBaseMethodsByName
            {
                get { return underlyingMethod.HidesBaseMethodsByName; }
            }

            public override bool IsVararg
            {
                get
                {
                    Debug.Assert(underlyingMethod.IsVararg);
                    return underlyingMethod.IsVararg;
                }
            }

            public override bool ReturnsVoid
            {
                get { return underlyingMethod.ReturnsVoid; }
            }

            public override bool IsAsync
            {
                get { return underlyingMethod.IsAsync; }
            }

            public override TypeSymbol ReturnType
            {
                get { return underlyingMethod.ReturnType; }
            }

            public override ReadOnlyArray<TypeSymbol> TypeArguments
            {
                get
                {
                    Debug.Assert(underlyingMethod.TypeArguments.IsNullOrEmpty);
                    return underlyingMethod.TypeArguments;
                }
            }

            public override ReadOnlyArray<TypeParameterSymbol> TypeParameters
            {
                get
                {
                    Debug.Assert(underlyingMethod.TypeParameters.IsNullOrEmpty);
                    return underlyingMethod.TypeParameters;
                }
            }

            public override ReadOnlyArray<ParameterSymbol> Parameters
            {
                get
                {
                    return underlyingMethod.Parameters;
                }
            }

            public override ReadOnlyArray<MethodSymbol> ExplicitInterfaceImplementations
            {
                get { return underlyingMethod.ExplicitInterfaceImplementations; }
            }

            public override ReadOnlyArray<CustomModifier> ReturnTypeCustomModifiers
            {
                get { return underlyingMethod.ReturnTypeCustomModifiers; }
            }

            public override Symbol AssociatedPropertyOrEvent
            {
                get { return underlyingMethod.AssociatedPropertyOrEvent; }
            }

            internal override Microsoft.Cci.CallingConvention CallingConvention
            {
                get
                {
                    Debug.Assert(((int)underlyingMethod.CallingConvention & 0x0f) == (int)Microsoft.Cci.CallingConvention.ExtraArguments);
                    return underlyingMethod.CallingConvention;
                }
            }

            public override Symbol ContainingSymbol
            {
                get { return underlyingMethod.ContainingSymbol; }
            }

            public override ReadOnlyArray<Location> Locations
            {
                get { return underlyingMethod.Locations; }
            }

            public override ReadOnlyArray<SyntaxReference> DeclaringSyntaxReferences
            {
                get { return underlyingMethod.DeclaringSyntaxReferences; }
            }

            public override Accessibility DeclaredAccessibility
            {
                get { return underlyingMethod.DeclaredAccessibility; }
            }

            public override bool IsStatic
            {
                get { return underlyingMethod.IsStatic; }
            }

            public override bool IsVirtual
            {
                get { return underlyingMethod.IsVirtual; }
            }

            public override bool IsOverride
            {
                get { return underlyingMethod.IsOverride; }
            }

            public override bool IsAbstract
            {
                get { return underlyingMethod.IsAbstract; }
            }

            public override bool IsSealed
            {
                get { return underlyingMethod.IsSealed; }
            }

            public override bool IsExtern
            {
                get { return underlyingMethod.IsExtern; }
            }

            internal sealed override ObsoleteAttributeData ObsoleteAttributeData
            {
                get { return null; }
            }

            internal override bool RequiresSecurityObject
            {
                get { return underlyingMethod.RequiresSecurityObject; }
            }

            public override DllImportData GetDllImportData()
            {
                return underlyingMethod.GetDllImportData();
            }

            internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
            {
                get { return underlyingMethod.ReturnValueMarshallingInformation; }
            }

            internal override bool HasDeclarativeSecurity
            {
                get { return underlyingMethod.HasDeclarativeSecurity; }
            }

            internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
            {
                return underlyingMethod.GetSecurityInformation();
            }

            internal override IEnumerable<string> GetAppliedConditionalSymbols()
            {
                return underlyingMethod.GetAppliedConditionalSymbols();
            }

            #endregion
        }
    }
}
