// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal class ChangeSignatureDialogViewModel : AbstractNotifyPropertyChanged
    {
        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly ParameterConfiguration _originalParameterConfiguration;

        private readonly ParameterViewModel _thisParameter;
        private readonly List<ParameterViewModel> _parametersWithoutDefaultValues;
        private readonly List<ParameterViewModel> _parametersWithDefaultValues;
        private readonly ParameterViewModel _paramsParameter;
        private HashSet<ParameterViewModel> _disabledParameters = new HashSet<ParameterViewModel>();
        public readonly int InsertPosition;

        private ImmutableArray<SymbolDisplayPart> _declarationParts;
        private bool _previewChanges;

        public readonly Document Document;

        internal ChangeSignatureDialogViewModel(
            ParameterConfiguration parameters,
            ISymbol symbol,
            Document document,
            int insertPosition,
            IClassificationFormatMap classificationFormatMap,
            ClassificationTypeMap classificationTypeMap)
        {
            _originalParameterConfiguration = parameters;
            Document = document;
            InsertPosition = insertPosition;
            _classificationFormatMap = classificationFormatMap;
            _classificationTypeMap = classificationTypeMap;

            var initialIndex = 1;

            if (parameters.ThisParameter != null)
            {
                _thisParameter = new ExistingParameterViewModel(this, parameters.ThisParameter, initialIndex++);
                _disabledParameters.Add(_thisParameter);
            }

            _declarationParts = symbol.ToDisplayParts(s_symbolDeclarationDisplayFormat);

            _parametersWithoutDefaultValues = parameters.ParametersWithoutDefaultValues.Select(p => new ExistingParameterViewModel(this, p, initialIndex++)).ToList<ParameterViewModel>();
            _parametersWithDefaultValues = parameters.RemainingEditableParameters.Select(p => new ExistingParameterViewModel(this, p, initialIndex++)).ToList<ParameterViewModel>();

            if (parameters.ParamsParameter != null)
            {
                _paramsParameter = new ExistingParameterViewModel(this, parameters.ParamsParameter, initialIndex++);
            }

            var selectedIndex = parameters.SelectedIndex;
            if (parameters.ThisParameter != null && selectedIndex == 0)
            {
                if (parameters.ParametersWithoutDefaultValues.Count + parameters.RemainingEditableParameters.Count > 0)
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
            NotifyPropertyChanged(nameof(IsOkButtonEnabled));
            NotifyPropertyChanged(nameof(CanRemove));
            NotifyPropertyChanged(nameof(RemoveAutomationText));
            NotifyPropertyChanged(nameof(CanRestore));
            NotifyPropertyChanged(nameof(RestoreAutomationText));
            NotifyPropertyChanged(nameof(CanEdit));
            NotifyPropertyChanged(nameof(EditAutomationText));
        }

        internal ParameterConfiguration GetParameterConfiguration()
        {
            return new ParameterConfiguration(
                _originalParameterConfiguration.ThisParameter,
                _parametersWithoutDefaultValues.Where(p => !p.IsRemoved).Select(p => p.CreateParameter()).ToList(),
                _parametersWithDefaultValues.Where(p => !p.IsRemoved).Select(p => p.CreateParameter()).ToList(),
                (_paramsParameter == null || _paramsParameter.IsRemoved) ? null : (_paramsParameter as ExistingParameterViewModel).CreateParameter(),
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
                if (parameter is ExistingParameterViewModel existingParameter)
                {
                    displayParts.AddRange(existingParameter.ParameterSymbol.ToDisplayParts(s_parameterDisplayFormat));
                }

                if (parameter is AddedParameterViewModel addedParameterViewModel)
                {
                    // TODO there should be another formatting for VB
                    displayParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, addedParameterViewModel.Type));
                    displayParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " "));
                    displayParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, null, addedParameterViewModel.Parameter)); ;
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
            NotifyPropertyChanged(nameof(IsOkButtonEnabled));
        }

        internal bool TrySubmit()
        {
            return IsOkButtonEnabled;
        }

        private bool IsDisabled(ParameterViewModel parameterViewModel)
        {
            return _disabledParameters.Contains(parameterViewModel);
        }

        public bool IsOkButtonEnabled
        {
            get
            {
                return true;
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
                NotifyPropertyChanged(nameof(CanEdit));
                NotifyPropertyChanged(nameof(EditAutomationText));
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

        public string EditAutomationText
        {
            get
            {
                if (!CanEdit)
                {
                    return string.Empty;
                }

                return string.Format(ServicesVSResources.Edit_0, AllParameters[SelectedIndex.Value].ParameterAutomationText);
            }
        }

        public abstract class ParameterViewModel
        {
            protected readonly ChangeSignatureDialogViewModel changeSignatureDialogViewModel;

            public abstract string Type { get; }
            public abstract string Parameter { get; }
            public abstract bool IsRemoved { get; set; }
            public abstract string ParameterAutomationText { get; }
            public abstract bool IsDisabled { get; }
            public abstract string Callsite { get; }

            public ParameterViewModel(ChangeSignatureDialogViewModel changeSignatureDialogViewModel)
            {
                this.changeSignatureDialogViewModel = changeSignatureDialogViewModel;
            }

            internal abstract Parameter CreateParameter();

            public abstract string InitialIndex { get; }
        }

        public class AddedParameterViewModel : ParameterViewModel
        {
            private readonly AddedParameter _addedParameter;

            public AddedParameterViewModel(ChangeSignatureDialogViewModel changeSignatureDialogViewModel, AddedParameter addedParameter)
                : base(changeSignatureDialogViewModel)
            {
                _addedParameter = addedParameter;
            }

            public override string Type => _addedParameter.TypeName;

            public override string Parameter => _addedParameter.ParameterName;

            public override bool IsRemoved { get => false; set => throw new InvalidOperationException(); }

            public override string ParameterAutomationText => $"{Type} {Parameter}";
            public override bool IsDisabled => false;
            public override string Callsite => _addedParameter.CallsiteValue;

            internal override Parameter CreateParameter()
                => new AddedParameter(Type, Parameter, Callsite);

            public override string InitialIndex => "NEW";
        }

        public class ExistingParameterViewModel : ParameterViewModel
        {
            public IParameterSymbol ParameterSymbol { get; }

            public ExistingParameterViewModel(ChangeSignatureDialogViewModel changeSignatureDialogViewModel, Parameter parameter, int initialIndex)
                : base(changeSignatureDialogViewModel)
            {
                ParameterSymbol = (parameter as ExistingParameter).Symbol;
                InitialIndex = initialIndex.ToString();
            }

            internal override Parameter CreateParameter()
            {
                return new ExistingParameter(ParameterSymbol);
            }

            public override string ParameterAutomationText => $"{Type} {Parameter}";

            public override string Callsite => string.Empty;

            public override string InitialIndex { get; }

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

                        if (changeSignatureDialogViewModel._thisParameter != null &&
                            ParameterSymbol == (changeSignatureDialogViewModel._thisParameter as ExistingParameterViewModel).ParameterSymbol)
                        {
                            return @this ?? string.Empty;
                        }
                        return string.Empty;
                    }
                }
            }

            public override string Type => ParameterSymbol.Type.ToDisplayString(s_parameterDisplayFormat);

            public override string Parameter => ParameterSymbol.Name;

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
