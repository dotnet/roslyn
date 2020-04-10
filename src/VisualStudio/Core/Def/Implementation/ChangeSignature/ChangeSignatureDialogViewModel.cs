﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal class ChangeSignatureDialogViewModel : AbstractNotifyPropertyChanged
    {
        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly INotificationService _notificationService;
        private readonly ParameterConfiguration _originalParameterConfiguration;

        // This can be changed to ParameterViewModel if we will allow adding 'this' parameter.
        private readonly ExistingParameterViewModel _thisParameter;
        private readonly List<ParameterViewModel> _parametersWithoutDefaultValues;
        private readonly List<ParameterViewModel> _parametersWithDefaultValues;

        // This can be changed to ParameterViewModel if we will allow adding 'params' parameter.
        private readonly ExistingParameterViewModel _paramsParameter;
        private HashSet<ParameterViewModel> _disabledParameters = new HashSet<ParameterViewModel>();
        private readonly int _insertPosition;

        private ImmutableArray<SymbolDisplayPart> _declarationParts;
        private bool _previewChanges;

        /// <summary>
        /// The document where the symbol we are changing signature is defined.
        /// </summary>
        private readonly Document _document;

        internal ChangeSignatureDialogViewModel(
            ParameterConfiguration parameters,
            ISymbol symbol,
            Document document,
            int insertPosition,
            IClassificationFormatMap classificationFormatMap,
            ClassificationTypeMap classificationTypeMap)
        {
            _originalParameterConfiguration = parameters;
            _document = document;
            _insertPosition = insertPosition;
            _classificationFormatMap = classificationFormatMap;
            _classificationTypeMap = classificationTypeMap;

            _notificationService = document.Project.Solution.Workspace.Services.GetService<INotificationService>();

            // This index is displayed to users. That is why we start it from 1.
            var initialDisplayIndex = 1;

            if (parameters.ThisParameter != null)
            {
                _thisParameter = new ExistingParameterViewModel(this, parameters.ThisParameter, initialDisplayIndex++);
                _disabledParameters.Add(_thisParameter);
            }

            _declarationParts = symbol.ToDisplayParts(s_symbolDeclarationDisplayFormat);

            _parametersWithoutDefaultValues = CreateParameterViewModels(parameters.ParametersWithoutDefaultValues, ref initialDisplayIndex);
            _parametersWithDefaultValues = CreateParameterViewModels(parameters.RemainingEditableParameters, ref initialDisplayIndex);

            if (parameters.ParamsParameter != null)
            {
                _paramsParameter = new ExistingParameterViewModel(this, parameters.ParamsParameter, initialDisplayIndex++);
            }

            var selectedIndex = parameters.SelectedIndex;
            // Currently, we do not support editing the ThisParameter. 
            // Therefore, if there is such parameter, we should move the selectedIndex.
            if (parameters.ThisParameter != null && selectedIndex == 0)
            {
                // If we have at least one paramter after the ThisParameter, select the first one after This.
                // Otherwise, do not select anything.
                if (parameters.ParametersWithoutDefaultValues.Length + parameters.RemainingEditableParameters.Length > 0)
                {
                    this.SelectedIndex = 1;
                }
                else
                {
                    this.SelectedIndex = null;
                }
            }
            else
            {
                this.SelectedIndex = selectedIndex;
            }
        }

        public AddParameterDialogViewModel CreateAddParameterDialogViewModel()
            => new AddParameterDialogViewModel(_document, _insertPosition);

        List<ParameterViewModel> CreateParameterViewModels(ImmutableArray<Parameter> parameters, ref int initialIndex)
        {
            var list = new List<ParameterViewModel>();
            foreach (ExistingParameter existingParameter in parameters)
            {
                list.Add(new ExistingParameterViewModel(this, existingParameter, initialIndex));
                initialIndex++;
            }

            return list;
        }

        public int GetStartingSelectionIndex()
        {
            if (_thisParameter == null)
            {
                return 0;
            }

            if (_parametersWithDefaultValues.Count + _parametersWithoutDefaultValues.Count > 0)
            {
                return 1;
            }

            return -1;
        }

        public bool PreviewChanges
        {
            get
            {
                return _previewChanges;
            }

            set
            {
                _previewChanges = value;
            }
        }

        public bool CanRemove
        {
            get
            {
                if (!AllParameters.Any())
                {
                    return false;
                }

                if (!SelectedIndex.HasValue)
                {
                    return false;
                }

                var index = SelectedIndex.Value;

                if (index == 0 && _thisParameter != null)
                {
                    return false;
                }

                return !AllParameters[index].IsRemoved;
            }
        }

        public bool CanRestore
        {
            get
            {
                if (!AllParameters.Any())
                {
                    return false;
                }

                if (!SelectedIndex.HasValue)
                {
                    return false;
                }

                var index = SelectedIndex.Value;

                if (index == 0 && _thisParameter != null)
                {
                    return false;
                }

                return AllParameters[index].IsRemoved;
            }
        }

        public bool CanEdit
        {
            get
            {
                if (!SelectedIndex.HasValue)
                {
                    return false;
                }

                // Cannot edit `this` parameter
                var index = SelectedIndex.Value;
                if (index == 0 && _thisParameter != null)
                {
                    return false;
                }

                // Cannot edit params parameter
                if (index >= (_thisParameter == null ? 0 : 1) + _parametersWithoutDefaultValues.Count + _parametersWithDefaultValues.Count)
                {
                    return false;
                }

                return !AllParameters[SelectedIndex.Value].IsRemoved;
            }
        }

        internal void Remove()
        {
            if (AllParameters[_selectedIndex.Value] is AddedParameterViewModel)
            {
                ParameterViewModel parameterToRemove = AllParameters[_selectedIndex.Value];
                _parametersWithoutDefaultValues.Remove(parameterToRemove);
            }
            else
            {
                AllParameters[_selectedIndex.Value].IsRemoved = true;
            }

            RemoveRestoreNotifyPropertyChanged();
        }

        internal void Restore()
        {
            AllParameters[_selectedIndex.Value].IsRemoved = false;
            RemoveRestoreNotifyPropertyChanged();
        }

        internal void AddParameter(AddedParameter addedParameter)
        {
            _parametersWithoutDefaultValues.Add(new AddedParameterViewModel(this, addedParameter));

            RemoveRestoreNotifyPropertyChanged();
        }

        internal void RemoveRestoreNotifyPropertyChanged()
        {
            NotifyPropertyChanged(nameof(AllParameters));
            NotifyPropertyChanged(nameof(SignatureDisplay));
            NotifyPropertyChanged(nameof(SignaturePreviewAutomationText));
            NotifyPropertyChanged(nameof(CanRemove));
            NotifyPropertyChanged(nameof(RemoveAutomationText));
            NotifyPropertyChanged(nameof(CanRestore));
            NotifyPropertyChanged(nameof(RestoreAutomationText));
            NotifyPropertyChanged(nameof(CanEdit));
        }

        internal ParameterConfiguration GetParameterConfiguration()
        {
            return new ParameterConfiguration(
                _originalParameterConfiguration.ThisParameter,
                _parametersWithoutDefaultValues.Where(p => !p.IsRemoved).Select(p => p.Parameter).ToImmutableArray(),
                _parametersWithDefaultValues.Where(p => !p.IsRemoved).Select(p => p.Parameter).ToImmutableArray(),
                (_paramsParameter == null || _paramsParameter.IsRemoved) ? null : _paramsParameter.Parameter as ExistingParameter,
                selectedIndex: -1);
        }

        private static readonly SymbolDisplayFormat s_symbolDeclarationDisplayFormat = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier,
            extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
            memberOptions:
                SymbolDisplayMemberOptions.IncludeType |
                SymbolDisplayMemberOptions.IncludeExplicitInterface |
                SymbolDisplayMemberOptions.IncludeAccessibility |
                SymbolDisplayMemberOptions.IncludeModifiers |
                SymbolDisplayMemberOptions.IncludeRef);

        private static readonly SymbolDisplayFormat s_parameterDisplayFormat = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier,
            parameterOptions:
                SymbolDisplayParameterOptions.IncludeType |
                SymbolDisplayParameterOptions.IncludeParamsRefOut |
                SymbolDisplayParameterOptions.IncludeDefaultValue |
                SymbolDisplayParameterOptions.IncludeExtensionThis |
                SymbolDisplayParameterOptions.IncludeName);

        public TextBlock SignatureDisplay
        {
            get
            {
                // TODO: Should probably use original syntax & formatting exactly instead of regenerating here
                var displayParts = GetSignatureDisplayParts();

                var textBlock = displayParts.ToTaggedText().ToTextBlock(_classificationFormatMap, _classificationTypeMap);

                foreach (var inline in textBlock.Inlines)
                {
                    inline.FontSize = 12;
                }

                textBlock.IsEnabled = false;
                return textBlock;
            }
        }

        public string SignaturePreviewAutomationText
        {
            get
            {
                return GetSignatureDisplayParts().Select(sdp => sdp.ToString()).Join(" ");
            }
        }

        internal string TEST_GetSignatureDisplayText()
            => GetSignatureDisplayParts().Select(p => p.ToString()).Join("");

        private List<SymbolDisplayPart> GetSignatureDisplayParts()
        {
            var displayParts = new List<SymbolDisplayPart>();

            displayParts.AddRange(_declarationParts);
            displayParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, "("));

            var first = true;
            foreach (var parameter in AllParameters.Where(p => !p.IsRemoved))
            {
                if (!first)
                {
                    displayParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, ","));
                    displayParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " "));
                }

                first = false;

                switch (parameter)
                {
                    case ExistingParameterViewModel existingParameter:
                        displayParts.AddRange(existingParameter.ParameterSymbol.ToDisplayParts(s_parameterDisplayFormat));
                        break;

                    case AddedParameterViewModel addedParameterViewModel:
                        var languageService = _document.GetLanguageService<IChangeSignatureViewModelFactoryService>();
                        displayParts.AddRange(languageService.GeneratePreviewDisplayParts(addedParameterViewModel));
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(parameter.GetType().ToString());
                }
            }

            displayParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, ")"));
            return displayParts;
        }

        public List<ParameterViewModel> AllParameters
        {
            get
            {
                var list = new List<ParameterViewModel>();
                if (_thisParameter != null)
                {
                    list.Add(_thisParameter);
                }

                list.AddRange(_parametersWithoutDefaultValues);
                list.AddRange(_parametersWithDefaultValues);

                if (_paramsParameter != null)
                {
                    list.Add(_paramsParameter);
                }

                return list;
            }
        }

        public bool CanMoveUp
        {
            get
            {
                if (!SelectedIndex.HasValue)
                {
                    return false;
                }

                var index = SelectedIndex.Value;
                index = _thisParameter == null ? index : index - 1;
                if (index <= 0 || index == _parametersWithoutDefaultValues.Count || index >= _parametersWithoutDefaultValues.Count + _parametersWithDefaultValues.Count)
                {
                    return false;
                }

                return true;
            }
        }

        public bool CanMoveDown
        {
            get
            {
                if (!SelectedIndex.HasValue)
                {
                    return false;
                }

                var index = SelectedIndex.Value;
                index = _thisParameter == null ? index : index - 1;
                if (index < 0 || index == _parametersWithoutDefaultValues.Count - 1 || index >= _parametersWithoutDefaultValues.Count + _parametersWithDefaultValues.Count - 1)
                {
                    return false;
                }

                return true;
            }
        }

        internal void MoveUp()
        {
            Debug.Assert(CanMoveUp);

            var index = SelectedIndex.Value;
            index = _thisParameter == null ? index : index - 1;
            Move(index < _parametersWithoutDefaultValues.Count ? _parametersWithoutDefaultValues : _parametersWithDefaultValues, index < _parametersWithoutDefaultValues.Count ? index : index - _parametersWithoutDefaultValues.Count, delta: -1);
        }

        internal void MoveDown()
        {
            Debug.Assert(CanMoveDown);

            var index = SelectedIndex.Value;
            index = _thisParameter == null ? index : index - 1;
            Move(index < _parametersWithoutDefaultValues.Count ? _parametersWithoutDefaultValues : _parametersWithDefaultValues, index < _parametersWithoutDefaultValues.Count ? index : index - _parametersWithoutDefaultValues.Count, delta: 1);
        }

        private void Move(List<ParameterViewModel> list, int index, int delta)
        {
            var param = list[index];
            list.RemoveAt(index);
            list.Insert(index + delta, param);

            SelectedIndex += delta;

            NotifyPropertyChanged(nameof(AllParameters));
            NotifyPropertyChanged(nameof(SignatureDisplay));
            NotifyPropertyChanged(nameof(SignaturePreviewAutomationText));
        }

        internal bool TrySubmit()
        {
            var canSubmit = AllParameters.Any(p => p.IsRemoved) ||
                AllParameters.Any(p => p is AddedParameterViewModel) ||
            !_parametersWithoutDefaultValues.OfType<ExistingParameterViewModel>().Select(p => p.ParameterSymbol).SequenceEqual(_originalParameterConfiguration.ParametersWithoutDefaultValues.Cast<ExistingParameter>().Select(p => p.Symbol)) ||
            !_parametersWithDefaultValues.OfType<ExistingParameterViewModel>().Select(p => p.ParameterSymbol).SequenceEqual(_originalParameterConfiguration.RemainingEditableParameters.Cast<ExistingParameter>().Select(p => p.Symbol));

            if (!canSubmit)
            {
                _notificationService.SendNotification(ServicesVSResources.You_must_change_the_signature, severity: NotificationSeverity.Information);
                return false;
            }

            return true;
        }

        private bool IsDisabled(ParameterViewModel parameterViewModel)
        {
            return _disabledParameters.Contains(parameterViewModel);
        }

        private int? _selectedIndex;
        public int? SelectedIndex
        {
            get
            {
                return _selectedIndex;
            }

            set
            {
                var newSelectedIndex = value == -1 ? null : value;
                if (newSelectedIndex == _selectedIndex)
                {
                    return;
                }

                _selectedIndex = newSelectedIndex;

                NotifyPropertyChanged(nameof(CanMoveUp));
                NotifyPropertyChanged(nameof(MoveUpAutomationText));
                NotifyPropertyChanged(nameof(CanMoveDown));
                NotifyPropertyChanged(nameof(MoveDownAutomationText));
                NotifyPropertyChanged(nameof(CanRemove));
                NotifyPropertyChanged(nameof(RemoveAutomationText));
                NotifyPropertyChanged(nameof(CanRestore));
                NotifyPropertyChanged(nameof(RestoreAutomationText));
                NotifyPropertyChanged(nameof(CanEdit));
            }
        }

        public string MoveUpAutomationText
        {
            get
            {
                if (!CanMoveUp)
                {
                    return string.Empty;
                }

                return string.Format(ServicesVSResources.Move_0_above_1, AllParameters[SelectedIndex.Value].ShortAutomationText, AllParameters[SelectedIndex.Value - 1].ShortAutomationText);
            }
        }

        public string MoveDownAutomationText
        {
            get
            {
                if (!CanMoveDown)
                {
                    return string.Empty;
                }

                return string.Format(ServicesVSResources.Move_0_below_1, AllParameters[SelectedIndex.Value].ShortAutomationText, AllParameters[SelectedIndex.Value + 1].ShortAutomationText);
            }
        }

        public string RemoveAutomationText
        {
            get
            {
                if (!CanRemove)
                {
                    return string.Empty;
                }

                return string.Format(ServicesVSResources.Remove_0, AllParameters[SelectedIndex.Value].ShortAutomationText);
            }
        }

        public string RestoreAutomationText
        {
            get
            {
                if (!CanRestore)
                {
                    return string.Empty;
                }

                return string.Format(ServicesVSResources.Restore_0, AllParameters[SelectedIndex.Value].ShortAutomationText);
            }
        }

        public abstract class ParameterViewModel
        {
            protected readonly ChangeSignatureDialogViewModel changeSignatureDialogViewModel;

            public abstract Parameter Parameter { get; }

            public abstract string Type { get; }
            public abstract string ParameterName { get; }
            public abstract bool IsRemoved { get; set; }
            public abstract string ShortAutomationText { get; }
            public abstract bool IsDisabled { get; }
            public abstract string CallSite { get; }

            public ParameterViewModel(ChangeSignatureDialogViewModel changeSignatureDialogViewModel)
            {
                this.changeSignatureDialogViewModel = changeSignatureDialogViewModel;
            }

            public abstract string InitialIndex { get; }
            public abstract string Modifier { get; }
            public abstract string Default { get; }

            public virtual string FullAutomationText
            {
                get
                {
                    var text = $"{Modifier} {Type} {Parameter}";
                    if (!string.IsNullOrWhiteSpace(Default))
                    {
                        text += $" = {Default}";
                    }

                    return text;
                }
            }
        }

        public class AddedParameterViewModel : ParameterViewModel
        {
            public override Parameter Parameter => _addedParameter;
            public readonly AddedParameter _addedParameter;

            public AddedParameterViewModel(ChangeSignatureDialogViewModel changeSignatureDialogViewModel, AddedParameter addedParameter)
                : base(changeSignatureDialogViewModel)
            {
                _addedParameter = addedParameter;
            }

            public override string Type => _addedParameter.TypeNameDisplayWithErrorIndicator;

            public override string ParameterName => _addedParameter.ParameterName;

            public override bool IsRemoved { get => false; set => throw new InvalidOperationException(); }

            public override string ShortAutomationText => $"{Type} {ParameterName}";
            public override string FullAutomationText
            {
                get
                {
                    var baseText = base.FullAutomationText;
                    return ServicesVSResources.Added_Parameter + baseText + string.Format(ServicesVSResources.Inserting_call_site_value_0, CallSite);
                }
            }

            public override bool IsDisabled => false;

            public override string CallSite => _addedParameter.CallSiteValue;

            public override string InitialIndex => ServicesVSResources.ChangeSignature_NewParameterIndicator;

            // Newly added parameters cannot have modifiers yet
            public override string Modifier => string.Empty;

            // Only required parameters are supported currently
            public override string Default => string.Empty;
        }

#nullable enable

        public class ExistingParameterViewModel : ParameterViewModel
        {
            public IParameterSymbol ParameterSymbol => _existingParameter.Symbol;

            private readonly ExistingParameter _existingParameter;

            public override Parameter Parameter => _existingParameter;

            public ExistingParameterViewModel(ChangeSignatureDialogViewModel changeSignatureDialogViewModel, ExistingParameter existingParameter, int initialIndex)
                : base(changeSignatureDialogViewModel)
            {
                _existingParameter = existingParameter;
                InitialIndex = initialIndex.ToString();
            }

            public override string ShortAutomationText => $"{Type} {Parameter.Name}";

            public override string CallSite => string.Empty;

            public override string InitialIndex { get; }

#nullable disable
            public override string Modifier
            {
                get
                {
                    switch (ParameterSymbol.Language)
                    {
                        case LanguageNames.CSharp:
                            return ModifierText("out", "ref", "in", "params", "this");
                        case LanguageNames.VisualBasic:
                            return ModifierText(@ref: "ByRef", @params: "ParamArray", @this: "Me");
                        default:
                            return string.Empty;
                    }

                    string ModifierText(string @out = default, string @ref = default, string @in = default, string @params = default, string @this = default)
                    {
                        switch (ParameterSymbol.RefKind)
                        {
                            case RefKind.Out:
                                return @out ?? string.Empty;
                            case RefKind.Ref:
                                return @ref ?? string.Empty;
                            case RefKind.In:
                                return @in ?? string.Empty;
                        }

                        if (ParameterSymbol.IsParams)
                        {
                            return @params ?? string.Empty;
                        }

                        if (changeSignatureDialogViewModel._thisParameter != null &&
                            ParameterSymbol == (changeSignatureDialogViewModel._thisParameter as ExistingParameterViewModel).ParameterSymbol)
                        {
                            return @this ?? string.Empty;
                        }
                        return string.Empty;
                    }
                }
            }

#nullable enable

            public override string Type => ParameterSymbol.Type.ToDisplayString(s_parameterDisplayFormat);

            public override string ParameterName => ParameterSymbol.Name;

            public override string Default
            {
                get
                {
                    if (!ParameterSymbol.HasExplicitDefaultValue)
                        return string.Empty;

                    return ParameterSymbol.Language switch
                    {
                        LanguageNames.CSharp => NullText("null", "default"),
                        LanguageNames.VisualBasic => NullText("Nothing", "Nothing"),
                        _ => string.Empty,
                    };

                    string NullText(string @null, string @default)
                    {
                        var value = ParameterSymbol.ExplicitDefaultValue;
                        return value == null
                            ? ParameterSymbol.Type.IsReferenceType ? @null : @default
                            : value is string ? "\"" + value.ToString() + "\"" : value.ToString();
                    }
                }
            }

            public override bool IsDisabled => changeSignatureDialogViewModel.IsDisabled(this);

            public bool NeedsBottomBorder
            {
                get
                {
                    if (this == changeSignatureDialogViewModel._thisParameter)
                    {
                        return true;
                    }

                    if (this == changeSignatureDialogViewModel._parametersWithoutDefaultValues.LastOrDefault() &&
                        (changeSignatureDialogViewModel._parametersWithDefaultValues.Any() || changeSignatureDialogViewModel._paramsParameter != null))
                    {
                        return true;
                    }

                    if (this == changeSignatureDialogViewModel._parametersWithDefaultValues.LastOrDefault() &&
                        changeSignatureDialogViewModel._paramsParameter != null)
                    {
                        return true;
                    }

                    return false;
                }
            }

            public override bool IsRemoved { get; set; }
        }
    }
}
