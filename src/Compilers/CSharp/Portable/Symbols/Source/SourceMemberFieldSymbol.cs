// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            TextSpan locationSpan)
            : base(containingType, name, syntax, locationSpan)
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

        protected void TypeChecks(TypeSymbol type, BindingDiagnosticBag diagnostics)
        {
            if (type.HasFileLocalTypes() && !ContainingType.HasFileLocalTypes())
            {
                diagnostics.Add(ErrorCode.ERR_FileTypeDisallowedInSignature, this.ErrorLocation, type, ContainingType);
            }
            else if (type.IsStatic)
            {
                // Cannot declare a variable of static type '{0}'
                diagnostics.Add(ErrorCode.ERR_VarDeclIsStaticClass, this.ErrorLocation, type);
            }
            else if (type.IsVoidType())
            {
                diagnostics.Add(ErrorCode.ERR_FieldCantHaveVoidType, TypeSyntax?.Location ?? this.GetFirstLocation());
            }
            else if (type.IsRestrictedType(ignoreSpanLikeTypes: true))
            {
                diagnostics.Add(ErrorCode.ERR_FieldCantBeRefAny, TypeSyntax?.Location ?? this.GetFirstLocation(), type);
            }
            else if (type.IsRefLikeType && (this.IsStatic || !containingType.IsRefLikeType))
            {
                diagnostics.Add(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, TypeSyntax?.Location ?? this.GetFirstLocation(), type);
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

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);
            if (!this.IsNoMoreVisibleThan(type, ref useSiteInfo))
            {
                // Inconsistent accessibility: field type '{1}' is less accessible than field '{0}'
                diagnostics.Add(ErrorCode.ERR_BadVisFieldType, this.ErrorLocation, this, type);
            }

            diagnostics.Add(this.ErrorLocation, useSiteInfo);
        }

        public abstract bool HasInitializer { get; }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;
            var value = this.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false);

            // Synthesize DecimalConstantAttribute when the default value is of type decimal
            if (this.IsConst && value != null
                && this.Type.SpecialType == SpecialType.System_Decimal)
            {
                var data = GetDecodedWellKnownAttributeData();

                if (data == null || data.ConstValue == CodeAnalysis.ConstantValue.Unset)
                {
                    AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDecimalConstantAttribute(value.DecimalValue));
                }
            }

            // Synthesize RequiredMemberAttribute if this field is required
            if (IsRequired)
            {
                AddSynthesizedAttribute(
                    ref attributes,
                    this.DeclaringCompilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_RequiredMemberAttribute__ctor));
            }
        }

        internal override void PostDecodeWellKnownAttributes(ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> allAttributeSyntaxNodes, BindingDiagnosticBag diagnostics, AttributeLocation symbolPart, WellKnownAttributeData decodedData)
        {
            base.PostDecodeWellKnownAttributes(boundAttributes, allAttributeSyntaxNodes, diagnostics, symbolPart, decodedData);

            // Ensure availability of `DecimalConstantAttribute`.
            if (IsConst && Type.SpecialType == SpecialType.System_Decimal &&
                GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false) is { } value &&
                !(decodedData is FieldWellKnownAttributeData fieldData && fieldData.ConstValue != CodeAnalysis.ConstantValue.Unset))
            {
                Binder.ReportUseSiteDiagnosticForSynthesizedAttribute(DeclaringCompilation,
                    WellKnownMember.System_Runtime_CompilerServices_DecimalConstantAttribute__ctor,
                    diagnostics,
                    syntax: SyntaxNode);
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

        internal static DeclarationModifiers MakeModifiers(NamedTypeSymbol containingType, SyntaxToken firstIdentifier, SyntaxTokenList modifiers, bool isRefField, BindingDiagnosticBag diagnostics, out bool modifierErrors)
        {
            bool isInterface = containingType.IsInterface;
            DeclarationModifiers defaultAccess =
                isInterface ? DeclarationModifiers.Public : DeclarationModifiers.Private;

            DeclarationModifiers allowedModifiers =
                DeclarationModifiers.AccessibilityMask |
                DeclarationModifiers.Const |
                DeclarationModifiers.New |
                DeclarationModifiers.ReadOnly |
                DeclarationModifiers.Static |
                DeclarationModifiers.Volatile |
                DeclarationModifiers.Fixed |
                DeclarationModifiers.Unsafe |
                DeclarationModifiers.Abstract |
                DeclarationModifiers.Required; // Some of these are filtered out later, when illegal, for better error messages.

            var errorLocation = new SourceLocation(firstIdentifier);
            DeclarationModifiers result = ModifierUtils.MakeAndCheckNonTypeMemberModifiers(
                isOrdinaryMethod: false, isForInterfaceMember: isInterface,
                modifiers, defaultAccess, allowedModifiers, errorLocation, diagnostics, out modifierErrors);

            if ((result & DeclarationModifiers.Abstract) != 0)
            {
                diagnostics.Add(ErrorCode.ERR_AbstractField, errorLocation);
                result &= ~DeclarationModifiers.Abstract;
            }

            if ((result & DeclarationModifiers.Fixed) != 0)
            {
                foreach (var modifier in modifiers)
                {
                    if (modifier.IsKind(SyntaxKind.FixedKeyword))
                        MessageID.IDS_FeatureFixedBuffer.CheckFeatureAvailability(diagnostics, modifier);
                }

                reportBadMemberFlagIfAny(result, DeclarationModifiers.Static, diagnostics, errorLocation);
                reportBadMemberFlagIfAny(result, DeclarationModifiers.ReadOnly, diagnostics, errorLocation);
                reportBadMemberFlagIfAny(result, DeclarationModifiers.Const, diagnostics, errorLocation);
                reportBadMemberFlagIfAny(result, DeclarationModifiers.Volatile, diagnostics, errorLocation);
                reportBadMemberFlagIfAny(result, DeclarationModifiers.Required, diagnostics, errorLocation);

                result &= ~(DeclarationModifiers.Static | DeclarationModifiers.ReadOnly | DeclarationModifiers.Const | DeclarationModifiers.Volatile | DeclarationModifiers.Required);
                Debug.Assert((result & ~(DeclarationModifiers.AccessibilityMask | DeclarationModifiers.Fixed | DeclarationModifiers.Unsafe | DeclarationModifiers.New)) == 0);
            }

            if ((result & DeclarationModifiers.Const) != 0)
            {
                if ((result & DeclarationModifiers.Static) != 0)
                {
                    // The constant '{0}' cannot be marked static
                    diagnostics.Add(ErrorCode.ERR_StaticConstant, errorLocation, firstIdentifier.ValueText);
                }

                reportBadMemberFlagIfAny(result, DeclarationModifiers.ReadOnly, diagnostics, errorLocation);
                reportBadMemberFlagIfAny(result, DeclarationModifiers.Volatile, diagnostics, errorLocation);
                reportBadMemberFlagIfAny(result, DeclarationModifiers.Unsafe, diagnostics, errorLocation);

                if (reportBadMemberFlagIfAny(result, DeclarationModifiers.Required, diagnostics, errorLocation))
                {
                    result &= ~DeclarationModifiers.Required;
                }

                result |= DeclarationModifiers.Static; // "constants are considered static members"
            }
            else
            {
                if ((result & DeclarationModifiers.Static) != 0 && (result & DeclarationModifiers.Required) != 0)
                {
                    // The modifier 'required' is not valid for this item
                    diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, SyntaxFacts.GetText(SyntaxKind.RequiredKeyword));
                    result &= ~DeclarationModifiers.Required;
                }

                // NOTE: always cascading on a const, so suppress.
                // NOTE: we're being a bit sneaky here - we're using the containingType rather than this symbol
                // to determine whether or not unsafe is allowed.  Since this symbol and the containing type are
                // in the same compilation, it won't make a difference.  We do, however, have to pass the error
                // location explicitly.
                containingType.CheckUnsafeModifier(result, errorLocation, diagnostics);
            }

            if (isRefField)
            {
                reportBadMemberFlagIfAny(result, DeclarationModifiers.Static, diagnostics, errorLocation);
                reportBadMemberFlagIfAny(result, DeclarationModifiers.Const, diagnostics, errorLocation);
                reportBadMemberFlagIfAny(result, DeclarationModifiers.Volatile, diagnostics, errorLocation);
            }

            return result;

            static bool reportBadMemberFlagIfAny(DeclarationModifiers result, DeclarationModifiers modifier, BindingDiagnosticBag diagnostics, SourceLocation errorLocation)
            {
                if ((result & modifier) != 0)
                {
                    // The modifier '{0}' is not valid for this item
                    diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, ModifierUtils.ConvertSingleModifierToSyntaxText(modifier));
                    return true;
                }
                return false;
            }
        }

#nullable enable
        internal sealed override void ForceComplete(SourceLocation? locationOpt, Predicate<Symbol>? filter, CancellationToken cancellationToken)
        {
            if (filter?.Invoke(this) == false)
            {
                return;
            }

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
#nullable disable

        internal override NamedTypeSymbol FixedImplementationType(PEModuleBuilder emitModule)
        {
            Debug.Assert(!this.IsFixedSizeBuffer, "Subclasses representing fixed fields must override");
            return null;
        }
    }

    internal class SourceMemberFieldSymbolFromDeclarator : SourceMemberFieldSymbol
    {
        private readonly bool _hasInitializer;

        private sealed class TypeAndRefKind
        {
            internal readonly RefKind RefKind;
            internal readonly TypeWithAnnotations Type;

            internal TypeAndRefKind(RefKind refKind, TypeWithAnnotations type)
            {
                RefKind = refKind;
                Type = type;
            }
        }

        private TypeAndRefKind _lazyTypeAndRefKind;

        // Non-zero if the type of the field has been inferred from the type of its initializer expression
        // and the errors of binding the initializer have been or are being reported to compilation diagnostics.
        private int _lazyFieldTypeInferred;

        internal SourceMemberFieldSymbolFromDeclarator(
            SourceMemberContainerTypeSymbol containingType,
            VariableDeclaratorSyntax declarator,
            DeclarationModifiers modifiers,
            bool modifierErrors,
            BindingDiagnosticBag diagnostics)
            : base(containingType, modifiers, declarator.Identifier.ValueText, declarator.GetReference(), declarator.Identifier.Span)
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

        public sealed override RefKind RefKind => GetTypeAndRefKind(ConsList<FieldSymbol>.Empty).RefKind;

        internal override bool HasPointerType
        {
            get
            {
                return TypeWithAnnotations.DefaultType.IsPointerOrFunctionPointer();
            }
        }

        internal sealed override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return GetTypeAndRefKind(fieldsBeingBound).Type;
        }

        private TypeAndRefKind GetTypeAndRefKind(ConsList<FieldSymbol> fieldsBeingBound)
        {
            Debug.Assert(fieldsBeingBound != null);

            if (_lazyTypeAndRefKind != null)
            {
                return _lazyTypeAndRefKind;
            }

            var declarator = VariableDeclaratorNode;
            var fieldSyntax = GetFieldDeclaration(declarator);
            var typeSyntax = fieldSyntax.Declaration.Type;
            var compilation = this.DeclaringCompilation;

            var diagnostics = BindingDiagnosticBag.GetInstance();
            RefKind refKind = RefKind.None;
            TypeWithAnnotations type;

            if (typeSyntax is ScopedTypeSyntax scopedType)
            {
                diagnostics.Add(ErrorCode.ERR_BadMemberFlag, ErrorLocation, SyntaxFacts.GetText(SyntaxKind.ScopedKeyword));
            }

            // When we have multiple declarators, we report the type diagnostics on only the first.
            var diagnosticsForFirstDeclarator = BindingDiagnosticBag.GetInstance();

            Symbol associatedPropertyOrEvent = this.AssociatedSymbol;
            if ((object)associatedPropertyOrEvent != null && associatedPropertyOrEvent.Kind == SymbolKind.Event)
            {
                EventSymbol @event = (EventSymbol)associatedPropertyOrEvent;
                if (@event.IsWindowsRuntimeEvent)
                {
                    NamedTypeSymbol tokenTableType = this.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T);
                    Binder.ReportUseSite(tokenTableType, diagnosticsForFirstDeclarator, this.ErrorLocation);

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
                    var typeOnly = typeSyntax.SkipScoped(out _).SkipRefInField(out refKind);
                    Debug.Assert(refKind is RefKind.None or RefKind.Ref or RefKind.RefReadOnly);
                    type = binder.BindType(typeOnly, diagnosticsForFirstDeclarator);
                    if (refKind != RefKind.None)
                    {
                        MessageID.IDS_FeatureRefFields.CheckFeatureAvailability(diagnostics, compilation, typeSyntax.SkipScoped(out _).Location);
                        if (!compilation.Assembly.RuntimeSupportsByRefFields)
                            diagnostics.Add(ErrorCode.ERR_RuntimeDoesNotSupportRefFields, ErrorLocation);

                        if (!containingType.IsRefLikeType)
                            diagnostics.Add(ErrorCode.ERR_RefFieldInNonRefStruct, ErrorLocation);

                        if (type.Type?.IsRefLikeType == true)
                            diagnostics.Add(ErrorCode.ERR_RefFieldCannotReferToRefStruct, typeSyntax.SkipScoped(out _).Location);
                    }
                }
                else
                {
                    bool isVar;
                    type = binder.BindTypeOrVarKeyword(typeSyntax.SkipScoped(out _).SkipRefInField(out RefKind refKindToAssert), diagnostics, out isVar);
                    Debug.Assert(refKindToAssert == RefKind.None); // Otherwise we might need to report an error
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
                            var syntaxNode = (EqualsValueClauseSyntax)declarator.Initializer;

                            var initializerBinder = new ImplicitlyTypedFieldBinder(binder, fieldsBeingBound);
                            var executableBinder = new ExecutableCodeBinder(syntaxNode, this, initializerBinder);
                            var initializerOpt = executableBinder.BindInferredVariableInitializer(diagnostics, RefKind.None, syntaxNode, declarator);

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

                    if (refKind != RefKind.None)
                    {
                        diagnostics.Add(ErrorCode.ERR_FixedFieldMustNotBeRef, ErrorLocation);
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
            if (Interlocked.CompareExchange(ref _lazyTypeAndRefKind, new TypeAndRefKind(refKind, type.WithModifiers(this.RequiredCustomModifiers)), null) == null)
            {
                TypeChecks(type.Type, diagnostics);

                // CONSIDER: SourceEventFieldSymbol would like to suppress these diagnostics.
                AddDeclarationDiagnostics(diagnostics);

                bool isFirstDeclarator = fieldSyntax.Declaration.Variables[0] == declarator;
                if (isFirstDeclarator)
                {
                    AddDeclarationDiagnostics(diagnosticsForFirstDeclarator);
                }

                state.NotePartComplete(CompletionPart.Type);
            }

            diagnostics.Free();
            diagnosticsForFirstDeclarator.Free();
            return _lazyTypeAndRefKind;
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

        protected sealed override ConstantValue MakeConstantValue(HashSet<SourceFieldSymbolWithSyntaxReference> dependencies, bool earlyDecodingWellKnownAttributes, BindingDiagnosticBag diagnostics)
        {
            if (!this.IsConst || VariableDeclaratorNode.Initializer == null)
            {
                return null;
            }

            return ConstantValueUtils.EvaluateFieldConstant(this, (EqualsValueClauseSyntax)VariableDeclaratorNode.Initializer, dependencies, earlyDecodingWellKnownAttributes, diagnostics);
        }

        public override bool IsDefinedInSourceTree(SyntaxTree tree, TextSpan? definedWithinSpan, CancellationToken cancellationToken = default(CancellationToken))
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

        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, BindingDiagnosticBag diagnostics)
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
