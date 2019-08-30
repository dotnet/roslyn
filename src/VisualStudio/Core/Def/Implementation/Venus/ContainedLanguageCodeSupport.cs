// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    internal static class ContainedLanguageCodeSupport
    {
        public static bool IsValidId(Document document, string identifier)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            return syntaxFacts.IsValidIdentifier(identifier);
        }

        public static bool TryGetBaseClassName(Document document, string className, CancellationToken cancellationToken, out string baseClassName)
        {
            baseClassName = null;
            var type = document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken).GetTypeByMetadataName(className);
            if (type == null || type.BaseType == null)
            {
                return false;
            }

            baseClassName = type.BaseType.ToDisplayString();
            return true;
        }

        public static string CreateUniqueEventName(
            Document document, string className, string objectName, string nameOfEvent, CancellationToken cancellationToken)
        {
            var type = document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken).GetTypeByMetadataName(className);
            var name = objectName + "_" + nameOfEvent;

            var semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken);

            var tree = document.GetSyntaxTreeSynchronously(cancellationToken);
            var typeNode = type.DeclaringSyntaxReferences.Where(r => r.SyntaxTree == tree).Select(r => r.GetSyntax(cancellationToken)).First();
            var codeModel = document.GetLanguageService<ICodeModelNavigationPointService>();
            var options = document.GetOptionsAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken);
            var point = codeModel.GetStartPoint(typeNode, options, EnvDTE.vsCMPart.vsCMPartBody);
            var reservedNames = semanticModel.LookupSymbols(point.Value.Position, type).Select(m => m.Name);

            return NameGenerator.EnsureUniqueness(name, reservedNames, document.GetLanguageService<ISyntaxFactsService>().IsCaseSensitive);
        }

        /// <summary>
        /// Determine what methods of <paramref name=" className"/> could possibly be used as event
        /// handlers.
        /// </summary>
        /// <param name="document">The document containing <paramref name="className"/>.</param>
        /// <param name="className">The name of the type whose methods should be considered.</param>
        /// <param name="objectTypeName">The fully qualified name of the type containing a member
        /// that is an event. (E.g. "System.Web.Forms.Button")</param>
        /// <param name="nameOfEvent">The name of the member in <paramref name="objectTypeName"/>
        /// that is the event (E.g. "Clicked")</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The display name of the method, and a unique to for the method.</returns>
        public static IEnumerable<Tuple<string, string>> GetCompatibleEventHandlers(
            Document document, string className, string objectTypeName, string nameOfEvent, CancellationToken cancellationToken)
        {
            var compilation = document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken);
            var type = compilation.GetTypeByMetadataName(className);
            if (type == null)
            {
                throw new InvalidOperationException();
            }

            var eventMember = GetEventSymbol(document, objectTypeName, nameOfEvent, type, cancellationToken);
            if (eventMember == null)
            {
                throw new InvalidOperationException();
            }

            var eventType = ((IEventSymbol)eventMember).Type;
            if (eventType.Kind != SymbolKind.NamedType)
            {
                throw new InvalidOperationException(ServicesVSResources.Event_type_is_invalid);
            }

            var methods = type.GetMembers().OfType<IMethodSymbol>().Where(m => m.CompatibleSignatureToDelegate((INamedTypeSymbol)eventType));
            return methods.Select(m => Tuple.Create(m.Name, ConstructMemberId(m)));
        }

        public static string GetEventHandlerMemberId(Document document, string className, string objectTypeName, string nameOfEvent, string eventHandlerName, CancellationToken cancellationToken)
        {
            var nameAndId = GetCompatibleEventHandlers(document, className, objectTypeName, nameOfEvent, cancellationToken).SingleOrDefault(pair => pair.Item1 == eventHandlerName);
            return nameAndId == null ? null : nameAndId.Item2;
        }

        /// <summary>
        /// Ensure that an event handler exists for a given event.
        /// </summary>
        /// <param name="thisDocument">The document corresponding to this operation.</param>
        /// <param name="targetDocument">The document to generate the event handler in if it doesn't
        /// exist.</param>
        /// <param name="className">The name of the type to generate the event handler in.</param>
        /// <param name="objectName">The name of the event member (if <paramref
        /// name="useHandlesClause"/> is true)</param>
        /// <param name="objectTypeName">The name of the type containing the event.</param>
        /// <param name="nameOfEvent">The name of the event member in <paramref
        /// name="objectTypeName"/></param>
        /// <param name="eventHandlerName">The name of the method to be hooked up to the
        /// event.</param>
        /// <param name="itemidInsertionPoint">The VS itemid of the file to generate the event
        /// handler in.</param>
        /// <param name="useHandlesClause">If true, a vb "Handles" clause will be generated for the
        /// handler.</param>
        /// <param name="additionalFormattingRule">An additional formatting rule that can be used to
        /// format the newly inserted method</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Either the unique id of the method if it already exists, or the unique id of
        /// the to be generated method, the text of the to be generated method, and the position in
        /// <paramref name="itemidInsertionPoint"/> where the text should be inserted.</returns>
        public static Tuple<string, string, VsTextSpan> EnsureEventHandler(
            Document thisDocument,
            Document targetDocument,
            string className,
            string objectName,
            string objectTypeName,
            string nameOfEvent,
            string eventHandlerName,
            uint itemidInsertionPoint,
            bool useHandlesClause,
            AbstractFormattingRule additionalFormattingRule,
            CancellationToken cancellationToken)
        {
            var thisCompilation = thisDocument.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken);
            var type = thisCompilation.GetTypeByMetadataName(className);

            var existingEventHandlers = GetCompatibleEventHandlers(targetDocument, className, objectTypeName, nameOfEvent, cancellationToken);
            var existingHandler = existingEventHandlers.SingleOrDefault(e => e.Item1 == eventHandlerName);
            if (existingHandler != null)
            {
                return Tuple.Create(existingHandler.Item2, (string)null, default(VsTextSpan));
            }

            // Okay, it doesn't exist yet.  Let's create it.
            var codeGenerationService = targetDocument.GetLanguageService<ICodeGenerationService>();
            var syntaxFactory = targetDocument.GetLanguageService<SyntaxGenerator>();
            var eventMember = GetEventSymbol(thisDocument, objectTypeName, nameOfEvent, type, cancellationToken);
            if (eventMember == null)
            {
                throw new InvalidOperationException();
            }

            var eventType = ((IEventSymbol)eventMember).Type;
            if (eventType.Kind != SymbolKind.NamedType || ((INamedTypeSymbol)eventType).DelegateInvokeMethod == null)
            {
                throw new InvalidOperationException(ServicesVSResources.Event_type_is_invalid);
            }

            var handlesExpressions = useHandlesClause
                ? ImmutableArray.Create(syntaxFactory.MemberAccessExpression(
                        objectName != null ? syntaxFactory.IdentifierName(objectName) : syntaxFactory.ThisExpression(),
                        syntaxFactory.IdentifierName(nameOfEvent)))
                : default;

            var invokeMethod = ((INamedTypeSymbol)eventType).DelegateInvokeMethod;
            var newMethod = CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes: default,
                accessibility: Accessibility.Protected,
                modifiers: new DeclarationModifiers(),
                returnType: targetDocument.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken).GetSpecialType(SpecialType.System_Void),
                refKind: RefKind.None,
                explicitInterfaceImplementations: default,
                name: eventHandlerName,
                typeParameters: default,
                parameters: invokeMethod.Parameters,
                statements: default,
                handlesExpressions: handlesExpressions);

            var annotation = new SyntaxAnnotation();
            newMethod = annotation.AddAnnotationToSymbol(newMethod);
            var codeModel = targetDocument.Project.LanguageServices.GetService<ICodeModelNavigationPointService>();
            var syntaxFacts = targetDocument.Project.LanguageServices.GetService<ISyntaxFactsService>();

            var targetSyntaxTree = targetDocument.GetSyntaxTreeSynchronously(cancellationToken);

            var position = type.Locations.First(loc => loc.SourceTree == targetSyntaxTree).SourceSpan.Start;
            var destinationType = syntaxFacts.GetContainingTypeDeclaration(targetSyntaxTree.GetRoot(cancellationToken), position);
            var options = targetDocument.GetOptionsAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken);
            var insertionPoint = codeModel.GetEndPoint(destinationType, options, EnvDTE.vsCMPart.vsCMPartBody);

            if (insertionPoint == null)
            {
                throw new InvalidOperationException(ServicesVSResources.Can_t_find_where_to_insert_member);
            }

            var newType = codeGenerationService.AddMethod(destinationType, newMethod, new CodeGenerationOptions(autoInsertionLocation: false), cancellationToken);
            var newRoot = targetSyntaxTree.GetRoot(cancellationToken).ReplaceNode(destinationType, newType);

            newRoot = Simplifier.ReduceAsync(
                targetDocument.WithSyntaxRoot(newRoot), Simplifier.Annotation, null, cancellationToken).WaitAndGetResult_Venus(cancellationToken).GetSyntaxRootSynchronously(cancellationToken);

            var formattingRules = additionalFormattingRule.Concat(Formatter.GetDefaultFormattingRules(targetDocument));

            newRoot = Formatter.Format(
                newRoot,
                Formatter.Annotation,
                targetDocument.Project.Solution.Workspace,
                targetDocument.GetOptionsAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken),
                formattingRules,
                cancellationToken);

            var newMember = newRoot.GetAnnotatedNodesAndTokens(annotation).Single();
            var newMemberText = newMember.ToFullString();

            // In VB, the final newline is likely a statement terminator in the parent - just add
            // one on so that things don't get messed.
            if (!newMemberText.EndsWith(Environment.NewLine, StringComparison.Ordinal))
            {
                newMemberText += Environment.NewLine;
            }

            return Tuple.Create(ConstructMemberId(newMethod), newMemberText, insertionPoint.Value.ToVsTextSpan());
        }

        public static bool TryGetMemberNavigationPoint(
            Document thisDocument,
            string className,
            string uniqueMemberID,
            out VsTextSpan textSpan,
            out Document targetDocument,
            CancellationToken cancellationToken)
        {
            targetDocument = null;
            textSpan = default;

            var type = thisDocument.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken).GetTypeByMetadataName(className);
            var member = LookupMemberId(type, uniqueMemberID);

            if (member == null)
            {
                return false;
            }

            var codeModel = thisDocument.Project.LanguageServices.GetService<ICodeModelNavigationPointService>();
            var memberNode = member.DeclaringSyntaxReferences.Select(r => r.GetSyntax(cancellationToken)).FirstOrDefault();
            if (memberNode != null)
            {
                var memberNodeDocument = thisDocument.Project.Solution.GetDocument(memberNode.SyntaxTree);
                var options = memberNodeDocument.GetOptionsAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken);
                var navigationPoint = codeModel.GetStartPoint(memberNode, options, EnvDTE.vsCMPart.vsCMPartNavigate);
                if (navigationPoint != null)
                {
                    targetDocument = memberNodeDocument;
                    textSpan = navigationPoint.Value.ToVsTextSpan();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get the display names and unique ids of all the members of the given type in <paramref
        /// name="className"/>.
        /// </summary>
        public static IEnumerable<Tuple<string, string>> GetMembers(
            Document document, string className, CODEMEMBERTYPE codeMemberType, CancellationToken cancellationToken)
        {
            var type = document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken).GetTypeByMetadataName(className);

            var compilation = document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken);
            var semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken);

            var allMembers = codeMemberType == CODEMEMBERTYPE.CODEMEMBERTYPE_EVENTS ?
                semanticModel.LookupSymbols(position: type.Locations[0].SourceSpan.Start, container: type, name: null) :
                type.GetMembers();

            var members = allMembers.Where(m => IncludeMember(m, codeMemberType, compilation));
            return members.Select(m => Tuple.Create(m.Name, ConstructMemberId(m)));
        }

        /// <summary>
        /// Try to do a symbolic rename the specified symbol.
        /// </summary>
        /// <returns>False ONLY if it can't resolve the name.  Other errors result in the normal
        /// exception being propagated.</returns>
        public static bool TryRenameElement(
            Document document,
            ContainedLanguageRenameType clrt,
            string oldFullyQualifiedName,
            string newFullyQualifiedName,
            IEnumerable<IRefactorNotifyService> refactorNotifyServices,
            CancellationToken cancellationToken)
        {
            var symbol = FindSymbol(document, clrt, oldFullyQualifiedName, cancellationToken);
            if (symbol == null)
            {
                return false;
            }

            if (CodeAnalysis.Workspace.TryGetWorkspace(document.GetTextSynchronously(cancellationToken).Container, out var workspace))
            {
                var newName = newFullyQualifiedName.Substring(newFullyQualifiedName.LastIndexOf('.') + 1);
                var optionSet = document.Project.Solution.Workspace.Options;
                var newSolution = Renamer.RenameSymbolAsync(document.Project.Solution, symbol, newName, optionSet, cancellationToken).WaitAndGetResult_Venus(cancellationToken);
                var changedDocuments = newSolution.GetChangedDocuments(document.Project.Solution);

                var undoTitle = string.Format(EditorFeaturesResources.Rename_0_to_1, symbol.Name, newName);
                using (var workspaceUndoTransaction = workspace.OpenGlobalUndoTransaction(undoTitle))
                {
                    // Notify third parties about the coming rename operation on the workspace, and let
                    // any exceptions propagate through
                    refactorNotifyServices.TryOnBeforeGlobalSymbolRenamed(workspace, changedDocuments, symbol, newName, throwOnFailure: true);

                    if (!workspace.TryApplyChanges(newSolution))
                    {
                        Exceptions.ThrowEFail();
                    }

                    // Notify third parties about the completed rename operation on the workspace, and
                    // let any exceptions propagate through
                    refactorNotifyServices.TryOnAfterGlobalSymbolRenamed(workspace, changedDocuments, symbol, newName, throwOnFailure: true);

                    workspaceUndoTransaction.Commit();
                }

                RenameTrackingDismisser.DismissRenameTracking(workspace, changedDocuments);
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IncludeMember(ISymbol member, CODEMEMBERTYPE memberType, Compilation compilation)
        {
            if (!member.CanBeReferencedByName)
            {
                return false;
            }

            switch (memberType)
            {
                case CODEMEMBERTYPE.CODEMEMBERTYPE_EVENT_HANDLERS:
                    // NOTE: the Dev10 C# codebase just returned 
                    if (member.Kind != SymbolKind.Method)
                    {
                        return false;
                    }

                    var method = (IMethodSymbol)member;
                    if (!method.ReturnsVoid)
                    {
                        return false;
                    }

                    if (method.Parameters.Length != 2)
                    {
                        return false;
                    }

                    if (!method.Parameters[0].Type.Equals(compilation.ObjectType))
                    {
                        return false;
                    }

                    if (!method.Parameters[1].Type.InheritsFromOrEquals(compilation.EventArgsType()))
                    {
                        return false;
                    }

                    return true;

                case CODEMEMBERTYPE.CODEMEMBERTYPE_EVENTS:
                    return member.Kind == SymbolKind.Event;

                case CODEMEMBERTYPE.CODEMEMBERTYPE_USER_FUNCTIONS:
                    return member.Kind == SymbolKind.Method;

                default:
                    throw new ArgumentException("InvalidValue", nameof(memberType));
            }
        }

        private static ISymbol FindSymbol(
            Document document, ContainedLanguageRenameType renameType, string fullyQualifiedName, CancellationToken cancellationToken)
        {
            switch (renameType)
            {
                case ContainedLanguageRenameType.CLRT_CLASS:
                    return document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken).GetTypeByMetadataName(fullyQualifiedName);

                case ContainedLanguageRenameType.CLRT_CLASSMEMBER:
                    var lastDot = fullyQualifiedName.LastIndexOf('.');
                    var typeName = fullyQualifiedName.Substring(0, lastDot);
                    var memberName = fullyQualifiedName.Substring(lastDot + 1, fullyQualifiedName.Length - lastDot - 1);
                    var type = document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken).GetTypeByMetadataName(typeName);
                    var semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken);
                    var membersOfName = type.GetMembers(memberName);
                    return membersOfName.SingleOrDefault();

                case ContainedLanguageRenameType.CLRT_NAMESPACE:
                    var ns = document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken).GlobalNamespace;
                    var parts = fullyQualifiedName.Split('.');
                    for (var i = 0; i < parts.Length && ns != null; i++)
                    {
                        ns = ns.GetNamespaceMembers().SingleOrDefault(n => n.Name == parts[i]);
                    }

                    return ns;

                case ContainedLanguageRenameType.CLRT_OTHER:
                    throw new NotSupportedException(ServicesVSResources.Can_t_rename_other_elements);

                default:
                    throw new InvalidOperationException(ServicesVSResources.Unknown_rename_type);
            }
        }

        internal static string ConstructMemberId(ISymbol member)
        {
            if (member.Kind == SymbolKind.Method)
            {
                return string.Format("{0}({1})", member.Name, string.Join(",", ((IMethodSymbol)member).Parameters.Select(p => p.Type.ToDisplayString())));
            }
            else if (member.Kind == SymbolKind.Event)
            {
                return member.Name + "(EVENT)";
            }
            else
            {
                throw new NotSupportedException(ServicesVSResources.IDs_are_not_supported_for_this_symbol_type);
            }
        }

        internal static ISymbol LookupMemberId(INamedTypeSymbol type, string uniqueMemberID)
        {
            var memberName = uniqueMemberID.Substring(0, uniqueMemberID.IndexOf('('));
            var members = type.GetMembers(memberName).Where(m => m.Kind == SymbolKind.Method);

            foreach (var m in members)
            {
                if (ConstructMemberId(m) == uniqueMemberID)
                {
                    return m;
                }
            }

            return null;
        }

        private static ISymbol GetEventSymbol(
            Document document, string objectTypeName, string nameOfEvent, INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            var compilation = document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken);
            var semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult_Venus(cancellationToken);

            var objectType = compilation.GetTypeByMetadataName(objectTypeName);
            if (objectType == null)
            {
                throw new InvalidOperationException();
            }

            var containingTree = document.GetSyntaxTreeSynchronously(cancellationToken);
            var typeLocation = type.Locations.FirstOrDefault(d => d.SourceTree == containingTree);
            if (typeLocation == null)
            {
                throw new InvalidOperationException();
            }

            return semanticModel.LookupSymbols(typeLocation.SourceSpan.Start, objectType, nameOfEvent).SingleOrDefault(m => m.Kind == SymbolKind.Event);
        }
    }
}
