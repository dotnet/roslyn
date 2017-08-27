﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal enum DisplayClassVariableKind
    {
        Local,
        Parameter,
        This,
    }

    /// <summary>
    /// A field in a display class that represents a captured
    /// variable: either a local, a parameter, or "this".
    /// </summary>
    internal sealed class DisplayClassVariable
    {
        internal readonly string Name;
        internal readonly DisplayClassVariableKind Kind;
        internal readonly DisplayClassInstance DisplayClassInstance;
        internal readonly ConsList<FieldSymbol> DisplayClassFields;

        internal DisplayClassVariable(string name, DisplayClassVariableKind kind, DisplayClassInstance displayClassInstance, ConsList<FieldSymbol> displayClassFields)
        {
            Debug.Assert(displayClassFields.Any());

            this.Name = name;
            this.Kind = kind;
            this.DisplayClassInstance = displayClassInstance;
            this.DisplayClassFields = displayClassFields;

            // Verify all type parameters are substituted.
            Debug.Assert(this.ContainingSymbol.IsContainingSymbolOfAllTypeParameters(this.Type));
        }

        internal TypeSymbol Type
        {
            get { return this.DisplayClassFields.Head.Type; }
        }

        internal Symbol ContainingSymbol
        {
            get { return this.DisplayClassInstance.ContainingSymbol; }
        }

        internal DisplayClassVariable ToOtherMethod(MethodSymbol method, TypeMap typeMap)
        {
            var otherInstance = this.DisplayClassInstance.ToOtherMethod(method, typeMap);
            return SubstituteFields(otherInstance, typeMap);
        }

        internal BoundExpression ToBoundExpression(SyntaxNode syntax)
        {
            var expr = this.DisplayClassInstance.ToBoundExpression(syntax);
            var fields = ArrayBuilder<FieldSymbol>.GetInstance();
            fields.AddRange(this.DisplayClassFields);
            fields.ReverseContents();
            foreach (var field in fields)
            {
                expr = new BoundFieldAccess(syntax, expr, field, constantValueOpt: null) { WasCompilerGenerated = true };
            }
            fields.Free();
            return expr;
        }

        internal DisplayClassVariable SubstituteFields(DisplayClassInstance otherInstance, TypeMap typeMap)
        {
            var otherFields = SubstituteFields(this.DisplayClassFields, typeMap);
            return new DisplayClassVariable(this.Name, this.Kind, otherInstance, otherFields);
        }

        private static ConsList<FieldSymbol> SubstituteFields(ConsList<FieldSymbol> fields, TypeMap typeMap)
        {
            if (!fields.Any())
            {
                return ConsList<FieldSymbol>.Empty;
            }

            var head = SubstituteField(fields.Head, typeMap);
            var tail = SubstituteFields(fields.Tail, typeMap);
            return tail.Prepend(head);
        }

        private static FieldSymbol SubstituteField(FieldSymbol field, TypeMap typeMap)
        {
            Debug.Assert(!field.IsStatic);
            Debug.Assert(!field.IsReadOnly || GeneratedNames.GetKind(field.Name) == GeneratedNameKind.AnonymousTypeField);
            Debug.Assert(field.CustomModifiers.Length == 0);
            // CONSIDER: Instead of digging fields out of the unsubstituted type and then performing substitution
            // on each one individually, we could dig fields out of the substituted type.
            return new EEDisplayClassFieldSymbol(typeMap.SubstituteNamedType(field.ContainingType), field.Name, typeMap.SubstituteType(field.Type).Type);
        }

        private sealed class EEDisplayClassFieldSymbol : FieldSymbol
        {
            private readonly NamedTypeSymbol _container;
            private readonly string _name;
            private readonly TypeSymbol _type;

            internal EEDisplayClassFieldSymbol(NamedTypeSymbol container, string name, TypeSymbol type)
            {
                _container = container;
                _name = name;
                _type = type;
            }

            public override Symbol AssociatedSymbol
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            public override Symbol ContainingSymbol
            {
                get { return _container; }
            }

            public override ImmutableArray<CustomModifier> CustomModifiers
            {
                get { return ImmutableArray<CustomModifier>.Empty; }
            }

            public override Accessibility DeclaredAccessibility
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            public override bool IsConst
            {
                get { return false; }
            }

            public override bool IsReadOnly
            {
                get { return false; }
            }

            public override bool IsStatic
            {
                get { return false; }
            }

            public override bool IsVolatile
            {
                get { return false; }
            }

            public override ImmutableArray<Location> Locations
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            public override string Name
            {
                get { return _name; }
            }

            internal override bool HasRuntimeSpecialName
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            internal override bool HasSpecialName
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            internal override bool IsNotSerialized
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            internal override MarshalPseudoCustomAttributeData MarshallingInformation
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            internal override ObsoleteAttributeData ObsoleteAttributeData
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            internal override int? TypeLayoutOffset
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
            {
                throw ExceptionUtilities.Unreachable;
            }

            internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
            {
                return _type;
            }
        }
    }
}
