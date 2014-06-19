using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp.Emit
{
    internal sealed class MethodDefinition : MethodReference, Microsoft.Cci.IMethodDefinition
    {
        private Microsoft.Cci.IMethodBody methodBody;

        public MethodDefinition(Module moduleBeingBuilt, MethodSymbol underlyingMethod)
            : base(moduleBeingBuilt, underlyingMethod)
        { }

        public override void Dispatch(Microsoft.Cci.IMetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.IMethodDefinition)this);
        }

        public void SetMethodBody(Microsoft.Cci.IMethodBody methodBody)
        {
            Contract.ThrowIfNull(methodBody);
            Contract.ThrowIfFalse(this.methodBody == null);

            this.methodBody = methodBody;
        }

        Microsoft.Cci.IMethodBody Microsoft.Cci.IMethodDefinition.Body
        {
            get { return this.methodBody; }
        }

        IEnumerable<Microsoft.Cci.IGenericMethodParameter> Microsoft.Cci.IMethodDefinition.GenericParameters
        {
            get 
            {
                foreach (var @param in UnderlyingMethod.TypeParameters)
                {
                    yield return (Microsoft.Cci.IGenericMethodParameter)ModuleBeingBuilt.Translate(@param);
                }
            }
        }

        bool Microsoft.Cci.IMethodDefinition.HasDeclarativeSecurity
        {
            get { return false; }
        }

        bool Microsoft.Cci.IMethodDefinition.IsAbstract
        {
            get { return UnderlyingMethod.IsAbstract; }
        }

        bool Microsoft.Cci.IMethodDefinition.IsAccessCheckedOnOverride
        {
            get { return false; }
        }

        bool Microsoft.Cci.IMethodDefinition.IsConstructor
        {
            get { return UnderlyingMethod.MethodKind == CSharp.MethodKind.Constructor; }
        }

        bool Microsoft.Cci.IMethodDefinition.IsExternal
        {
            get { return UnderlyingMethod.IsExtern; }
        }

        bool Microsoft.Cci.IMethodDefinition.IsForwardReference
        {
            get { return false; }
        }

        bool Microsoft.Cci.IMethodDefinition.IsHiddenBySignature
        {
            get { return true; }
        }

        bool Microsoft.Cci.IMethodDefinition.IsNativeCode
        {
            get { return false; }
        }

        bool Microsoft.Cci.IMethodDefinition.IsNewSlot
        {
            get 
            {
                return UnderlyingMethod.IsVirtual || UnderlyingMethod.IsAbstract;
            }
        }

        bool Microsoft.Cci.IMethodDefinition.IsNeverInlined
        {
            get { return false; }
        }

        bool Microsoft.Cci.IMethodDefinition.IsNeverOptimized
        {
            get { return false; }
        }

        bool Microsoft.Cci.IMethodDefinition.IsPlatformInvoke
        {
            get { return false; }
        }

        bool Microsoft.Cci.IMethodDefinition.IsRuntimeImplemented
        {
            get { return false; }
        }

        bool Microsoft.Cci.IMethodDefinition.IsRuntimeInternal
        {
            get { return false; }
        }

        bool Microsoft.Cci.IMethodDefinition.IsRuntimeSpecial
        {
            get { return UnderlyingMethod.MethodKind == CSharp.MethodKind.Constructor; }
        }

        bool Microsoft.Cci.IMethodDefinition.IsSealed
        {
            get 
            {
                return UnderlyingMethod.IsSealed;
            }
        }

        bool Microsoft.Cci.IMethodDefinition.IsSpecialName
        {
            get { return UnderlyingMethod.MethodKind == CSharp.MethodKind.Constructor; }
        }

        bool Microsoft.Cci.IMethodDefinition.IsStatic
        {
            get
            {
                return UnderlyingMethod.IsStatic;
            }
        }

        bool Microsoft.Cci.IMethodDefinition.IsSynchronized
        {
            get { return false; }
        }

        bool Microsoft.Cci.IMethodDefinition.IsVirtual
        {
            get
            {
                return UnderlyingMethod.IsVirtual || UnderlyingMethod.IsAbstract;
            }
        }

        bool Microsoft.Cci.IMethodDefinition.IsUnmanaged
        {
            get { return false; }
        }

        IEnumerable<Microsoft.Cci.IParameterDefinition> Microsoft.Cci.IMethodDefinition.Parameters
        {
            get
            {
                foreach (var p in UnderlyingMethod.Parameters)
                {
                    yield return (Microsoft.Cci.IParameterDefinition)ModuleBeingBuilt.Translate(p);
                }
            }
        }

        bool Microsoft.Cci.IMethodDefinition.PreserveSignature
        {
            get { return false; }
        }

        Microsoft.Cci.IPlatformInvokeInformation Microsoft.Cci.IMethodDefinition.PlatformInvokeData
        {
            get { throw new NotImplementedException(); }
        }

        bool Microsoft.Cci.IMethodDefinition.RequiresSecurityObject
        {
            get { return false; }
        }

        IEnumerable<Microsoft.Cci.ICustomAttribute> Microsoft.Cci.IMethodDefinition.ReturnValueAttributes
        {
            get { return Enumerable.Empty<Microsoft.Cci.ICustomAttribute>(); }
        }

        bool Microsoft.Cci.IMethodDefinition.ReturnValueIsMarshalledExplicitly
        {
            get { return false; }
        }

        Microsoft.Cci.IMarshallingInformation Microsoft.Cci.IMethodDefinition.ReturnValueMarshallingInformation
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<Microsoft.Cci.ISecurityAttribute> Microsoft.Cci.IMethodDefinition.SecurityAttributes
        {
            get { throw new NotImplementedException(); }
        }

        Microsoft.Cci.ITypeDefinition Microsoft.Cci.ITypeDefinitionMember.ContainingTypeDefinition
        {
            get 
            {
                return (Microsoft.Cci.ITypeDefinition)ModuleBeingBuilt.Translate(UnderlyingMethod.ContainingType, true); 
            }
        }

        Microsoft.Cci.TypeMemberVisibility Microsoft.Cci.ITypeDefinitionMember.Visibility
        {
            get
            {
                return Module.MemberVisibility(UnderlyingMethod.DeclaredAccessibility);
            }
        }

        protected override Microsoft.Cci.IDefinition AsDefinition
        {
            get { return this; }
        }



        Microsoft.Cci.INestedTypeDefinition Microsoft.Cci.ITypeDefinitionMember.AsNestedTypeDefinition
        {
            get { return null; }
        }
    }
}
