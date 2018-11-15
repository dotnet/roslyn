// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using static Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface.ExtractInterfaceDialogViewModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
{
    internal class PullMemberUpViewModel : AbstractNotifyPropertyChanged
    {
        public ObservableCollection<PullUpMemberSymbolView> SelectedMembersContainer { get; set; }

        public ObservableCollection<MemberSymbolViewModelGraphNode> TargetMembersContainer { get; set; }

        public ImmutableDictionary<ISymbol, PullUpMemberSymbolView> SymbolToMemberViewMap { get; }

        public MemberSymbolViewModelGraphNode SelectedTarget { get => _selectedTarget; set => SetProperty(ref _selectedTarget, value, nameof(SelectedTarget)); }

        private MemberSymbolViewModelGraphNode _selectedTarget;

        private bool _selectAllAndDeselectAllChecked;

        private readonly SemanticModel _semanticModel;

        private readonly Dictionary<ISymbol, IEnumerable<ISymbol>> _dependentsMap;

        public bool SelectAllAndDeselectAllChecked
        {
            get => _selectAllAndDeselectAllChecked;
            set
            {
                _selectAllAndDeselectAllChecked = value;
                NotifyPropertyChanged($"{nameof(SelectAllAndDeselectAllChecked)}");
            }
        }

        internal PullMemberUpViewModel(
            SemanticModel semanticModel,
            ISymbol selectedNodeSymbol,
            IGlyphService glyphService)
        {
            var allMembers = selectedNodeSymbol.ContainingType.GetMembers().Where(
                    member => {
                        if (member is IMethodSymbol methodSymbol)
                        {
                            return methodSymbol.MethodKind == MethodKind.Ordinary;
                        }
                        else if (member is IFieldSymbol fieldSymbol)
                        {
                            return !member.IsImplicitlyDeclared;
                        }
                        else
                        {
                            return member.Kind == SymbolKind.Property || member.Kind == SymbolKind.Event;
                        }
                    });

            var memberinitalStates = allMembers.
                Select(member => new PullUpMemberSymbolView(member, glyphService)
                {
                    IsChecked = member.Equals(selectedNodeSymbol),
                    MakeAbstract = false,
                    IsMakeAbstractSelectable = member.Kind != SymbolKind.Field && !member.IsAbstract,
                    IsSelectable = true
                }).OrderByDescending(memberSymbolView => memberSymbolView.MemberSymbol.DeclaredAccessibility);
            SelectedMembersContainer = new ObservableCollection<PullUpMemberSymbolView>(memberinitalStates);
            SymbolToMemberViewMap = SelectedMembersContainer.
                ToImmutableDictionary(symbolView => symbolView.MemberSymbol);

            var baseTypeTree = MemberSymbolViewModelGraphNode.CreateInheritanceGraph(selectedNodeSymbol.ContainingType, glyphService);

            _semanticModel = semanticModel;
            TargetMembersContainer = baseTypeTree.BaseTypeGraphNodes;
            SelectAllAndDeselectAllChecked = true;
            _dependentsMap = new Dictionary<ISymbol, IEnumerable<ISymbol>>();
        }

        public IEnumerable<ISymbol> FindDependents(ISymbol member)
        {
            if (_dependentsMap.TryGetValue(member, out var dependents))
            {
                return dependents;
            }
            else
            {
                dependents = DependentsBuilder.Build(
                    _semanticModel, member,
                    SelectedMembersContainer.Select(memberView => memberView.MemberSymbol).ToImmutableHashSet());
                _dependentsMap.Add(member, dependents);
                return dependents;
            }
        }

        internal AnalysisResult CreateAnaysisResult()
        {
            // Check box won't be cleared when it is disabled. It is made to prevent user
            // loses their choice when moves around the target type
            var membersInfo = SelectedMembersContainer.
                Where(memberSymbolView => memberSymbolView.IsChecked && memberSymbolView.IsSelectable).
                Select(memberSymbolView =>
                    (memberSymbolView.MemberSymbol,
                    memberSymbolView.MakeAbstract &&
                    memberSymbolView.IsMakeAbstractSelectable));
            var result = PullMembersUpAnalysisBuilder.BuildAnalysisResult(
                SelectedTarget.MemberSymbol as INamedTypeSymbol,
                membersInfo);
            return result;
        }
    }

    internal class PullUpMemberSymbolView : MemberSymbolViewModel
    {
        private bool _isMakeAbstractSelectable;

        private bool _isSelectable;

        public Visibility MakeAbstractVisibility
        {
            get
            {
                if (MemberSymbol.Kind == SymbolKind.Field || MemberSymbol.IsAbstract)
                {
                    return Visibility.Hidden;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }
        
        public bool IsMakeAbstractSelectable { get => _isMakeAbstractSelectable; set => SetProperty(ref _isMakeAbstractSelectable, value); }

        public bool MakeAbstract { get; set; }

        public bool IsSelectable { get => _isSelectable; set => SetProperty(ref _isSelectable, value, nameof(IsSelectable)); }

        public string Accessibility => MemberSymbol.DeclaredAccessibility.ToString();

        public PullUpMemberSymbolView(ISymbol symbol, IGlyphService glyphService) : base(symbol, glyphService)
        {
        }
    }

    internal class MemberSymbolViewModelGraphNode : MemberSymbolViewModel 
    {
        public ObservableCollection<MemberSymbolViewModelGraphNode> BaseTypeGraphNodes { get; private set; }

        public bool IsExpanded { get; set; }

        public string Namespace => ServicesVSResources.Namespace + MemberSymbol.ContainingNamespace?.Name ??
            ServicesVSResources.Namespace + ServicesVSResources.Global_name_space;

        private MemberSymbolViewModelGraphNode(ISymbol node, IGlyphService glyphService) : base(node, glyphService)
        {
            BaseTypeGraphNodes = new ObservableCollection<MemberSymbolViewModelGraphNode>();
        }

        /// <summary>
        /// Use breadth first search to create the inheritance graph. If several types share one same base type 
        /// then this base type will be put into the descendants of each types. This method assume there is no loop in the
        /// graph.
        /// </summary>
        internal static MemberSymbolViewModelGraphNode CreateInheritanceGraph(INamedTypeSymbol root, IGlyphService glyphService)
        {
            var rootNode = new MemberSymbolViewModelGraphNode(root, glyphService) { IsChecked = false, IsExpanded = true};
            var queue = new Queue<MemberSymbolViewModelGraphNode>();
            queue.Enqueue(rootNode);
            while (queue.Any())
            {
                var currentNode = queue.Dequeue();
                var currentSymbol = currentNode.MemberSymbol as INamedTypeSymbol;
                var allDirectBaseTypes = currentSymbol.Interfaces.Concat(currentSymbol.BaseType).Where(baseType => baseType != null);
                AddBasesAndInterfaceToQueue(glyphService, queue,  currentNode, allDirectBaseTypes);
            }

            return rootNode;
        }

        private static void AddBasesAndInterfaceToQueue(
            IGlyphService glyphService,
            Queue<MemberSymbolViewModelGraphNode> queue,
            MemberSymbolViewModelGraphNode parentNode,
            IEnumerable<INamedTypeSymbol> allDirectBaseTypes)
        {
            var validBaseTypes = allDirectBaseTypes.
                Where(baseType => baseType.DeclaringSyntaxReferences.Length > 0 &&
                    baseType.Locations.Any(location => location.IsInSource));
            foreach (var baseType in validBaseTypes)
            {
                var correspondingGraphNode = new MemberSymbolViewModelGraphNode(baseType, glyphService)
                {
                    IsChecked = false,
                    IsExpanded = true, 
                };

                parentNode.BaseTypeGraphNodes.Add(correspondingGraphNode);
                queue.Enqueue(correspondingGraphNode);
            }
        }
    }
}
