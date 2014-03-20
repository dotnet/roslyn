// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedIntrinsicOperatorSymbol : MethodSymbol
    {
        private readonly TypeSymbol containingType;
        private readonly string name;
        private readonly ImmutableArray<ParameterSymbol> parameters;
        private readonly TypeSymbol returnType;
        private readonly bool isCheckedBuiltin;

        public SynthesizedIntrinsicOperatorSymbol(TypeSymbol leftType, string name, TypeSymbol rightType, TypeSymbol returnType, bool isCheckedBuiltin)
        {
            if (leftType.Equals(rightType, ignoreCustomModifiers: true))
            {
                this.containingType = leftType;
            }
            else if (rightType.Equals(returnType, ignoreCustomModifiers: true))
            {
                this.containingType = rightType;
            }
            else
            {
                Debug.Assert(leftType.Equals(returnType, ignoreCustomModifiers: true));
                this.containingType = leftType;
            }

            this.name = name;
            this.returnType = returnType;

            Debug.Assert((leftType.IsDynamic() || rightType.IsDynamic()) == returnType.IsDynamic());
            Debug.Assert(containingType.IsDynamic() == returnType.IsDynamic());

            this.parameters = (new ParameterSymbol[] {new SynthesizedOperatorParameterSymbol(this, leftType, 0, "left"),
                                                      new SynthesizedOperatorParameterSymbol(this, rightType, 1, "right")}).AsImmutableOrNull();
            this.isCheckedBuiltin = isCheckedBuiltin;
        }

        public SynthesizedIntrinsicOperatorSymbol(TypeSymbol container, string name, TypeSymbol returnType, bool isCheckedBuiltin)
        {
            this.containingType = container;
            this.name = name;
            this.returnType = returnType;
            this.parameters = (new ParameterSymbol[] { new SynthesizedOperatorParameterSymbol(this, container, 0, "value") }).AsImmutableOrNull();
            this.isCheckedBuiltin = isCheckedBuiltin;
        }

        public override string Name
        {
            get
            {
                return name;
            }
        }

        public override bool IsCheckedBuiltin
        {
            get
            {
                return isCheckedBuiltin;
            }
        }

        public override MethodKind MethodKind
        {
            get
            {
                return MethodKind.BuiltinOperator;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return true;
            }
        }

        internal override CSharpCompilation DeclaringCompilation
        {
            get
            {
                return null;
            }
        }

        public override string GetDocumentationCommentId()
        {
            return null;
        }

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal override bool IsMetadataFinal()
        {
            return false;
        }

        public override int Arity
        {
            get
            {
                return 0;
            }
        }

        public override bool IsExtensionMethod
        {
            get
            {
                return false;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return true;
            }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get
            {
                return System.Reflection.MethodImplAttributes.Managed;
            }
        }

        internal override bool HasDeclarativeSecurity
        {
            get
            {
                return false;
            }
        }

        public override DllImportData GetDllImportData()
        {
            return null;
        }

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation()
        {
            return SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>();
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get
            {
                return null;
            }
        }

        internal override bool RequiresSecurityObject
        {
            get
            {
                return false;
            }
        }

        public override bool HidesBaseMethodsByName
        {
            get
            {
                return false;
            }
        }

        public override bool IsVararg
        {
            get
            {
                return false;
            }
        }

        public override bool ReturnsVoid
        {
            get
            {
                return false;
            }
        }

        public override bool IsAsync
        {
            get
            {
                return false;
            }
        }

        public override TypeSymbol ReturnType
        {
            get
            {
                return returnType;
            }
        }

        public override ImmutableArray<TypeSymbol> TypeArguments
        {
            get
            {
                return ImmutableArray<TypeSymbol>.Empty;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                return parameters;
            }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                return ImmutableArray<MethodSymbol>.Empty;
            }
        }

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get
            {
                return ImmutableArray<CustomModifier>.Empty;
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return null;
            }
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        internal override Cci.CallingConvention CallingConvention
        {
            get
            {
                return Cci.CallingConvention.Default;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get
            {
                return false;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return containingType as NamedTypeSymbol;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray<Location>.Empty;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return Accessibility.Public;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return true;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return false;
            }
        }

        public override bool IsOverride
        {
            get
            {
                return false;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return false;
            }
        }

        public override bool IsExtern
        {
            get
            {
                return false;
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return null;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == (object)this)
            {
                return true;
            }

            var other = obj as SynthesizedIntrinsicOperatorSymbol;

            if ((object)other == null)
            {
                return false;
            }

            if (isCheckedBuiltin == other.isCheckedBuiltin &&
                parameters.Length == other.parameters.Length &&
                string.Equals(name, other.name, StringComparison.Ordinal) &&
                containingType == other.containingType &&
                returnType == other.returnType)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].Type != other.parameters[i].Type)
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(name, Hash.Combine(containingType, parameters.Length));
        }

        private sealed class SynthesizedOperatorParameterSymbol : SynthesizedParameterSymbol
        {
            public SynthesizedOperatorParameterSymbol(
                SynthesizedIntrinsicOperatorSymbol container,
                TypeSymbol type,
                int ordinal,
                string name
            ) : base(container, type, ordinal, RefKind.None, name, ImmutableArray<CustomModifier>.Empty)

            {
            }

            public override bool Equals(object obj)
            {
                if (obj == (object)this)
                {
                    return true;
                }

                var other = obj as SynthesizedOperatorParameterSymbol;

                if ((object)other == null)
                {
                    return false;
                }

                return Ordinal == other.Ordinal && ContainingSymbol == other.ContainingSymbol;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(ContainingSymbol, Ordinal.GetHashCode());
            }
        }
    }
}
