// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class SourceNamedTypeSymbol
    {
        private SynthesizedEnumValueFieldSymbol _lazyEnumValueField;
        private NamedTypeSymbol _lazyEnumUnderlyingType = ErrorTypeSymbol.UnknownResultType;

        /// <summary>
        /// For enum types, gets the underlying type. Returns null on all other
        /// kinds of types.
        /// </summary>
        public override NamedTypeSymbol EnumUnderlyingType
        {
            get
            {
                if (ReferenceEquals(_lazyEnumUnderlyingType, ErrorTypeSymbol.UnknownResultType))
                {
                    BindingDiagnosticBag diagnostics = BindingDiagnosticBag.GetInstance();
                    if ((object)Interlocked.CompareExchange(ref _lazyEnumUnderlyingType, this.GetEnumUnderlyingType(diagnostics), ErrorTypeSymbol.UnknownResultType) ==
                        (object)ErrorTypeSymbol.UnknownResultType)
                    {
                        AddDeclarationDiagnostics(diagnostics);
                        this.state.NotePartComplete(CompletionPart.EnumUnderlyingType);
                    }
                    diagnostics.Free();
                }

                return _lazyEnumUnderlyingType;
            }
        }

        private NamedTypeSymbol GetEnumUnderlyingType(BindingDiagnosticBag diagnostics)
        {
            if (this.TypeKind != TypeKind.Enum)
            {
                return null;
            }

            var compilation = this.DeclaringCompilation;
            var decl = this.declaration.Declarations[0];
            var bases = GetBaseListOpt(decl);
            if (bases != null)
            {
                var types = bases.Types;
                if (types.Count > 0)
                {
                    var typeSyntax = types[0].Type;

                    var baseBinder = compilation.GetBinder(bases);
                    var type = baseBinder.BindType(typeSyntax, diagnostics).Type;

                    // Error types are not exposed to the caller. In those
                    // cases, the underlying type is treated as int.
                    if (!type.SpecialType.IsValidEnumUnderlyingType())
                    {
                        diagnostics.Add(ErrorCode.ERR_IntegralTypeExpected, typeSyntax.Location);
                        type = compilation.GetSpecialType(SpecialType.System_Int32);
                    }

                    return (NamedTypeSymbol)type;
                }
            }

            NamedTypeSymbol defaultUnderlyingType = compilation.GetSpecialType(SpecialType.System_Int32);
            Binder.ReportUseSite(defaultUnderlyingType, diagnostics, this.GetFirstLocation());
            return defaultUnderlyingType;
        }

        /// <summary>
        /// For enum types, returns the synthesized instance field used
        /// for generating metadata. Returns null for non-enum types.
        /// </summary>
        internal FieldSymbol EnumValueField
        {
            get
            {
                if (this.TypeKind != TypeKind.Enum)
                {
                    return null;
                }

                if ((object)_lazyEnumValueField == null)
                {
                    Debug.Assert((object)this.EnumUnderlyingType != null);
                    Interlocked.CompareExchange(ref _lazyEnumValueField, new SynthesizedEnumValueFieldSymbol(this), null);
                }

                return _lazyEnumValueField;
            }
        }
    }
}
