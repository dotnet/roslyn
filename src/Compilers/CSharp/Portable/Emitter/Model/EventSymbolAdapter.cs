// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class EventSymbol :
        Cci.IEventDefinition
    {
        #region IEventDefinition Members

        IEnumerable<Cci.IMethodReference> Cci.IEventDefinition.GetAccessors(EmitContext context)
        {
            CheckDefinitionInvariant();

            var addMethod = this.AddMethod;
            RoslynDebug.Assert((object?)addMethod != null);
            if (addMethod.ShouldInclude(context))
            {
                yield return addMethod;
            }

            var removeMethod = this.RemoveMethod;
            RoslynDebug.Assert((object?)removeMethod != null);
            if (removeMethod.ShouldInclude(context))
            {
                yield return removeMethod;
            }
        }

        Cci.IMethodReference Cci.IEventDefinition.Adder
        {
            get
            {
                CheckDefinitionInvariant();
                MethodSymbol? addMethod = this.AddMethod;
                RoslynDebug.Assert((object?)addMethod != null);
                return addMethod;
            }
        }

        Cci.IMethodReference Cci.IEventDefinition.Remover
        {
            get
            {
                CheckDefinitionInvariant();
                MethodSymbol? removeMethod = this.RemoveMethod;
                RoslynDebug.Assert((object?)removeMethod != null);
                return removeMethod;
            }
        }

        bool Cci.IEventDefinition.IsRuntimeSpecial
        {
            get
            {
                CheckDefinitionInvariant();
                return HasRuntimeSpecialName;
            }
        }

        internal virtual bool HasRuntimeSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return false;
            }
        }

        bool Cci.IEventDefinition.IsSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return this.HasSpecialName;
            }
        }

        Cci.IMethodReference? Cci.IEventDefinition.Caller
        {
            get
            {
                CheckDefinitionInvariant();
                return null; // C# doesn't use the raise/fire accessor
            }
        }

        Cci.ITypeReference Cci.IEventDefinition.GetType(EmitContext context)
        {
            return ((PEModuleBuilder)context.Module).Translate(this.Type, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt, diagnostics: context.Diagnostics);
        }

        #endregion

        #region ITypeDefinitionMember Members

        Cci.ITypeDefinition Cci.ITypeDefinitionMember.ContainingTypeDefinition
        {
            get
            {
                CheckDefinitionInvariant();
                return this.ContainingType;
            }
        }

        Cci.TypeMemberVisibility Cci.ITypeDefinitionMember.Visibility
        {
            get
            {
                CheckDefinitionInvariant();
                return PEModuleBuilder.MemberVisibility(this);
            }
        }

        #endregion

        #region ITypeMemberReference Members

        Cci.ITypeReference Cci.ITypeMemberReference.GetContainingType(EmitContext context)
        {
            CheckDefinitionInvariant();
            return this.ContainingType;
        }

        #endregion

        #region IReference Members

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            CheckDefinitionInvariant();
            visitor.Visit((Cci.IEventDefinition)this);
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            CheckDefinitionInvariant();
            return this;
        }

        #endregion

        #region INamedEntity Members

        string Cci.INamedEntity.Name
        {
            get
            {
                CheckDefinitionInvariant();
                return this.MetadataName;
            }
        }

        #endregion
    }
}
