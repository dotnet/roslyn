using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class FieldInitializerBinder : LocalScopeBinder
    {
        private readonly Symbol containingMember;

        internal FieldInitializerBinder(FieldSymbol containingMember, Binder next)
            : base(null, next)
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
            get { return BindingLocation.FieldInitializer; }
        }
    }
}
