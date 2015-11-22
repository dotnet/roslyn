// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.RuntimeMembers;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Contains methods related to synthesizing bound nodes in initial binding 
    /// form that needs lowering, primarily method bodies for compiler-generated methods.
    /// </summary>
    internal static class MethodBodySynthesizer
    {
        internal static ImmutableArray<BoundStatement> ConstructScriptConstructorBody(
            BoundStatement loweredBody,
            MethodSymbol constructor,
            SynthesizedSubmissionFields previousSubmissionFields,
            CSharpCompilation compilation)
        {
            // Script field initializers have to be emitted after the call to the base constructor because they can refer to "this" instance.
            //
            // Unlike regular field initializers, initializers of global script variables can access "this" instance. 
            // If the base class had a constructor that initializes its state a global variable would access partially initialized object. 
            // For this reason Script class must always derive directly from a class that has no state (System.Object).

            CSharpSyntaxNode syntax = loweredBody.Syntax;

            // base constructor call:
            Debug.Assert((object)constructor.ContainingType.BaseTypeNoUseSiteDiagnostics == null || constructor.ContainingType.BaseTypeNoUseSiteDiagnostics.SpecialType == SpecialType.System_Object);
            var objectType = constructor.ContainingAssembly.GetSpecialType(SpecialType.System_Object);

            BoundExpression receiver = new BoundThisReference(syntax, constructor.ContainingType) { WasCompilerGenerated = true };

            BoundStatement baseConstructorCall =
                new BoundExpressionStatement(syntax,
                    new BoundCall(syntax,
                        receiverOpt: receiver,
                        method: objectType.InstanceConstructors[0],
                        arguments: ImmutableArray<BoundExpression>.Empty,
                        argumentNamesOpt: ImmutableArray<string>.Empty,
                        argumentRefKindsOpt: ImmutableArray<RefKind>.Empty,
                        isDelegateCall: false,
                        expanded: false,
                        invokedAsExtensionMethod: false,
                        argsToParamsOpt: ImmutableArray<int>.Empty,
                        resultKind: LookupResultKind.Viable,
                        type: objectType)
                    { WasCompilerGenerated = true })
                { WasCompilerGenerated = true };

            var statements = ArrayBuilder<BoundStatement>.GetInstance();
            statements.Add(baseConstructorCall);

            if (constructor.IsSubmissionConstructor)
            {
                // submission initialization:
                MakeSubmissionInitialization(statements, syntax, constructor, previousSubmissionFields, compilation);
            }

            statements.Add(loweredBody);
            return statements.ToImmutableAndFree();
        }

        /// <summary>
        /// Generates a submission initialization part of a Script type constructor that represents an interactive submission.
        /// </summary>
        /// <remarks>
        /// The constructor takes a parameter of type Microsoft.CodeAnalysis.Scripting.Session - the session reference.
        /// It adds the object being constructed into the session by calling Microsoft.CSharp.RuntimeHelpers.SessionHelpers.SetSubmission,
        /// and retrieves strongly typed references on all previous submission script classes whose members are referenced by this submission.
        /// The references are stored to fields of the submission (<paramref name="synthesizedFields"/>).
        /// </remarks>
        private static void MakeSubmissionInitialization(
            ArrayBuilder<BoundStatement> statements,
            CSharpSyntaxNode syntax,
            MethodSymbol submissionConstructor,
            SynthesizedSubmissionFields synthesizedFields,
            CSharpCompilation compilation)
        {
            Debug.Assert(submissionConstructor.ParameterCount == 1);

            var submissionArrayReference = new BoundParameter(syntax, submissionConstructor.Parameters[0]) { WasCompilerGenerated = true };

            var intType = compilation.GetSpecialType(SpecialType.System_Int32);
            var objectType = compilation.GetSpecialType(SpecialType.System_Object);
            var thisReference = new BoundThisReference(syntax, submissionConstructor.ContainingType) { WasCompilerGenerated = true };

            var slotIndex = compilation.GetSubmissionSlotIndex();
            Debug.Assert(slotIndex >= 0);

            // <submission_array>[<slot_index] = this;
            statements.Add(new BoundExpressionStatement(syntax,
                new BoundAssignmentOperator(syntax,
                    new BoundArrayAccess(syntax,
                        submissionArrayReference,
                        ImmutableArray.Create<BoundExpression>(new BoundLiteral(syntax, ConstantValue.Create(slotIndex), intType) { WasCompilerGenerated = true }),
                        objectType)
                    { WasCompilerGenerated = true },
                    thisReference,
                    RefKind.None,
                    thisReference.Type)
                { WasCompilerGenerated = true })
            { WasCompilerGenerated = true });

            var hostObjectField = synthesizedFields.GetHostObjectField();
            if ((object)hostObjectField != null)
            {
                // <host_object> = (<host_object_type>)<submission_array>[0]
                statements.Add(
                    new BoundExpressionStatement(syntax,
                        new BoundAssignmentOperator(syntax,
                            new BoundFieldAccess(syntax, thisReference, hostObjectField, ConstantValue.NotAvailable) { WasCompilerGenerated = true },
                            BoundConversion.Synthesized(syntax,
                                new BoundArrayAccess(syntax,
                                    submissionArrayReference,
                                    ImmutableArray.Create<BoundExpression>(new BoundLiteral(syntax, ConstantValue.Create(0), intType) { WasCompilerGenerated = true }),
                                    objectType),
                                Conversion.ExplicitReference,
                                false,
                                true,
                                ConstantValue.NotAvailable,
                                hostObjectField.Type.TypeSymbol 
                            ),
                            hostObjectField.Type.TypeSymbol)
                        { WasCompilerGenerated = true })
                    { WasCompilerGenerated = true });
            }

            foreach (var field in synthesizedFields.FieldSymbols)
            {
                var targetScriptType = (ImplicitNamedTypeSymbol)field.Type.TypeSymbol;
                var targetSubmissionIndex = targetScriptType.DeclaringCompilation.GetSubmissionSlotIndex();
                Debug.Assert(targetSubmissionIndex >= 0);

                // this.<field> = (<target_script_type>)<submission_array>[<target_submission_index>];
                statements.Add(
                    new BoundExpressionStatement(syntax,
                        new BoundAssignmentOperator(syntax,
                            new BoundFieldAccess(syntax, thisReference, field, ConstantValue.NotAvailable) { WasCompilerGenerated = true },
                            BoundConversion.Synthesized(syntax,
                                new BoundArrayAccess(syntax,
                                    submissionArrayReference,
                                    ImmutableArray.Create<BoundExpression>(new BoundLiteral(syntax, ConstantValue.Create(targetSubmissionIndex), intType) { WasCompilerGenerated = true }),
                                    objectType)
                                { WasCompilerGenerated = true },
                                Conversion.ExplicitReference,
                                false,
                                true,
                                ConstantValue.NotAvailable,
                                targetScriptType
                            ),
                            targetScriptType
                        )
                        { WasCompilerGenerated = true })
                    { WasCompilerGenerated = true });
            }
        }

        /// <summary>
        /// Construct a body for an auto-property accessor (updating or returning the backing field).
        /// </summary>
        internal static BoundBlock ConstructAutoPropertyAccessorBody(SourceMethodSymbol accessor)
        {
            Debug.Assert(accessor.MethodKind == MethodKind.PropertyGet || accessor.MethodKind == MethodKind.PropertySet);

            var property = (SourcePropertySymbol)accessor.AssociatedSymbol;
            CSharpSyntaxNode syntax = property.CSharpSyntaxNode;
            BoundExpression thisReference = null;
            if (!accessor.IsStatic)
            {
                var thisSymbol = accessor.ThisParameter;
                thisReference = new BoundThisReference(syntax, thisSymbol.Type.TypeSymbol) { WasCompilerGenerated = true };
            }

            var field = property.BackingField;
            var fieldAccess = new BoundFieldAccess(syntax, thisReference, field, ConstantValue.NotAvailable) { WasCompilerGenerated = true };
            BoundStatement statement;

            if (accessor.MethodKind == MethodKind.PropertyGet)
            {
                statement = new BoundReturnStatement(syntax, fieldAccess) { WasCompilerGenerated = true };
            }
            else
            {
                Debug.Assert(accessor.MethodKind == MethodKind.PropertySet);
                var parameter = accessor.Parameters[0];
                statement = new BoundExpressionStatement(
                    syntax,
                    new BoundAssignmentOperator(
                        syntax,
                        fieldAccess,
                        new BoundParameter(syntax, parameter) { WasCompilerGenerated = true },
                        property.Type.TypeSymbol)
                    { WasCompilerGenerated = true })
                { WasCompilerGenerated = true };
            }

            statement = new BoundSequencePoint(accessor.SyntaxNode, statement) { WasCompilerGenerated = true };

            return BoundBlock.SynthesizedNoLocals(syntax, statement);
        }

        /// <summary>
        /// Generate an accessor for a field-like event.
        /// </summary>
        internal static BoundBlock ConstructFieldLikeEventAccessorBody(SourceEventSymbol eventSymbol, bool isAddMethod, CSharpCompilation compilation, DiagnosticBag diagnostics)
        {
            Debug.Assert(eventSymbol.HasAssociatedField);
            return eventSymbol.IsWindowsRuntimeEvent
                ? ConstructFieldLikeEventAccessorBody_WinRT(eventSymbol, isAddMethod, compilation, diagnostics)
                : ConstructFieldLikeEventAccessorBody_Regular(eventSymbol, isAddMethod, compilation, diagnostics);
        }

        /// <summary>
        /// Generate a thread-safe accessor for a WinRT field-like event.
        /// 
        /// Add:
        ///   return EventRegistrationTokenTable&lt;Event&gt;.GetOrCreateEventRegistrationTokenTable(ref _tokenTable).AddEventHandler(value);
        /// 
        /// Remove:
        ///   EventRegistrationTokenTable&lt;Event&gt;.GetOrCreateEventRegistrationTokenTable(ref _tokenTable).RemoveEventHandler(value);
        /// </summary>
        internal static BoundBlock ConstructFieldLikeEventAccessorBody_WinRT(SourceEventSymbol eventSymbol, bool isAddMethod, CSharpCompilation compilation, DiagnosticBag diagnostics)
        {
            CSharpSyntaxNode syntax = eventSymbol.CSharpSyntaxNode;

            MethodSymbol accessor = isAddMethod ? eventSymbol.AddMethod : eventSymbol.RemoveMethod;
            Debug.Assert((object)accessor != null);

            FieldSymbol field = eventSymbol.AssociatedField;
            Debug.Assert((object)field != null);

            NamedTypeSymbol fieldType = (NamedTypeSymbol)field.Type.TypeSymbol;
            Debug.Assert(fieldType.Name == "EventRegistrationTokenTable");

            MethodSymbol getOrCreateMethod = (MethodSymbol)Binder.GetWellKnownTypeMember(
                compilation,
                WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__GetOrCreateEventRegistrationTokenTable,
                diagnostics,
                syntax: syntax);

            if ((object)getOrCreateMethod == null)
            {
                Debug.Assert(diagnostics.HasAnyErrors());
                return null;
            }

            getOrCreateMethod = getOrCreateMethod.AsMember(fieldType);

            WellKnownMember processHandlerMember = isAddMethod
                ? WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__AddEventHandler
                : WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__RemoveEventHandler;

            MethodSymbol processHandlerMethod = (MethodSymbol)Binder.GetWellKnownTypeMember(
                compilation,
                processHandlerMember,
                diagnostics,
                syntax: syntax);

            if ((object)processHandlerMethod == null)
            {
                Debug.Assert(diagnostics.HasAnyErrors());
                return null;
            }

            processHandlerMethod = processHandlerMethod.AsMember(fieldType);

            // _tokenTable
            BoundFieldAccess fieldAccess = new BoundFieldAccess(
                syntax,
                field.IsStatic ? null : new BoundThisReference(syntax, accessor.ThisParameter.Type.TypeSymbol),
                field,
                constantValueOpt: null)
            { WasCompilerGenerated = true };

            // EventRegistrationTokenTable<Event>.GetOrCreateEventRegistrationTokenTable(ref _tokenTable)
            BoundCall getOrCreateCall = BoundCall.Synthesized(
                syntax,
                receiverOpt: null,
                method: getOrCreateMethod,
                arg0: fieldAccess);

            // value
            BoundParameter parameterAccess = new BoundParameter(
                syntax,
                accessor.Parameters[0]);

            // EventRegistrationTokenTable<Event>.GetOrCreateEventRegistrationTokenTable(ref _tokenTable).AddHandler(value) // or RemoveHandler
            BoundCall processHandlerCall = BoundCall.Synthesized(
                syntax,
                receiverOpt: getOrCreateCall,
                method: processHandlerMethod,
                arg0: parameterAccess);

            if (isAddMethod)
            {
                // {
                //     return EventRegistrationTokenTable<Event>.GetOrCreateEventRegistrationTokenTable(ref _tokenTable).AddHandler(value);
                // }   
                BoundStatement returnStatement = BoundReturnStatement.Synthesized(syntax, processHandlerCall);
                return BoundBlock.SynthesizedNoLocals(syntax, returnStatement);
            }
            else
            {
                // {
                //     EventRegistrationTokenTable<Event>.GetOrCreateEventRegistrationTokenTable(ref _tokenTable).RemoveHandler(value);
                //     return;
                // }  
                BoundStatement callStatement = new BoundExpressionStatement(syntax, processHandlerCall);
                BoundStatement returnStatement = new BoundReturnStatement(syntax, expressionOpt: null);
                return BoundBlock.SynthesizedNoLocals(syntax, callStatement, returnStatement);
            }
        }

        /// <summary>
        /// Generate a thread-safe accessor for a regular field-like event.
        /// 
        /// DelegateType tmp0 = _event; //backing field
        /// DelegateType tmp1;
        /// DelegateType tmp2;
        /// do {
        ///     tmp1 = tmp0;
        ///     tmp2 = (DelegateType)Delegate.Combine(tmp1, value); //Remove for -=
        ///     tmp0 = Interlocked.CompareExchange&lt;DelegateType&gt;(ref _event, tmp2, tmp1);
        /// } while ((object)tmp0 != (object)tmp1);
        /// 
        /// Note, if System.Threading.Interlocked.CompareExchange&lt;T&gt; is not available,
        /// we emit the following code and mark the method Synchronized (unless it is a struct).
        /// 
        /// _event = (DelegateType)Delegate.Combine(_event, value); //Remove for -=
        /// 
        /// </summary>
        internal static BoundBlock ConstructFieldLikeEventAccessorBody_Regular(SourceEventSymbol eventSymbol, bool isAddMethod, CSharpCompilation compilation, DiagnosticBag diagnostics)
        {
            CSharpSyntaxNode syntax = eventSymbol.CSharpSyntaxNode;

            TypeSymbol delegateType = eventSymbol.Type.TypeSymbol;
            MethodSymbol accessor = isAddMethod ? eventSymbol.AddMethod : eventSymbol.RemoveMethod;
            ParameterSymbol thisParameter = accessor.ThisParameter;

            TypeSymbol boolType = compilation.GetSpecialType(SpecialType.System_Boolean);

            SpecialMember updateMethodId = isAddMethod ? SpecialMember.System_Delegate__Combine : SpecialMember.System_Delegate__Remove;
            MethodSymbol updateMethod = (MethodSymbol)compilation.GetSpecialTypeMember(updateMethodId);

            BoundStatement @return = new BoundReturnStatement(syntax,
                expressionOpt: null)
            { WasCompilerGenerated = true };

            if (updateMethod == null)
            {
                MemberDescriptor memberDescriptor = SpecialMembers.GetDescriptor(updateMethodId);
                diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_MissingPredefinedMember,
                                                                      memberDescriptor.DeclaringTypeMetadataName,
                                                                      memberDescriptor.Name),
                                                                      syntax.Location));

                return BoundBlock.SynthesizedNoLocals(syntax, @return);
            }

            Binder.ReportUseSiteDiagnostics(updateMethod, diagnostics, syntax);

            BoundThisReference fieldReceiver = eventSymbol.IsStatic ?
                null :
                new BoundThisReference(syntax, thisParameter.Type.TypeSymbol) { WasCompilerGenerated = true };

            BoundFieldAccess boundBackingField = new BoundFieldAccess(syntax,
                receiver: fieldReceiver,
                fieldSymbol: eventSymbol.AssociatedField,
                constantValueOpt: null)
            { WasCompilerGenerated = true };

            BoundParameter boundParameter = new BoundParameter(syntax,
                parameterSymbol: accessor.Parameters[0])
            { WasCompilerGenerated = true };

            BoundExpression delegateUpdate;

            MethodSymbol compareExchangeMethod = (MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Threading_Interlocked__CompareExchange_T);

            if ((object)compareExchangeMethod == null)
            {
                // (DelegateType)Delegate.Combine(_event, value)
                delegateUpdate = BoundConversion.SynthesizedNonUserDefined(syntax,
                    operand: BoundCall.Synthesized(syntax,
                        receiverOpt: null,
                        method: updateMethod,
                        arguments: ImmutableArray.Create<BoundExpression>(boundBackingField, boundParameter)),
                    kind: ConversionKind.ExplicitReference,
                    type: delegateType);

                // _event = (DelegateType)Delegate.Combine(_event, value);
                BoundStatement eventUpdate = new BoundExpressionStatement(syntax,
                    expression: new BoundAssignmentOperator(syntax,
                        left: boundBackingField,
                        right: delegateUpdate,
                        type: delegateType)
                    { WasCompilerGenerated = true })
                { WasCompilerGenerated = true };

                return BoundBlock.SynthesizedNoLocals(syntax,
                    statements: ImmutableArray.Create<BoundStatement>(
                        eventUpdate,
                        @return));
            }

            compareExchangeMethod = compareExchangeMethod.Construct(ImmutableArray.Create<TypeSymbol>(delegateType));

            Binder.ReportUseSiteDiagnostics(compareExchangeMethod, diagnostics, syntax);

            GeneratedLabelSymbol loopLabel = new GeneratedLabelSymbol("loop");

            const int numTemps = 3;

            LocalSymbol[] tmps = new LocalSymbol[numTemps];
            BoundLocal[] boundTmps = new BoundLocal[numTemps];

            for (int i = 0; i < numTemps; i++)
            {
                tmps[i] = new SynthesizedLocal(accessor, TypeSymbolWithAnnotations.Create(delegateType), SynthesizedLocalKind.LoweringTemp);
                boundTmps[i] = new BoundLocal(syntax, tmps[i], null, delegateType);
            }

            // tmp0 = _event;
            BoundStatement tmp0Init = new BoundExpressionStatement(syntax,
                expression: new BoundAssignmentOperator(syntax,
                    left: boundTmps[0],
                    right: boundBackingField,
                    type: delegateType)
                { WasCompilerGenerated = true })
            { WasCompilerGenerated = true };

            // LOOP:
            BoundStatement loopStart = new BoundLabelStatement(syntax,
                label: loopLabel)
            { WasCompilerGenerated = true };

            // tmp1 = tmp0;
            BoundStatement tmp1Update = new BoundExpressionStatement(syntax,
                expression: new BoundAssignmentOperator(syntax,
                    left: boundTmps[1],
                    right: boundTmps[0],
                    type: delegateType)
                { WasCompilerGenerated = true })
            { WasCompilerGenerated = true };

            // (DelegateType)Delegate.Combine(tmp1, value)
            delegateUpdate = BoundConversion.SynthesizedNonUserDefined(syntax,
                operand: BoundCall.Synthesized(syntax,
                    receiverOpt: null,
                    method: updateMethod,
                    arguments: ImmutableArray.Create<BoundExpression>(boundTmps[1], boundParameter)),
                kind: ConversionKind.ExplicitReference,
                type: delegateType);

            // tmp2 = (DelegateType)Delegate.Combine(tmp1, value);
            BoundStatement tmp2Update = new BoundExpressionStatement(syntax,
                expression: new BoundAssignmentOperator(syntax,
                    left: boundTmps[2],
                    right: delegateUpdate,
                    type: delegateType)
                { WasCompilerGenerated = true })
            { WasCompilerGenerated = true };

            // Interlocked.CompareExchange<DelegateType>(ref _event, tmp2, tmp1)
            BoundExpression compareExchange = BoundCall.Synthesized(syntax,
                receiverOpt: null,
                method: compareExchangeMethod,
                arguments: ImmutableArray.Create<BoundExpression>(boundBackingField, boundTmps[2], boundTmps[1]));

            // tmp0 = Interlocked.CompareExchange<DelegateType>(ref _event, tmp2, tmp1);
            BoundStatement tmp0Update = new BoundExpressionStatement(syntax,
                expression: new BoundAssignmentOperator(syntax,
                    left: boundTmps[0],
                    right: compareExchange,
                    type: delegateType)
                { WasCompilerGenerated = true })
            { WasCompilerGenerated = true };

            // tmp0 == tmp1 // i.e. exit when they are equal, jump to start otherwise
            BoundExpression loopExitCondition = new BoundBinaryOperator(syntax,
                operatorKind: BinaryOperatorKind.ObjectEqual,
                left: boundTmps[0],
                right: boundTmps[1],
                constantValueOpt: null,
                methodOpt: null,
                resultKind: LookupResultKind.Viable,
                type: boolType)
            { WasCompilerGenerated = true };

            // branchfalse (tmp0 == tmp1) LOOP
            BoundStatement loopEnd = new BoundConditionalGoto(syntax,
                condition: loopExitCondition,
                jumpIfTrue: false,
                label: loopLabel)
            { WasCompilerGenerated = true };

            return new BoundBlock(syntax,
                locals: tmps.AsImmutable(),
                localFunctions: ImmutableArray<LocalFunctionSymbol>.Empty,
                statements: ImmutableArray.Create<BoundStatement>(
                    tmp0Init,
                    loopStart,
                    tmp1Update,
                    tmp2Update,
                    tmp0Update,
                    loopEnd,
                    @return))
            { WasCompilerGenerated = true };
        }

        internal static BoundBlock ConstructDestructorBody(MethodSymbol method, BoundBlock block)
        {
            var syntax = block.Syntax;

            Debug.Assert(method.MethodKind == MethodKind.Destructor);
            Debug.Assert(syntax.Kind() == SyntaxKind.Block);

            // If this is a destructor and a base type has a Finalize method (see GetBaseTypeFinalizeMethod for exact 
            // requirements), then we need to call that method in a finally block.  Otherwise, just return block as-is.
            // NOTE: the Finalize method need not be a destructor or be overridden by the current method.
            MethodSymbol baseTypeFinalize = GetBaseTypeFinalizeMethod(method);

            if ((object)baseTypeFinalize != null)
            {
                BoundStatement baseFinalizeCall = new BoundSequencePointWithSpan( //sequence point to mimic Dev10
                    syntax,
                    new BoundExpressionStatement(
                        syntax,
                        BoundCall.Synthesized(
                            syntax,
                            new BoundBaseReference(
                                syntax,
                                method.ContainingType)
                            { WasCompilerGenerated = true },
                            baseTypeFinalize)
                        )
                    { WasCompilerGenerated = true },
                    ((BlockSyntax)syntax).CloseBraceToken.Span);

                return new BoundBlock(
                    syntax,
                    ImmutableArray<LocalSymbol>.Empty,
                    ImmutableArray<LocalFunctionSymbol>.Empty,
                    ImmutableArray.Create<BoundStatement>(
                        new BoundTryStatement(
                            syntax,
                            block,
                            ImmutableArray<BoundCatchBlock>.Empty,
                            new BoundBlock(
                                syntax,
                                ImmutableArray<LocalSymbol>.Empty,
                                ImmutableArray<LocalFunctionSymbol>.Empty,
                                ImmutableArray.Create<BoundStatement>(
                                    baseFinalizeCall)
                            )
                            { WasCompilerGenerated = true }
                        )
                        { WasCompilerGenerated = true }));
            }

            return block;
        }

        /// <summary>
        /// Look for a base type method named "Finalize" that is protected (or protected internal), has no parameters, 
        /// and returns void.  It doesn't need to be virtual or a destructor.
        /// </summary>
        /// <remarks>
        /// You may assume that this would share code and logic with PEMethodSymbol.OverridesRuntimeFinalizer, 
        /// but FUNCBRECCS::bindDestructor has its own loop that performs these checks (differently).
        /// </remarks>
        private static MethodSymbol GetBaseTypeFinalizeMethod(MethodSymbol method)
        {
            NamedTypeSymbol baseType = method.ContainingType.BaseTypeNoUseSiteDiagnostics;
            while ((object)baseType != null)
            {
                foreach (Symbol member in baseType.GetMembers(WellKnownMemberNames.DestructorName))
                {
                    if (member.Kind == SymbolKind.Method)
                    {
                        MethodSymbol baseTypeMethod = (MethodSymbol)member;
                        Accessibility accessibility = baseTypeMethod.DeclaredAccessibility;
                        if ((accessibility == Accessibility.ProtectedOrInternal || accessibility == Accessibility.Protected) &&
                            baseTypeMethod.ParameterCount == 0 &&
                            baseTypeMethod.Arity == 0 && // NOTE: the native compiler doesn't check this, so it broken IL.
                            baseTypeMethod.ReturnsVoid) // NOTE: not checking for virtual
                        {
                            return baseTypeMethod;
                        }
                    }
                }

                baseType = baseType.BaseTypeNoUseSiteDiagnostics;
            }
            return null;
        }
    }
}
