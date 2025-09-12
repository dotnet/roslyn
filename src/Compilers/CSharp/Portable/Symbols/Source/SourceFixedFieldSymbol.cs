// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class SourceFixedFieldSymbol : SourceMemberFieldSymbolFromDeclarator
    {
        private const int FixedSizeNotInitialized = -1;

        // In a fixed-size field declaration, stores the fixed size of the buffer
        private int _fixedSize = FixedSizeNotInitialized;

        internal SourceFixedFieldSymbol(
            SourceMemberContainerTypeSymbol containingType,
            VariableDeclaratorSyntax declarator,
            DeclarationModifiers modifiers,
            bool modifierErrors,
            BindingDiagnosticBag diagnostics)
            : base(containingType, declarator, modifiers, modifierErrors, diagnostics)
        {
            // Checked in parser: a fixed field declaration requires a length in square brackets

            Debug.Assert(this.IsFixedSizeBuffer);
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;
            var systemType = compilation.GetWellKnownType(WellKnownType.System_Type);
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);
            var item1 = new TypedConstant(systemType, TypedConstantKind.Type, ((PointerTypeSymbol)this.Type).PointedAtType);
            var item2 = new TypedConstant(intType, TypedConstantKind.Primitive, this.FixedSize);
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(
                WellKnownMember.System_Runtime_CompilerServices_FixedBufferAttribute__ctor,
                ImmutableArray.Create<TypedConstant>(item1, item2)));
        }

        public sealed override int FixedSize
        {
            get
            {
                if (_fixedSize == FixedSizeNotInitialized)
                {
                    BindingDiagnosticBag diagnostics = BindingDiagnosticBag.GetInstance();
                    int size = 0;

                    VariableDeclaratorSyntax declarator = VariableDeclaratorNode;
                    if (declarator.ArgumentList == null)
                    {
                        // Diagnostic reported by parser.
                    }
                    else
                    {
                        SeparatedSyntaxList<ArgumentSyntax> arguments = declarator.ArgumentList.Arguments;

                        if (arguments.Count == 0 || arguments[0].Expression.Kind() == SyntaxKind.OmittedArraySizeExpression)
                        {
                            Debug.Assert(declarator.ArgumentList.ContainsDiagnostics, "The parser should have caught this.");
                        }
                        else
                        {
                            if (arguments.Count > 1)
                            {
                                diagnostics.Add(ErrorCode.ERR_FixedBufferTooManyDimensions, declarator.ArgumentList.Location);
                            }

                            ExpressionSyntax sizeExpression = arguments[0].Expression;

                            BinderFactory binderFactory = this.DeclaringCompilation.GetBinderFactory(SyntaxTree);
                            Binder binder = binderFactory.GetBinder(sizeExpression);
                            binder = new ExecutableCodeBinder(sizeExpression, binder.ContainingMemberOrLambda, binder).GetBinder(sizeExpression);

                            TypeSymbol intType = binder.GetSpecialType(SpecialType.System_Int32, diagnostics, sizeExpression);
                            BoundExpression boundSizeExpression = binder.GenerateConversionForAssignment(
                                intType,
                                binder.BindValue(sizeExpression, diagnostics, Binder.BindValueKind.RValue),
                                diagnostics);

                            // GetAndValidateConstantValue doesn't generate a very intuitive-reading diagnostic
                            // for this situation, but this is what the Dev10 compiler produces.
                            ConstantValue sizeConstant = ConstantValueUtils.GetAndValidateConstantValue(boundSizeExpression, this, intType, sizeExpression, diagnostics);

                            Debug.Assert(sizeConstant != null);
                            Debug.Assert(sizeConstant.IsIntegral || diagnostics.HasAnyErrors() || sizeExpression.HasErrors);

                            if (sizeConstant.IsIntegral)
                            {
                                int int32Value = sizeConstant.Int32Value;
                                if (int32Value > 0)
                                {
                                    size = int32Value;

                                    TypeSymbol elementType = ((PointerTypeSymbol)this.Type).PointedAtType;
                                    int elementSize = elementType.FixedBufferElementSizeInBytes();
                                    long totalSize = elementSize * 1L * int32Value;
                                    if (totalSize > int.MaxValue)
                                    {
                                        // Fixed size buffer of length '{0}' and type '{1}' is too big
                                        diagnostics.Add(ErrorCode.ERR_FixedOverflow, sizeExpression.Location, int32Value, elementType);
                                    }
                                }
                                else
                                {
                                    diagnostics.Add(ErrorCode.ERR_InvalidFixedArraySize, sizeExpression.Location);
                                }
                            }
                        }
                    }

                    // Winner writes diagnostics.
                    if (Interlocked.CompareExchange(ref _fixedSize, size, FixedSizeNotInitialized) == FixedSizeNotInitialized)
                    {
                        this.AddDeclarationDiagnostics(diagnostics);
                        state.NotePartComplete(CompletionPart.FixedSize);
                    }

                    diagnostics.Free();
                }

                Debug.Assert(_fixedSize != FixedSizeNotInitialized);
                return _fixedSize;
            }
        }

        internal override NamedTypeSymbol FixedImplementationType(PEModuleBuilder emitModule)
        {
            return emitModule.SetFixedImplementationType(this);
        }
    }

    internal sealed class FixedFieldImplementationType : SynthesizedContainer
    {
        internal const string FixedElementFieldName = "FixedElementField";

        private readonly SourceMemberFieldSymbol _field;
        private readonly MethodSymbol _constructor;
        private readonly FieldSymbol _internalField;

        public FixedFieldImplementationType(SourceMemberFieldSymbol field)
            : base(GeneratedNames.MakeFixedFieldImplementationName(field.Name))
        {
            _field = field;
            _constructor = new SynthesizedInstanceConstructor(this);
            _internalField = new SynthesizedFieldSymbol(this, ((PointerTypeSymbol)field.Type).PointedAtType, FixedElementFieldName, DeclarationModifiers.Public);
        }

        public override Symbol ContainingSymbol
        {
            get { return _field.ContainingType; }
        }

        public override TypeKind TypeKind
        {
            get { return TypeKind.Struct; }
        }

        internal override MethodSymbol Constructor
        {
            get { return _constructor; }
        }

        internal override TypeLayout Layout
        {
            get
            {
                int nElements = _field.FixedSize;
                var elementType = ((PointerTypeSymbol)_field.Type).PointedAtType;
                int elementSize = elementType.FixedBufferElementSizeInBytes();
                const int alignment = 0;
                int totalSize = nElements * elementSize;
                const LayoutKind layoutKind = LayoutKind.Sequential;
                return new TypeLayout(layoutKind, totalSize, alignment);
            }
        }

        internal override CharSet MarshallingCharSet
        {
            get
            {
                // We manually propagate the CharSet field of StructLayout attribute for fabricated structs implementing fixed buffers.
                // See void AttrBind::EmitStructLayoutAttributeCharSet(AttributeNode *attr) in native codebase.
                return _field.ContainingType.MarshallingCharSet;
            }
        }
        internal override FieldSymbol FixedElementField
        {
            get { return _internalField; }
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);
            var compilation = ContainingSymbol.DeclaringCompilation;
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_UnsafeValueTypeAttribute__ctor));
        }

        public override IEnumerable<string> MemberNames
        {
            get { return SpecializedCollections.SingletonEnumerable(FixedElementFieldName); }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            return ImmutableArray.Create<Symbol>(_constructor, _internalField);
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return
                (name == _constructor.Name) ? ImmutableArray.Create<Symbol>(_constructor) :
                (name == FixedElementFieldName) ? ImmutableArray.Create<Symbol>(_internalField) :
                ImmutableArray<Symbol>.Empty;
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Public; }
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
            => ContainingAssembly.GetSpecialType(SpecialType.System_ValueType);

        public sealed override bool AreLocalsZeroed
            => throw ExceptionUtilities.Unreachable();

        internal override bool IsRecord => false;
        internal override bool IsRecordStruct => false;
        internal override bool HasPossibleWellKnownCloneMethod() => false;
    }
}
