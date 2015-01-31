// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.ProjectManagement;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateType
{
    internal abstract partial class AbstractGenerateTypeService<TService, TSimpleNameSyntax, TObjectCreationExpressionSyntax, TExpressionSyntax, TTypeDeclarationSyntax, TArgumentSyntax>
    {
        private class GenerateTypeCodeAction : CodeAction
        {
            private readonly bool intoNamespace;
            private readonly bool inNewFile;
            private readonly TService service;
            private readonly Document document;
            private readonly State state;
            private readonly string equivalenceKey;

            public GenerateTypeCodeAction(
                TService service,
                Document document,
                State state,
                bool intoNamespace,
                bool inNewFile)
            {
                this.service = service;
                this.document = document;
                this.state = state;
                this.intoNamespace = intoNamespace;
                this.inNewFile = inNewFile;
                this.equivalenceKey = Title;
            }

            private static string FormatDisplayText(
                State state,
                bool inNewFile,
                string destination)
            {
                var finalName = GetTypeName(state);

                if (inNewFile)
                {
                    return string.Format(FeaturesResources.GenerateForInNewFile,
                        state.IsStruct ? "struct" : state.IsInterface ? "interface" : "class",
                        state.Name, destination);
                }
                else
                {
                    return string.Format(FeaturesResources.GenerateForIn,
                        state.IsStruct ? "struct" : state.IsInterface ? "interface" : "class",
                        state.Name, destination);
                }
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            {
                var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                var editor = new Editor(this.service, semanticDocument, state, intoNamespace, inNewFile, cancellationToken: cancellationToken);

                return await editor.GetOperationsAsync().ConfigureAwait(false);
            }

            public override string Title
            {
                get
                {
                    if (intoNamespace)
                    {
                        var namespaceToGenerateIn = string.IsNullOrEmpty(state.NamespaceToGenerateInOpt) ? FeaturesResources.GlobalNamespace : state.NamespaceToGenerateInOpt;
                        return FormatDisplayText(state, inNewFile, namespaceToGenerateIn);
                    }
                    else
                    {
                        return FormatDisplayText(state, inNewFile: false, destination: state.TypeToGenerateInOpt.Name);
                    }
                }
            }

            public override string EquivalenceKey
            {
                get
                {
                    return equivalenceKey;
                }
            }
        }

        private class GenerateTypeCodeActionWithOption : CodeActionWithOptions
        {
            private readonly TService service;
            private readonly Document document;
            private readonly State state;

            internal GenerateTypeCodeActionWithOption(TService service, Document document, State state)
            {
                this.service = service;
                this.document = document;
                this.state = state;
            }

            public override string Title
            {
                get
                {
                    return FeaturesResources.GenerateNewType;
                }
            }

            public override string EquivalenceKey
            {
                get
                {
                    return state.Name;
                }
            }

            public override object GetOptions(CancellationToken cancellationToken)
            {
                var generateTypeOptionsService = document.Project.Solution.Workspace.Services.GetService<IGenerateTypeOptionsService>();
                var notificationService = document.Project.Solution.Workspace.Services.GetService<INotificationService>();
                var projectManagementService = document.Project.Solution.Workspace.Services.GetService<IProjectManagementService>();
                var syntaxFactsService = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                var typeKindValue = GetTypeKindOption(state);
                var isPublicOnlyAccessibility = IsPublicOnlyAccessibility(state, document.Project);
                return generateTypeOptionsService.GetGenerateTypeOptions(
                    state.Name,
                    new GenerateTypeDialogOptions(isPublicOnlyAccessibility, typeKindValue, state.IsAttribute),
                    document,
                    notificationService,
                    projectManagementService,
                    syntaxFactsService);
            }

            private bool IsPublicOnlyAccessibility(State state, Project project)
            {
                return service.IsPublicOnlyAccessibility(state.NameOrMemberAccessExpression, project) || service.IsPublicOnlyAccessibility(state.SimpleName, project);
            }

            private TypeKindOptions GetTypeKindOption(State state)
            {
                TypeKindOptions typeKindValue;

                var gotPreassignedTypeOptions = GetPredefinedTypeKindOption(state, out typeKindValue);
                if (!gotPreassignedTypeOptions)
                {
                    typeKindValue = state.IsSimpleNameGeneric ? TypeKindOptionsHelper.RemoveOptions(typeKindValue, TypeKindOptions.GenericInCompatibleTypes) : typeKindValue;
                    typeKindValue = state.IsMembersWithModule ? TypeKindOptionsHelper.AddOption(typeKindValue, TypeKindOptions.Module) : typeKindValue;
                    typeKindValue = state.IsInterfaceOrEnumNotAllowedInTypeContext ? TypeKindOptionsHelper.RemoveOptions(typeKindValue, TypeKindOptions.Interface, TypeKindOptions.Enum) : typeKindValue;
                    typeKindValue = state.IsDelegateAllowed ? typeKindValue : TypeKindOptionsHelper.RemoveOptions(typeKindValue, TypeKindOptions.Delegate);
                    typeKindValue = state.IsEnumNotAllowed ? TypeKindOptionsHelper.RemoveOptions(typeKindValue, TypeKindOptions.Enum) : typeKindValue;
                }

                return typeKindValue;
            }

            private bool GetPredefinedTypeKindOption(State state, out TypeKindOptions typeKindValueFinal)
            {
                if (state.IsAttribute)
                {
                    typeKindValueFinal = TypeKindOptions.Attribute;
                    return true;
                }

                TypeKindOptions typeKindValue = TypeKindOptions.None;
                if (service.TryGetBaseList(state.NameOrMemberAccessExpression, out typeKindValue) || service.TryGetBaseList(state.SimpleName, out typeKindValue))
                {
                    typeKindValueFinal = typeKindValue;
                    return true;
                }

                if (state.IsClassInterfaceTypes)
                {
                    typeKindValueFinal = TypeKindOptions.BaseList;
                    return true;
                }

                if (state.IsDelegateOnly)
                {
                    typeKindValueFinal = TypeKindOptions.Delegate;
                    return true;
                }

                if (state.IsTypeGeneratedIntoNamespaceFromMemberAccess)
                {
                    typeKindValueFinal = state.IsSimpleNameGeneric ? TypeKindOptionsHelper.RemoveOptions(TypeKindOptions.MemberAccessWithNamespace, TypeKindOptions.GenericInCompatibleTypes) : TypeKindOptions.MemberAccessWithNamespace;
                    typeKindValueFinal = state.IsEnumNotAllowed ? TypeKindOptionsHelper.RemoveOptions(typeKindValueFinal, TypeKindOptions.Enum) : typeKindValueFinal;
                    return true;
                }

                typeKindValueFinal = TypeKindOptions.AllOptions;
                return false;
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
            {
                IEnumerable<CodeActionOperation> operations = null;

                var generateTypeOptions = options as GenerateTypeOptionsResult;
                if (generateTypeOptions != null && !generateTypeOptions.IsCancelled)
                {
                    var semanticDocument = SemanticDocument.CreateAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);
                    var editor = new Editor(this.service, semanticDocument, state, true, generateTypeOptions, cancellationToken);
                    operations = await editor.GetOperationsAsync().ConfigureAwait(false);
                }

                return operations;
            }
        }
    }
}
