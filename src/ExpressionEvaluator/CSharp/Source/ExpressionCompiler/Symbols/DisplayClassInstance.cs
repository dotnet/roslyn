// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal abstract class DisplayClassInstance
    {
        internal abstract Symbol ContainingSymbol { get; }

        internal abstract NamedTypeSymbol Type { get; }

        internal abstract DisplayClassInstance ToOtherMethod(MethodSymbol method, TypeMap typeMap);

        internal abstract BoundExpression ToBoundExpression(SyntaxNode syntax);
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

        internal override BoundExpression ToBoundExpression(SyntaxNode syntax)
        {
            return new BoundLocal(syntax, this.Local, constantValueOpt: null, type: this.Local.Type) { WasCompilerGenerated = true };
        }
    }

    internal sealed class DisplayClassInstanceFromParameter : DisplayClassInstance
    {
        internal readonly ParameterSymbol Parameter;

        internal DisplayClassInstanceFromParameter(ParameterSymbol parameter)
        {
            Debug.Assert((object)parameter != null);
            Debug.Assert(parameter.Name.EndsWith("this", StringComparison.Ordinal) ||
                parameter.Name.Equals("", StringComparison.Ordinal) || // unnamed
                parameter.Name.Equals("value", StringComparison.Ordinal) || // display class instance passed to local function as parameter
                GeneratedNames.GetKind(parameter.Name) == GeneratedNameKind.TransparentIdentifier);
            this.Parameter = parameter;
        }

        internal override Symbol ContainingSymbol
        {
            get { return this.Parameter.ContainingSymbol; }
        }

        internal override NamedTypeSymbol Type
        {
            get { return (NamedTypeSymbol)this.Parameter.Type; }
        }

        internal override DisplayClassInstance ToOtherMethod(MethodSymbol method, TypeMap typeMap)
        {
            Debug.Assert(method.IsStatic);
            var otherOrdinal = this.ContainingSymbol.IsStatic
                ? this.Parameter.Ordinal
                : (this.Parameter.Ordinal + 1);
            var otherParameter = method.Parameters[otherOrdinal];
            return new DisplayClassInstanceFromParameter(otherParameter);
        }

        internal override BoundExpression ToBoundExpression(SyntaxNode syntax)
        {
            return new BoundParameter(syntax, this.Parameter) { WasCompilerGenerated = true };
        }
    }
}
