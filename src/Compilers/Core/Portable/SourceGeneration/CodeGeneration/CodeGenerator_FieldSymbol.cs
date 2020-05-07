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
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = Accessibility.NotApplicable,
            SymbolModifiers modifiers = SymbolModifiers.None,
            Optional<object> constantValue = default)
            => new FieldSymbol(
                name,
                type,
                attributes,
                declaredAccessibility,
                modifiers,
                constantValue,
                isFixedSizeBuffer: false);

        public static IFieldSymbol FixedSizeBuffer(
            string name,
            ITypeSymbol type,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = Accessibility.NotApplicable,
            SymbolModifiers modifiers = SymbolModifiers.None,
            Optional<object> constantValue = default)
            => new FieldSymbol(
                name,
                type,
                attributes,
                declaredAccessibility,
                modifiers,
                constantValue,
                isFixedSizeBuffer: true);

        public static IFieldSymbol With(
            this IFieldSymbol field,
            Optional<string> name = default,
            Optional<ITypeSymbol> type = default,
            Optional<ImmutableArray<AttributeData>> attributes = default,
            Optional<Accessibility> declaredAccessibility = default,
            Optional<SymbolModifiers> modifiers = default,
            Optional<Optional<object>> constantValue = default)
        {
            return new FieldSymbol(
                name.GetValueOr(field.Name),
                type.GetValueOr(field.Type),
                attributes.GetValueOr(field.GetAttributes()),
                declaredAccessibility.GetValueOr(field.DeclaredAccessibility),
                modifiers.GetValueOr(field.GetModifiers()),
                constantValue.GetValueOr(GetConstantValue(field)),
                field.IsFixedSizeBuffer);
        }

        private static Optional<object> GetConstantValue(IFieldSymbol field)
            => field.HasConstantValue ? new Optional<object>(field.ConstantValue) : default;

        private class FieldSymbol : Symbol, IFieldSymbol
        {
            private readonly ImmutableArray<AttributeData> _attributes;

            public FieldSymbol(
                string name,
                ITypeSymbol type,
                ImmutableArray<AttributeData> attributes,
                Accessibility declaredAccessibility,
                SymbolModifiers modifiers,
                Optional<object> constantValue,
                bool isFixedSizeBuffer)
            {
                Name = name;
                Type = type;
                DeclaredAccessibility = declaredAccessibility;
                Modifiers = modifiers;
                _attributes = attributes;
                HasConstantValue = constantValue.HasValue;
                ConstantValue = constantValue.Value;
                IsFixedSizeBuffer = isFixedSizeBuffer;
            }

            public override Accessibility DeclaredAccessibility { get; }
            public override SymbolKind Kind => SymbolKind.Field;
            public override SymbolModifiers Modifiers { get; }
            public override string Name { get; }

            public bool IsConst => (Modifiers & SymbolModifiers.Const) != 0;
            public bool IsReadOnly => (Modifiers & SymbolModifiers.ReadOnly) != 0;
            public bool IsVolatile => (Modifiers & SymbolModifiers.Volatile) != 0;

            public bool IsFixedSizeBuffer { get; }
            public ITypeSymbol Type { get; }

            public bool HasConstantValue { get; }
            public object ConstantValue { get; }

            public override ImmutableArray<AttributeData> GetAttributes()
                => _attributes;

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitField(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitField(this);

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
