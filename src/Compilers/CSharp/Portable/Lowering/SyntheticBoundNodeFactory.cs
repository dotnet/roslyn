// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A helper class for synthesizing quantities of code.
    /// </summary>
    internal sealed class SyntheticBoundNodeFactory
    {
        /// <summary>
        /// Thrown by the bound node factory when there is a failure to synthesize code.
        /// An appropriate diagnostic is included that should be reported.  Currently
        /// the only diagnostic handled through this mechanism is a missing special/well-known
        /// member.
        /// </summary>
        public class MissingPredefinedMember : Exception
        {
            public MissingPredefinedMember(Diagnostic error) : base(error.ToString())
            {
                this.Diagnostic = error;
            }

            public Diagnostic Diagnostic { get; }
        }

        public CSharpCompilation Compilation { get { return CompilationState.Compilation; } }
        public SyntaxNode Syntax { get; set; }
        public PEModuleBuilder ModuleBuilderOpt { get { return CompilationState.ModuleBuilderOpt; } }
        public DiagnosticBag Diagnostics { get; }
        public TypeCompilationState CompilationState { get; }

        // Current enclosing type, or null if not available.
        private NamedTypeSymbol _currentType;
        public NamedTypeSymbol CurrentType
        {
            get { return _currentType; }
            set
            {
                _currentType = value;
                CheckCurrentType();
            }
        }

        // current method, possibly a lambda or local function, or null if not available
        private MethodSymbol _currentFunction;
        public MethodSymbol CurrentFunction
        {
            get { return _currentFunction; }
            set
            {
                _currentFunction = value;
                if ((object)value != null &&
                    value.MethodKind != MethodKind.AnonymousFunction &&
                    value.MethodKind != MethodKind.LocalFunction)
                {
                    _topLevelMethod = value;
                    _currentType = value.ContainingType;
                }
                CheckCurrentType();
            }
        }

        // The nearest enclosing non-lambda method, or null if not available
        private MethodSymbol _topLevelMethod;
        public MethodSymbol TopLevelMethod
        {
            get { return _topLevelMethod; }
            private set
            {
                _topLevelMethod = value;
                CheckCurrentType();
            }
        }

        /// <summary>
        /// A binder suitable for performing overload resolution to synthesize a call to a helper method.
        /// </summary>
        private Binder _binder;

        internal BoundExpression MakeInvocationExpression(
            BinderFlags flags,
            SyntaxNode node,
            BoundExpression receiver,
            string methodName,
            ImmutableArray<BoundExpression> args,
            DiagnosticBag diagnostics,
            ImmutableArray<TypeSymbol> typeArgs = default(ImmutableArray<TypeSymbol>),
            bool allowUnexpandedForm = true)
        {
            if (_binder == null || _binder.Flags != flags)
            {
                _binder = new SyntheticBinderImpl(this).WithFlags(flags);
            }

            return _binder.MakeInvocationExpression(
                node,
                receiver,
                methodName,
                args,
                diagnostics,
                typeArgs: typeArgs.IsDefault ? default(ImmutableArray<TypeWithAnnotations>) : typeArgs.SelectAsArray(t => TypeWithAnnotations.Create(t)),
                allowFieldsAndProperties: false,
                allowUnexpandedForm: allowUnexpandedForm);
        }

        /// <summary>
        /// A binder used only for performing overload resolution of runtime helper methods.
        /// </summary>
        private sealed class SyntheticBinderImpl : BuckStopsHereBinder
        {
            private readonly SyntheticBoundNodeFactory _factory;
            internal SyntheticBinderImpl(SyntheticBoundNodeFactory factory) : base(factory.Compilation)
            {
                _factory = factory;
            }

            internal override Symbol ContainingMemberOrLambda { get { return _factory.CurrentFunction; } }
            internal override bool IsAccessibleHelper(Symbol symbol, TypeSymbol accessThroughType, out bool failedThroughTypeCheck, ref HashSet<DiagnosticInfo> useSiteDiagnostics, ConsList<TypeSymbol> basesBeingResolved)
            {
                return AccessCheck.IsSymbolAccessible(symbol, _factory.CurrentType, accessThroughType, out failedThroughTypeCheck, ref useSiteDiagnostics, basesBeingResolved);
            }
        }

        /// <summary>
        /// Create a bound node factory. Note that the use of the factory to get special or well-known members
        /// that do not exist will result in an exception of type <see cref="MissingPredefinedMember"/> being thrown.
        /// </summary>
        /// <param name="topLevelMethod">The top-level method that will contain the code</param>
        /// <param name="node">The syntax node to which generated code should be attributed</param>
        /// <param name="compilationState">The state of compilation of the enclosing type</param>
        /// <param name="diagnostics">A bag where any diagnostics should be output</param>
        public SyntheticBoundNodeFactory(MethodSymbol topLevelMethod, SyntaxNode node, TypeCompilationState compilationState, DiagnosticBag diagnostics)
            : this(topLevelMethod, topLevelMethod.ContainingType, node, compilationState, diagnostics)
        {
        }

        /// <param name="topLevelMethodOpt">The top-level method that will contain the code</param>
        /// <param name="currentClassOpt">The enclosing class</param>
        /// <param name="node">The syntax node to which generated code should be attributed</param>
        /// <param name="compilationState">The state of compilation of the enclosing type</param>
        /// <param name="diagnostics">A bag where any diagnostics should be output</param>
        public SyntheticBoundNodeFactory(MethodSymbol topLevelMethodOpt, NamedTypeSymbol currentClassOpt, SyntaxNode node, TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            Debug.Assert(compilationState != null);
            Debug.Assert(diagnostics != null);

            this.CompilationState = compilationState;
            this.CurrentType = currentClassOpt;
            this.TopLevelMethod = topLevelMethodOpt;
            this.CurrentFunction = topLevelMethodOpt;
            this.Syntax = node;
            this.Diagnostics = diagnostics;
        }

        [Conditional("DEBUG")]
        private void CheckCurrentType()
        {
            if ((object)CurrentType != null)
            {
                Debug.Assert((object)TopLevelMethod == null || TypeSymbol.Equals(TopLevelMethod.ContainingType, CurrentType, TypeCompareKind.ConsiderEverything2));

                // In EE scenarios, lambdas and local functions are considered to be contained by the
                // user-defined methods, rather than the EE-defined methods for which we are generating
                // bound nodes. This is because the containing symbols are used to determine the type
                // of the "this" parameter, which we need to be the user-defined types.
                Debug.Assert((object)CurrentFunction == null ||
                    CurrentFunction.MethodKind == MethodKind.AnonymousFunction ||
                    CurrentFunction.MethodKind == MethodKind.LocalFunction ||
                    TypeSymbol.Equals(CurrentFunction.ContainingType, CurrentType, TypeCompareKind.ConsiderEverything2));
            }
        }

        public void AddNestedType(NamedTypeSymbol nestedType)
        {
            ModuleBuilderOpt.AddSynthesizedDefinition(CurrentType, nestedType);
        }

        public void OpenNestedType(NamedTypeSymbol nestedType)
        {
            // TODO: we used to have an invariant that a bound node factory was tied to a
            // single enclosing class.  This breaks that.  It would be nice to reintroduce that
            // invariant.
            AddNestedType(nestedType);
            CurrentFunction = null;
            TopLevelMethod = null;
            CurrentType = nestedType;
        }

        public BoundHoistedFieldAccess HoistedField(FieldSymbol field)
        {
            return new BoundHoistedFieldAccess(Syntax, field, field.Type);
        }

        public StateMachineFieldSymbol StateMachineField(TypeWithAnnotations type, string name, bool isPublic = false, bool isThis = false)
        {
            var result = new StateMachineFieldSymbol(CurrentType, type, name, isPublic, isThis);
            AddField(CurrentType, result);
            return result;
        }

        public StateMachineFieldSymbol StateMachineField(TypeSymbol type, string name, bool isPublic = false, bool isThis = false)
        {
            var result = new StateMachineFieldSymbol(CurrentType, TypeWithAnnotations.Create(type), name, isPublic, isThis);
            AddField(CurrentType, result);
            return result;
        }

        public StateMachineFieldSymbol StateMachineField(TypeSymbol type, string name, SynthesizedLocalKind synthesizedKind, int slotIndex)
        {
            var result = new StateMachineFieldSymbol(CurrentType, type, name, synthesizedKind, slotIndex, isPublic: false);
            AddField(CurrentType, result);
            return result;
        }

        public StateMachineFieldSymbol StateMachineField(TypeSymbol type, string name, LocalSlotDebugInfo slotDebugInfo, int slotIndex)
        {
            var result = new StateMachineFieldSymbol(CurrentType, type, name, slotDebugInfo, slotIndex, isPublic: false);
            AddField(CurrentType, result);
            return result;
        }

        public void AddField(NamedTypeSymbol containingType, FieldSymbol field)
        {
            ModuleBuilderOpt.AddSynthesizedDefinition(containingType, field);
        }

        public GeneratedLabelSymbol GenerateLabel(string prefix)
        {
            return new GeneratedLabelSymbol(prefix);
        }

        public BoundThisReference This()
        {
            Debug.Assert(CurrentFunction is { IsStatic: false });
            return new BoundThisReference(Syntax, CurrentFunction.ThisParameter.Type) { WasCompilerGenerated = true };
        }

        public BoundExpression This(LocalSymbol thisTempOpt)
        {
            return (thisTempOpt != null) ? Local(thisTempOpt) : (BoundExpression)This();
        }

        public BoundBaseReference Base(NamedTypeSymbol baseType)
        {
            Debug.Assert((object)CurrentFunction != null && !CurrentFunction.IsStatic);
            return new BoundBaseReference(Syntax, baseType) { WasCompilerGenerated = true };
        }

        public BoundBadExpression BadExpression(TypeSymbol type)
        {
            return new BoundBadExpression(Syntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, ImmutableArray<BoundExpression>.Empty, type, hasErrors: true);
        }

        public BoundParameter Parameter(ParameterSymbol p)
        {
            return new BoundParameter(Syntax, p, p.Type) { WasCompilerGenerated = true };
        }

        public BoundFieldAccess Field(BoundExpression receiver, FieldSymbol f)
        {
            return new BoundFieldAccess(Syntax, receiver, f, ConstantValue.NotAvailable, LookupResultKind.Viable, f.Type) { WasCompilerGenerated = true };
        }

        public BoundFieldAccess InstanceField(FieldSymbol f)
        {
            return this.Field(this.This(), f);
        }

        public BoundExpression Property(WellKnownMember member)
        {
            return Property(null, member);
        }

        public BoundExpression Property(BoundExpression receiverOpt, WellKnownMember member)
        {
            var propertySym = (PropertySymbol)WellKnownMember(member);
            Debug.Assert(receiverOpt == null ||
                receiverOpt.Type.GetMembers(propertySym.Name).OfType<PropertySymbol>().Single() == propertySym);
            Binder.ReportUseSiteDiagnostics(propertySym, Diagnostics, Syntax);
            return Property(receiverOpt, propertySym);
        }

        public BoundExpression Property(BoundExpression receiverOpt, PropertySymbol property)
        {
            Debug.Assert((receiverOpt == null) == property.IsStatic);
            return Call(receiverOpt, property.GetMethod); // TODO: should we use property.GetBaseProperty().GetMethod to ensure we generate a call to the overridden method?
        }

        public BoundExpression Indexer(BoundExpression receiverOpt, PropertySymbol property, BoundExpression arg0)
        {
            Debug.Assert((receiverOpt == null) == property.IsStatic);
            return Call(receiverOpt, property.GetMethod, arg0); // TODO: should we use property.GetBaseProperty().GetMethod to ensure we generate a call to the overridden method?
        }

        public NamedTypeSymbol SpecialType(SpecialType st)
        {
            NamedTypeSymbol specialType = Compilation.GetSpecialType(st);
            Binder.ReportUseSiteDiagnostics(specialType, Diagnostics, Syntax);
            return specialType;
        }

        public ArrayTypeSymbol WellKnownArrayType(WellKnownType elementType)
        {
            return Compilation.CreateArrayTypeSymbol(WellKnownType(elementType));
        }

        public NamedTypeSymbol WellKnownType(WellKnownType wt)
        {
            NamedTypeSymbol wellKnownType = Compilation.GetWellKnownType(wt);
            Binder.ReportUseSiteDiagnostics(wellKnownType, Diagnostics, Syntax);
            return wellKnownType;
        }

        /// <summary>
        /// Get the symbol for a well-known member. The use of this method to get a well-known member
        /// that does not exist will result in an exception of type MissingPredefinedMember being thrown
        /// containing an appropriate diagnostic for the caller to report.
        /// </summary>
        /// <param name="wm">The desired well-known member</param>
        /// <param name="isOptional">If true, the method may return null for a missing member without an exception</param>
        /// <returns>A symbol for the well-known member, or null if it is missing and isOptions == true</returns>
        public Symbol WellKnownMember(WellKnownMember wm, bool isOptional = false)
        {
            Symbol wellKnownMember = Binder.GetWellKnownTypeMember(Compilation, wm, Diagnostics, syntax: Syntax, isOptional: true);
            if (wellKnownMember == null && !isOptional)
            {
                RuntimeMembers.MemberDescriptor memberDescriptor = WellKnownMembers.GetDescriptor(wm);
                var diagnostic = new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_MissingPredefinedMember, memberDescriptor.DeclaringTypeMetadataName, memberDescriptor.Name), Syntax.Location);
                throw new MissingPredefinedMember(diagnostic);
            }

            return wellKnownMember;
        }

        public MethodSymbol WellKnownMethod(WellKnownMember wm, bool isOptional = false)
        {
            return (MethodSymbol)WellKnownMember(wm, isOptional);
        }

        /// <summary>
        /// Get the symbol for a special member. The use of this method to get a special member
        /// that does not exist will result in an exception of type MissingPredefinedMember being thrown
        /// containing an appropriate diagnostic for the caller to report.
        /// </summary>
        /// <param name="sm">The desired special member</param>
        /// <returns>A symbol for the special member.</returns>
        public Symbol SpecialMember(SpecialMember sm)
        {
            Symbol specialMember = Compilation.GetSpecialTypeMember(sm);
            if (specialMember == null)
            {
                RuntimeMembers.MemberDescriptor memberDescriptor = SpecialMembers.GetDescriptor(sm);
                var diagnostic = new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_MissingPredefinedMember, memberDescriptor.DeclaringTypeMetadataName, memberDescriptor.Name), Syntax.Location);
                throw new MissingPredefinedMember(diagnostic);
            }

            Binder.ReportUseSiteDiagnostics(specialMember, Diagnostics, Syntax);
            return specialMember;
        }

        public MethodSymbol SpecialMethod(SpecialMember sm)
        {
            return (MethodSymbol)SpecialMember(sm);
        }

        public PropertySymbol SpecialProperty(SpecialMember sm)
        {
            return (PropertySymbol)SpecialMember(sm);
        }

        public BoundExpressionStatement Assignment(BoundExpression left, BoundExpression right, bool isRef = false)
        {
            return ExpressionStatement(AssignmentExpression(left, right, isRef));
        }

        public BoundExpressionStatement ExpressionStatement(BoundExpression expr)
        {
            return new BoundExpressionStatement(Syntax, expr) { WasCompilerGenerated = true };
        }

        public BoundAssignmentOperator AssignmentExpression(BoundExpression left, BoundExpression right, bool isRef = false)
        {
            Debug.Assert(left.Type.Equals(right.Type, TypeCompareKind.AllIgnoreOptions) ||
                    StackOptimizerPass1.IsFixedBufferAssignmentToRefLocal(left, right, isRef) ||
                    right.Type.IsErrorType() || left.Type.IsErrorType());

            return new BoundAssignmentOperator(Syntax, left, right, left.Type, isRef: isRef) { WasCompilerGenerated = true };
        }

        public BoundBlock Block()
        {
            return Block(ImmutableArray<BoundStatement>.Empty);
        }

        public BoundBlock Block(ImmutableArray<BoundStatement> statements)
        {
            return Block(ImmutableArray<LocalSymbol>.Empty, statements);
        }

        public BoundBlock Block(params BoundStatement[] statements)
        {
            return Block(ImmutableArray.Create(statements));
        }

        public BoundBlock Block(ImmutableArray<LocalSymbol> locals, params BoundStatement[] statements)
        {
            return Block(locals, ImmutableArray.Create(statements));
        }

        public BoundBlock Block(ImmutableArray<LocalSymbol> locals, ImmutableArray<BoundStatement> statements)
        {
            return new BoundBlock(Syntax, locals, statements) { WasCompilerGenerated = true };
        }

        public BoundBlock Block(ImmutableArray<LocalSymbol> locals, ImmutableArray<LocalFunctionSymbol> localFunctions, params BoundStatement[] statements)
        {
            return Block(locals, localFunctions, ImmutableArray.Create(statements));
        }

        public BoundBlock Block(ImmutableArray<LocalSymbol> locals, ImmutableArray<LocalFunctionSymbol> localFunctions, ImmutableArray<BoundStatement> statements)
        {
            return new BoundBlock(Syntax, locals, localFunctions, statements) { WasCompilerGenerated = true };
        }

        public BoundExtractedFinallyBlock ExtractedFinallyBlock(BoundBlock finallyBlock)
        {
            return new BoundExtractedFinallyBlock(Syntax, finallyBlock) { WasCompilerGenerated = true };
        }

        public BoundStatementList StatementList()
        {
            return StatementList(ImmutableArray<BoundStatement>.Empty);
        }

        public BoundStatementList StatementList(ImmutableArray<BoundStatement> statements)
        {
            return new BoundStatementList(Syntax, statements) { WasCompilerGenerated = true };
        }

        public BoundStatementList StatementList(BoundStatement first, BoundStatement second)
        {
            return new BoundStatementList(Syntax, ImmutableArray.Create(first, second)) { WasCompilerGenerated = true };
        }

        public BoundReturnStatement Return(BoundExpression expression = null)
        {
            if (expression != null)
            {
                // If necessary, add a conversion on the return expression.
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                var conversion = Compilation.Conversions.ClassifyConversionFromType(expression.Type, CurrentFunction.ReturnType, ref useSiteDiagnostics);
                Debug.Assert(useSiteDiagnostics.IsNullOrEmpty());
                Debug.Assert(conversion.Kind != ConversionKind.NoConversion);
                if (conversion.Kind != ConversionKind.Identity)
                {
                    Debug.Assert(CurrentFunction.RefKind == RefKind.None);
                    expression = BoundConversion.Synthesized(Syntax, expression, conversion, false, explicitCastInCode: false, conversionGroupOpt: null, ConstantValue.NotAvailable, CurrentFunction.ReturnType);
                }
            }

            return new BoundReturnStatement(Syntax, CurrentFunction.RefKind, expression) { WasCompilerGenerated = true };
        }

        public void CloseMethod(BoundStatement body)
        {
            Debug.Assert((object)CurrentFunction != null);
            if (body.Kind != BoundKind.Block)
            {
                body = Block(body);
            }

            CompilationState.AddSynthesizedMethod(CurrentFunction, body);
            CurrentFunction = null;
        }

        public LocalSymbol SynthesizedLocal(
            TypeSymbol type,
            SyntaxNode syntax = null,
            bool isPinned = false,
            RefKind refKind = RefKind.None,
            SynthesizedLocalKind kind = SynthesizedLocalKind.LoweringTemp
#if DEBUG
            ,
            [CallerLineNumber]int createdAtLineNumber = 0,
            [CallerFilePath]string createdAtFilePath = null
#endif
            )
        {
            return new SynthesizedLocal(CurrentFunction, TypeWithAnnotations.Create(type), kind, syntax, isPinned, refKind
#if DEBUG
                , createdAtLineNumber, createdAtFilePath
#endif
                );
        }

        public ParameterSymbol SynthesizedParameter(TypeSymbol type, string name, MethodSymbol container = null, int ordinal = 0)
        {
            return SynthesizedParameterSymbol.Create(container, TypeWithAnnotations.Create(type), ordinal, RefKind.None, name);
        }

        public BoundBinaryOperator Binary(BinaryOperatorKind kind, TypeSymbol type, BoundExpression left, BoundExpression right)
        {
            return new BoundBinaryOperator(this.Syntax, kind, ConstantValue.NotAvailable, null, LookupResultKind.Viable, left, right, type) { WasCompilerGenerated = true };
        }

        public BoundAsOperator As(BoundExpression operand, TypeSymbol type)
        {
            return new BoundAsOperator(this.Syntax, operand, Type(type), Conversion.ExplicitReference, type) { WasCompilerGenerated = true };
        }

        public BoundIsOperator Is(BoundExpression operand, TypeSymbol type)
        {
            HashSet<DiagnosticInfo> discarded = null;
            // Because compiler-generated nodes are not lowered, this conversion is not used later in the compiler.
            // But it is a required part of the `BoundIsOperator` node, so we compute a conversion here.
            Conversion c = Compilation.Conversions.ClassifyBuiltInConversion(operand.Type, type, ref discarded);
            return new BoundIsOperator(this.Syntax, operand, Type(type), c, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean)) { WasCompilerGenerated = true };
        }

        public BoundBinaryOperator LogicalAnd(BoundExpression left, BoundExpression right)
        {
            return Binary(BinaryOperatorKind.LogicalBoolAnd, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean), left, right);
        }

        public BoundBinaryOperator LogicalOr(BoundExpression left, BoundExpression right)
        {
            return Binary(BinaryOperatorKind.LogicalBoolOr, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean), left, right);
        }

        public BoundBinaryOperator IntEqual(BoundExpression left, BoundExpression right)
        {
            return Binary(BinaryOperatorKind.IntEqual, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean), left, right);
        }

        public BoundBinaryOperator ObjectEqual(BoundExpression left, BoundExpression right)
        {
            return Binary(BinaryOperatorKind.ObjectEqual, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean), left, right);
        }

        public BoundBinaryOperator ObjectNotEqual(BoundExpression left, BoundExpression right)
        {
            return Binary(BinaryOperatorKind.ObjectNotEqual, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean), left, right);
        }

        public BoundBinaryOperator IntNotEqual(BoundExpression left, BoundExpression right)
        {
            return Binary(BinaryOperatorKind.IntNotEqual, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean), left, right);
        }

        public BoundBinaryOperator IntLessThan(BoundExpression left, BoundExpression right)
        {
            return Binary(BinaryOperatorKind.IntLessThan, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean), left, right);
        }

        public BoundBinaryOperator IntGreaterThanOrEqual(BoundExpression left, BoundExpression right)
        {
            return Binary(BinaryOperatorKind.IntGreaterThanOrEqual, SpecialType(CodeAnalysis.SpecialType.System_Boolean), left, right);
        }

        public BoundBinaryOperator IntSubtract(BoundExpression left, BoundExpression right)
        {
            return Binary(BinaryOperatorKind.IntSubtraction, SpecialType(CodeAnalysis.SpecialType.System_Int32), left, right);
        }

        public BoundLiteral Literal(int value)
        {
            return new BoundLiteral(Syntax, ConstantValue.Create(value), SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Int32)) { WasCompilerGenerated = true };
        }

        public BoundLiteral Literal(uint value)
        {
            return new BoundLiteral(Syntax, ConstantValue.Create(value), SpecialType(Microsoft.CodeAnalysis.SpecialType.System_UInt32)) { WasCompilerGenerated = true };
        }

        public BoundLiteral Literal(ConstantValue value, TypeSymbol type)
        {
            return new BoundLiteral(Syntax, value, type) { WasCompilerGenerated = true };
        }

        public BoundObjectCreationExpression New(NamedTypeSymbol type, params BoundExpression[] args)
        {
            var ctor = type.InstanceConstructors.Single(c => c.ParameterCount == args.Length);
            return New(ctor, args);
        }

        public BoundObjectCreationExpression New(MethodSymbol ctor, params BoundExpression[] args)
            => New(ctor, args.ToImmutableArray());

        public BoundObjectCreationExpression New(MethodSymbol ctor, ImmutableArray<BoundExpression> args)
            => new BoundObjectCreationExpression(Syntax, ctor, binderOpt: null, args) { WasCompilerGenerated = true };

        public BoundObjectCreationExpression New(WellKnownMember wm, ImmutableArray<BoundExpression> args)
        {
            var ctor = WellKnownMethod(wm, isOptional: false);
            return new BoundObjectCreationExpression(Syntax, ctor, binderOpt: null, args) { WasCompilerGenerated = true };
        }

        public BoundExpression InstanceCall(BoundExpression receiver, string name, BoundExpression arg)
        {
            return MakeInvocationExpression(BinderFlags.None, this.Syntax, receiver, name, ImmutableArray.Create(arg), this.Diagnostics);
        }

        public BoundExpression InstanceCall(BoundExpression receiver, string name)
        {
            return MakeInvocationExpression(BinderFlags.None, this.Syntax, receiver, name, ImmutableArray<BoundExpression>.Empty, this.Diagnostics);
        }

        public BoundExpression StaticCall(TypeSymbol receiver, string name, params BoundExpression[] args)
        {
            return MakeInvocationExpression(BinderFlags.None, this.Syntax, this.Type(receiver), name, args.ToImmutableArray(), this.Diagnostics);
        }

        public BoundExpression StaticCall(TypeSymbol receiver, string name, ImmutableArray<BoundExpression> args, bool allowUnexpandedForm)
        {
            return MakeInvocationExpression(BinderFlags.None, this.Syntax, this.Type(receiver), name, args, this.Diagnostics, allowUnexpandedForm: allowUnexpandedForm);
        }

        public BoundExpression StaticCall(BinderFlags flags, TypeSymbol receiver, string name, ImmutableArray<TypeSymbol> typeArgs, params BoundExpression[] args)
        {
            return MakeInvocationExpression(flags, this.Syntax, this.Type(receiver), name, args.ToImmutableArray(), this.Diagnostics, typeArgs);
        }

        public BoundExpression StaticCall(TypeSymbol receiver, MethodSymbol method, params BoundExpression[] args)
        {
            if ((object)method == null)
            {
                return new BoundBadExpression(Syntax, default(LookupResultKind), ImmutableArray<Symbol>.Empty, args.AsImmutable(), receiver);
            }

            return Call(null, method, args);
        }

        public BoundExpression StaticCall(MethodSymbol method, ImmutableArray<BoundExpression> args)
            => Call(null, method, args);

        public BoundExpression StaticCall(WellKnownMember method, params BoundExpression[] args)
        {
            MethodSymbol methodSymbol = WellKnownMethod(method);
            Binder.ReportUseSiteDiagnostics(methodSymbol, Diagnostics, Syntax);
            Debug.Assert(methodSymbol.IsStatic);
            return Call(null, methodSymbol, args);
        }

        public BoundExpression StaticCall(SpecialMember method, params BoundExpression[] args)
        {
            MethodSymbol methodSymbol = SpecialMethod(method);
            Binder.ReportUseSiteDiagnostics(methodSymbol, Diagnostics, Syntax);
            Debug.Assert(methodSymbol.IsStatic);
            return Call(null, methodSymbol, args);
        }

        public BoundCall Call(BoundExpression receiver, MethodSymbol method)
        {
            return Call(receiver, method, ImmutableArray<BoundExpression>.Empty);
        }

        public BoundCall Call(BoundExpression receiver, MethodSymbol method, BoundExpression arg0)
        {
            return Call(receiver, method, ImmutableArray.Create(arg0));
        }

        public BoundCall Call(BoundExpression receiver, MethodSymbol method, BoundExpression arg0, BoundExpression arg1)
        {
            return Call(receiver, method, ImmutableArray.Create(arg0, arg1));
        }

        public BoundCall Call(BoundExpression receiver, MethodSymbol method, params BoundExpression[] args)
        {
            return Call(receiver, method, ImmutableArray.Create<BoundExpression>(args));
        }

        public BoundCall Call(BoundExpression receiver, WellKnownMember method, BoundExpression arg0)
            => Call(receiver, WellKnownMethod(method), ImmutableArray.Create(arg0));

        public BoundCall Call(BoundExpression receiver, MethodSymbol method, ImmutableArray<BoundExpression> args)
        {
            Debug.Assert(method.ParameterCount == args.Length);

            return new BoundCall(
                Syntax, receiver, method, args,
                default(ImmutableArray<String>), method.ParameterRefKinds, false, false, false,
                default(ImmutableArray<int>), LookupResultKind.Viable, null, method.ReturnType,
                hasErrors: method.OriginalDefinition is ErrorMethodSymbol)
            { WasCompilerGenerated = true };
        }

        public BoundCall Call(BoundExpression receiver, MethodSymbol method, ImmutableArray<RefKind> refKinds, ImmutableArray<BoundExpression> args)
        {
            Debug.Assert(method.ParameterCount == args.Length);
            return new BoundCall(
                Syntax, receiver, method, args,
                default(ImmutableArray<String>), refKinds, false, false, false,
                ImmutableArray<int>.Empty, LookupResultKind.Viable, null, method.ReturnType)
            { WasCompilerGenerated = true };
        }

        public BoundExpression Conditional(BoundExpression condition, BoundExpression consequence, BoundExpression alternative, TypeSymbol type)
        {
            return new BoundConditionalOperator(Syntax, false, condition, consequence, alternative, default(ConstantValue), type) { WasCompilerGenerated = true };
        }

        public BoundExpression ComplexConditionalReceiver(BoundExpression valueTypeReceiver, BoundExpression referenceTypeReceiver)
        {
            Debug.Assert(TypeSymbol.Equals(valueTypeReceiver.Type, referenceTypeReceiver.Type, TypeCompareKind.ConsiderEverything2));
            return new BoundComplexConditionalReceiver(Syntax, valueTypeReceiver, referenceTypeReceiver, valueTypeReceiver.Type) { WasCompilerGenerated = true };
        }

        public BoundExpression Coalesce(BoundExpression left, BoundExpression right)
        {
            Debug.Assert(left.Type.Equals(right.Type, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes) || left.Type.IsErrorType());
            Debug.Assert(left.Type.IsReferenceType);

            return new BoundNullCoalescingOperator(Syntax, left, right, Conversion.Identity, BoundNullCoalescingOperatorResultKind.LeftType, left.Type) { WasCompilerGenerated = true };
        }

        public BoundStatement If(BoundExpression condition, BoundStatement thenClause, BoundStatement elseClauseOpt = null)
        {
            return If(condition, ImmutableArray<LocalSymbol>.Empty, thenClause, elseClauseOpt);
        }

        public BoundStatement ConditionalGoto(BoundExpression condition, LabelSymbol label, bool jumpIfTrue)
        {
            return new BoundConditionalGoto(Syntax, condition, jumpIfTrue, label) { WasCompilerGenerated = true };
        }

        public BoundStatement If(BoundExpression condition, ImmutableArray<LocalSymbol> locals, BoundStatement thenClause, BoundStatement elseClauseOpt = null)
        {
            // We translate
            //    if (condition) thenClause else elseClause
            // as
            //    {
            //       ConditionalGoto(!condition) alternative
            //       thenClause
            //       goto afterif;
            //       alternative:
            //       elseClause
            //       afterif:
            //    }
            Debug.Assert(thenClause != null);

            var statements = ArrayBuilder<BoundStatement>.GetInstance();
            var afterif = new GeneratedLabelSymbol("afterif");

            if (elseClauseOpt != null)
            {
                var alt = new GeneratedLabelSymbol("alternative");

                statements.Add(ConditionalGoto(condition, alt, false));
                statements.Add(thenClause);
                statements.Add(Goto(afterif));
                if (!locals.IsDefaultOrEmpty)
                {
                    var firstPart = this.Block(locals, statements.ToImmutable());
                    statements.Clear();
                    statements.Add(firstPart);
                }

                statements.Add(Label(alt));
                statements.Add(elseClauseOpt);
            }
            else
            {
                statements.Add(ConditionalGoto(condition, afterif, false));
                statements.Add(thenClause);
                if (!locals.IsDefaultOrEmpty)
                {
                    var firstPart = this.Block(locals, statements.ToImmutable());
                    statements.Clear();
                    statements.Add(firstPart);
                }
            }

            statements.Add(Label(afterif));
            return Block(statements.ToImmutableAndFree());
        }

        public BoundThrowStatement Throw(BoundExpression e = null)
        {
            return new BoundThrowStatement(Syntax, e) { WasCompilerGenerated = true };
        }

        public BoundLocal Local(LocalSymbol local)
        {
            return new BoundLocal(Syntax, local, null, local.Type) { WasCompilerGenerated = true };
        }

        public BoundExpression MakeSequence(LocalSymbol temp, params BoundExpression[] parts)
        {
            return MakeSequence(ImmutableArray.Create<LocalSymbol>(temp), parts);
        }

        public BoundExpression MakeSequence(params BoundExpression[] parts)
        {
            return MakeSequence(ImmutableArray<LocalSymbol>.Empty, parts);
        }

        public BoundExpression MakeSequence(ImmutableArray<LocalSymbol> locals, params BoundExpression[] parts)
        {
            var builder = ArrayBuilder<BoundExpression>.GetInstance();
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var part = parts[i];
                if (LocalRewriter.ReadIsSideeffecting(part))
                {
                    builder.Add(parts[i]);
                }
            }
            var lastExpression = parts[parts.Length - 1];

            if (locals.IsDefaultOrEmpty && builder.Count == 0)
            {
                builder.Free();
                return lastExpression;
            }

            return Sequence(locals, builder.ToImmutableAndFree(), lastExpression);
        }

        public BoundSequence Sequence(BoundExpression[] sideEffects, BoundExpression result, TypeSymbol type = null)
        {
            return new BoundSequence(Syntax, ImmutableArray<LocalSymbol>.Empty, sideEffects.AsImmutableOrNull(), result, type ?? result.Type) { WasCompilerGenerated = true };
        }

        public BoundExpression Sequence(ImmutableArray<LocalSymbol> locals, ImmutableArray<BoundExpression> sideEffects, BoundExpression result)
        {
            return
                locals.IsDefaultOrEmpty && sideEffects.IsDefaultOrEmpty
                ? result
                : new BoundSequence(Syntax, locals, sideEffects, result, result.Type) { WasCompilerGenerated = true };
        }

        public BoundSpillSequence SpillSequence(ImmutableArray<LocalSymbol> locals, ImmutableArray<BoundStatement> sideEffects, BoundExpression result)
        {
            return new BoundSpillSequence(Syntax, locals, sideEffects, result, result.Type) { WasCompilerGenerated = true };
        }

        /// <summary>
        /// An internal helper class for building a switch statement.
        /// </summary>
        internal class SyntheticSwitchSection
        {
            public readonly ImmutableArray<int> Values;
            public readonly ImmutableArray<BoundStatement> Statements;
            public SyntheticSwitchSection(ImmutableArray<int> Values, ImmutableArray<BoundStatement> Statements)
            {
                this.Values = Values;
                this.Statements = Statements;
            }
        }

        public SyntheticSwitchSection SwitchSection(int value, params BoundStatement[] statements)
        {
            return new SyntheticSwitchSection(ImmutableArray.Create(value), ImmutableArray.Create(statements));
        }

        public SyntheticSwitchSection SwitchSection(List<int> values, params BoundStatement[] statements)
        {
            return new SyntheticSwitchSection(ImmutableArray.CreateRange(values), ImmutableArray.Create(statements));
        }

        /// <summary>
        /// Produce an int switch.
        /// </summary>
        public BoundStatement Switch(BoundExpression ex, ImmutableArray<SyntheticSwitchSection> sections)
        {
            Debug.Assert(ex.Type.SpecialType == CodeAnalysis.SpecialType.System_Int32);

            if (sections.Length == 0)
            {
                return ExpressionStatement(ex);
            }

            CheckSwitchSections(sections);
            GeneratedLabelSymbol breakLabel = new GeneratedLabelSymbol("break");
            var caseBuilder = ArrayBuilder<(ConstantValue Value, LabelSymbol label)>.GetInstance();
            var statements = ArrayBuilder<BoundStatement>.GetInstance();
            statements.Add(null); // placeholder at statements[0] for the dispatch
            foreach (var section in sections)
            {
                LabelSymbol sectionLabel = new GeneratedLabelSymbol("case " + section.Values[0]);
                statements.Add(Label(sectionLabel));
                statements.AddRange(section.Statements);

                foreach (var value in section.Values)
                {
                    caseBuilder.Add((ConstantValue.Create(value), sectionLabel));
                }
            }

            statements.Add(Label(breakLabel));
            Debug.Assert(statements[0] == null);
            statements[0] = new BoundSwitchDispatch(Syntax, ex, caseBuilder.ToImmutableAndFree(), breakLabel, null)
            { WasCompilerGenerated = true };
            return Block(statements.ToImmutableAndFree());
        }

        /// <summary>
        /// Check for (and assert that there are no) duplicate case labels in the switch.
        /// </summary>
        /// <param name="sections"></param>
        [Conditional("DEBUG")]
        private static void CheckSwitchSections(ImmutableArray<SyntheticSwitchSection> sections)
        {
            var labels = new HashSet<int>();
            foreach (var s in sections)
            {
                foreach (var v2 in s.Values)
                {
                    Debug.Assert(!labels.Contains(v2));
                    labels.Add(v2);
                }
            }
        }

        public BoundGotoStatement Goto(LabelSymbol label)
        {
            return new BoundGotoStatement(Syntax, label) { WasCompilerGenerated = true };
        }

        public BoundLabelStatement Label(LabelSymbol label)
        {
            return new BoundLabelStatement(Syntax, label) { WasCompilerGenerated = true };
        }

        public BoundLiteral Literal(Boolean value)
        {
            return new BoundLiteral(Syntax, ConstantValue.Create(value), SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean)) { WasCompilerGenerated = true };
        }

        public BoundLiteral Literal(string value)
        {
            var stringConst = ConstantValue.Create(value);
            return StringLiteral(stringConst);
        }

        public BoundLiteral StringLiteral(ConstantValue stringConst)
        {
            Debug.Assert(stringConst.IsString || stringConst.IsNull);
            return new BoundLiteral(Syntax, stringConst, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_String)) { WasCompilerGenerated = true };
        }

        public BoundLiteral StringLiteral(String stringValue)
        {
            return StringLiteral(ConstantValue.Create(stringValue));
        }

        public BoundArrayLength ArrayLength(BoundExpression array)
        {
            Debug.Assert((object)array.Type != null && array.Type.IsArray());
            return new BoundArrayLength(Syntax, array, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Int32));
        }

        public BoundArrayAccess ArrayAccessFirstElement(BoundExpression array)
        {
            Debug.Assert((object)array.Type != null && array.Type.IsArray());
            int rank = ((ArrayTypeSymbol)array.Type).Rank;
            ImmutableArray<BoundExpression> firstElementIndices = ArrayBuilder<BoundExpression>.GetInstance(rank, Literal(0)).ToImmutableAndFree();
            return ArrayAccess(array, firstElementIndices);
        }

        public BoundArrayAccess ArrayAccess(BoundExpression array, params BoundExpression[] indices)
        {
            return ArrayAccess(array, indices.AsImmutableOrNull());
        }

        public BoundArrayAccess ArrayAccess(BoundExpression array, ImmutableArray<BoundExpression> indices)
        {
            Debug.Assert((object)array.Type != null && array.Type.IsArray());
            return new BoundArrayAccess(Syntax, array, indices, ((ArrayTypeSymbol)array.Type).ElementType);
        }

        public BoundStatement BaseInitialization()
        {
            // TODO: add diagnostics for when things fall apart
            NamedTypeSymbol baseType = CurrentFunction.ThisParameter.Type.BaseTypeNoUseSiteDiagnostics;
            var ctor = baseType.InstanceConstructors.Single(c => c.ParameterCount == 0);
            return new BoundExpressionStatement(Syntax, Call(Base(baseType), ctor)) { WasCompilerGenerated = true };
        }

        public BoundStatement SequencePoint(SyntaxNode syntax, BoundStatement statement)
        {
            return new BoundSequencePoint(syntax, statement);
        }

        public BoundStatement SequencePointWithSpan(CSharpSyntaxNode syntax, TextSpan span, BoundStatement statement)
        {
            return new BoundSequencePointWithSpan(syntax, statement, span);
        }

        public BoundStatement HiddenSequencePoint(BoundStatement statementOpt = null)
        {
            return new BoundSequencePoint(null, statementOpt) { WasCompilerGenerated = true };
        }

        public BoundStatement ThrowNull()
        {
            return Throw(Null(Compilation.GetWellKnownType(Microsoft.CodeAnalysis.WellKnownType.System_Exception)));
        }

        public BoundExpression ThrowExpression(BoundExpression thrown, TypeSymbol type)
        {
            return new BoundThrowExpression(thrown.Syntax, thrown, type) { WasCompilerGenerated = true };
        }

        public BoundExpression Null(TypeSymbol type)
        {
            return Null(type, Syntax);
        }

        public static BoundExpression Null(TypeSymbol type, SyntaxNode syntax)
        {
            Debug.Assert(type.CanBeAssignedNull());
            BoundExpression nullLiteral = new BoundLiteral(syntax, ConstantValue.Null, type) { WasCompilerGenerated = true };
            return type.IsPointerType()
                ? BoundConversion.SynthesizedNonUserDefined(syntax, nullLiteral, Conversion.NullToPointer, type)
                : nullLiteral;
        }

        public BoundTypeExpression Type(TypeSymbol type)
        {
            return new BoundTypeExpression(Syntax, null, type) { WasCompilerGenerated = true };
        }

        public BoundExpression Typeof(WellKnownType type)
        {
            return Typeof(WellKnownType(type));
        }

        public BoundExpression Typeof(TypeSymbol type)
        {
            return new BoundTypeOfOperator(
                Syntax,
                Type(type),
                WellKnownMethod(CodeAnalysis.WellKnownMember.System_Type__GetTypeFromHandle),
                WellKnownType(CodeAnalysis.WellKnownType.System_Type))
            { WasCompilerGenerated = true };
        }

        public BoundExpression Typeof(TypeWithAnnotations type)
        {
            return Typeof(type.Type);
        }

        public ImmutableArray<BoundExpression> TypeOfs(ImmutableArray<TypeWithAnnotations> typeArguments)
        {
            return typeArguments.SelectAsArray(Typeof);
        }

        public BoundExpression TypeofDynamicOperationContextType()
        {
            return Typeof(this.CompilationState.DynamicOperationContextType);
        }

        public BoundExpression Sizeof(TypeSymbol type)
        {
            return new BoundSizeOfOperator(Syntax, Type(type), Binder.GetConstantSizeOf(type), SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Int32)) { WasCompilerGenerated = true };
        }

        internal BoundExpression ConstructorInfo(MethodSymbol ctor)
        {
            return new BoundMethodInfo(
                Syntax,
                ctor,
                GetMethodFromHandleMethod(ctor.ContainingType),
                WellKnownType(Microsoft.CodeAnalysis.WellKnownType.System_Reflection_ConstructorInfo))
            { WasCompilerGenerated = true };
        }

        public BoundExpression MethodDefIndex(MethodSymbol method)
        {
            return new BoundMethodDefIndex(
                Syntax,
                method,
                SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Int32))
            { WasCompilerGenerated = true };
        }

        /// <summary>
        /// Synthesizes an expression that evaluates to the current module's MVID.
        /// </summary>
        /// <returns></returns>
        public BoundExpression ModuleVersionId()
        {
            return new BoundModuleVersionId(Syntax, WellKnownType(Microsoft.CodeAnalysis.WellKnownType.System_Guid)) { WasCompilerGenerated = true };
        }

        public BoundExpression ModuleVersionIdString()
        {
            return new BoundModuleVersionIdString(Syntax, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_String)) { WasCompilerGenerated = true };
        }

        public BoundExpression InstrumentationPayloadRoot(int analysisKind, TypeSymbol payloadType)
        {
            return new BoundInstrumentationPayloadRoot(Syntax, analysisKind, payloadType) { WasCompilerGenerated = true };
        }

        public BoundExpression MaximumMethodDefIndex()
        {
            return new BoundMaximumMethodDefIndex(
                Syntax,
                SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Int32))
            { WasCompilerGenerated = true };
        }

        /// <summary>
        /// Synthesizes an expression that evaluates to the index of a source document in the table of debug source documents.
        /// </summary>
        public BoundExpression SourceDocumentIndex(Cci.DebugSourceDocument document)
        {
            return new BoundSourceDocumentIndex(
                Syntax,
                document,
                SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Int32))
            { WasCompilerGenerated = true };
        }

        public BoundExpression MethodInfo(MethodSymbol method)
        {
            // The least overridden virtual method is only called for value type receivers
            // in special circumstances. These circumstances are exactly the checks performed by
            // MayUseCallForStructMethod (which is also used by the emitter when determining
            // whether or not to call a method with a value type receiver directly).
            if (!method.ContainingType.IsValueType || !Microsoft.CodeAnalysis.CSharp.CodeGen.CodeGenerator.MayUseCallForStructMethod(method))
            {
                method = method.GetConstructedLeastOverriddenMethod(this.CompilationState.Type);
            }

            return new BoundMethodInfo(
                Syntax,
                method,
                GetMethodFromHandleMethod(method.ContainingType),
                WellKnownType(Microsoft.CodeAnalysis.WellKnownType.System_Reflection_MethodInfo))
            { WasCompilerGenerated = true };
        }

        public BoundExpression FieldInfo(FieldSymbol field)
        {
            return new BoundFieldInfo(
                Syntax,
                field,
                GetFieldFromHandleMethod(field.ContainingType),
                WellKnownType(Microsoft.CodeAnalysis.WellKnownType.System_Reflection_FieldInfo))
            { WasCompilerGenerated = true };
        }

        private MethodSymbol GetMethodFromHandleMethod(NamedTypeSymbol methodContainer)
        {
            return WellKnownMethod(
                (methodContainer.AllTypeArgumentCount() == 0 && !methodContainer.IsAnonymousType) ?
                CodeAnalysis.WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle :
                CodeAnalysis.WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle2);
        }

        private MethodSymbol GetFieldFromHandleMethod(NamedTypeSymbol fieldContainer)
        {
            return WellKnownMethod(
                (fieldContainer.AllTypeArgumentCount() == 0) ?
                CodeAnalysis.WellKnownMember.System_Reflection_FieldInfo__GetFieldFromHandle :
                CodeAnalysis.WellKnownMember.System_Reflection_FieldInfo__GetFieldFromHandle2);
        }

        public BoundExpression Convert(TypeSymbol type, BoundExpression arg)
        {
            if (TypeSymbol.Equals(type, arg.Type, TypeCompareKind.ConsiderEverything2))
            {
                return arg;
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            Conversion c = Compilation.Conversions.ClassifyConversionFromExpression(arg, type, ref useSiteDiagnostics);
            Debug.Assert(c.Exists);
            Debug.Assert(useSiteDiagnostics.IsNullOrEmpty());

            // If this happens, we should probably check if the method has ObsoleteAttribute.
            Debug.Assert((object)c.Method == null, "Why are we synthesizing a user-defined conversion after initial binding?");

            return Convert(type, arg, c);
        }

        public BoundExpression Convert(TypeSymbol type, BoundExpression arg, Conversion conversion, bool isChecked = false)
        {
            // NOTE: We can see user-defined conversions at this point because there are places in the bound tree where
            // the binder stashes Conversion objects for later consumption (e.g. foreach, nullable, increment).
            if ((object)conversion.Method != null && !TypeSymbol.Equals(conversion.Method.Parameters[0].Type, arg.Type, TypeCompareKind.ConsiderEverything2))
            {
                arg = Convert(conversion.Method.Parameters[0].Type, arg);
            }

            if (conversion.Kind == ConversionKind.ImplicitReference && arg.IsLiteralNull())
            {
                return Null(type);
            }

            if (conversion.Kind == ConversionKind.ExplicitNullable &&
                arg.Type.IsNullableType() &&
                arg.Type.GetNullableUnderlyingType().Equals(type, TypeCompareKind.AllIgnoreOptions))
            {
                // A conversion to unbox a nullable value is produced when binding a pattern-matching
                // operation from an operand of type T? to a pattern of type T.
                return this.Call(arg, this.SpecialMethod(CodeAnalysis.SpecialMember.System_Nullable_T_get_Value).AsMember((NamedTypeSymbol)arg.Type));
            }

            return new BoundConversion(Syntax, arg, conversion, @checked: isChecked, explicitCastInCode: true, conversionGroupOpt: null, null, type) { WasCompilerGenerated = true };
        }

        public BoundExpression ArrayOrEmpty(TypeSymbol elementType, BoundExpression[] elements)
        {
            return ArrayOrEmpty(elementType, elements.AsImmutable());
        }

        /// <summary>
        /// Helper that will use Array.Empty if available and elements have 0 length
        /// NOTE: it is valid only if we know that the API that is being called will not
        ///       retain or use the array argument for any purpose (like locking or key in a hash table)
        ///       Typical example of valid use is Linq.Expressions factories - they do not make any
        ///       assumptions about array arguments and do not keep them or rely on their identity.
        /// </summary>
        public BoundExpression ArrayOrEmpty(TypeSymbol elementType, ImmutableArray<BoundExpression> elements)
        {
            if (elements.Length == 0)
            {
                MethodSymbol arrayEmpty = WellKnownMethod(CodeAnalysis.WellKnownMember.System_Array__Empty, isOptional: true);
                if ((object)arrayEmpty != null)
                {
                    arrayEmpty = arrayEmpty.Construct(ImmutableArray.Create(elementType));
                    return Call(null, arrayEmpty);
                }
            }

            return Array(elementType, elements);
        }

        public BoundExpression Array(TypeSymbol elementType, ImmutableArray<BoundExpression> elements)
        {
            return new BoundArrayCreation(
                Syntax,
                ImmutableArray.Create<BoundExpression>(Literal(elements.Length)),
                new BoundArrayInitialization(Syntax, elements) { WasCompilerGenerated = true },
                Compilation.CreateArrayTypeSymbol(elementType));
        }

        public BoundExpression Array(TypeSymbol elementType, BoundExpression length)
        {
            return new BoundArrayCreation(
               Syntax,
               ImmutableArray.Create<BoundExpression>(length),
               null,
               Compilation.CreateArrayTypeSymbol(elementType))
            { WasCompilerGenerated = true };
        }

        internal BoundExpression Default(TypeSymbol type)
        {
            return Default(type, Syntax);
        }

        internal static BoundExpression Default(TypeSymbol type, SyntaxNode syntax)
        {
            return new BoundDefaultExpression(syntax, type) { WasCompilerGenerated = true };
        }

        internal BoundStatement Try(
            BoundBlock tryBlock,
            ImmutableArray<BoundCatchBlock> catchBlocks,
            BoundBlock finallyBlock = null,
            LabelSymbol finallyLabel = null)
        {
            return new BoundTryStatement(Syntax, tryBlock, catchBlocks, finallyBlock, finallyLabel) { WasCompilerGenerated = true };
        }

        internal ImmutableArray<BoundCatchBlock> CatchBlocks(
            params BoundCatchBlock[] catchBlocks)
        {
            return catchBlocks.AsImmutableOrNull();
        }

        internal BoundCatchBlock Catch(
            LocalSymbol local,
            BoundBlock block)
        {
            var source = Local(local);
            return new BoundCatchBlock(Syntax, ImmutableArray.Create(local), source, source.Type, exceptionFilterOpt: null, body: block, isSynthesizedAsyncCatchAll: false);
        }

        internal BoundCatchBlock Catch(
            BoundExpression source,
            BoundBlock block)
        {
            return new BoundCatchBlock(Syntax, ImmutableArray<LocalSymbol>.Empty, source, source.Type, exceptionFilterOpt: null, body: block, isSynthesizedAsyncCatchAll: false);
        }

        internal BoundTryStatement Fault(BoundBlock tryBlock, BoundBlock faultBlock)
        {
            return new BoundTryStatement(Syntax, tryBlock, ImmutableArray<BoundCatchBlock>.Empty, faultBlock, finallyLabelOpt: null, preferFaultHandler: true);
        }

        internal BoundExpression NullOrDefault(TypeSymbol typeSymbol)
        {
            return NullOrDefault(typeSymbol, this.Syntax);
        }

        internal static BoundExpression NullOrDefault(TypeSymbol typeSymbol, SyntaxNode syntax)
        {
            return typeSymbol.IsReferenceType ? Null(typeSymbol, syntax) : Default(typeSymbol, syntax);
        }

        internal BoundExpression Not(
            BoundExpression expression)
        {
            return new BoundUnaryOperator(expression.Syntax, UnaryOperatorKind.BoolLogicalNegation, expression, null, null, LookupResultKind.Viable, expression.Type);
        }

        /// <summary>
        /// Takes an expression and returns the bound local expression "temp"
        /// and the bound assignment expression "temp = expr".
        /// </summary>
        public BoundLocal StoreToTemp(
            BoundExpression argument,
            out BoundAssignmentOperator store,
            RefKind refKind = RefKind.None,
            SynthesizedLocalKind kind = SynthesizedLocalKind.LoweringTemp,
            SyntaxNode syntaxOpt = null
#if DEBUG
            , [CallerLineNumber]int callerLineNumber = 0
            , [CallerFilePath]string callerFilePath = null
#endif
            )
        {
            MethodSymbol containingMethod = this.CurrentFunction;

            switch (refKind)
            {
                case RefKind.Out:
                    refKind = RefKind.Ref;
                    break;

                case RefKind.In:
                    if (!Binder.HasHome(argument,
                                        Binder.AddressKind.ReadOnly,
                                        containingMethod,
                                        Compilation.IsPeVerifyCompatEnabled,
                                        stackLocalsOpt: null))
                    {
                        // If there was an explicit 'in' on the argument then we should have verified
                        // earlier that we always have a home.
                        Debug.Assert(argument.GetRefKind() != RefKind.In);
                        refKind = RefKind.None;
                    }
                    break;
                case RefKindExtensions.StrictIn:
                case RefKind.None:
                case RefKind.Ref:
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(refKind);
            }

            var syntax = argument.Syntax;
            var type = argument.Type;

            var local = new BoundLocal(
                syntax,
                new SynthesizedLocal(
                    containingMethod,
                    TypeWithAnnotations.Create(type),
                    kind,
#if DEBUG
                    createdAtLineNumber: callerLineNumber,
                    createdAtFilePath: callerFilePath,
#endif
                    syntaxOpt: syntaxOpt ?? (kind.IsLongLived() ? syntax : null),
                    isPinned: false,
                    refKind: refKind),
                null,
                type);

            store = new BoundAssignmentOperator(
                syntax,
                local,
                argument,
                refKind != RefKind.None,
                type);

            return local;
        }

        internal BoundStatement NoOp(NoOpStatementFlavor noOpStatementFlavor)
        {
            return new BoundNoOpStatement(Syntax, noOpStatementFlavor);
        }

        internal BoundLocal MakeTempForDiscard(BoundDiscardExpression node, ArrayBuilder<LocalSymbol> temps)
        {
            LocalSymbol temp;
            BoundLocal result = MakeTempForDiscard(node, out temp);
            temps.Add(temp);
            return result;
        }

        internal BoundLocal MakeTempForDiscard(BoundDiscardExpression node, out LocalSymbol temp)
        {
            temp = new SynthesizedLocal(this.CurrentFunction, TypeWithAnnotations.Create(node.Type), SynthesizedLocalKind.LoweringTemp);

            return new BoundLocal(node.Syntax, temp, constantValueOpt: null, type: node.Type) { WasCompilerGenerated = true };
        }

        internal ImmutableArray<BoundExpression> MakeTempsForDiscardArguments(ImmutableArray<BoundExpression> arguments, ArrayBuilder<LocalSymbol> builder)
        {
            var discardsPresent = arguments.Any(a => a.Kind == BoundKind.DiscardExpression);

            if (discardsPresent)
            {
                arguments = arguments.SelectAsArray(
                    (arg, t) => arg.Kind == BoundKind.DiscardExpression ? t.factory.MakeTempForDiscard((BoundDiscardExpression)arg, t.builder) : arg,
                    (factory: this, builder: builder));
            }

            return arguments;
        }
    }
}
