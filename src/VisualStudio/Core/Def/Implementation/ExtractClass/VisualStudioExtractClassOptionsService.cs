// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExtractClass;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.MainDialog;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ExtractClass
{
    [ExportWorkspaceService(typeof(IExtractClassOptionsService), ServiceLayer.Host), Shared]
    internal class VisualStudioExtractClassOptionsService : IExtractClassOptionsService
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IGlyphService _glyphService;
        private readonly IWaitIndicator _waitIndicator;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioExtractClassOptionsService(
            IThreadingContext threadingContext,
            IGlyphService glyphService,
            IWaitIndicator waitIndicator)
        {
            _threadingContext = threadingContext;
            _glyphService = glyphService;
            _waitIndicator = waitIndicator;
        }

        public async Task<ExtractClassOptions?> GetExtractClassOptionsAsync(Document document, INamedTypeSymbol selectedType, ISymbol? selectedMember)
        {
            var notificationService = document.Project.Solution.Workspace.Services.GetRequiredService<INotificationService>();

            var membersInType = selectedType.GetMembers().
               WhereAsArray(member => MemberAndDestinationValidator.IsMemberValid(member));

            var memberViewModels = membersInType
                .SelectAsArray(member =>
                    new PullMemberUpSymbolViewModel(member, _glyphService)
                    {
                        // The member user selected will be checked at the beginning.
                        IsChecked = SymbolEquivalenceComparer.Instance.Equals(selectedMember, member),
                        MakeAbstract = false,
                        IsMakeAbstractCheckable = !member.IsKind(SymbolKind.Field) && !member.IsAbstract,
                        IsCheckable = true
                    });

            using var cancellationTokenSource = new CancellationTokenSource();
            var memberToDependentsMap = SymbolDependentsBuilder.FindMemberToDependentsMap(membersInType, document.Project, cancellationTokenSource.Token);

            var conflictingTypeNames = selectedType.ContainingNamespace.GetAllTypes(cancellationTokenSource.Token).Select(t => t.Name);
            var candidateName = selectedType.Name + "Base";
            var defaultTypeName = NameGenerator.GenerateUniqueName(candidateName, name => !conflictingTypeNames.Contains(name));

            var containingNamespaceDisplay = selectedType.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : selectedType.ContainingNamespace.ToDisplayString();

            var generatedNameTypeParameterSuffix = ExtractTypeHelpers.GetTypeParameterSuffix(document, selectedType, membersInType);

            var viewModel = new ExtractClassViewModel(
                _waitIndicator,
                notificationService,
                memberViewModels,
                memberToDependentsMap,
                defaultTypeName,
                containingNamespaceDisplay,
                document.Project.Language,
                generatedNameTypeParameterSuffix,
                conflictingTypeNames.ToImmutableArray(),
                document.GetRequiredLanguageService<ISyntaxFactsService>());

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dialog = new ExtractClassDialog(viewModel);

            var result = dialog.ShowModal();

            if (result.GetValueOrDefault())
            {
                return new ExtractClassOptions(
                    viewModel.DestinationViewModel.FileName,
                    viewModel.DestinationViewModel.TypeName,
                    viewModel.DestinationViewModel.Destination == CommonControls.NewTypeDestination.CurrentFile,
                    viewModel.MemberSelectionViewModel.CheckedMembers.SelectAsArray(m => new ExtractClassMemberAnalysisResult(m.Symbol, m.MakeAbstract)));
            }

            return null;
        }
    }
}
