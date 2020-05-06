// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static IFieldSymbol Field(
            string name,
            ITypeSymbol type,
            Accessibility declaredAccessibility = Accessibility.NotApplicable,
            SymbolModifiers modifiers = SymbolModifiers.None,
            Optional<object> constantValue = default,
            bool isFixedSizeBuffer = false)
            => new FieldSymbol(
                name,
                type,
                declaredAccessibility,
                modifiers,
                constantValue,
                isFixedSizeBuffer);

        public static IFieldSymbol With(
            this IFieldSymbol field,
            Optional<string> name = default,
            Optional<ITypeSymbol> type = default,
            Optional<Accessibility> declaredAccessibility = default,
            Optional<SymbolModifiers> modifiers = default,
            Optional<Optional<object>> constantValue = default,
            Optional<bool> isFixedSizeBuffer = default)
        {
            return new FieldSymbol(
                name.GetValueOr(field.Name),
                type.GetValueOr(field.Type),
                declaredAccessibility.GetValueOr(field.DeclaredAccessibility),
                modifiers.GetValueOr(field.GetModifiers()),
                constantValue.GetValueOr(GetConstantValue(field)),
                isFixedSizeBuffer.GetValueOr(field.IsFixedSizeBuffer));
        }

        private static Optional<object> GetConstantValue(IFieldSymbol field)
            => field.HasConstantValue ? new Optional<object>(field.ConstantValue) : default;

        private class FieldSymbol : Symbol, IFieldSymbol
        {
            public FieldSymbol(
                string name,
                ITypeSymbol type,
                Accessibility declaredAccessibility,
                SymbolModifiers modifiers,
                Optional<object> constantValue,
                bool isFixedSizeBuffer)
            {
                Name = name;
                Type = type;
                DeclaredAccessibility = declaredAccessibility;
                Modifiers = modifiers;
                HasConstantValue = constantValue.HasValue;
                ConstantValue = constantValue.Value;
                IsFixedSizeBuffer = isFixedSizeBuffer;
            }

            public override Accessibility DeclaredAccessibility { get; }
            public override SymbolKind Kind => SymbolKind.Field;
            public override SymbolModifiers Modifiers { get; }
            public override string Name { get; }

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitField(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitField(this);

            public bool IsConst => (Modifiers & SymbolModifiers.Const) != 0;
            public bool IsReadOnly => (Modifiers & SymbolModifiers.ReadOnly) != 0;
            public bool IsVolatile => (Modifiers & SymbolModifiers.Volatile) != 0;

            public bool IsFixedSizeBuffer { get; }
            public ITypeSymbol Type { get; }

            public bool HasConstantValue { get; }
            public object ConstantValue { get; }

            #region default implementation

            public ISymbol AssociatedSymbol => throw new NotImplementedException();

            public NullableAnnotation NullableAnnotation => throw new NotImplementedException();

            public ImmutableArray<CustomModifier> CustomModifiers => throw new NotImplementedException();

            public IFieldSymbol CorrespondingTupleField => throw new NotImplementedException();

            IFieldSymbol IFieldSymbol.OriginalDefinition => throw new NotImplementedException();

            #endregion
        }
    }
}
