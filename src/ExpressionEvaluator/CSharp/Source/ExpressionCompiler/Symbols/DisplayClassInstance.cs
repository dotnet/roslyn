// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal abstract class DisplayClassInstance
    {
        internal abstract Symbol ContainingSymbol { get; }

        internal abstract NamedTypeSymbol Type { get; }

        internal abstract DisplayClassInstance ToOtherMethod(MethodSymbol method, TypeMap typeMap);

        internal abstract BoundExpression ToBoundExpression(CSharpSyntaxNode syntax);
    }

    internal sealed class DisplayClassInstanceFromLocal : DisplayClassInstance
    {
        internal readonly EELocalSymbol Local;

        internal DisplayClassInstanceFromLocal(EELocalSymbol local)
        {
            Debug.Assert(!local.IsPinned);
            Debug.Assert(local.RefKind == RefKind.None);
            Debug.Assert(local.DeclarationKind == LocalDeclarationKind.RegularVariable);

            this.Local = local;
        }

        internal override Symbol ContainingSymbol
        {
            get { return this.Local.ContainingSymbol; }
        }

        internal override NamedTypeSymbol Type
        {
            get { return (NamedTypeSymbol)this.Local.Type; }
        }

        internal override DisplayClassInstance ToOtherMethod(MethodSymbol method, TypeMap typeMap)
        {
            var otherInstance = (EELocalSymbol)this.Local.ToOtherMethod(method, typeMap);
            return new DisplayClassInstanceFromLocal(otherInstance);
        }

        internal override BoundExpression ToBoundExpression(CSharpSyntaxNode syntax)
        {
            return new BoundLocal(syntax, this.Local, constantValueOpt: null, type: this.Local.Type) { WasCompilerGenerated = true };
        }
    }

    internal sealed class DisplayClassInstanceFromThis : DisplayClassInstance
    {
        internal readonly ParameterSymbol ThisParameter;

        internal DisplayClassInstanceFromThis(ParameterSymbol thisParameter)
        {
            Debug.Assert(thisParameter != null);
            this.ThisParameter = thisParameter;
        }

        internal override Symbol ContainingSymbol
        {
            get { return this.ThisParameter.ContainingSymbol; }
        }

        internal override NamedTypeSymbol Type
        {
            get { return (NamedTypeSymbol)this.ThisParameter.Type; }
        }

        internal override DisplayClassInstance ToOtherMethod(MethodSymbol method, TypeMap typeMap)
        {
            Debug.Assert(method.IsStatic);
            var otherParameter = method.Parameters[0];
            return new DisplayClassInstanceFromThis(otherParameter);
        }

        internal override BoundExpression ToBoundExpression(CSharpSyntaxNode syntax)
        {
            return new BoundParameter(syntax, this.ThisParameter) { WasCompilerGenerated = true };
        }
    }
}
