// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal abstract class DisplayClassInstance
    {
        internal abstract Symbol ContainingSymbol { get; }

        internal abstract TypeSymbol Type { get; }

        internal abstract DisplayClassInstance ToOtherMethod(MethodSymbol method, TypeMap typeMap);

        internal abstract BoundExpression ToBoundExpression(SyntaxNode syntax);

        internal string GetDebuggerDisplay(ConsList<FieldSymbol> fields)
        {
            return GetDebuggerDisplay(GetInstanceName(), fields);
        }

        private static string GetDebuggerDisplay(string expr, ConsList<FieldSymbol> fields)
        {
            return fields.Any()
                ? $"{GetDebuggerDisplay(expr, fields.Tail)}.{fields.Head.Name}"
                : expr;
        }

        protected abstract string GetInstanceName();
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

        internal override TypeSymbol Type
        {
            get { return this.Local.Type; }
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

        protected override string GetInstanceName() => Local.Name;
    }

    internal sealed class DisplayClassInstanceFromParameter : DisplayClassInstance
    {
        internal readonly ParameterSymbol Parameter;

        internal DisplayClassInstanceFromParameter(ParameterSymbol parameter)
        {
            Debug.Assert((object)parameter != null);
            Debug.Assert(parameter.Name.EndsWith("this", StringComparison.Ordinal) ||
                parameter.Name.Length == 0 || // unnamed
                parameter.Name.Equals("value", StringComparison.Ordinal) || // display class instance passed to local function as parameter
                GeneratedNameParser.GetKind(parameter.Name) == GeneratedNameKind.TransparentIdentifier);
            this.Parameter = parameter;
        }

        internal override Symbol ContainingSymbol
        {
            get { return this.Parameter.ContainingSymbol; }
        }

        internal override TypeSymbol Type
        {
            get { return this.Parameter.Type; }
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

        protected override string GetInstanceName() => Parameter.Name;
    }
}
