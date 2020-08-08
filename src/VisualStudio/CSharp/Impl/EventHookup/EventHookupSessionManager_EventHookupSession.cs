// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    internal sealed partial class EventHookupSessionManager
    {
        /// <summary>
        /// A session begins when an '=' is typed after a '+' and requires determining whether the
        /// += is being used to add an event handler to an event. If it is, then we also determine 
        /// a candidate name for the event handler.
        /// </summary>
        internal class EventHookupSession : ForegroundThreadAffinitizedObject
        {
            public readonly Task<string> GetEventNameTask;
            private readonly CancellationTokenSource _cancellationTokenSource;
            private readonly ITrackingPoint _trackingPoint;
            private readonly ITrackingSpan _trackingSpan;
            private readonly ITextView _textView;
            private readonly ITextBuffer _subjectBuffer;

            public event Action Dismissed = () => { };

            // For testing purposes only! Should always be null except in tests.
            internal Mutex TESTSessionHookupMutex = null;

            public ITrackingPoint TrackingPoint
            {
                get
                {
                    AssertIsForeground();
                    return _trackingPoint;
                }
            }

            public ITrackingSpan TrackingSpan
            {
                get
                {
                    AssertIsForeground();
                    return _trackingSpan;
                }
            }

            public ITextView TextView
            {
                get
                {
                    AssertIsForeground();
                    return _textView;
                }
            }

            public ITextBuffer SubjectBuffer
            {
                get
                {
                    AssertIsForeground();
                    return _subjectBuffer;
                }
            }

            public void Cancel()
            {
                AssertIsForeground();
                _cancellationTokenSource.Cancel();
            }

            public EventHookupSession(
                EventHookupSessionManager eventHookupSessionManager,
                EventHookupCommandHandler commandHandler,
                ITextView textView,
                ITextBuffer subjectBuffer,
                IAsynchronousOperationListener asyncListener,
                Mutex testSessionHookupMutex)
                : base(eventHookupSessionManager.ThreadingContext)
            {
                AssertIsForeground();
                _cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _cancellationTokenSource.Token;
                _textView = textView;
                _subjectBuffer = subjectBuffer;
                this.TESTSessionHookupMutex = testSessionHookupMutex;

                var document = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document != null && document.Project.Solution.Workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
                {
                    var position = textView.GetCaretPoint(subjectBuffer).Value.Position;
                    _trackingPoint = textView.TextSnapshot.CreateTrackingPoint(position, PointTrackingMode.Negative);
                    _trackingSpan = textView.TextSnapshot.CreateTrackingSpan(new Span(position, 1), SpanTrackingMode.EdgeInclusive);

                    var asyncToken = asyncListener.BeginAsyncOperation(GetType().Name + ".Start");

                    this.GetEventNameTask = Task.Factory.SafeStartNewFromAsync(
                        () => DetermineIfEventHookupAndGetHandlerNameAsync(document, position, cancellationToken),
                        cancellationToken,
                        TaskScheduler.Default);

                    var continuedTask = this.GetEventNameTask.SafeContinueWithFromAsync(
                        async t =>
                        {
                            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);

                            if (t.Result != null)
                            {
                                commandHandler.EventHookupSessionManager.EventHookupFoundInSession(this);
                            }
                        },
                        cancellationToken,
                        TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);

                    continuedTask.CompletesAsyncOperation(asyncToken);
                }
                else
                {
                    _trackingPoint = textView.TextSnapshot.CreateTrackingPoint(0, PointTrackingMode.Negative);
                    _trackingSpan = textView.TextSnapshot.CreateTrackingSpan(new Span(), SpanTrackingMode.EdgeInclusive);
                    this.GetEventNameTask = SpecializedTasks.Null<string>();
                    eventHookupSessionManager.CancelAndDismissExistingSessions();
                }
            }

            private async Task<string> DetermineIfEventHookupAndGetHandlerNameAsync(Document document, int position, CancellationToken cancellationToken)
            {
                AssertIsBackground();

                // For test purposes only!
                if (TESTSessionHookupMutex != null)
                {
                    TESTSessionHookupMutex.WaitOne();
                    TESTSessionHookupMutex.ReleaseMutex();
                }

                using (Logger.LogBlock(FunctionId.EventHookup_Determine_If_Event_Hookup, cancellationToken))
                {
                    var plusEqualsToken = await GetPlusEqualsTokenInsideAddAssignExpressionAsync(document, position, cancellationToken).ConfigureAwait(false);
                    if (plusEqualsToken == null)
                    {
                        return null;
                    }

                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                    var eventSymbol = GetEventSymbol(semanticModel, plusEqualsToken.Value, cancellationToken);
                    if (eventSymbol == null)
                    {
                        return null;
                    }

                    var namingRule = await document.GetApplicableNamingRuleAsync(
                        new SymbolKindOrTypeKind(MethodKind.Ordinary),
                        new DeclarationModifiers(isStatic: plusEqualsToken.Value.Parent.IsInStaticContext()),
                        Accessibility.Private, cancellationToken).ConfigureAwait(false);

                    return GetEventHandlerName(
                        eventSymbol, plusEqualsToken.Value, semanticModel,
                        document.GetLanguageService<ISyntaxFactsService>(), namingRule);
                }
            }

            private async Task<SyntaxToken?> GetPlusEqualsTokenInsideAddAssignExpressionAsync(Document document, int position, CancellationToken cancellationToken)
            {
                AssertIsBackground();
                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

                if (token.Kind() != SyntaxKind.PlusEqualsToken)
                {
                    return null;
                }

                if (!token.Parent.IsKind(SyntaxKind.AddAssignmentExpression))
                {
                    return null;
                }

                return token;
            }

            private IEventSymbol GetEventSymbol(SemanticModel semanticModel, SyntaxToken plusEqualsToken, CancellationToken cancellationToken)
            {
                AssertIsBackground();
                if (!(plusEqualsToken.Parent is AssignmentExpressionSyntax parentToken))
                {
                    return null;
                }

                var symbol = semanticModel.GetSymbolInfo(parentToken.Left, cancellationToken).Symbol;
                if (symbol == null)
                {
                    return null;
                }

                return symbol as IEventSymbol;
            }

            private string GetEventHandlerName(
                IEventSymbol eventSymbol, SyntaxToken plusEqualsToken, SemanticModel semanticModel,
                ISyntaxFactsService syntaxFactsService, NamingRule namingRule)
            {
                AssertIsBackground();
                var objectPart = GetNameObjectPart(eventSymbol, plusEqualsToken, semanticModel, syntaxFactsService);
                var basename = namingRule.NamingStyle.CreateName(ImmutableArray.Create(
                    string.Format("{0}_{1}", objectPart, eventSymbol.Name)));

                var reservedNames = semanticModel.LookupSymbols(plusEqualsToken.SpanStart).Select(m => m.Name);

                return NameGenerator.EnsureUniqueness(basename, reservedNames);
            }

            /// <summary>
            /// Take another look at the LHS of the += node -- we need to figure out a default name
            /// for the event handler, and that's usually based on the object (which is usually a
            /// field of 'this', but not always) to which the event belongs. So, if the event is 
            /// something like 'button1.Click' or 'this.listBox1.Select', we want the names 
            /// 'button1' and 'listBox1' respectively. If the field belongs to 'this', then we use
            /// the name of this class, as we do if we can't make any sense out of the parse tree.
            /// </summary>
            private string GetNameObjectPart(IEventSymbol eventSymbol, SyntaxToken plusEqualsToken, SemanticModel semanticModel, ISyntaxFactsService syntaxFactsService)
            {
                AssertIsBackground();
                var parentToken = plusEqualsToken.Parent as AssignmentExpressionSyntax;

                if (parentToken.Left is MemberAccessExpressionSyntax memberAccessExpression)
                {
                    // This is expected -- it means the last thing is(probably) the event name. We 
                    // already have that in eventSymbol. What we need is the LHS of that dot.

                    var lhs = memberAccessExpression.Expression;

                    if (lhs is MemberAccessExpressionSyntax lhsMemberAccessExpression)
                    {
                        // Okay, cool.  The name we're after is in the RHS of this dot.
                        return lhsMemberAccessExpression.Name.ToString();
                    }

                    if (lhs is NameSyntax lhsNameSyntax)
                    {
                        // Even easier -- the LHS of the dot is the name itself
                        return lhsNameSyntax.ToString();
                    }
                }

                // If we didn't find an object name above, then the object name is the name of this class.
                // Note: For generic, it's ok(it's even a good idea) to exclude type variables,
                // because the name is only used as a prefix for the method name.

                var typeDeclaration = syntaxFactsService.GetContainingTypeDeclaration(
                    semanticModel.SyntaxTree.GetRoot(),
                    plusEqualsToken.SpanStart) as BaseTypeDeclarationSyntax;

                return typeDeclaration != null
                    ? typeDeclaration.Identifier.Text
                    : eventSymbol.ContainingType.Name;
            }
        }
    }
}
