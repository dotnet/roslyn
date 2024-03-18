// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.EventHookup;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup;

internal partial class EventHookupCommandHandler : IChainedCommandHandler<TabKeyCommandArgs>
{
    public CommandState GetCommandState(TabKeyCommandArgs args, Func<CommandState> nextHandler)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        if (EventHookupSessionManager.CurrentSession != null)
        {
            return CommandState.Available;
        }
        else
        {
            return nextHandler();
        }
    }

    public void ExecuteCommand(TabKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        if (!TryExecuteCommand(args, nextHandler))
        {
            // If we didn't process this tag to emit an event handler, just pass the tab through to the buffer normally.
            EventHookupSessionManager.DismissExistingSessions(cancelBackgroundTasks: true);
            nextHandler();
        }
    }

    private bool TryExecuteCommand(TabKeyCommandArgs args, Action nextHandler)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        if (!_globalOptions.GetOption(EventHookupOptionsStorage.EventHookup))
            return false;

        if (EventHookupSessionManager.CurrentSession == null)
            return false;

        // For test purposes only!
        if (EventHookupSessionManager.CurrentSession.TESTSessionHookupMutex != null)
        {
            try
            {
                EventHookupSessionManager.CurrentSession.TESTSessionHookupMutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }
        }

        var subjectBuffer = args.SubjectBuffer;
        var caretPoint = args.TextView.GetCaretPoint(subjectBuffer);
        if (caretPoint is null)
            return false;

        var currentSnapshot = subjectBuffer.CurrentSnapshot;
        var document = currentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document is null)
            return false;

        // Now emit the event asynchronously.
        var token = _asyncListener.BeginAsyncOperation(nameof(ExecuteCommand));

        // Capture everything we need off of the session manager as we'll be dismissing the core session immediately
        // before we kick off the work to emit hte event.
        var eventNameTask = EventHookupSessionManager.CurrentSession.GetEventNameTask;
        var applicableToSpan = EventHookupSessionManager.CurrentSession.TrackingSpan.GetSpan(currentSnapshot);

        // Ensure no matter what that once tab is hit that we're back to the initial no-session state. We do not
        // want to cancel the bg tasks kicked off as we need their values to actually emit the event.
        EventHookupSessionManager.DismissExistingSessions(cancelBackgroundTasks: false);

        var task = ExecuteCommandAsync(
            args,
            nextHandler,
            applicableToSpan,
            document,
            eventNameTask,
            caretPoint.Value);
        task.CompletesAsyncOperation(token);

        // At this point, we've taken control, so don't send the tab into the buffer.  But do dismiss the overall
        // session.  We no longer need it.
        return true;
    }

    private async Task ExecuteCommandAsync(
        TabKeyCommandArgs args,
        Action nextHandler,
        SnapshotSpan applicableToSpan,
        Document document,
        Task<string> getEventNameTask,
        SnapshotPoint initialCaretPoint)
    {
        try
        {
            _threadingContext.ThrowIfNotOnUIThread();
            await ExecuteCommandWorkerAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) when (FatalError.ReportAndCatch(ex))
        {
        }

        return;

        async Task ExecuteCommandWorkerAsync()
        {
            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;

            var factory = document.Project.Solution.Workspace.Services.GetRequiredService<IBackgroundWorkIndicatorFactory>();
            using var waitContext = factory.Create(
                textView,
                applicableToSpan,
                CSharpEditorResources.Generating_event);

            var cancellationToken = waitContext.UserCancellationToken;

            var eventHandlerMethodName = await getEventNameTask.WithCancellation(cancellationToken).ConfigureAwait(false);

            var solutionAndRenameSpan = await TryGetNewSolutionWithAddedMethodAsync(
                document, eventHandlerMethodName, initialCaretPoint.Position, cancellationToken).ConfigureAwait(false);

            // We're about to make an edit ourselves.  so disable the cancellation that happens on editing.
            waitContext.CancelOnEdit = false;

            // switch back to the UI thread.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            _threadingContext.ThrowIfNotOnUIThread();

            // If anything changed in the view between computation and application, bail out.
            if (solutionAndRenameSpan is null ||
                applicableToSpan.Snapshot.Version != subjectBuffer.CurrentSnapshot.Version ||
                textView.GetCaretPoint(subjectBuffer) != initialCaretPoint)
            {
                nextHandler();
                return;
            }

            var (solutionWithEventHandler, renameSpan) = solutionAndRenameSpan.Value;
            document.Project.Solution.Workspace.TryApplyChanges(solutionWithEventHandler);
            _threadingContext.ThrowIfNotOnUIThread();

            _inlineRenameService.StartInlineSession(document, renameSpan, cancellationToken);
            textView.SetSelection(renameSpan.ToSnapshotSpan(subjectBuffer.CurrentSnapshot));
        }
    }

    private async Task<(Solution solution, TextSpan renameSpan)?> TryGetNewSolutionWithAddedMethodAsync(
        Document document, string eventHandlerMethodName, int position, CancellationToken cancellationToken)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        // Mark the += token with an annotation so we can find it after formatting
        var plusEqualsTokenAnnotation = new SyntaxAnnotation();

        var documentWithNameAndAnnotationsAdded = await AddMethodNameAndAnnotationsToSolutionAsync(
            document, eventHandlerMethodName, position, plusEqualsTokenAnnotation, cancellationToken).ConfigureAwait(false);
        var semanticDocument = await SemanticDocument.CreateAsync(
            documentWithNameAndAnnotationsAdded, cancellationToken).ConfigureAwait(false);
        var options = (CSharpCodeGenerationOptions)await document.GetCodeGenerationOptionsAsync(
            globalOptions, cancellationToken).ConfigureAwait(false);
        var updatedRoot = AddGeneratedHandlerMethodToSolution(
            semanticDocument, options, eventHandlerMethodName, plusEqualsTokenAnnotation, cancellationToken);

        if (updatedRoot == null)
            return null;

        var cleanupOptions = await documentWithNameAndAnnotationsAdded.GetCodeCleanupOptionsAsync(
            globalOptions, cancellationToken).ConfigureAwait(false);
        var simplifiedDocument = await Simplifier.ReduceAsync(
            documentWithNameAndAnnotationsAdded.WithSyntaxRoot(updatedRoot), Simplifier.Annotation, cleanupOptions.SimplifierOptions, cancellationToken).ConfigureAwait(false);
        var formattedDocument = await Formatter.FormatAsync(
            simplifiedDocument, Formatter.Annotation, cleanupOptions.FormattingOptions, cancellationToken).ConfigureAwait(false);

        var newRoot = await formattedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var plusEqualTokenEndPosition = newRoot.GetAnnotatedNodesAndTokens(plusEqualsTokenAnnotation)
                                           .Single().Span.End;

        var newText = await formattedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var newSolution = document.Project.Solution.WithDocumentText(formattedDocument.Id, newText);

        var finalDocument = newSolution.GetRequiredDocument(formattedDocument.Id);
        var finalRoot = await finalDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = finalRoot.FindTokenOnRightOfPosition(plusEqualTokenEndPosition);
        var renameSpan = token.Span;
        var memberAccessExpression = token.GetAncestor<MemberAccessExpressionSyntax>();
        if (memberAccessExpression != null)
        {
            // the event hookup might look like `MyEvent += this.GeneratedHandlerName;`
            renameSpan = memberAccessExpression.Name.Span;
        }

        return (newSolution, renameSpan);
    }

    private static async Task<Document> AddMethodNameAndAnnotationsToSolutionAsync(
        Document document,
        string eventHandlerMethodName,
        int position,
        SyntaxAnnotation plusEqualsTokenAnnotation,
        CancellationToken cancellationToken)
    {
        // First find the event hookup to determine if we are in a static context.
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var plusEqualsToken = root.FindTokenOnLeftOfPosition(position);
        var eventHookupExpression = plusEqualsToken.GetAncestor<AssignmentExpressionSyntax>();
        var typeDecl = eventHookupExpression.GetAncestor<TypeDeclarationSyntax>();

        var textToInsert = eventHandlerMethodName + ";";
        if (!eventHookupExpression.IsInStaticContext() && typeDecl is not null)
        {
            // This will be simplified later if it's not needed.
            textToInsert = "this." + textToInsert;
        }

        // Next, perform a textual insertion of the event handler method name.
        var textChange = new TextChange(new TextSpan(position, 0), textToInsert);
        var newText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var documentWithNameAdded = document.WithText(newText);

        // Now find the event hookup again to add the appropriate annotations.
        root = await documentWithNameAdded.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        plusEqualsToken = root.FindTokenOnLeftOfPosition(position);
        eventHookupExpression = plusEqualsToken.GetAncestor<AssignmentExpressionSyntax>();

        var updatedEventHookupExpression = eventHookupExpression
            .ReplaceToken(plusEqualsToken, plusEqualsToken.WithAdditionalAnnotations(plusEqualsTokenAnnotation))
            .WithRight(eventHookupExpression.Right.WithAdditionalAnnotations(Simplifier.Annotation))
            .WithAdditionalAnnotations(Formatter.Annotation);

        var rootWithUpdatedEventHookupExpression = root.ReplaceNode(eventHookupExpression, updatedEventHookupExpression);
        return documentWithNameAdded.WithSyntaxRoot(rootWithUpdatedEventHookupExpression);
    }

    private static SyntaxNode AddGeneratedHandlerMethodToSolution(
        SemanticDocument document,
        CSharpCodeGenerationOptions options,
        string eventHandlerMethodName,
        SyntaxAnnotation plusEqualsTokenAnnotation,
        CancellationToken cancellationToken)
    {
        var root = document.Root;
        var eventHookupExpression = root.GetAnnotatedNodesAndTokens(plusEqualsTokenAnnotation).Single().AsToken().GetAncestor<AssignmentExpressionSyntax>();

        var typeDecl = eventHookupExpression.GetAncestor<TypeDeclarationSyntax>();

        var generatedMethodSymbol = GetMethodSymbol(document, eventHandlerMethodName, eventHookupExpression, cancellationToken);

        if (generatedMethodSymbol == null)
        {
            return null;
        }

        var container = (SyntaxNode)typeDecl ?? eventHookupExpression.GetAncestor<CompilationUnitSyntax>();

        var codeGenerator = document.Document.GetRequiredLanguageService<ICodeGenerationService>();
        var codeGenOptions = codeGenerator.GetInfo(new CodeGenerationContext(afterThisLocation: eventHookupExpression.GetLocation()), options, root.SyntaxTree.Options);
        var newContainer = codeGenerator.AddMethod(container, generatedMethodSymbol, codeGenOptions, cancellationToken);

        return root.ReplaceNode(container, newContainer);
    }

    private static IMethodSymbol GetMethodSymbol(
        SemanticDocument semanticDocument,
        string eventHandlerMethodName,
        AssignmentExpressionSyntax eventHookupExpression,
        CancellationToken cancellationToken)
    {
        var semanticModel = semanticDocument.SemanticModel;
        var symbolInfo = semanticModel.GetSymbolInfo(eventHookupExpression.Left, cancellationToken);

        var symbol = symbolInfo.Symbol;
        if (symbol == null || symbol.Kind != SymbolKind.Event)
        {
            return null;
        }

        var typeInference = semanticDocument.Document.GetLanguageService<ITypeInferenceService>();
        var delegateType = typeInference.InferDelegateType(semanticModel, eventHookupExpression.Right, cancellationToken);
        if (delegateType == null || delegateType.DelegateInvokeMethod == null)
        {
            return null;
        }

        var syntaxFactory = semanticDocument.Document.GetLanguageService<SyntaxGenerator>();
        var delegateInvokeMethod = delegateType.DelegateInvokeMethod.RemoveInaccessibleAttributesAndAttributesOfTypes(semanticDocument.SemanticModel.Compilation.Assembly);

        return CodeGenerationSymbolFactory.CreateMethodSymbol(
            attributes: default,
            accessibility: Accessibility.Private,
            modifiers: new DeclarationModifiers(isStatic: eventHookupExpression.IsInStaticContext()),
            returnType: delegateInvokeMethod.ReturnType,
            refKind: delegateInvokeMethod.RefKind,
            explicitInterfaceImplementations: default,
            name: eventHandlerMethodName,
            typeParameters: default,
            parameters: delegateInvokeMethod.Parameters,
            statements: [CodeGenerationHelpers.GenerateThrowStatement(syntaxFactory, semanticDocument, "System.NotImplementedException")]);
    }
}
