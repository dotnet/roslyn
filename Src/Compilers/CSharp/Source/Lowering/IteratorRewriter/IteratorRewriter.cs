// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class IteratorRewriter : StateMachineRewriter
    {
        /// <summary>
        /// Rewrite an iterator method into a state machine class.
        /// </summary>
        /// <param name="body">The original body of the method</param>
        /// <param name="method">The method's identity</param>
        /// <param name="compilationState">The collection of generated methods that result from this transformation and which must be emitted</param>
        /// <param name="diagnostics">Diagnostic bag for diagnostics.</param>
        /// <param name="generateDebugInfo"></param>
        internal static BoundStatement Rewrite(
            BoundStatement body,
            MethodSymbol method,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics,
            bool generateDebugInfo)
        {
            TypeSymbol elementType = method.IteratorElementType;
            if ((object)elementType == null)
            {
                return body;
            }

            // Figure out what kind of iterator we are generating.
            bool isEnumerable;
            switch (method.ReturnType.OriginalDefinition.SpecialType)
            {
                case SpecialType.System_Collections_IEnumerable:
                case SpecialType.System_Collections_Generic_IEnumerable_T:
                    isEnumerable = true;
                    break;

                case SpecialType.System_Collections_IEnumerator:
                case SpecialType.System_Collections_Generic_IEnumerator_T:
                    isEnumerable = false;
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(method.ReturnType.OriginalDefinition.SpecialType);
            }

            var iteratorClass = new IteratorClass(method, isEnumerable, elementType, compilationState);
            return new IteratorRewriter(body, method, isEnumerable, iteratorClass, compilationState, diagnostics, generateDebugInfo).Rewrite();
        }

        private readonly TypeSymbol elementType;
        private readonly bool isEnumerable;

        private FieldSymbol currentField;
        private FieldSymbol initialThreadIdField;

        private IteratorRewriter(
            BoundStatement body,
            MethodSymbol method,
            bool isEnumerable,
            IteratorClass iteratorClass,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics,
            bool generateDebugInfo)
            : base(body, method, iteratorClass, compilationState, diagnostics, generateDebugInfo)
        {
            // the element type may contain method type parameters, which are now alpha-renamed into type parameters of the generated class
            this.elementType = iteratorClass.ElementType;

            this.isEnumerable = isEnumerable;
        }

        protected override bool PreserveInitialLocals
        {
            get { return isEnumerable; }
        }

        protected override void GenerateFields()
        {
            // Add a field: T current
            currentField = F.SynthesizeField(elementType, GeneratedNames.MakeIteratorCurrentBackingFieldName());

            // if it is an iterable, add a field: int initialThreadId
            var threadType = F.Compilation.GetWellKnownType(WellKnownType.System_Threading_Thread);
            initialThreadIdField = isEnumerable && (object)threadType != null && !threadType.IsErrorType()
                ? F.SynthesizeField(F.SpecialType(SpecialType.System_Int32), GeneratedNames.MakeIteratorCurrentThreadIdName())
                : null;
        }

        protected override void GenerateMethodImplementations()
        {
            try
            {
                GenerateMethodImplementationsInternal();
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
            }
        }

        private void GenerateMethodImplementationsInternal()
        {
            BoundExpression managedThreadId = null; // Thread.CurrentThread.ManagedThreadId

            // Add bool IEnumerator.MoveNext() and void IDisposable.Dispose()
            {
                var disposeMethod = F.OpenMethodImplementation(SpecialMember.System_IDisposable__Dispose, debuggerHidden: true);
                var moveNextMethod = F.OpenMethodImplementation(SpecialMember.System_Collections_IEnumerator__MoveNext, methodName: "MoveNext");
                GenerateMoveNextAndDispose(moveNextMethod, disposeMethod);
            }

            if (isEnumerable)
            {
                // generate the code for GetEnumerator()
                // .NET Core has removed the Thread class. We can the managed thread id by making a call to 
                // Environment.CurrentManagedThreadId. If that method is not present (pre 4.5) fall back to the old behavior.
                //    IEnumerable<elementType> result;
                //    if (this.initialThreadId == Thread.CurrentThread.ManagedThreadId && this.state == -2)
                //    {
                //        this.state = 0;
                //        result = this;
                //    }
                //    else
                //    {
                //        result = new Ints0_Impl(0);
                //    }
                //    result.parameter = this.parameterProxy; // copy all of the parameter proxies

                // Add IEnumerator<int> IEnumerable<int>.GetEnumerator()
                var getEnumeratorGeneric = F.OpenMethodImplementation(
                    F.SpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(elementType),
                    SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator, debuggerHidden: true);

                var bodyBuilder = ArrayBuilder<BoundStatement>.GetInstance();
                var resultVariable = F.SynthesizedLocal(stateMachineClass, null);      // iteratorClass result;
                BoundStatement makeIterator = F.Assignment(F.Local(resultVariable), F.New(stateMachineClass.Constructor, F.Literal(0))); // result = new IteratorClass(0)

                var thisInitialized = F.GenerateLabel("thisInitialized");

                if ((object)initialThreadIdField != null)
                {
                    MethodSymbol currentManagedThreadIdMethod = null;

                    PropertySymbol currentManagedThreadIdProperty = F.WellKnownMember(WellKnownMember.System_Environment__CurrentManagedThreadId, isOptional: true) as PropertySymbol;

                    if ((object)currentManagedThreadIdProperty != null)
                    {
                        currentManagedThreadIdMethod = currentManagedThreadIdProperty.GetMethod;
                    }

                    if ((object)currentManagedThreadIdMethod != null)
                    {
                        managedThreadId = F.Call(null, currentManagedThreadIdMethod);
                    }
                    else
                    {
                        managedThreadId = F.Property(F.Property(WellKnownMember.System_Threading_Thread__CurrentThread), WellKnownMember.System_Threading_Thread__ManagedThreadId);
                    }

                    makeIterator = F.If(
                        condition: F.LogicalAnd(                                   // if (this.state == -2 && this.initialThreadId == Thread.CurrentThread.ManagedThreadId)
                            F.IntEqual(F.Field(F.This(), stateField), F.Literal(StateMachineStates.FinishedStateMachine)),
                            F.IntEqual(F.Field(F.This(), initialThreadIdField), managedThreadId)),
                        thenClause: F.Block(                                       // then
                            F.Assignment(F.Field(F.This(), stateField), F.Literal(StateMachineStates.FirstUnusedState)),  // this.state = 0;
                            F.Assignment(F.Local(resultVariable), F.This()),       // result = this;
                            method.IsStatic || method.ThisParameter.Type.IsReferenceType?   // if this is a reference type, no need to copy it since it is not assignable
                                F.Goto(thisInitialized) :                          // goto thisInitialized
                                (BoundStatement)F.Block()),                              
                        elseClause:
                            makeIterator // else result = new IteratorClass(0)
                        );
                }
                bodyBuilder.Add(makeIterator);

                // Initialize all the parameter copies
                var copySrc = initialParameters;
                var copyDest = variableProxies;
                if (!method.IsStatic)
                {
                    // starting with "this"
                    CapturedSymbolReplacement proxy;
                    if (copyDest.TryGetValue(method.ThisParameter, out proxy))
                    {
                        bodyBuilder.Add(
                            F.Assignment(
                                proxy.Replacement(F.Syntax, stateMachineType => F.Local(resultVariable)),
                                copySrc[method.ThisParameter].Replacement(F.Syntax, stateMachineType => F.This())));
                    }
                }

                bodyBuilder.Add(F.Label(thisInitialized));

                foreach (var parameter in method.Parameters)
                {
                    CapturedSymbolReplacement proxy;
                    if (copyDest.TryGetValue(parameter, out proxy))
                    {
                        bodyBuilder.Add(
                            F.Assignment(
                                proxy.Replacement(F.Syntax, stateMachineType => F.Local(resultVariable)),
                                copySrc[parameter].Replacement(F.Syntax, stateMachineType => F.This())));
                    }
                }

                bodyBuilder.Add(F.Return(F.Local(resultVariable)));
                F.CloseMethod(F.Block(ImmutableArray.Create<LocalSymbol>(resultVariable), bodyBuilder.ToImmutableAndFree()));

                // Generate IEnumerable.GetEnumerator
                var getEnumerator = F.OpenMethodImplementation(SpecialMember.System_Collections_IEnumerable__GetEnumerator, debuggerHidden: true);
                F.CloseMethod(F.Return(F.Call(F.This(), getEnumeratorGeneric)));
            }

            // Add T IEnumerator<T>.Current
            {
                F.OpenPropertyImplementation(
                    F.SpecialType(SpecialType.System_Collections_Generic_IEnumerator_T).Construct(elementType),
                    SpecialMember.System_Collections_Generic_IEnumerator_T__Current,
                    debuggerHidden: true);

                F.CloseMethod(F.Return(F.Field(F.This(), currentField)));
            }

            // Add void IEnumerator.Reset()
            {
                F.OpenMethodImplementation(SpecialMember.System_Collections_IEnumerator__Reset, debuggerHidden: true);
                F.CloseMethod(F.Throw(F.New(F.WellKnownType(WellKnownType.System_NotSupportedException))));
            }

            // Add object IEnumerator.Current
            {
                F.OpenPropertyImplementation(SpecialMember.System_Collections_IEnumerator__Current, debuggerHidden: true);
                F.CloseMethod(F.Return(F.Field(F.This(), currentField)));
            }

            // Add a body for the constructor
            {
                F.CurrentMethod = stateMachineClass.Constructor;
                var bodyBuilder = ArrayBuilder<BoundStatement>.GetInstance();
                bodyBuilder.Add(F.BaseInitialization());
                bodyBuilder.Add(F.Assignment(F.Field(F.This(), stateField), F.Parameter(F.CurrentMethod.Parameters[0]))); // this.state = state;

                if (isEnumerable && (object)initialThreadIdField != null)
                {
                    // this.initialThreadId = Thread.CurrentThread.ManagedThreadId;
                    bodyBuilder.Add(F.Assignment(F.Field(F.This(), initialThreadIdField), managedThreadId));
                }

                bodyBuilder.Add(F.Return());
                F.CloseMethod(F.Block(bodyBuilder.ToImmutableAndFree()));
                bodyBuilder = null;
            }
        }

        protected override bool IsStateFieldPublic
        {
            get { return false; }
        }

        protected override void InitializeStateMachine(ArrayBuilder<BoundStatement> bodyBuilder, NamedTypeSymbol frameType, LocalSymbol stateMachineLocal)
        {
            // var stateMachineLocal = new IteratorImplementationClass(N)
            // where N is either 0 (if we're producing an enumerator) or -2 (if we're producing an enumerable)
            int initialState = isEnumerable ? StateMachineStates.FinishedStateMachine : StateMachineStates.FirstUnusedState;
            bodyBuilder.Add(
                F.Assignment(
                    F.Local(stateMachineLocal),
                    F.New(stateMachineClass.Constructor.AsMember(frameType), F.Literal(initialState))));
        }

        protected override BoundStatement GenerateReplacementBody(LocalSymbol stateMachineVariable, NamedTypeSymbol frameType)
        {
            return F.Return(F.Local(stateMachineVariable));
        }

        private void GenerateMoveNextAndDispose(
            SynthesizedImplementationMethod moveNextMethod,
            SynthesizedImplementationMethod disposeMethod)
        {
            var rewriter = new IteratorMethodToClassRewriter(
                F,
                method,
                stateField,
                currentField,
                variablesCaptured,
                variableProxies,
                diagnostics,
                generateDebugInfo);

            rewriter.GenerateMoveNextAndDispose(body, moveNextMethod, disposeMethod);
        }
    }
}
