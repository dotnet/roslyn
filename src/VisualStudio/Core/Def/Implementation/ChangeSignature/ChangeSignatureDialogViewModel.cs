// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal class ChangeSignatureDialogViewModel : AbstractNotifyPropertyChanged
    {
        private readonly INotificationService _notificationService;
        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly ParameterConfiguration _originalParameterConfiguration;
        private readonly ISymbol _symbol;

        private readonly ParameterViewModel _thisParameter;
        private readonly List<ParameterViewModel> _parameterGroup1;
        private readonly List<ParameterViewModel> _parameterGroup2;
        private readonly ParameterViewModel _paramsParameter;
        private readonly HashSet<IParameterSymbol> _disabledParameters = new HashSet<IParameterSymbol>();
        private ImmutableArray<SymbolDisplayPart> _declarationParts;
        private bool _previewChanges;

        internal ChangeSignatureDialogViewModel(INotificationService notificationService, ParameterConfiguration parameters, ISymbol symbol, IClassificationFormatMap classificationFormatMap, ClassificationTypeMap classificationTypeMap)
        {
            _originalParameterConfiguration = parameters;
            _notificationService = notificationService;
            _classificationFormatMap = classificationFormatMap;
            _classificationTypeMap = classificationTypeMap;

            if (parameters.ThisParameter != null)
            {
                _thisParameter = new ParameterViewModel(this, parameters.ThisParameter);
                _disabledParameters.Add(parameters.ThisParameter);
            }

            if (parameters.ParamsParameter != null)
            {
                _paramsParameter = new ParameterViewModel(this, parameters.ParamsParameter);
            }

            _symbol = symbol;
            _declarationParts = symbol.ToDisplayParts(s_symbolDeclarationDisplayFormat);

            _parameterGroup1 = parameters.ParametersWithoutDefaultValues.Select(p => new ParameterViewModel(this, p)).ToList();
            _parameterGroup2 = parameters.RemainingEditableParameters.Select(p => new ParameterViewModel(this, p)).ToList();

            var selectedIndex = parameters.SelectedIndex;
            this.SelectedIndex = (parameters.ThisParameter != null && selectedIndex == 0) ? 1 : selectedIndex;
        }

        public int GetStartingSelectionIndex()
        {
            return _thisParameter == null ? 0 : 1;
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
                if (!SelectedIndex.HasValue)
                {
                    return false;
                }

                var index = SelectedIndex.Value;

                if (index == 0 && _thisParameter != null)
                {
                    return false;
                }

                // index = thisParameter == null ? index : index - 1;

                return !AllParameters[index].IsRemoved;
            }
        }

        public bool CanRestore
        {
            get
            {
                if (!SelectedIndex.HasValue)
                {
                    return false;
                }

                var index = SelectedIndex.Value;

                if (index == 0 && _thisParameter != null)
                {
                    return false;
                }

                // index = thisParameter == null ? index : index - 1;

                return AllParameters[index].IsRemoved;
            }
        }

        internal void Remove()
        {
            AllParameters[_selectedIndex.Value].IsRemoved = true;
            NotifyPropertyChanged(nameof(AllParameters));
            NotifyPropertyChanged(nameof(SignatureDisplay));
            NotifyPropertyChanged(nameof(SignaturePreviewAutomationText));
            NotifyPropertyChanged(nameof(IsOkButtonEnabled));
            NotifyPropertyChanged(nameof(CanRemove));
            NotifyPropertyChanged(nameof(RemoveAutomationText));
            NotifyPropertyChanged(nameof(CanRestore));
            NotifyPropertyChanged(nameof(RestoreAutomationText));
        }

        internal void Restore()
        {
            AllParameters[_selectedIndex.Value].IsRemoved = false;
            NotifyPropertyChanged(nameof(AllParameters));
            NotifyPropertyChanged(nameof(SignatureDisplay));
            NotifyPropertyChanged(nameof(SignaturePreviewAutomationText));
            NotifyPropertyChanged(nameof(IsOkButtonEnabled));
            NotifyPropertyChanged(nameof(CanRemove));
            NotifyPropertyChanged(nameof(RemoveAutomationText));
            NotifyPropertyChanged(nameof(CanRestore));
            NotifyPropertyChanged(nameof(RestoreAutomationText));
        }

        internal ParameterConfiguration GetParameterConfiguration()
        {
            return new ParameterConfiguration(
                _originalParameterConfiguration.ThisParameter,
                _parameterGroup1.Where(p => !p.IsRemoved).Select(p => p.ParameterSymbol).ToList(),
                _parameterGroup2.Where(p => !p.IsRemoved).Select(p => p.ParameterSymbol).ToList(),
                (_paramsParameter == null || _paramsParameter.IsRemoved) ? null : _paramsParameter.ParameterSymbol,
                selectedIndex: -1);
        }

        private static readonly SymbolDisplayFormat s_symbolDeclarationDisplayFormat = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
            extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
            memberOptions:
                SymbolDisplayMemberOptions.IncludeType |
                SymbolDisplayMemberOptions.IncludeExplicitInterface |
                SymbolDisplayMemberOptions.IncludeAccessibility |
                SymbolDisplayMemberOptions.IncludeModifiers |
                SymbolDisplayMemberOptions.IncludeRef);

        private static readonly SymbolDisplayFormat s_parameterDisplayFormat = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
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
        {
            return GetSignatureDisplayParts().Select(p => p.ToString()).Join("");
        }

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
                displayParts.AddRange(parameter.ParameterSymbol.ToDisplayParts(s_parameterDisplayFormat));
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

                list.AddRange(_parameterGroup1);
                list.AddRange(_parameterGroup2);

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
                if (index <= 0 || index == _parameterGroup1.Count || index >= _parameterGroup1.Count + _parameterGroup2.Count)
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
                if (index < 0 || index == _parameterGroup1.Count - 1 || index >= _parameterGroup1.Count + _parameterGroup2.Count - 1)
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
            Move(index < _parameterGroup1.Count ? _parameterGroup1 : _parameterGroup2, index < _parameterGroup1.Count ? index : index - _parameterGroup1.Count, delta: -1);
        }

        internal void MoveDown()
        {
            Debug.Assert(CanMoveDown);

            var index = SelectedIndex.Value;
            index = _thisParameter == null ? index : index - 1;
            Move(index < _parameterGroup1.Count ? _parameterGroup1 : _parameterGroup2, index < _parameterGroup1.Count ? index : index - _parameterGroup1.Count, delta: 1);
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
            NotifyPropertyChanged(nameof(IsOkButtonEnabled));
        }

        internal bool TrySubmit()
        {
            return IsOkButtonEnabled;
        }

        private bool IsDisabled(ParameterViewModel parameterViewModel)
        {
            return _disabledParameters.Contains(parameterViewModel.ParameterSymbol);
        }

        private IList<ParameterViewModel> GetSelectedGroup()
        {
            var index = SelectedIndex;
            index = _thisParameter == null ? index : index - 1;
            return index < _parameterGroup1.Count ? _parameterGroup1 : index < _parameterGroup1.Count + _parameterGroup2.Count ? _parameterGroup2 : SpecializedCollections.EmptyList<ParameterViewModel>();
        }

        public bool IsOkButtonEnabled
        {
            get
            {
                return AllParameters.Any(p => p.IsRemoved) ||
                    !_parameterGroup1.Select(p => p.ParameterSymbol).SequenceEqual(_originalParameterConfiguration.ParametersWithoutDefaultValues) ||
                    !_parameterGroup2.Select(p => p.ParameterSymbol).SequenceEqual(_originalParameterConfiguration.RemainingEditableParameters);
            }
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

                return string.Format(ServicesVSResources.Move_0_above_1, AllParameters[SelectedIndex.Value].ParameterAutomationText, AllParameters[SelectedIndex.Value - 1].ParameterAutomationText);
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

                return string.Format(ServicesVSResources.Move_0_below_1, AllParameters[SelectedIndex.Value].ParameterAutomationText, AllParameters[SelectedIndex.Value + 1].ParameterAutomationText);
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

                return string.Format(ServicesVSResources.Remove_0, AllParameters[SelectedIndex.Value].ParameterAutomationText);
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

                return string.Format(ServicesVSResources.Restore_0, AllParameters[SelectedIndex.Value].ParameterAutomationText);
            }
        }

        public class ParameterViewModel
        {
            private readonly ChangeSignatureDialogViewModel _changeSignatureDialogViewModel;

            public IParameterSymbol ParameterSymbol { get; }

            public ParameterViewModel(ChangeSignatureDialogViewModel changeSignatureDialogViewModel, IParameterSymbol parameter)
            {
                _changeSignatureDialogViewModel = changeSignatureDialogViewModel;
                ParameterSymbol = parameter;
            }

            public string ParameterAutomationText => $"{Type} {Parameter}";

            public string Modifier
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

                        if (_changeSignatureDialogViewModel._thisParameter != null &&
                            Equals(ParameterSymbol, _changeSignatureDialogViewModel._thisParameter.ParameterSymbol))
                        {
                            return @this ?? string.Empty;
                        }
                        return string.Empty;
                    }
                }
            }

            public string Type => ParameterSymbol.Type.ToDisplayString(s_parameterDisplayFormat);

            public string Parameter => ParameterSymbol.Name;

            public string Default
            {
                get
                {
                    if (!ParameterSymbol.HasExplicitDefaultValue)
                    {
                        return string.Empty;
                    }
                    switch (ParameterSymbol.Language)
                    {
                        case LanguageNames.CSharp:
                            return NullText("null");
                        case LanguageNames.VisualBasic:
                            return NullText("Nothing");
                    }
                    return string.Empty;

                    string NullText(string @null)
                    {
                        return ParameterSymbol.ExplicitDefaultValue == null ? @null :
                               ParameterSymbol.ExplicitDefaultValue is string ? "\"" + ParameterSymbol.ExplicitDefaultValue.ToString() + "\"" :
                               ParameterSymbol.ExplicitDefaultValue.ToString();
                    }

                }
            }

            public bool IsDisabled => _changeSignatureDialogViewModel.IsDisabled(this);

            public bool NeedsBottomBorder
            {
                get
                {
                    if (this == _changeSignatureDialogViewModel._thisParameter)
                    {
                        return true;
                    }

                    if (this == _changeSignatureDialogViewModel._parameterGroup1.LastOrDefault() &&
                        (_changeSignatureDialogViewModel._parameterGroup2.Any() || _changeSignatureDialogViewModel._paramsParameter != null))
                    {
                        return true;
                    }

                    if (this == _changeSignatureDialogViewModel._parameterGroup2.LastOrDefault() &&
                        _changeSignatureDialogViewModel._paramsParameter != null)
                    {
                        return true;
                    }

                    return false;
                }
            }

            public bool IsRemoved { get; set; }
        }
    }
}
