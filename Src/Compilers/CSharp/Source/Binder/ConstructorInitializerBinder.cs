using System;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class ConstructorInitializerBinder : Binder
    {
        private readonly Symbol containingMember;

        internal ConstructorInitializerBinder(MethodSymbol containingMember, Binder next)
            : base(next)
        {
            this.containingMember = containingMember;
        }

        internal override Symbol ContainingMemberOrLambda
        {
            get { return this.containingMember; }
        }

        //the raison d'etre of this class
        internal override BindingLocation BindingLocation
        {
            get { return BindingLocation.ConstructorInitializer; }
        }
    }
}
