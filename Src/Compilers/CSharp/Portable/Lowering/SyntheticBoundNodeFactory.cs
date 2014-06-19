// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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

            public Diagnostic Diagnostic { get; private set; }
        }

        public CSharpCompilation Compilation { get { return CompilationState.Compilation; } }
        public CSharpSyntaxNode Syntax { get; set; }
        public PEModuleBuilder ModuleBuilderOpt { get { return CompilationState.ModuleBuilderOpt; } }
        public DiagnosticBag Diagnostics { get; private set; }
        public TypeCompilationState CompilationState { get; private set; }

        // Current enclosing type, or null if not available.
        private NamedTypeSymbol currentClass;
        public NamedTypeSymbol CurrentClass
        {
            get { return currentClass; }
            set
            {
                currentClass = value;
                CheckCurrentClass();
            }
        }

        // current method, possibly a lambda, or null if not available
        private MethodSymbol currentMethod;
        public MethodSymbol CurrentMethod
        {
            get { return currentMethod; }
            set
            {
                currentMethod = value;
                if ((object)value != null && value.MethodKind != MethodKind.AnonymousFunction)
                {
                    topLevelMethod = value;
                    currentClass = value.ContainingType;
                }
                CheckCurrentClass();
            }
        }

        // The nearest enclosing non-lambda method, or null if not available
        private MethodSymbol topLevelMethod;
        public MethodSymbol TopLevelMethod
        {
            get { return topLevelMethod; }
            private set
            {
                topLevelMethod = value;
                CheckCurrentClass();
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
        public SyntheticBoundNodeFactory(MethodSymbol topLevelMethod, CSharpSyntaxNode node, TypeCompilationState compilationState, DiagnosticBag diagnostics)
            : this(topLevelMethod, topLevelMethod.ContainingType, node, compilationState, diagnostics)
        {
        }

        /// <param name="topLevelMethodOpt">The top-level method that will contain the code</param>
        /// <param name="currentClassOpt">The enclosing class</param>
        /// <param name="node">The syntax node to which generated code should be attributed</param>
        /// <param name="compilationState">The state of compilation of the enclosing type</param>
        /// <param name="diagnostics">A bag where any diagnostics should be output</param>
        public SyntheticBoundNodeFactory(MethodSymbol topLevelMethodOpt, NamedTypeSymbol currentClassOpt, CSharpSyntaxNode node, TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            Debug.Assert(compilationState != null);
            Debug.Assert(diagnostics != null);

            this.CompilationState = compilationState;
            this.TopLevelMethod = topLevelMethodOpt;
            this.CurrentClass = currentClassOpt;
            this.Syntax = node;
            this.Diagnostics = diagnostics;
        }

        [Conditional("DEBUG")]
        private void CheckCurrentClass()
        {
            if ((object)CurrentClass != null)
            {
                Debug.Assert((object)TopLevelMethod == null || TopLevelMethod.ContainingType == CurrentClass);
                Debug.Assert((object)CurrentMethod == null || CurrentMethod.ContainingType == CurrentClass);
            }
        }

        public void AddNestedType(NamedTypeSymbol nestedType)
        {
            ModuleBuilderOpt.AddSynthesizedDefinition(CurrentClass, nestedType);
        }

        public void OpenNestedType(NamedTypeSymbol nestedType)
        {
            // TODO: we used to have an invariant that a bound node factory was tied to a
            // single enclosing class.  This breaks that.  It would be nice to reintroduce that
            // invariant.
            AddNestedType(nestedType);
            CurrentMethod = null;
            TopLevelMethod = null;
            CurrentClass = nestedType;
        }

        public BoundHoistedFieldAccess HoistedField(FieldSymbol field)
        {
            return new BoundHoistedFieldAccess(Syntax, field, field.Type);
        }

        public SynthesizedFieldSymbolBase StateMachineField(TypeSymbol fieldType, string name, bool isPublic)
        {
            var result = new StateMachineFieldSymbol(CurrentClass, fieldType, name, isPublic);
            AddField(CurrentClass, result);
            return result;
        }

        public SynthesizedFieldSymbolBase StateMachineField(TypeSymbol fieldType, string name, int iteratorLocalIndex)
        {
            var result = new StateMachineHoistedLocalSymbol(CurrentClass, fieldType, name, iteratorLocalIndex);
            AddField(CurrentClass, result);
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
            Debug.Assert((object)CurrentMethod != null && !CurrentMethod.IsStatic);
            return new BoundThisReference(Syntax, CurrentMethod.ThisParameter.Type) { WasCompilerGenerated = true };
        }

        public BoundExpression This(LocalSymbol thisTempOpt)
        {
            return (thisTempOpt != null) ? Local(thisTempOpt) : (BoundExpression)This();
        }

        public BoundBaseReference Base()
        {
            Debug.Assert((object)CurrentMethod != null && !CurrentMethod.IsStatic);
            return new BoundBaseReference(Syntax, CurrentMethod.ThisParameter.Type.BaseTypeNoUseSiteDiagnostics) { WasCompilerGenerated = true };
        }

        public BoundParameter Parameter(ParameterSymbol p)
        {
            return new BoundParameter(Syntax, p, p.Type) { WasCompilerGenerated = true };
        }

        public BoundFieldAccess Field(BoundExpression receiver, FieldSymbol f)
        {
            return new BoundFieldAccess(Syntax, receiver, f, ConstantValue.NotAvailable, LookupResultKind.Viable, f.Type) { WasCompilerGenerated = true };
        }

        public BoundExpression Property(WellKnownMember member)
        {
            var propertySym = WellKnownMember(member) as PropertySymbol;
            //if (propertySym == null) return BoundBadExpression
            Binder.ReportUseSiteDiagnostics(propertySym, Diagnostics, Syntax);
            Debug.Assert(propertySym.IsStatic);
            return Call(null, propertySym.GetMethod);
        }

        public BoundExpression Property(BoundExpression receiver, WellKnownMember member)
        {
            var propertySym = WellKnownMember(member) as PropertySymbol;
            Debug.Assert(!propertySym.IsStatic);
            Debug.Assert(receiver.Type.GetMembers(propertySym.Name).OfType<PropertySymbol>().Single() == propertySym);
            //if (propertySym == null) return BoundBadExpression
            Binder.ReportUseSiteDiagnostics(propertySym, Diagnostics, Syntax);
            Debug.Assert(!propertySym.IsStatic);
            return Call(receiver, propertySym.GetMethod);
        }

        public BoundExpression Property(BoundExpression receiver, string name)
        {
            // TODO: unroll loop and add diagnostics for failure
            // TODO: should we use GetBaseProperty() to ensure we generate a call to the overridden method?
            // TODO: replace this with a mechanism that uses WellKnownMember instead of string names.
            var property = receiver.Type.GetMembers(name).OfType<PropertySymbol>().Single();
            Debug.Assert(!property.IsStatic);
            return Call(receiver, property.GetMethod); // TODO: should we use property.GetBaseProperty().GetMethod to ensure we generate a call to the overridden method?
        }

        public BoundExpression Property(NamedTypeSymbol receiver, string name)
        {
            // TODO: unroll loop and add diagnostics for failure
            var property = receiver.GetMembers(name).OfType<PropertySymbol>().Single();
            Debug.Assert(property.IsStatic);
            return Call(null, property.GetMethod);
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
                WellKnownType containingType = (WellKnownType)memberDescriptor.DeclaringTypeId;
                var diagnostic = new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_MissingPredefinedMember, containingType.GetMetadataName(), memberDescriptor.Name), Syntax.Location);
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
                SpecialType containingType = (SpecialType)memberDescriptor.DeclaringTypeId;
                var diagnostic = new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_MissingPredefinedMember, containingType.GetMetadataName(), memberDescriptor.Name), Syntax.Location);
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

        public BoundExpressionStatement Assignment(BoundExpression left, BoundExpression right, RefKind refKind = RefKind.None)
        {
            return ExpressionStatement(AssignmentExpression(left, right, refKind));
        }

        public BoundExpressionStatement ExpressionStatement(BoundExpression expr)
        {
            return new BoundExpressionStatement(Syntax, expr) { WasCompilerGenerated = true };
        }

        public BoundAssignmentOperator AssignmentExpression(BoundExpression left, BoundExpression right, RefKind refKind = RefKind.None)
        {
            Debug.Assert(left.Type.Equals(right.Type, ignoreDynamic: true) || right.Type.IsErrorType() || left.Type.IsErrorType());
            return new BoundAssignmentOperator(Syntax, left, right, left.Type, refKind: refKind) { WasCompilerGenerated = true };
        }

        public BoundBlock Block(ImmutableArray<BoundStatement> statements)
        {
            return Block(ImmutableArray<LocalSymbol>.Empty, statements);
        }

        public BoundBlock Block(params BoundStatement[] statements)
        {
            return Block(ImmutableArray.Create<BoundStatement>(statements));
        }

        public BoundBlock Block(ImmutableArray<LocalSymbol> locals, params BoundStatement[] statements)
        {
            return Block(locals, ImmutableArray.Create<BoundStatement>(statements));
        }

        public BoundBlock Block(ImmutableArray<LocalSymbol> locals, ImmutableArray<BoundStatement> statements)
        {
            return new BoundBlock(Syntax, locals, statements) { WasCompilerGenerated = true };
        }

        public BoundReturnStatement Return(BoundExpression expression = null)
        {
            if (expression != null)
            {
                // If necessary, add a conversion on the return expression.
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                var conversion = Compilation.Conversions.ClassifyConversion(expression.Type, CurrentMethod.ReturnType, ref useSiteDiagnostics);
                Debug.Assert(useSiteDiagnostics.IsNullOrEmpty());
                Debug.Assert(conversion.Kind != ConversionKind.NoConversion);
                if (conversion.Kind != ConversionKind.Identity)
                {
                    expression = BoundConversion.Synthesized(Syntax, expression, conversion, false, false, ConstantValue.NotAvailable, CurrentMethod.ReturnType);
                }
            }

            return new BoundReturnStatement(Syntax, expression) { WasCompilerGenerated = true };
        }

        public void CloseMethod(BoundStatement body)
        {
            Debug.Assert((object)CurrentMethod != null);
            if (body.Kind != BoundKind.Block) body = Block(body);
            CompilationState.AddSynthesizedMethod(CurrentMethod, body);
            CurrentMethod = null;
        }

        public LocalSymbol SynthesizedLocal(TypeSymbol type, string name = null, CSharpSyntaxNode syntax = null, bool isPinned = false, RefKind refKind = RefKind.None, TempKind tempKind = TempKind.None)
        {
            return new SynthesizedLocal(CurrentMethod, type, name, syntax, isPinned: isPinned, refKind: refKind, tempKind: tempKind);
        }

        public ParameterSymbol SynthesizedParameter(TypeSymbol type, string name, MethodSymbol container = null, int ordinal = 0)
        {
            return new SynthesizedParameterSymbol(container, type, ordinal, RefKind.None, name);
        }

        public BoundBinaryOperator Binary(BinaryOperatorKind kind, TypeSymbol type, BoundExpression left, BoundExpression right)
        {
            return new BoundBinaryOperator(this.Syntax, kind, left, right, ConstantValue.NotAvailable, null, LookupResultKind.Viable, type) { WasCompilerGenerated = true };
        }

        public BoundAsOperator As(BoundExpression operand, TypeSymbol type)
        {
            return new BoundAsOperator(this.Syntax, operand, Type(type), Conversion.ExplicitReference, type) { WasCompilerGenerated = true };
        }

        public BoundBinaryOperator LogicalAnd(BoundExpression left, BoundExpression right)
        {
            return Binary(BinaryOperatorKind.LogicalBoolAnd, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean), left, right);
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

        public BoundLiteral Literal(int value)
        {
            return new BoundLiteral(Syntax, ConstantValue.Create(value), SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Int32)) { WasCompilerGenerated = true };
        }

        public BoundLiteral Literal(uint value)
        {
            return new BoundLiteral(Syntax, ConstantValue.Create(value), SpecialType(Microsoft.CodeAnalysis.SpecialType.System_UInt32)) { WasCompilerGenerated = true };
        }

        public BoundObjectCreationExpression New(NamedTypeSymbol type, params BoundExpression[] args)
        {
            // TODO: add diagnostics for when things fall apart
            var ctor = type.InstanceConstructors.Single(c => c.ParameterCount == args.Length);
            return New(ctor, args);
        }

        public BoundObjectCreationExpression New(MethodSymbol ctor, params BoundExpression[] args)
        {
            // TODO: add diagnostics for when things fall apart
            return new BoundObjectCreationExpression(Syntax, ctor, args) { WasCompilerGenerated = true };
        }

        public BoundExpression StaticCall(TypeSymbol receiver, string name, params BoundExpression[] args)
        {
            return StaticCall(receiver, name, ImmutableArray<TypeSymbol>.Empty, args);
        }

        public BoundExpression StaticCall(TypeSymbol receiver, string name, ImmutableArray<TypeSymbol> typeArgs, params BoundExpression[] args)
        {
            return StaticCall(receiver, FindMethod(receiver, name, typeArgs, args.AsImmutableOrNull()), args);
        }

        public BoundExpression StaticCall(TypeSymbol receiver, MethodSymbol method, params BoundExpression[] args)
        {
            if ((object)method == null)
            {
                return new BoundBadExpression(Syntax, default(LookupResultKind), ImmutableArray<Symbol>.Empty, ((BoundNode[])args).AsImmutableOrNull(), receiver);
            }

            return Call(null, method, args);
        }

        private MethodSymbol FindMethod(TypeSymbol receiver, string name, ImmutableArray<TypeSymbol> typeArgs, ImmutableArray<BoundExpression> args)
        {
            MethodSymbol found = null;
            bool ambiguous = false;
            ImmutableArray<Symbol> candidates = receiver.GetMembers(name);
            foreach (var candidate in candidates)
            {
                var method = candidate as MethodSymbol;
                if ((object)method == null || method.Arity != typeArgs.Length || method.ParameterCount != args.Length) continue;
                if (method.Arity != 0) method = method.Construct(typeArgs);
                var parameters = method.Parameters;
                bool exact = true;

                for (int i = 0; i < args.Length; i++)
                {
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    if (parameters[i].RefKind != RefKind.None ||
                        !((object)args[i].Type == null && parameters[i].Type.IsReferenceType ||
                        Compilation.Conversions.ClassifyConversion(args[i].Type, parameters[i].Type, ref useSiteDiagnostics).IsImplicit))
                    {
                        Debug.Assert(useSiteDiagnostics.IsNullOrEmpty());
                        goto nextCandidate;
                    }

                    Debug.Assert(useSiteDiagnostics.IsNullOrEmpty());
                    exact = exact && args[i].Type == parameters[i].Type;
                }

                if (exact) return method;
                if ((object)found != null)
                {
                    ambiguous = true;
                }
                found = method;

                nextCandidate: ;
            }

            if (ambiguous) ReportLibraryProblem(ErrorCode.ERR_LibraryMethodNotUnique, receiver, name, typeArgs, args);
            else if ((object)found == null) ReportLibraryProblem(ErrorCode.ERR_LibraryMethodNotFound, receiver, name, typeArgs, args);
            return found;
        }

        MethodSymbol ReportLibraryProblem(ErrorCode code, TypeSymbol receiver, string name, ImmutableArray<TypeSymbol> typeArgs, ImmutableArray<BoundExpression> args)
        {
            var methodSig = new StringBuilder();
            methodSig.Append(name);
            if (!typeArgs.IsEmpty)
            {
                methodSig.Append("<");
                bool wasFirst = true;
                foreach (var t in typeArgs)
                {
                    if (!wasFirst) methodSig.Append(", ");
                    methodSig.Append(t.ToDisplayString());
                    wasFirst = false;
                }
                methodSig.Append(">");
            }
            {
                methodSig.Append("(");
                bool wasFirst = true;
                foreach (var a in args)
                {
                    if (!wasFirst) methodSig.Append(", ");
                    methodSig.Append(a.Type.ToDisplayString());
                    wasFirst = false;
                }
                methodSig.Append(")");
            }

            Diagnostics.Add(code, Syntax.Location, receiver, methodSig.ToString());
            return null;
        }

        ////public BoundCall Call(BoundExpression receiver, WellKnownMember method, params BoundExpression[] args)
        ////{
        ////    return Call(receiver, (MethodSymbol)Compilation.GetWellKnownTypeMember(method), args);
        ////}

        public BoundCall Call(BoundExpression receiver, MethodSymbol method, params BoundExpression[] args)
        {
            return Call(receiver, method, ImmutableArray.Create<BoundExpression>(args));
        }

        public BoundCall Call(BoundExpression receiver, MethodSymbol method, ImmutableArray<BoundExpression> args)
        {
            Debug.Assert(method.ParameterCount == args.Length);
            return new BoundCall(
                Syntax, receiver, method, args,
                ImmutableArray<String>.Empty, ImmutableArray<RefKind>.Empty, false, false, false,
                ImmutableArray<int>.Empty, LookupResultKind.Viable, method.ReturnType) { WasCompilerGenerated = true };
        }

        public BoundCall Call(BoundExpression receiver, TypeSymbol declaringType, string methodName, ImmutableArray<BoundExpression> args)
        {
            return Call(receiver, FindMethod(declaringType, methodName, ImmutableArray<TypeSymbol>.Empty, args), args);
        }

        public BoundCall Call(BoundExpression receiver, TypeSymbol declaringType, string methodName, ImmutableArray<TypeSymbol> typeArgs, ImmutableArray<BoundExpression> args)
        {
            return Call(receiver, FindMethod(declaringType, methodName, typeArgs, args), args);
        }

        public BoundExpression Conditional(BoundExpression condition, BoundExpression consequence, BoundExpression alternative, TypeSymbol type)
        {
            return new BoundConditionalOperator(Syntax, condition, consequence, alternative, default(ConstantValue), type) { WasCompilerGenerated = true };
        }

        public BoundExpression Coalesce(BoundExpression left, BoundExpression right)
        {
            Debug.Assert(left.Type.Equals(right.Type, ignoreCustomModifiers: true));
            Debug.Assert(left.Type.IsReferenceType);

            return new BoundNullCoalescingOperator(Syntax, left, right, Conversion.Identity, left.Type) { WasCompilerGenerated = true };
        }

        public BoundStatement If(BoundExpression condition, BoundStatement thenClause, BoundStatement elseClause)
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
            Debug.Assert(thenClause != null && elseClause != null);
            var afterif = new GeneratedLabelSymbol("afterif");
            var alt = new GeneratedLabelSymbol("alternative");
            return Block(
                new BoundConditionalGoto(Syntax, condition, false, alt) { WasCompilerGenerated = true },
                thenClause,
                Goto(afterif),
                Label(alt),
                elseClause,
                Label(afterif)
                );
        }

        public BoundStatement For(BoundExpression initialization, BoundExpression termination, BoundExpression increment, BoundStatement body)
        {
            //      for(<initialization>; <increment>; <termination>)
            //      {
            //          <body>;
            //      }

            //  Lowered form:

            //      <initialization>;
            //      goto LoopCondition;
            //      LoopStart:
            //      <body>
            //      <increment>
            //      LoopCondition:
            //      if(<termination>)
            //          goto LoopStart;
            var lLoopStart = GenerateLabel("LoopStart");
            var lLoopCondition = GenerateLabel("LoopCondition");
            return Block(ExpressionStatement(initialization),
                         Goto(lLoopCondition),
                         Label(lLoopStart),
                         body,
                         ExpressionStatement(increment),
                         Label(lLoopCondition),
                         If(termination, Goto(lLoopStart)));
        }

        public BoundStatement If(BoundExpression condition, BoundStatement thenClause)
        {
            return If(condition, thenClause, Block());
        }

        public BoundThrowStatement Throw(BoundExpression e = null)
        {
            return new BoundThrowStatement(Syntax, e) { WasCompilerGenerated = true };
        }

        public BoundLocal Local(LocalSymbol local)
        {
            return new BoundLocal(Syntax, local, null, local.Type) { WasCompilerGenerated = true };
        }

        public BoundExpression Sequence(LocalSymbol temp, params BoundExpression[] parts)
        {
            return Sequence(ImmutableArray.Create<LocalSymbol>(temp), parts);
        }

        public BoundExpression Sequence(params BoundExpression[] parts)
        {
            return Sequence(ImmutableArray<LocalSymbol>.Empty, parts);
        }

        public BoundExpression Sequence(ImmutableArray<LocalSymbol> locals, params BoundExpression[] parts)
        {
            var builder = ArrayBuilder<BoundExpression>.GetInstance();
            for (int i = 0; i < parts.Length - 1; i++) builder.Add(parts[i]);
            var lastExpression = parts[parts.Length - 1];
            return Sequence(locals, builder.ToImmutableAndFree(), lastExpression);
        }

        public BoundExpression Sequence(BoundExpression[] sideEffects, BoundExpression result, TypeSymbol type = null)
        {
            return new BoundSequence(Syntax, ImmutableArray<LocalSymbol>.Empty, sideEffects.AsImmutableOrNull(), result, type ?? result.Type) { WasCompilerGenerated = true };
        }

        public BoundExpression Sequence(ImmutableArray<LocalSymbol> locals, ImmutableArray<BoundExpression> sideEffects, BoundExpression result)
        {
            return new BoundSequence(Syntax, locals, sideEffects, result, result.Type) { WasCompilerGenerated = true };
        }

        public BoundStatement Switch(BoundExpression ex, params BoundSwitchSection[] sections)
        {
            Debug.Assert(ex.Type.SpecialType != Microsoft.CodeAnalysis.SpecialType.System_String); // BoundSwitchStatement.StringEquality not set

            if (sections.Length == 0) return ExpressionStatement(ex);
            GeneratedLabelSymbol breakLabel = new GeneratedLabelSymbol("break");
            var s = ImmutableArray.Create<BoundSwitchSection>(sections);
            CheckSwitchSections(s);
            return new BoundSwitchStatement(
                Syntax,
                ImmutableArray<LocalSymbol>.Empty,
                ex,
                null,
                ImmutableArray<LocalSymbol>.Empty,
                s,
                breakLabel,
                null) { WasCompilerGenerated = true };
        }

        public BoundStatement Switch(BoundExpression ex, IEnumerable<BoundSwitchSection> sections)
        {
            return Switch(ex, sections.ToArray());
        }

        /// <summary>
        /// Check for (and assert that there are no) duplicate case labels in the switch.
        /// </summary>
        /// <param name="sections"></param>
        [Conditional("DEBUG")]
        private static void CheckSwitchSections(ImmutableArray<BoundSwitchSection> sections)
        {
            var labels = new HashSet<int>();
            foreach (var s in sections)
            {
                foreach (var l in s.BoundSwitchLabels)
                {
                    var sl = (SourceLabelSymbol)l.Label;
                    var v1 = sl.SwitchCaseLabelConstant.Int32Value;
                    var v2 = l.ExpressionOpt;
                    Debug.Assert(v2 == null || v1 == v2.ConstantValue.Int32Value);
                    Debug.Assert(!labels.Contains(v1));
                    labels.Add(v1);
                }
            }
            //Console.WriteLine();
        }

        public BoundSwitchSection SwitchSection(int value, params BoundStatement[] statements)
        {
            var label = new SourceLabelSymbol(CurrentMethod, ConstantValue.Create(value));
            var switchLabel = new BoundSwitchLabel(Syntax, label) { WasCompilerGenerated = true };
            return new BoundSwitchSection(Syntax, ImmutableArray.Create<BoundSwitchLabel>(switchLabel), ImmutableArray.Create<BoundStatement>(statements)) { WasCompilerGenerated = true };
        }

        public BoundSwitchSection SwitchSection(List<int> values, params BoundStatement[] statements)
        {
            var builder = ArrayBuilder<BoundSwitchLabel>.GetInstance();
            foreach (var i in values)
            {
                var label = new SourceLabelSymbol(CurrentMethod, ConstantValue.Create(i));
                builder.Add(new BoundSwitchLabel(Syntax, label) { WasCompilerGenerated = true });
            }

            return new BoundSwitchSection(Syntax, builder.ToImmutableAndFree(), ImmutableArray.Create<BoundStatement>(statements)) { WasCompilerGenerated = true };
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

        public BoundStatement BaseInitialization(params BoundExpression[] args)
        {
            // TODO: add diagnostics for when things fall apart
            var ctor = CurrentMethod.ThisParameter.Type.BaseTypeNoUseSiteDiagnostics.InstanceConstructors.Single(c => c.ParameterCount == args.Length);
            return new BoundExpressionStatement(Syntax, Call(Base(), ctor, args)) { WasCompilerGenerated = true };
        }

        public BoundStatement SequencePoint(CSharpSyntaxNode syntax, BoundStatement statement)
        {
            return new BoundSequencePoint(syntax, statement);
        }

        public BoundStatement SequencePointWithSpan(CSharpSyntaxNode syntax, TextSpan span, BoundStatement statement)
        {
            return new BoundSequencePointWithSpan(syntax, statement, span);
        }

        public BoundStatement HiddenSequencePoint()
        {
            return new BoundSequencePoint(null, null) { WasCompilerGenerated = true };
        }

        public BoundStatement ThrowNull()
        {
            return Throw(Null(Compilation.GetWellKnownType(Microsoft.CodeAnalysis.WellKnownType.System_Exception)));
        }

        public BoundExpression Null(TypeSymbol type)
        {
            BoundExpression nullLiteral = new BoundLiteral(Syntax, ConstantValue.Null, type) { WasCompilerGenerated = true };
            return type.IsPointerType()
                ? BoundConversion.SynthesizedNonUserDefined(Syntax, nullLiteral, ConversionKind.NullToPointer, type)
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
                WellKnownType(CodeAnalysis.WellKnownType.System_Type)) { WasCompilerGenerated = true };
        }

        public ImmutableArray<BoundExpression> TypeOfs(ImmutableArray<TypeSymbol> typeArguments)
        {
            return typeArguments.SelectAsArray(Typeof);
        }

        public BoundExpression MethodInfo(WellKnownMember method)
        {
            return MethodInfo(WellKnownMethod(method));
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
                WellKnownType(Microsoft.CodeAnalysis.WellKnownType.System_Reflection_ConstructorInfo)) { WasCompilerGenerated = true };
        }

        public BoundExpression MethodInfo(MethodSymbol method)
        {
            var originalMethod = method.GetConstructedLeastOverriddenMethod(this.CompilationState.Type);
            return new BoundMethodInfo(
                Syntax,
                originalMethod,
                GetMethodFromHandleMethod(originalMethod.ContainingType),
                WellKnownType(Microsoft.CodeAnalysis.WellKnownType.System_Reflection_MethodInfo)) { WasCompilerGenerated = true };
        }

        public BoundExpression FieldInfo(FieldSymbol field)
        {
            return new BoundFieldInfo(
                Syntax,
                field,
                GetFieldFromHandleMethod(field.ContainingType),
                WellKnownType(Microsoft.CodeAnalysis.WellKnownType.System_Reflection_FieldInfo)) { WasCompilerGenerated = true };
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
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            Conversion c = Compilation.Conversions.ClassifyConversionFromExpression(arg, type, ref useSiteDiagnostics);
            Debug.Assert(useSiteDiagnostics.IsNullOrEmpty());

            // If this happens, we should probably check if the method has ObsoleteAttribute.
            Debug.Assert((object)c.Method == null, "Why are we synthesizing a user-defined conversion after initial binding?");
            
            return Convert(type, arg, c);
        }

        public BoundExpression Convert(TypeSymbol type, BoundExpression arg, Conversion conversion, bool isChecked = false)
        {
            // NOTE: We can see user-defined conversions at this point because there are places in the bound tree where
            // the binder stashes Conversion objects for later consumption (e.g. foreach, nullable, increment).
            if ((object)conversion.Method != null && conversion.Method.Parameters[0].Type != arg.Type)
            {
                arg = Convert(conversion.Method.Parameters[0].Type, arg);
            }

            return new BoundConversion(Syntax, arg, conversion, isChecked, true, null, type) { WasCompilerGenerated = true };
        }

        public BoundExpression Convert(TypeSymbol type, BoundExpression arg, ConversionKind conversionKind, bool isChecked = false)
        {
            return new BoundConversion(
                Syntax,
                arg,
                conversionKind,
                LookupResultKind.Viable,
                isBaseConversion: false,
                symbolOpt: null,
                @checked: isChecked,
                explicitCastInCode: true,
                isExtensionMethod: false,
                isArrayIndex: false,
                constantValueOpt: null,
                type: type)
                { WasCompilerGenerated = true };
        }

        public BoundExpression Array(TypeSymbol elementType, BoundExpression[] elements)
        {
            return Array(elementType, elements.AsImmutableOrNull());
        }

        public BoundExpression Array(TypeSymbol elementType, ImmutableArray<BoundExpression> elements)
        {
            return new BoundArrayCreation(
                Syntax,
                ImmutableArray.Create<BoundExpression>(Literal(elements.Length)),
                new BoundArrayInitialization(Syntax, elements) { WasCompilerGenerated = true },
                Compilation.CreateArrayTypeSymbol(elementType));
        }

        internal BoundExpression Default(TypeSymbol type)
        {
            return new BoundDefaultOperator(Syntax, type) { WasCompilerGenerated = true };
        }

        internal BoundStatement Try(
            BoundBlock tryBlock,
            ImmutableArray<BoundCatchBlock> catchBlocks,
            BoundBlock finallyBlock = null)
        {
            return new BoundTryStatement(Syntax, tryBlock, catchBlocks, finallyBlock) { WasCompilerGenerated = true };
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
            return new BoundCatchBlock(Syntax, ImmutableArray.Create(local), source, source.Type, exceptionFilterOpt: null, body: block);
        }

        internal BoundCatchBlock Catch(
            BoundExpression source,
            BoundBlock block)
        {
            return new BoundCatchBlock(Syntax, ImmutableArray<LocalSymbol>.Empty, source, source.Type, exceptionFilterOpt: null, body: block);
        }

        internal BoundTryStatement Fault(BoundBlock tryBlock, BoundBlock faultBlock)
        {
            return new BoundTryStatement(Syntax, tryBlock, ImmutableArray<BoundCatchBlock>.Empty, faultBlock, preferFaultHandler: true);
        }

        internal BoundExpression NullOrDefault(TypeSymbol typeSymbol)
        {
            return typeSymbol.IsValueType ? Default(typeSymbol) : Null(typeSymbol);
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
        public BoundLocal StoreToTemp(BoundExpression argument, out BoundAssignmentOperator store, RefKind refKind = RefKind.None, TempKind tempKind = TempKind.None)
        {
            MethodSymbol containingMethod = this.CurrentMethod;
            var syntax = argument.Syntax;
            var type = argument.Type;
            var local = new BoundLocal(
                syntax,
                new SynthesizedLocal(containingMethod, type, syntax: (tempKind == TempKind.None) ? null : syntax, refKind: refKind, tempKind: tempKind),
                null,
                type);
            store = new BoundAssignmentOperator(
                syntax,
                local,
                argument,
                refKind,
                type);
            return local;
        }

        internal BoundStatement NoOp(NoOpStatementFlavor noOpStatementFlavor)
        {
            return new BoundNoOpStatement(Syntax, noOpStatementFlavor);
        }
    }
}
