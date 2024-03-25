// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup;

internal sealed partial class EventHookupSessionManager
{
    /// <summary>
    /// A session begins when an '=' is typed after a '+' and requires determining whether the
    /// += is being used to add an event handler to an event. If it is, then we also determine 
    /// a candidate name for the event handler.
    /// </summary>
    internal class EventHookupSession
    {
        private readonly IThreadingContext _threadingContext;
        private readonly ITrackingSpan _trackingSpan;
        private readonly ITextView _textView;
        private readonly ITextBuffer _subjectBuffer;
        private readonly IGlobalOptionService _globalOptions;

        public event Action Dismissed = () => { };

        // For testing purposes only! Should always be null except in tests.
        internal Mutex? TESTSessionHookupMutex = null;

        private Task<string?>? _eventNameTask;
        private CancellationTokenSource? _cancellationTokenSource;

        public ITrackingSpan TrackingSpan
        {
            get
            {
                _threadingContext.ThrowIfNotOnUIThread();
                return _trackingSpan;
            }
        }

        public ITextView TextView
        {
            get
            {
                _threadingContext.ThrowIfNotOnUIThread();
                return _textView;
            }
        }

        public ITextBuffer SubjectBuffer
        {
            get
            {
                _threadingContext.ThrowIfNotOnUIThread();
                return _subjectBuffer;
            }
        }

        public void CancelBackgroundTasks()
        {
            _threadingContext.ThrowIfNotOnUIThread();
            _cancellationTokenSource?.Cancel();
        }

        public EventHookupSession(
            EventHookupSessionManager eventHookupSessionManager,
            EventHookupCommandHandler commandHandler,
            ITextView textView,
            ITextBuffer subjectBuffer,
            int position,
            Document document,
            IAsynchronousOperationListener asyncListener,
            IGlobalOptionService globalOptions,
            Mutex testSessionHookupMutex)
        {
            _threadingContext = eventHookupSessionManager.ThreadingContext;
            _threadingContext.ThrowIfNotOnUIThread();

            _cancellationTokenSource = new();
            var cancellationToken = _cancellationTokenSource.Token;
            _textView = textView;
            _subjectBuffer = subjectBuffer;
            _globalOptions = globalOptions;
            this.TESTSessionHookupMutex = testSessionHookupMutex;

            // If the caret is at the end of the document we just create an empty span
            var length = subjectBuffer.CurrentSnapshot.Length > position + 1 ? 1 : 0;
            _trackingSpan = subjectBuffer.CurrentSnapshot.CreateTrackingSpan(new Span(position, length), SpanTrackingMode.EdgeInclusive);

            var asyncToken = asyncListener.BeginAsyncOperation(GetType().Name + ".Start");

            _eventNameTask = Task.Factory.SafeStartNewFromAsync(
                () => DetermineIfEventHookupAndGetHandlerNameAsync(document, position, cancellationToken),
                cancellationToken,
                TaskScheduler.Default);

            var continuedTask = _eventNameTask.SafeContinueWithFromAsync(
                async t =>
                {
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);

                    // Once we compute the name, update the tooltip (if we haven't already been dismissed)
                    if (this._eventNameTask != null && t.Result != null)
                    {
                        commandHandler.EventHookupSessionManager.EventHookupFoundInSession(this, t.Result);
                    }
                },
                cancellationToken,
                TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            continuedTask.CompletesAsyncOperation(asyncToken);
        }

        public (Task<string?> eventNameTask, CancellationTokenSource cancellationTokenSource) DetachEventNameTask()
        {
            _threadingContext.ThrowIfNotOnUIThread();

            Contract.ThrowIfNull(_eventNameTask);
            Contract.ThrowIfNull(_cancellationTokenSource);

            var eventNameTask = _eventNameTask;
            var cancellationTokenSource = _cancellationTokenSource;

            _eventNameTask = null;
            _cancellationTokenSource = null;

            return (eventNameTask, cancellationTokenSource);
        }

        private async Task<string?> DetermineIfEventHookupAndGetHandlerNameAsync(Document document, int position, CancellationToken cancellationToken)
        {
            _threadingContext.ThrowIfNotOnBackgroundThread();

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

                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var eventSymbol = GetEventSymbol(semanticModel, plusEqualsToken.Value, cancellationToken);
                if (eventSymbol == null)
                {
                    return null;
                }

                var namingRule = await document.GetApplicableNamingRuleAsync(
                    new SymbolKindOrTypeKind(MethodKind.Ordinary),
                    new DeclarationModifiers(isStatic: plusEqualsToken.Value.GetRequiredParent().IsInStaticContext()),
                    Accessibility.Private,
                    _globalOptions.CreateProvider(),
                    cancellationToken).ConfigureAwait(false);

                return GetEventHandlerName(
                    eventSymbol, plusEqualsToken.Value, semanticModel,
                    document.GetRequiredLanguageService<ISyntaxFactsService>(), namingRule);
            }
        }

        private async Task<SyntaxToken?> GetPlusEqualsTokenInsideAddAssignExpressionAsync(Document document, int position, CancellationToken cancellationToken)
        {
            _threadingContext.ThrowIfNotOnBackgroundThread();
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
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

        private IEventSymbol? GetEventSymbol(SemanticModel semanticModel, SyntaxToken plusEqualsToken, CancellationToken cancellationToken)
        {
            _threadingContext.ThrowIfNotOnBackgroundThread();
            if (plusEqualsToken.Parent is not AssignmentExpressionSyntax parentToken)
            {
                return null;
            }

            return semanticModel.GetSymbolInfo(parentToken.Left, cancellationToken).Symbol as IEventSymbol;
        }

        private string? GetEventHandlerName(
            IEventSymbol eventSymbol, SyntaxToken plusEqualsToken, SemanticModel semanticModel,
            ISyntaxFactsService syntaxFactsService, NamingRule namingRule)
        {
            _threadingContext.ThrowIfNotOnBackgroundThread();
            var objectPart = GetNameObjectPart(eventSymbol, plusEqualsToken, semanticModel, syntaxFactsService);
            if (objectPart is null)
                return null;

            var basename = namingRule.NamingStyle.CreateName([string.Format("{0}_{1}", objectPart, eventSymbol.Name)]);

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
        private string? GetNameObjectPart(IEventSymbol eventSymbol, SyntaxToken plusEqualsToken, SemanticModel semanticModel, ISyntaxFactsService syntaxFactsService)
        {
            _threadingContext.ThrowIfNotOnBackgroundThread();
            if (plusEqualsToken.Parent is not AssignmentExpressionSyntax assignmentExpression)
                return null;

            if (assignmentExpression.Left is MemberAccessExpressionSyntax memberAccessExpression)
            {
                // This is expected -- it means the last thing is(probably) the event name. We 
                // already have that in eventSymbol. What we need is the LHS of that dot.

                var lhs = memberAccessExpression.Expression.GetRightmostName();

                if (lhs is GenericNameSyntax lhsGenericNameSyntax)
                {
                    // For generic we must exclude type variables
                    return lhsGenericNameSyntax.Identifier.Text;
                }

                if (lhs != null)
                {
                    return lhs.ToString();
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
