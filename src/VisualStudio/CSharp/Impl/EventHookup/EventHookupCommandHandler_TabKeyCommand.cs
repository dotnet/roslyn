// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    internal partial class EventHookupCommandHandler : IChainedCommandHandler<TabKeyCommandArgs>
    {
        public void ExecuteCommand(TabKeyCommandArgs args, Action nextHandler, CommandExecutionContext cotext)
        {
            AssertIsForeground();
            if (!args.SubjectBuffer.GetFeatureOnOffOption(InternalFeatureOnOffOptions.EventHookup))
            {
                nextHandler();
                return;
            }

            if (EventHookupSessionManager.CurrentSession == null)
            {
                nextHandler();
                return;
            }

            // Handling tab is currently uncancellable.
            HandleTabWorker(args.TextView, args.SubjectBuffer, nextHandler, CancellationToken.None);
        }

        public CommandState GetCommandState(TabKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            if (EventHookupSessionManager.CurrentSession != null)
            {
                return CommandState.Available;
            }
            else
            {
                return nextHandler();
            }
        }

        private void HandleTabWorker(ITextView textView, ITextBuffer subjectBuffer, Action nextHandler, CancellationToken cancellationToken)
        {
            AssertIsForeground();

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

            // Blocking wait (if necessary) to determine whether to consume the tab and
            // generate the event handler.
            EventHookupSessionManager.CurrentSession.GetEventNameTask.Wait(cancellationToken);

            string eventHandlerMethodName = null;
            if (EventHookupSessionManager.CurrentSession.GetEventNameTask.Status == TaskStatus.RanToCompletion)
            {
                eventHandlerMethodName = EventHookupSessionManager.CurrentSession.GetEventNameTask.WaitAndGetResult(cancellationToken);
            }

            if (eventHandlerMethodName == null ||
                EventHookupSessionManager.CurrentSession.TextView != textView)
            {
                nextHandler();
                EventHookupSessionManager.CancelAndDismissExistingSessions();
                return;
            }

            // This tab means we should generate the event handler method. Begin the code
            // generation process.
            GenerateAndAddEventHandler(textView, subjectBuffer, eventHandlerMethodName, nextHandler, cancellationToken);
        }

        private void GenerateAndAddEventHandler(ITextView textView, ITextBuffer subjectBuffer, string eventHandlerMethodName, Action nextHandler, CancellationToken cancellationToken)
        {
            AssertIsForeground();

            using (Logger.LogBlock(FunctionId.EventHookup_Generate_Handler, cancellationToken))
            {
                EventHookupSessionManager.CancelAndDismissExistingSessions();

                var workspace = textView.TextSnapshot.TextBuffer.GetWorkspace();
                if (workspace == null)
                {
                    nextHandler();
                    EventHookupSessionManager.CancelAndDismissExistingSessions();
                    return;
                }

                var document = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                {
                    Contract.Fail("Event Hookup could not find the document for the IBufferView.");
                }

                var position = textView.GetCaretPoint(subjectBuffer).Value.Position;
                var solutionWithEventHandler = CreateSolutionWithEventHandler(
                    document,
                    eventHandlerMethodName,
                    position,
                    out var plusEqualTokenEndPosition,
                    cancellationToken);

                if (solutionWithEventHandler == null)
                {
                    Contract.Fail("Event Hookup could not create solution with event handler.");
                }

                // The new solution is created, so start user observable changes

                if (!workspace.TryApplyChanges(solutionWithEventHandler))
                {
                    Contract.Fail("Event Hookup could not update the solution.");
                }

                // The += token will not move during this process, so it is safe to use that
                // position as a location from which to find the identifier we're renaming.
                BeginInlineRename(workspace, textView, subjectBuffer, plusEqualTokenEndPosition, cancellationToken);
            }
        }

        private Solution CreateSolutionWithEventHandler(
            Document document,
            string eventHandlerMethodName,
            int position,
            out int plusEqualTokenEndPosition,
            CancellationToken cancellationToken)
        {
            AssertIsForeground();

            // Mark the += token with an annotation so we can find it after formatting
            var plusEqualsTokenAnnotation = new SyntaxAnnotation();

            var documentWithNameAndAnnotationsAdded = AddMethodNameAndAnnotationsToSolution(document, eventHandlerMethodName, position, plusEqualsTokenAnnotation, cancellationToken);
            var semanticDocument = SemanticDocument.CreateAsync(documentWithNameAndAnnotationsAdded, cancellationToken).WaitAndGetResult(cancellationToken);
            var updatedRoot = AddGeneratedHandlerMethodToSolution(semanticDocument, eventHandlerMethodName, plusEqualsTokenAnnotation, cancellationToken);

            if (updatedRoot == null)
            {
                plusEqualTokenEndPosition = 0;
                return null;
            }

            var simplifiedDocument = Simplifier.ReduceAsync(documentWithNameAndAnnotationsAdded.WithSyntaxRoot(updatedRoot), Simplifier.Annotation, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
            var formattedDocument = Formatter.FormatAsync(simplifiedDocument, Formatter.Annotation, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);

            var newRoot = formattedDocument.GetSyntaxRootSynchronously(cancellationToken);
            plusEqualTokenEndPosition = newRoot.GetAnnotatedNodesAndTokens(plusEqualsTokenAnnotation)
                                               .Single().Span.End;

            return document.Project.Solution.WithDocumentText(
                formattedDocument.Id, formattedDocument.GetTextSynchronously(cancellationToken));
        }

        private Document AddMethodNameAndAnnotationsToSolution(
            Document document,
            string eventHandlerMethodName,
            int position,
            SyntaxAnnotation plusEqualsTokenAnnotation,
            CancellationToken cancellationToken)
        {
            // First find the event hookup to determine if we are in a static context.
            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            var plusEqualsToken = root.FindTokenOnLeftOfPosition(position);
            var eventHookupExpression = plusEqualsToken.GetAncestor<AssignmentExpressionSyntax>();

            var textToInsert = eventHandlerMethodName + ";";
            if (!eventHookupExpression.IsInStaticContext())
            {
                // This will be simplified later if it's not needed.
                textToInsert = "this." + textToInsert;
            }

            // Next, perform a textual insertion of the event handler method name.
            var textChange = new TextChange(new TextSpan(position, 0), textToInsert);
            var newText = document.GetTextSynchronously(cancellationToken).WithChanges(textChange);
            var documentWithNameAdded = document.WithText(newText);

            // Now find the event hookup again to add the appropriate annotations.
            root = documentWithNameAdded.GetSyntaxRootSynchronously(cancellationToken);
            plusEqualsToken = root.FindTokenOnLeftOfPosition(position);
            eventHookupExpression = plusEqualsToken.GetAncestor<AssignmentExpressionSyntax>();

            var updatedEventHookupExpression = eventHookupExpression
                .ReplaceToken(plusEqualsToken, plusEqualsToken.WithAdditionalAnnotations(plusEqualsTokenAnnotation))
                .WithRight(eventHookupExpression.Right.WithAdditionalAnnotations(Simplifier.Annotation))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var rootWithUpdatedEventHookupExpression = root.ReplaceNode(eventHookupExpression, updatedEventHookupExpression);
            return documentWithNameAdded.WithSyntaxRoot(rootWithUpdatedEventHookupExpression);
        }

        private SyntaxNode AddGeneratedHandlerMethodToSolution(
            SemanticDocument document,
            string eventHandlerMethodName,
            SyntaxAnnotation plusEqualsTokenAnnotation,
            CancellationToken cancellationToken)
        {
            var root = document.Root as SyntaxNode;
            var eventHookupExpression = root.GetAnnotatedNodesAndTokens(plusEqualsTokenAnnotation).Single().AsToken().GetAncestor<AssignmentExpressionSyntax>();

            var generatedMethodSymbol = GetMethodSymbol(document, eventHandlerMethodName, eventHookupExpression, cancellationToken);

            if (generatedMethodSymbol == null)
            {
                return null;
            }

            var typeDecl = eventHookupExpression.GetAncestor<TypeDeclarationSyntax>();

            var typeDeclWithMethodAdded = CodeGenerator.AddMethodDeclaration(typeDecl, generatedMethodSymbol, document.Project.Solution.Workspace, new CodeGenerationOptions(afterThisLocation: eventHookupExpression.GetLocation()));

            return root.ReplaceNode(typeDecl, typeDeclWithMethodAdded);
        }

        private IMethodSymbol GetMethodSymbol(
            SemanticDocument semanticDocument,
            string eventHandlerMethodName,
            AssignmentExpressionSyntax eventHookupExpression,
            CancellationToken cancellationToken)
        {
            var semanticModel = semanticDocument.SemanticModel as SemanticModel;
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
                statements: ImmutableArray.Create(
                    CodeGenerationHelpers.GenerateThrowStatement(syntaxFactory, semanticDocument, "System.NotImplementedException")));
        }

        private void BeginInlineRename(Workspace workspace, ITextView textView, ITextBuffer subjectBuffer, int plusEqualTokenEndPosition, CancellationToken cancellationToken)
        {
            AssertIsForeground();

            if (_inlineRenameService.ActiveSession == null)
            {
                var document = textView.TextSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document != null)
                {
                    // In the middle of a user action, cannot cancel.
                    var root = document.GetSyntaxRootSynchronously(cancellationToken);
                    var token = root.FindTokenOnRightOfPosition(plusEqualTokenEndPosition);
                    var editSpan = token.Span;
                    var memberAccessExpression = token.GetAncestor<MemberAccessExpressionSyntax>();
                    if (memberAccessExpression != null)
                    {
                        // the event hookup might look like `MyEvent += this.GeneratedHandlerName;`
                        editSpan = memberAccessExpression.Name.Span;
                    }

                    _inlineRenameService.StartInlineSession(document, editSpan, cancellationToken);
                    textView.SetSelection(editSpan.ToSnapshotSpan(textView.TextSnapshot));
                }
            }
        }
    }
}
