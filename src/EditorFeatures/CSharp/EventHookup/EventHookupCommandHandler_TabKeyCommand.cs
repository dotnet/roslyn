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
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup;

internal partial class EventHookupCommandHandler : IChainedCommandHandler<TabKeyCommandArgs>
{
    private static readonly SyntaxAnnotation s_plusEqualsTokenAnnotation = new();

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
            nextHandler();
        }

        // We always dismiss the tracking session once a tab has gone through.  Either we didn't handle it (and
        // nextHandler was called above).  Or we did handle it, in which case the bg async work owns the experience now.
        EventHookupSessionManager.DismissExistingSessions();
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
        // before we kick off the work to emit hte event.  Detach the bg work that was already kicked off so that we
        // own its lifetime from now on.
        var (eventNameTask, eventNameTokenSource) = EventHookupSessionManager.CurrentSession.DetachEventNameTask();
        var applicableToSpan = EventHookupSessionManager.CurrentSession.TrackingSpan.GetSpan(currentSnapshot);

        var task = ExecuteCommandAsync(
            args,
            nextHandler,
            applicableToSpan,
            document,
            eventNameTask,
            eventNameTokenSource,
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
        Task<string?> eventNameTask,
        CancellationTokenSource eventNameCancellationTokenSource,
        SnapshotPoint initialCaretPoint)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        var textView = args.TextView;
        var subjectBuffer = args.SubjectBuffer;

        // Don't want any exceptions bubble up from this point on (they have no where to go since we effectively did a
        // fire-and-forget).  So we instead handle things ourselves, reporting NFWs if unforseen things happened.
        try
        {
            if (!await TryExecuteCommandAsync().ConfigureAwait(true))
            {
                _threadingContext.ThrowIfNotOnUIThread();

                // We didn't successfully handle the command.  If no other changes have gotten through in the mean time,
                // then attempt to send the tab through to the editor.  If other changes went through, don't send the
                // tab through as it's likely to make things worse.
                if (applicableToSpan.Snapshot.Version == subjectBuffer.CurrentSnapshot.Version &&
                    textView.GetCaretPoint(subjectBuffer) == initialCaretPoint)
                {
                    nextHandler();
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) when (FatalError.ReportAndCatch(ex))
        {
        }
        finally
        {
            // Once we finish doing our own work (including potentially cancelling out), ensure that any BG worked
            // kicked off to compute the event name is canceled as well so it doesn't keep consuming resources.
            eventNameCancellationTokenSource.Cancel();
        }

        return;

        async Task<bool> TryExecuteCommandAsync()
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var factory = document.Project.Solution.Workspace.Services.GetRequiredService<IBackgroundWorkIndicatorFactory>();
            using var waitContext = factory.Create(
                textView,
                applicableToSpan,
                CSharpEditorResources.Generating_event);

            var cancellationToken = waitContext.UserCancellationToken;

            var solutionAndRenameSpan = await TryGetNewSolutionWithAddedMethodAsync(
                document, eventNameTask, initialCaretPoint.Position, cancellationToken).ConfigureAwait(true);
            if (solutionAndRenameSpan is null)
                return false;

            _threadingContext.ThrowIfNotOnUIThread();

            // If anything changed in the view between computation and application, bail out.
            if (applicableToSpan.Snapshot.Version != subjectBuffer.CurrentSnapshot.Version ||
                textView.GetCaretPoint(subjectBuffer) != initialCaretPoint)
            {
                return false;
            }

            // We're about to make an edit ourselves.  so disable the cancellation that happens on editing.
            waitContext.CancelOnEdit = false;

            var workspace = document.Project.Solution.Workspace;
            if (!workspace.TryApplyChanges(solutionAndRenameSpan.Value.solution))
                return false;

            var renameSpan = solutionAndRenameSpan.Value.renameSpan;
            if (_inlineRenameService.ActiveSession is null)
            {
                var updatedDocument = workspace.CurrentSolution.GetRequiredDocument(document.Id);
                _inlineRenameService.StartInlineSession(updatedDocument, renameSpan, cancellationToken);
            }

            textView.SetSelection(renameSpan.ToSnapshotSpan(subjectBuffer.CurrentSnapshot));
            return true;
        }
    }

    private async Task<(Solution solution, TextSpan renameSpan)?> TryGetNewSolutionWithAddedMethodAsync(
        Document document, Task<string?> eventNameTask, int position, CancellationToken cancellationToken)
    {
        var eventHandlerMethodName = await eventNameTask.WithCancellation(cancellationToken).ConfigureAwait(false);
        if (eventHandlerMethodName is null)
            return null;

        // Mark the += token with an annotation so we can find it after formatting
        var documentWithNameAndAnnotationsAdded = await AddMethodNameAndAnnotationsToSolutionAsync(
            document, eventHandlerMethodName, position, cancellationToken).ConfigureAwait(false);
        if (documentWithNameAndAnnotationsAdded is null)
            return null;

        var semanticDocument = await SemanticDocument.CreateAsync(
            documentWithNameAndAnnotationsAdded, cancellationToken).ConfigureAwait(false);
        var options = (CSharpCodeGenerationOptions)await document.GetCodeGenerationOptionsAsync(
            _globalOptions, cancellationToken).ConfigureAwait(false);
        var updatedRoot = AddGeneratedHandlerMethodToSolution(
            semanticDocument, options, eventHandlerMethodName, cancellationToken);

        if (updatedRoot == null)
            return null;

        var cleanupOptions = await documentWithNameAndAnnotationsAdded.GetCodeCleanupOptionsAsync(
            _globalOptions, cancellationToken).ConfigureAwait(false);
        var simplifiedDocument = await Simplifier.ReduceAsync(
            documentWithNameAndAnnotationsAdded.WithSyntaxRoot(updatedRoot), Simplifier.Annotation, cleanupOptions.SimplifierOptions, cancellationToken).ConfigureAwait(false);
        var formattedDocument = await Formatter.FormatAsync(
            simplifiedDocument, Formatter.Annotation, cleanupOptions.FormattingOptions, cancellationToken).ConfigureAwait(false);

        var newRoot = await formattedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var plusEqualTokenEndPosition = newRoot
            .GetAnnotatedNodesAndTokens(s_plusEqualsTokenAnnotation)
            .Single().Span.End;

        var newText = await formattedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var newSolution = document.Project.Solution.WithDocumentText(formattedDocument.Id, newText);

        var finalDocument = newSolution.GetRequiredDocument(formattedDocument.Id);
        var finalRoot = await finalDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
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

    private static async Task<Document?> AddMethodNameAndAnnotationsToSolutionAsync(
        Document document,
        string eventHandlerMethodName,
        int position,
        CancellationToken cancellationToken)
    {
        // First find the event hookup to determine if we are in a static context.
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var plusEqualsToken = root.FindTokenOnLeftOfPosition(position);

        if (plusEqualsToken.Parent is not AssignmentExpressionSyntax eventHookupExpression)
            return null;

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
        newText = newText.WithChanges(textChange);
        var documentWithNameAdded = document.WithText(newText);

        // Now find the event hookup again to add the appropriate annotations.
        root = await documentWithNameAdded.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        plusEqualsToken = root.FindTokenOnLeftOfPosition(position);
        if (plusEqualsToken.Parent is not AssignmentExpressionSyntax)
            return null;

        eventHookupExpression = (AssignmentExpressionSyntax)plusEqualsToken.Parent;
        if (eventHookupExpression is null)
            return null;

        var updatedEventHookupExpression = eventHookupExpression
            .ReplaceToken(plusEqualsToken, plusEqualsToken.WithAdditionalAnnotations(s_plusEqualsTokenAnnotation))
            .WithRight(eventHookupExpression.Right.WithAdditionalAnnotations(Simplifier.Annotation))
            .WithAdditionalAnnotations(Formatter.Annotation);

        var rootWithUpdatedEventHookupExpression = root.ReplaceNode(eventHookupExpression, updatedEventHookupExpression);
        return documentWithNameAdded.WithSyntaxRoot(rootWithUpdatedEventHookupExpression);
    }

    private static SyntaxNode? AddGeneratedHandlerMethodToSolution(
        SemanticDocument document,
        CSharpCodeGenerationOptions options,
        string eventHandlerMethodName,
        CancellationToken cancellationToken)
    {
        var root = document.Root;
        var eventHookupExpression = root
            .GetAnnotatedNodesAndTokens(s_plusEqualsTokenAnnotation).Single().AsToken()
            .GetAncestor<AssignmentExpressionSyntax>();
        Contract.ThrowIfNull(eventHookupExpression);

        var typeDecl = eventHookupExpression.GetAncestor<TypeDeclarationSyntax>();

        var generatedMethodSymbol = GetMethodSymbol(document, eventHandlerMethodName, eventHookupExpression, cancellationToken);
        if (generatedMethodSymbol == null)
            return null;

        var container = (SyntaxNode?)typeDecl ?? eventHookupExpression.GetAncestor<CompilationUnitSyntax>()!;

        var codeGenerator = document.Document.GetRequiredLanguageService<ICodeGenerationService>();
        var codeGenOptions = codeGenerator.GetInfo(new CodeGenerationContext(afterThisLocation: eventHookupExpression.GetLocation()), options, root.SyntaxTree.Options);
        var newContainer = codeGenerator.AddMethod(container, generatedMethodSymbol, codeGenOptions, cancellationToken);

        return root.ReplaceNode(container, newContainer);
    }

    private static IMethodSymbol? GetMethodSymbol(
        SemanticDocument semanticDocument,
        string eventHandlerMethodName,
        AssignmentExpressionSyntax eventHookupExpression,
        CancellationToken cancellationToken)
    {
        var semanticModel = semanticDocument.SemanticModel;
        var symbolInfo = semanticModel.GetSymbolInfo(eventHookupExpression.Left, cancellationToken);

        var symbol = symbolInfo.Symbol;
        if (symbol is not { Kind: SymbolKind.Event })
            return null;

        var typeInference = semanticDocument.Document.GetRequiredLanguageService<ITypeInferenceService>();
        var delegateType = typeInference.InferDelegateType(semanticModel, eventHookupExpression.Right, cancellationToken);
        if (delegateType is not { DelegateInvokeMethod: { } delegateInvokeMethod })
            return null;

        var syntaxFactory = semanticDocument.Document.GetRequiredLanguageService<SyntaxGenerator>();
        delegateInvokeMethod = delegateInvokeMethod.RemoveInaccessibleAttributesAndAttributesOfTypes(semanticDocument.SemanticModel.Compilation.Assembly);

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
            statements: [CodeGenerationHelpers.GenerateThrowStatement(syntaxFactory, semanticDocument, "System.NotImplementedException")!]);
    }
}
