// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class SourceMemberFieldSymbol : SourceFieldSymbolWithSyntaxReference
    {
        private readonly DeclarationModifiers _modifiers;

        internal SourceMemberFieldSymbol(
            SourceMemberContainerTypeSymbol containingType,
            DeclarationModifiers modifiers,
            string name,
            SyntaxReference syntax,
            Location location)
            : base(containingType, name, syntax, location)
        {
            _modifiers = modifiers;
        }

        protected sealed override DeclarationModifiers Modifiers
        {
            get
            {
                return _modifiers;
            }
        }

        protected abstract TypeSyntax TypeSyntax { get; }

        protected abstract SyntaxTokenList ModifiersTokenList { get; }

        protected void TypeChecks(TypeSymbol type, DiagnosticBag diagnostics)
        {
            if (type.IsStatic)
            {
                // Cannot declare a variable of static type '{0}'
                diagnostics.Add(ErrorCode.ERR_VarDeclIsStaticClass, this.ErrorLocation, type);
            }
            else if (type.IsVoidType())
            {
                diagnostics.Add(ErrorCode.ERR_FieldCantHaveVoidType, TypeSyntax?.Location ?? this.Locations[0]);
            }
            else if (type.IsRestrictedType(ignoreSpanLikeTypes: true))
            {
                diagnostics.Add(ErrorCode.ERR_FieldCantBeRefAny, TypeSyntax?.Location ?? this.Locations[0], type);
            }
            else if (type.IsRefLikeType && (this.IsStatic || !containingType.IsRefLikeType))
            {
                diagnostics.Add(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, TypeSyntax?.Location ?? this.Locations[0], type);
            }
            else if (IsConst && !type.CanBeConst())
            {
                SyntaxToken constToken = default(SyntaxToken);
                foreach (var modifier in ModifiersTokenList)
                {
                    if (modifier.Kind() == SyntaxKind.ConstKeyword)
                    {
                        constToken = modifier;
                        break;
                    }
                }
                Debug.Assert(constToken.Kind() == SyntaxKind.ConstKeyword);

                diagnostics.Add(ErrorCode.ERR_BadConstType, constToken.GetLocation(), type);
            }
            else if (IsVolatile && !type.IsValidVolatileFieldType())
            {
                // '{0}': a volatile field cannot be of the type '{1}'
                diagnostics.Add(ErrorCode.ERR_VolatileStruct, this.ErrorLocation, this, type);
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            if (!this.IsNoMoreVisibleThan(type, ref useSiteDiagnostics))
            {
                // Inconsistent accessibility: field type '{1}' is less accessible than field '{0}'
                diagnostics.Add(ErrorCode.ERR_BadVisFieldType, this.ErrorLocation, this, type);
            }

            diagnostics.Add(this.ErrorLocation, useSiteDiagnostics);
        }

        public abstract bool HasInitializer { get; }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;
            var value = this.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false);

            // Synthesize DecimalConstantAttribute when the default value is of type decimal
            if (this is
            {
                IsConst: true,
                Type: { SpecialType: SpecialType.System_Decimal }
            } && value is { })
            {
                var data = GetDecodedWellKnownAttributeData();

                if (data == null || data.ConstValue == CodeAnalysis.ConstantValue.Unset)
                {
                    AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDecimalConstantAttribute(value.DecimalValue));
                }
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return null;
            }
        }

        public override int FixedSize
        {
            get
            {
                Debug.Assert(!this.IsFixedSizeBuffer, "Subclasses representing fixed fields must override");
                state.NotePartComplete(CompletionPart.FixedSize);
                return 0;
            }
        }

        internal static DeclarationModifiers MakeModifiers(NamedTypeSymbol containingType, SyntaxToken firstIdentifier, SyntaxTokenList modifiers, DiagnosticBag diagnostics, out bool modifierErrors)
        {
            DeclarationModifiers defaultAccess =
                (containingType.IsInterface) ? DeclarationModifiers.Public : DeclarationModifiers.Private;

            DeclarationModifiers allowedModifiers =
                DeclarationModifiers.AccessibilityMask |
                DeclarationModifiers.Const |
                DeclarationModifiers.New |
                DeclarationModifiers.ReadOnly |
                DeclarationModifiers.Static |
                DeclarationModifiers.Volatile |
                DeclarationModifiers.Fixed |
                DeclarationModifiers.Unsafe |
                DeclarationModifiers.Abstract; // filtered out later

            var errorLocation = new SourceLocation(firstIdentifier);
            DeclarationModifiers result = ModifierUtils.MakeAndCheckNontypeMemberModifiers(
                modifiers, defaultAccess, allowedModifiers, errorLocation, diagnostics, out modifierErrors);

            if ((result & DeclarationModifiers.Abstract) != 0)
            {
                diagnostics.Add(ErrorCode.ERR_AbstractField, errorLocation);
                result &= ~DeclarationModifiers.Abstract;
            }

            if ((result & DeclarationModifiers.Fixed) != 0)
            {
                if ((result & DeclarationModifiers.Static) != 0)
                {
                    // The modifier 'static' is not valid for this item
                    diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, SyntaxFacts.GetText(SyntaxKind.StaticKeyword));
                }

                if ((result & DeclarationModifiers.ReadOnly) != 0)
                {
                    // The modifier 'readonly' is not valid for this item
                    diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, SyntaxFacts.GetText(SyntaxKind.ReadOnlyKeyword));
                }

                if ((result & DeclarationModifiers.Const) != 0)
                {
                    // The modifier 'const' is not valid for this item
                    diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, SyntaxFacts.GetText(SyntaxKind.ConstKeyword));
                }

                if ((result & DeclarationModifiers.Volatile) != 0)
                {
                    // The modifier 'volatile' is not valid for this item
                    diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, SyntaxFacts.GetText(SyntaxKind.VolatileKeyword));
                }

                result &= ~(DeclarationModifiers.Static | DeclarationModifiers.ReadOnly | DeclarationModifiers.Const | DeclarationModifiers.Volatile);
                Debug.Assert((result & ~(DeclarationModifiers.AccessibilityMask | DeclarationModifiers.Fixed | DeclarationModifiers.Unsafe | DeclarationModifiers.New)) == 0);
            }


            if ((result & DeclarationModifiers.Const) != 0)
            {
                if ((result & DeclarationModifiers.Static) != 0)
                {
                    // The constant '{0}' cannot be marked static
                    diagnostics.Add(ErrorCode.ERR_StaticConstant, errorLocation, firstIdentifier.ValueText);
                }

                if ((result & DeclarationModifiers.ReadOnly) != 0)
                {
                    // The modifier 'readonly' is not valid for this item
                    diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, SyntaxFacts.GetText(SyntaxKind.ReadOnlyKeyword));
                }

                if ((result & DeclarationModifiers.Volatile) != 0)
                {
                    // The modifier 'volatile' is not valid for this item
                    diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, SyntaxFacts.GetText(SyntaxKind.VolatileKeyword));
                }

                if ((result & DeclarationModifiers.Unsafe) != 0)
                {
                    // The modifier 'unsafe' is not valid for this item
                    diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, SyntaxFacts.GetText(SyntaxKind.UnsafeKeyword));
                }

                result |= DeclarationModifiers.Static; // "constants are considered static members"
            }
            else
            {
                // NOTE: always cascading on a const, so suppress.
                // NOTE: we're being a bit sneaky here - we're using the containingType rather than this symbol
                // to determine whether or not unsafe is allowed.  Since this symbol and the containing type are
                // in the same compilation, it won't make a difference.  We do, however, have to pass the error
                // location explicitly.
                containingType.CheckUnsafeModifier(result, errorLocation, diagnostics);
            }

            return result;
        }

        internal sealed override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var incompletePart = state.NextIncompletePart;
                switch (incompletePart)
                {
                    case CompletionPart.Attributes:
                        GetAttributes();
                        break;

                    case CompletionPart.Type:
                        GetFieldType(ConsList<FieldSymbol>.Empty);
                        break;

                    case CompletionPart.FixedSize:
                        int discarded = this.FixedSize;
                        break;

                    case CompletionPart.ConstantValue:
                        GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false);
                        break;

                    case CompletionPart.None:
                        return;

                    default:
                        // any other values are completion parts intended for other kinds of symbols
                        state.NotePartComplete(CompletionPart.All & ~CompletionPart.FieldSymbolAll);
                        break;
                }

                state.SpinWaitComplete(incompletePart, cancellationToken);
            }
        }

        internal override NamedTypeSymbol FixedImplementationType(PEModuleBuilder emitModule)
        {
            Debug.Assert(!this.IsFixedSizeBuffer, "Subclasses representing fixed fields must override");
            return null;
        }
    }

    internal class SourceMemberFieldSymbolFromDeclarator : SourceMemberFieldSymbol
    {
        private readonly bool _hasInitializer;

        private TypeWithAnnotations.Boxed _lazyType;

        // Non-zero if the type of the field has been inferred from the type of its initializer expression
        // and the errors of binding the initializer have been or are being reported to compilation diagnostics.
        private int _lazyFieldTypeInferred;

        internal SourceMemberFieldSymbolFromDeclarator(
            SourceMemberContainerTypeSymbol containingType,
            VariableDeclaratorSyntax declarator,
            DeclarationModifiers modifiers,
            bool modifierErrors,
            DiagnosticBag diagnostics)
            : base(containingType, modifiers, declarator.Identifier.ValueText, declarator.GetReference(), declarator.Identifier.GetLocation())
        {
            _hasInitializer = declarator.Initializer != null;

            this.CheckAccessibility(diagnostics);

            if (!modifierErrors)
            {
                this.ReportModifiersDiagnostics(diagnostics);
            }

            if (containingType.IsInterface)
            {
                if (this.IsStatic)
                {
                    Binder.CheckFeatureAvailability(declarator, MessageID.IDS_DefaultInterfaceImplementation, diagnostics, ErrorLocation);

                    if (!ContainingAssembly.RuntimeSupportsDefaultInterfaceImplementation)
                    {
                        diagnostics.Add(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, ErrorLocation);
                    }
                }
                else
                {
                    diagnostics.Add(ErrorCode.ERR_InterfacesCantContainFields, ErrorLocation);
                }
            }
        }

        protected sealed override TypeSyntax TypeSyntax
        {
            get
            {
                return GetFieldDeclaration(VariableDeclaratorNode).Declaration.Type;
            }
        }

        protected sealed override SyntaxTokenList ModifiersTokenList
        {
            get
            {
                return GetFieldDeclaration(VariableDeclaratorNode).Modifiers;
            }
        }

        public sealed override bool HasInitializer
        {
            get { return _hasInitializer; }
        }

        protected VariableDeclaratorSyntax VariableDeclaratorNode
        {
            get
            {
                return (VariableDeclaratorSyntax)this.SyntaxNode;
            }
        }

        private static BaseFieldDeclarationSyntax GetFieldDeclaration(CSharpSyntaxNode declarator)
        {
            return (BaseFieldDeclarationSyntax)declarator.Parent.Parent;
        }

        protected override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
        {
            get
            {
                if (this.containingType.AnyMemberHasAttributes)
                {
                    return GetFieldDeclaration(this.SyntaxNode).AttributeLists;
                }

                return default(SyntaxList<AttributeListSyntax>);
            }
        }

        internal override bool HasPointerType
        {
            get
            {
                if (_lazyType != null)
                {
                    Debug.Assert(_lazyType.Value.DefaultType.IsPointerType() ==
                        IsPointerFieldSyntactically());

                    return _lazyType.Value.DefaultType.IsPointerType();
                }

                return IsPointerFieldSyntactically();
            }
        }

        private bool IsPointerFieldSyntactically()
        {
            var declaration = GetFieldDeclaration(VariableDeclaratorNode).Declaration;
            if (declaration.Type.Kind() == SyntaxKind.PointerType)
            {
                // public int * Blah;   // pointer
                return true;
            }

            foreach (var singleVariable in declaration.Variables)
            {
                var argList = singleVariable.ArgumentList;
                if (argList != null && argList.Arguments.Count != 0)
                {
                    // public int Blah[10];     // fixed buffer
                    return true;
                }
            }

            return false;
        }

        internal sealed override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            Debug.Assert(fieldsBeingBound != null);

            if (_lazyType != null)
            {
                return _lazyType.Value;
            }

            var declarator = VariableDeclaratorNode;
            var fieldSyntax = GetFieldDeclaration(declarator);
            var typeSyntax = fieldSyntax.Declaration.Type;

            var compilation = this.DeclaringCompilation;

            var diagnostics = DiagnosticBag.GetInstance();
            TypeWithAnnotations type;

            // When we have multiple declarators, we report the type diagnostics on only the first.
            DiagnosticBag diagnosticsForFirstDeclarator = DiagnosticBag.GetInstance();

            Symbol associatedPropertyOrEvent = this.AssociatedSymbol;
            if ((object)associatedPropertyOrEvent != null && associatedPropertyOrEvent.Kind == SymbolKind.Event)
            {
                EventSymbol @event = (EventSymbol)associatedPropertyOrEvent;
                if (@event.IsWindowsRuntimeEvent)
                {
                    NamedTypeSymbol tokenTableType = this.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T);
                    Binder.ReportUseSiteDiagnostics(tokenTableType, diagnosticsForFirstDeclarator, this.ErrorLocation);

                    // CONSIDER: Do we want to guard against the possibility that someone has created their own EventRegistrationTokenTable<T>
                    // type that has additional generic constraints?
                    type = TypeWithAnnotations.Create(tokenTableType.Construct(ImmutableArray.Create(@event.TypeWithAnnotations)));
                }
                else
                {
                    type = @event.TypeWithAnnotations;
                }
            }
            else
            {
                var binderFactory = compilation.GetBinderFactory(SyntaxTree);
                var binder = binderFactory.GetBinder(typeSyntax);

                binder = binder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);
                if (!ContainingType.IsScriptClass)
                {
                    type = binder.BindType(typeSyntax, diagnosticsForFirstDeclarator);
                }
                else
                {
                    bool isVar;
                    type = binder.BindTypeOrVarKeyword(typeSyntax, diagnostics, out isVar);

                    Debug.Assert(type.HasType || isVar);

                    if (isVar)
                    {
                        if (this.IsConst)
                        {
                            diagnosticsForFirstDeclarator.Add(ErrorCode.ERR_ImplicitlyTypedVariableCannotBeConst, typeSyntax.Location);
                        }

                        if (fieldsBeingBound.ContainsReference(this))
                        {
                            diagnostics.Add(ErrorCode.ERR_RecursivelyTypedVariable, this.ErrorLocation, this);
                            type = default;
                        }
                        else if (fieldSyntax.Declaration.Variables.Count > 1)
                        {
                            diagnosticsForFirstDeclarator.Add(ErrorCode.ERR_ImplicitlyTypedVariableMultipleDeclarator, typeSyntax.Location);
                        }
                        else if (this.IsConst && this.ContainingType.IsScriptClass)
                        {
                            // For const var in script, we won't try to bind the initializer (case below), as it can lead to an unbound recursion
                            type = default;
                        }
                        else
                        {
                            fieldsBeingBound = new ConsList<FieldSymbol>(this, fieldsBeingBound);

                            var initializerBinder = new ImplicitlyTypedFieldBinder(binder, fieldsBeingBound);
                            var initializerOpt = initializerBinder.BindInferredVariableInitializer(diagnostics, RefKind.None, (EqualsValueClauseSyntax)declarator.Initializer, declarator);

                            if (initializerOpt != null)
                            {
                                if ((object)initializerOpt.Type != null && !initializerOpt.Type.IsErrorType())
                                {
                                    type = TypeWithAnnotations.Create(initializerOpt.Type);
                                }

                                _lazyFieldTypeInferred = 1;
                            }
                        }

                        if (!type.HasType)
                        {
                            type = TypeWithAnnotations.Create(binder.CreateErrorType("var"));
                        }
                    }
                }

                if (IsFixedSizeBuffer)
                {
                    type = TypeWithAnnotations.Create(new PointerTypeSymbol(type));

                    if (ContainingType.TypeKind != TypeKind.Struct)
                    {
                        diagnostics.Add(ErrorCode.ERR_FixedNotInStruct, ErrorLocation);
                    }

                    var elementType = ((PointerTypeSymbol)type.Type).PointedAtType;
                    int elementSize = elementType.FixedBufferElementSizeInBytes();
                    if (elementSize == 0)
                    {
                        var loc = typeSyntax.Location;
                        diagnostics.Add(ErrorCode.ERR_IllegalFixedType, loc);
                    }

                    if (!binder.InUnsafeRegion)
                    {
                        diagnosticsForFirstDeclarator.Add(ErrorCode.ERR_UnsafeNeeded, declarator.Location);
                    }
                }
            }

            // update the lazyType only if it contains value last seen by the current thread:
            if (Interlocked.CompareExchange(ref _lazyType, new TypeWithAnnotations.Boxed(type.WithModifiers(this.RequiredCustomModifiers)), null) == null)
            {
                TypeChecks(type.Type, diagnostics);

                // CONSIDER: SourceEventFieldSymbol would like to suppress these diagnostics.
                compilation.DeclarationDiagnostics.AddRange(diagnostics);

                bool isFirstDeclarator = fieldSyntax.Declaration.Variables[0] == declarator;
                if (isFirstDeclarator)
                {
                    compilation.DeclarationDiagnostics.AddRange(diagnosticsForFirstDeclarator);
                }

                state.NotePartComplete(CompletionPart.Type);
            }

            diagnostics.Free();
            diagnosticsForFirstDeclarator.Free();
            return _lazyType.Value;
        }

        internal bool FieldTypeInferred(ConsList<FieldSymbol> fieldsBeingBound)
        {
            if (!ContainingType.IsScriptClass)
            {
                return false;
            }

            GetFieldType(fieldsBeingBound);

            // lazyIsImplicitlyTypedField can only transition from value 0 to 1:
            return _lazyFieldTypeInferred != 0 || Volatile.Read(ref _lazyFieldTypeInferred) != 0;
        }

        protected sealed override ConstantValue MakeConstantValue(HashSet<SourceFieldSymbolWithSyntaxReference> dependencies, bool earlyDecodingWellKnownAttributes, DiagnosticBag diagnostics)
        {
            if (!this.IsConst || VariableDeclaratorNode.Initializer == null)
            {
                return null;
            }

            return ConstantValueUtils.EvaluateFieldConstant(this, (EqualsValueClauseSyntax)VariableDeclaratorNode.Initializer, dependencies, earlyDecodingWellKnownAttributes, diagnostics);
        }

        internal override bool IsDefinedInSourceTree(SyntaxTree tree, TextSpan? definedWithinSpan, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.SyntaxTree == tree)
            {
                if (!definedWithinSpan.HasValue)
                {
                    return true;
                }

                var fieldDeclaration = GetFieldDeclaration(this.SyntaxNode);
                return fieldDeclaration.SyntaxTree.HasCompilationUnitRoot && fieldDeclaration.Span.IntersectsWith(definedWithinSpan.Value);
            }

            return false;
        }

        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, DiagnosticBag diagnostics)
        {
            // This check prevents redundant ManagedAddr diagnostics on the underlying pointer field of a fixed-size buffer
            if (!IsFixedSizeBuffer)
            {
                Type.CheckAllConstraints(DeclaringCompilation, conversions, ErrorLocation, diagnostics);
            }

            base.AfterAddingTypeMembersChecks(conversions, diagnostics);
        }
    }
}
