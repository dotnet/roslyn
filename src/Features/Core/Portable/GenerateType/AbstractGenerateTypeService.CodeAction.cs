// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.ProjectManagement;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateType
{
    internal abstract partial class AbstractGenerateTypeService<TService, TSimpleNameSyntax, TObjectCreationExpressionSyntax, TExpressionSyntax, TTypeDeclarationSyntax, TArgumentSyntax>
    {
        private class GenerateTypeCodeAction : CodeAction
        {
            private readonly bool _intoNamespace;
            private readonly bool _inNewFile;
            private readonly TService _service;
            private readonly Document _document;
            private readonly State _state;
            private readonly string _equivalenceKey;
            private readonly CodeAndImportGenerationOptionsProvider _fallbackOptions;

            public GenerateTypeCodeAction(
                TService service,
                Document document,
                State state,
                CodeAndImportGenerationOptionsProvider fallbackOptions,
                bool intoNamespace,
                bool inNewFile)
            {
                _service = service;
                _document = document;
                _state = state;
                _fallbackOptions = fallbackOptions;
                _intoNamespace = intoNamespace;
                _inNewFile = inNewFile;
                _equivalenceKey = Title;
            }

            private static string FormatDisplayText(
                State state,
                bool inNewFile,
                bool isNested)
            {
                if (inNewFile)
                {
                    return string.Format(FeaturesResources.Generate_0_1_in_new_file,
                        state.IsStruct ? "struct" : state.IsInterface ? "interface" : "class",
                        state.Name);
                }
                else
                {
                    return string.Format(isNested ? FeaturesResources.Generate_nested_0_1 : FeaturesResources.Generate_0_1,
                        state.IsStruct ? "struct" : state.IsInterface ? "interface" : "class",
                        state.Name);
                }
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            {
                var semanticDocument = await SemanticDocument.CreateAsync(_document, cancellationToken).ConfigureAwait(false);

                var editor = new Editor(_service, semanticDocument, _state, _fallbackOptions, _intoNamespace, _inNewFile, cancellationToken);

                return await editor.GetOperationsAsync().ConfigureAwait(false);
            }

            public override string Title
                => _intoNamespace
                    ? FormatDisplayText(_state, _inNewFile, isNested: false)
                    : FormatDisplayText(_state, inNewFile: false, isNested: true);

            public override string EquivalenceKey => _equivalenceKey;
        }

        private sealed class GenerateTypeCodeActionWithOption : CodeActionWithOptions
        {
            private readonly TService _service;
            private readonly Document _document;
            private readonly State _state;
            private readonly CodeAndImportGenerationOptionsProvider _fallbackOptions;

            internal GenerateTypeCodeActionWithOption(TService service, Document document, State state, CodeAndImportGenerationOptionsProvider fallbackOptions)
            {
                _service = service;
                _document = document;
                _state = state;
                _fallbackOptions = fallbackOptions;
            }

            public override string Title => FeaturesResources.Generate_new_type;

            public override string EquivalenceKey => _state.Name;

            public override object GetOptions(CancellationToken cancellationToken)
            {
                var generateTypeOptionsService = _document.Project.Solution.Workspace.Services.GetRequiredService<IGenerateTypeOptionsService>();
                var notificationService = _document.Project.Solution.Workspace.Services.GetService<INotificationService>();
                var projectManagementService = _document.Project.Solution.Workspace.Services.GetService<IProjectManagementService>();
                var syntaxFactsService = _document.GetLanguageService<ISyntaxFactsService>();
                var typeKindValue = GetTypeKindOption(_state);
                var isPublicOnlyAccessibility = IsPublicOnlyAccessibility(_state, _document.Project);
                return generateTypeOptionsService.GetGenerateTypeOptions(
                    _state.Name,
                    new GenerateTypeDialogOptions(isPublicOnlyAccessibility, typeKindValue, _state.IsAttribute),
                    _document,
                    notificationService,
                    projectManagementService,
                    syntaxFactsService);
            }

            private bool IsPublicOnlyAccessibility(State state, Project project)
                => _service.IsPublicOnlyAccessibility(state.NameOrMemberAccessExpression, project) || _service.IsPublicOnlyAccessibility(state.SimpleName, project);

            private TypeKindOptions GetTypeKindOption(State state)
            {
                var gotPreassignedTypeOptions = GetPredefinedTypeKindOption(state, out var typeKindValue);
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

                if (_service.TryGetBaseList(state.NameOrMemberAccessExpression, out var typeKindValue) ||
                    _service.TryGetBaseList(state.SimpleName, out typeKindValue))
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
                var operations = SpecializedCollections.EmptyEnumerable<CodeActionOperation>();

                if (options is GenerateTypeOptionsResult generateTypeOptions && !generateTypeOptions.IsCancelled)
                {
                    var semanticDocument = await SemanticDocument.CreateAsync(_document, cancellationToken).ConfigureAwait(false);
                    var editor = new Editor(_service, semanticDocument, _state, _fallbackOptions, fromDialog: true, generateTypeOptions, cancellationToken);
                    operations = await editor.GetOperationsAsync().ConfigureAwait(false);
                }

                return operations;
            }
        }
    }
}
