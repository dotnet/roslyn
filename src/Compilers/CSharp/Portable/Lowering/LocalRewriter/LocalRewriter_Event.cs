// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.RuntimeMembers;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitEventAssignmentOperator(BoundEventAssignmentOperator node)
        {
            BoundExpression rewrittenReceiverOpt = VisitExpression(node.ReceiverOpt);
            BoundExpression rewrittenArgument = VisitExpression(node.Argument);

            if (rewrittenReceiverOpt != null && node.Event.ContainingAssembly.IsLinked && node.Event.ContainingType.IsInterfaceType())
            {
                var @interface = node.Event.ContainingType;

                foreach (var attrData in @interface.GetAttributes())
                {
                    if (attrData.IsTargetAttribute(@interface, AttributeDescription.ComEventInterfaceAttribute) &&
                        attrData.CommonConstructorArguments.Length == 2)
                    {
                        return RewriteNoPiaEventAssignmentOperator(node, rewrittenReceiverOpt, rewrittenArgument);
                    }
                }
            }

            if (node.Event.IsWindowsRuntimeEvent)
            {
                EventAssignmentKind kind = node.IsAddition ? EventAssignmentKind.Addition : EventAssignmentKind.Subtraction;
                return RewriteWindowsRuntimeEventAssignmentOperator(node.Syntax, node.Event, kind, node.IsDynamic, rewrittenReceiverOpt, rewrittenArgument);
            }

            var rewrittenArguments = ImmutableArray.Create<BoundExpression>(rewrittenArgument);

            MethodSymbol method = node.IsAddition ? node.Event.AddMethod : node.Event.RemoveMethod;
            return MakeCall(node.Syntax, rewrittenReceiverOpt, method, rewrittenArguments, node.Type);
        }

        private enum EventAssignmentKind
        {
            Assignment,
            Addition,
            Subtraction,
        }

        /// <summary>
        /// If we have a WinRT type event, we need to encapsulate the adder call
        /// (which returns an EventRegistrationToken) with a call to 
        /// WindowsRuntimeMarshal.AddEventHandler or RemoveEventHandler, but these
        /// require us to create a new Func representing the adder and another
        /// Action representing the Remover.
        /// 
        /// The rewritten call looks something like:
        /// 
        /// WindowsRuntimeMarshal.AddEventHandler&lt;EventHandler&gt;
        ///     (new Func&lt;EventHandler, EventRegistrationToken&gt;(@object.add), 
        ///      new Action&lt;EventRegistrationToken&gt;(@object.remove), handler);
        /// 
        /// Where @object is a compiler-generated local temp if needed.
        /// </summary>
        /// <remarks>
        /// TODO: use or delete isDynamic.
        /// </remarks>
        private BoundExpression RewriteWindowsRuntimeEventAssignmentOperator(CSharpSyntaxNode syntax, EventSymbol eventSymbol, EventAssignmentKind kind, bool isDynamic, BoundExpression rewrittenReceiverOpt, BoundExpression rewrittenArgument)
        {
            BoundAssignmentOperator tempAssignment = null;
            BoundLocal boundTemp = null;
            if (!eventSymbol.IsStatic && CanChangeValueBetweenReads(rewrittenReceiverOpt))
            {
                boundTemp = _factory.StoreToTemp(rewrittenReceiverOpt, out tempAssignment);
            }

            NamedTypeSymbol tokenType = _factory.WellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken);
            NamedTypeSymbol marshalType = _factory.WellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal);

            NamedTypeSymbol actionType = _factory.WellKnownType(WellKnownType.System_Action_T).Construct(tokenType);

            TypeSymbol eventType = eventSymbol.Type.TypeSymbol;

            BoundExpression delegateCreationArgument = boundTemp ?? rewrittenReceiverOpt ?? _factory.Type(eventType);

            BoundDelegateCreationExpression removeDelegate = new BoundDelegateCreationExpression(
                syntax: syntax,
                argument: delegateCreationArgument,
                methodOpt: eventSymbol.RemoveMethod,
                isExtensionMethod: false,
                type: actionType);

            BoundExpression clearCall = null;
            if (kind == EventAssignmentKind.Assignment)
            {
                MethodSymbol clearMethod;
                if (TryGetWellKnownTypeMember(syntax, WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal__RemoveAllEventHandlers, out clearMethod))
                {
                    clearCall = MakeCall(
                        syntax: syntax,
                        rewrittenReceiver: null,
                        method: clearMethod,
                        rewrittenArguments: ImmutableArray.Create<BoundExpression>(removeDelegate),
                        type: clearMethod.ReturnType.TypeSymbol);
                }
                else
                {
                    clearCall = new BoundBadExpression(syntax, LookupResultKind.NotInvocable, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(removeDelegate), ErrorTypeSymbol.UnknownResultType);
                }
            }

            ImmutableArray<BoundExpression> marshalArguments;
            WellKnownMember helper;
            if (kind == EventAssignmentKind.Subtraction)
            {
                helper = WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal__RemoveEventHandler_T;
                marshalArguments = ImmutableArray.Create<BoundExpression>(removeDelegate, rewrittenArgument);
            }
            else
            {
                NamedTypeSymbol func2Type = _factory.WellKnownType(WellKnownType.System_Func_T2).Construct(eventType, tokenType);

                BoundDelegateCreationExpression addDelegate = new BoundDelegateCreationExpression(
                    syntax: syntax,
                    argument: delegateCreationArgument,
                    methodOpt: eventSymbol.AddMethod,
                    isExtensionMethod: false,
                    type: func2Type);

                helper = WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal__AddEventHandler_T;
                marshalArguments = ImmutableArray.Create<BoundExpression>(addDelegate, removeDelegate, rewrittenArgument);
            }

            BoundExpression marshalCall;

            MethodSymbol marshalMethod;
            if (TryGetWellKnownTypeMember(syntax, helper, out marshalMethod))
            {
                marshalMethod = marshalMethod.Construct(eventType);

                marshalCall = MakeCall(
                    syntax: syntax,
                    rewrittenReceiver: null,
                    method: marshalMethod,
                    rewrittenArguments: marshalArguments,
                    type: marshalMethod.ReturnType.TypeSymbol);
            }
            else
            {
                marshalCall = new BoundBadExpression(syntax, LookupResultKind.NotInvocable, ImmutableArray<Symbol>.Empty, StaticCast<BoundNode>.From(marshalArguments), ErrorTypeSymbol.UnknownResultType);
            }

            // In this case, we don't need a sequence.
            if (boundTemp == null && clearCall == null)
            {
                return marshalCall;
            }

            ImmutableArray<LocalSymbol> tempSymbols = boundTemp == null
                ? ImmutableArray<LocalSymbol>.Empty
                : ImmutableArray.Create<LocalSymbol>(boundTemp.LocalSymbol);

            ArrayBuilder<BoundExpression> sideEffects = ArrayBuilder<BoundExpression>.GetInstance(2); //max size
            if (clearCall != null) sideEffects.Add(clearCall);
            if (tempAssignment != null) sideEffects.Add(tempAssignment);
            Debug.Assert(sideEffects.Any(), "Otherwise, we shouldn't be building a sequence");

            return new BoundSequence(syntax, tempSymbols, sideEffects.ToImmutableAndFree(), marshalCall, marshalCall.Type);
        }

        private BoundExpression VisitWindowsRuntimeEventFieldAssignmentOperator(CSharpSyntaxNode syntax, BoundEventAccess left, BoundExpression right)
        {
            Debug.Assert(left.IsUsableAsField);

            EventSymbol eventSymbol = left.EventSymbol;
            Debug.Assert(eventSymbol.HasAssociatedField);
            Debug.Assert(eventSymbol.IsWindowsRuntimeEvent);

            BoundExpression rewrittenReceiverOpt = left.ReceiverOpt == null ? null : VisitExpression(left.ReceiverOpt);
            BoundExpression rewrittenRight = VisitExpression(right);

            const bool isDynamic = false;
            return RewriteWindowsRuntimeEventAssignmentOperator(
                syntax,
                eventSymbol,
                EventAssignmentKind.Assignment,
                isDynamic,
                rewrittenReceiverOpt,
                rewrittenRight);
        }

        public override BoundNode VisitEventAccess(BoundEventAccess node)
        {
            // We didn't get here via VisitEventAssignmentOperator (i.e. += or -=),
            // so the event better be field-like.
            Debug.Assert(node.IsUsableAsField);

            BoundExpression rewrittenReceiver = VisitExpression(node.ReceiverOpt);
            return MakeEventAccess(node.Syntax, rewrittenReceiver, node.EventSymbol, node.ConstantValue, node.ResultKind, node.Type);
        }

        private BoundExpression MakeEventAccess(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenReceiver,
            EventSymbol eventSymbol,
            ConstantValue constantValueOpt,
            LookupResultKind resultKind,
            TypeSymbol type)
        {
            Debug.Assert(eventSymbol.HasAssociatedField);

            FieldSymbol fieldSymbol = eventSymbol.AssociatedField;
            Debug.Assert((object)fieldSymbol != null);

            if (!eventSymbol.IsWindowsRuntimeEvent)
            {
                return MakeFieldAccess(syntax, rewrittenReceiver, fieldSymbol, constantValueOpt, resultKind, type);
            }

            NamedTypeSymbol fieldType = (NamedTypeSymbol)fieldSymbol.Type.TypeSymbol;
            Debug.Assert(fieldType.Name == "EventRegistrationTokenTable");

            // _tokenTable
            BoundFieldAccess fieldAccess = new BoundFieldAccess(
                syntax,
                fieldSymbol.IsStatic ? null : rewrittenReceiver,
                fieldSymbol,
                constantValueOpt: null)
            { WasCompilerGenerated = true };

            BoundExpression getOrCreateCall;

            MethodSymbol getOrCreateMethod;
            if (TryGetWellKnownTypeMember(syntax, WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__GetOrCreateEventRegistrationTokenTable, out getOrCreateMethod))
            {
                getOrCreateMethod = getOrCreateMethod.AsMember(fieldType);

                // EventRegistrationTokenTable<Event>.GetOrCreateEventRegistrationTokenTable(ref _tokenTable)
                getOrCreateCall = BoundCall.Synthesized(
                    syntax,
                    receiverOpt: null,
                    method: getOrCreateMethod,
                    arg0: fieldAccess);
            }
            else
            {
                getOrCreateCall = new BoundBadExpression(syntax, LookupResultKind.NotInvocable, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(fieldAccess), ErrorTypeSymbol.UnknownResultType);
            }

            PropertySymbol invocationListProperty;
            if (TryGetWellKnownTypeMember(syntax, WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__InvocationList, out invocationListProperty))
            {
                MethodSymbol invocationListAccessor = invocationListProperty.GetMethod;

                if ((object)invocationListAccessor == null)
                {
                    string accessorName = SourcePropertyAccessorSymbol.GetAccessorName(invocationListProperty.Name,
                        getNotSet: true,
                        isWinMdOutput: invocationListProperty.IsCompilationOutputWinMdObj());
                    _diagnostics.Add(new CSDiagnosticInfo(ErrorCode.ERR_MissingPredefinedMember, invocationListProperty.ContainingType, accessorName), syntax.Location);
                }
                else
                {
                    invocationListAccessor = invocationListAccessor.AsMember(fieldType);
                    return _factory.Call(getOrCreateCall, invocationListAccessor);
                }
            }

            return new BoundBadExpression(syntax, LookupResultKind.NotInvocable, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(getOrCreateCall), ErrorTypeSymbol.UnknownResultType);
        }

        private BoundExpression RewriteNoPiaEventAssignmentOperator(BoundEventAssignmentOperator node, BoundExpression rewrittenReceiver, BoundExpression rewrittenArgument)
        {
            // In the new NoPIA scenario, myPIA.event += someevent translates into
            //
            // new System.Runtime.InteropServices.ComAwareEventInfo(typeof(myPIA), "event").AddEventHandler(myPIA, someevent)

            BoundExpression result = null;

            CSharpSyntaxNode oldSyntax = _factory.Syntax;
            _factory.Syntax = node.Syntax;


            var ctor = _factory.WellKnownMethod(WellKnownMember.System_Runtime_InteropServices_ComAwareEventInfo__ctor);

            if ((object)ctor != null)
            {
                var addRemove = _factory.WellKnownMethod(node.IsAddition ? WellKnownMember.System_Runtime_InteropServices_ComAwareEventInfo__AddEventHandler :
                                                                          WellKnownMember.System_Runtime_InteropServices_ComAwareEventInfo__RemoveEventHandler);

                if ((object)addRemove != null)
                {
                    BoundExpression eventInfo = _factory.New(ctor, _factory.Typeof(node.Event.ContainingType), _factory.Literal(node.Event.MetadataName));
                    result = _factory.Call(eventInfo, addRemove,
                                          _factory.Convert(addRemove.Parameters[0].Type.TypeSymbol, rewrittenReceiver),
                                          _factory.Convert(addRemove.Parameters[1].Type.TypeSymbol, rewrittenArgument));
                }
            }

            _factory.Syntax = oldSyntax;

            // The code we just generated doesn't contain any direct references to the event itself,
            // but the com event binder needs the event to exist on the local type. We'll poke the pia reference
            // cache directly so that the event is embedded.
            var module = this.EmitModule;
            if (module != null)
            {
                module.EmbeddedTypesManagerOpt.EmbedEventIfNeedTo(node.Event, node.Syntax, _diagnostics, isUsedForComAwareEventBinding: true);
            }

            if (result != null)
            {
                return result;
            }

            return new BoundBadExpression(node.Syntax, LookupResultKind.NotCreatable, ImmutableArray.Create<Symbol>(node.Event),
                                          ImmutableArray.Create<BoundNode>(rewrittenReceiver, rewrittenArgument), ErrorTypeSymbol.UnknownResultType);
        }
    }
}
