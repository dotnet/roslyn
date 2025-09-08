// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature;

internal sealed partial class ChangeSignatureDialogViewModel : AbstractNotifyPropertyChanged
{
    private readonly IClassificationFormatMap _classificationFormatMap;
    private readonly ClassificationTypeMap _classificationTypeMap;
    private readonly INotificationService _notificationService;
    private readonly ParameterConfiguration _originalParameterConfiguration;

    // This can be changed to ParameterViewModel if we will allow adding 'this' parameter.
    private readonly ExistingParameterViewModel? _thisParameter;
    private readonly List<ParameterViewModel> _parametersWithoutDefaultValues;
    private readonly List<ParameterViewModel> _parametersWithDefaultValues;

    // This can be changed to ParameterViewModel if we will allow adding 'params' parameter.
    private readonly ExistingParameterViewModel? _paramsParameter;
    private readonly HashSet<ParameterViewModel> _disabledParameters = [];

    private readonly ImmutableArray<SymbolDisplayPart> _declarationParts;

    /// <summary>
    /// The document where the symbol we are changing signature is defined.
    /// </summary>
    private readonly SemanticDocument _document;
    private readonly int _positionForTypeBinding;

    internal ChangeSignatureDialogViewModel(
        SemanticDocument document,
        ParameterConfiguration parameters,
        ISymbol symbol,
        int positionForTypeBinding,
        IClassificationFormatMap classificationFormatMap,
        ClassificationTypeMap classificationTypeMap)
    {
        _document = document;
        _originalParameterConfiguration = parameters;
        _positionForTypeBinding = positionForTypeBinding;
        _classificationFormatMap = classificationFormatMap;
        _classificationTypeMap = classificationTypeMap;

        _notificationService = document.Project.Solution.Services.GetRequiredService<INotificationService>();

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

        UpdateNameConflictMarkers();

        var selectedIndex = parameters.SelectedIndex;
        // Currently, we do not support editing the ThisParameter. 
        // Therefore, if there is such parameter, we should move the selectedIndex.
        if (parameters.ThisParameter != null && selectedIndex == 0)
        {
            // If we have at least one parameter after the ThisParameter, select the first one after This.
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

    private void UpdateNameConflictMarkers()
    {
        var parameterNameOverlapMap = new Dictionary<string, List<ParameterViewModel>>();
        foreach (var parameter in AllParameters)
        {
            if (!parameter.IsRemoved)
            {
                parameterNameOverlapMap
                    .GetOrAdd(parameter.ParameterName, _ => [])
                    .Add(parameter);
            }
            else
            {
                parameter.HasParameterNameConflict = Visibility.Collapsed;
            }
        }

        foreach (var parameterName in parameterNameOverlapMap.Keys)
        {
            var matchingParameters = parameterNameOverlapMap[parameterName];
            if (matchingParameters.Count > 1)
            {
                foreach (var matchingParameter in matchingParameters)
                {
                    matchingParameter.HasParameterNameConflict = Visibility.Visible;
                }
            }
            else
            {
                matchingParameters.Single().HasParameterNameConflict = Visibility.Collapsed;
            }
        }

        NotifyPropertyChanged(nameof(AllParameters));
    }

    public AddParameterDialogViewModel CreateAddParameterDialogViewModel()
        => new(_document, _positionForTypeBinding);

    private List<ParameterViewModel> CreateParameterViewModels(ImmutableArray<Parameter> parameters, ref int initialIndex)
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

    public bool PreviewChanges { get; set; }

    public bool CanRemove
    {
        get
        {
            if (!EditableParameterSelected(out var index))
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
            if (!EditableParameterSelected(out var index))
            {
                return false;
            }

            return AllParameters[index].IsRemoved;
        }
    }

    private bool EditableParameterSelected(out int index)
    {
        index = -1;

        if (!AllParameters.Any())
        {
            return false;
        }

        if (!SelectedIndex.HasValue)
        {
            return false;
        }

        index = SelectedIndex.Value;

        if (index == 0 && _thisParameter != null)
        {
            return false;
        }

        return true;
    }

    internal void Remove()
    {
        if (AllParameters[SelectedIndex!.Value] is AddedParameterViewModel)
        {
            var parameterToRemove = AllParameters[SelectedIndex!.Value];

            if (!_parametersWithoutDefaultValues.Remove(parameterToRemove))
            {
                _parametersWithDefaultValues.Remove(parameterToRemove);
            }
        }
        else
        {
            AllParameters[SelectedIndex!.Value].IsRemoved = true;
        }

        UpdateNameConflictMarkers();
        RemoveRestoreNotifyPropertyChanged();
    }

    internal void Restore()
    {
        AllParameters[SelectedIndex!.Value].IsRemoved = false;
        UpdateNameConflictMarkers();
        RemoveRestoreNotifyPropertyChanged();
    }

    internal void AddParameter(AddedParameter addedParameter)
    {
        if (addedParameter.IsRequired)
        {
            _parametersWithoutDefaultValues.Add(new AddedParameterViewModel(this, addedParameter));
        }
        else
        {
            _parametersWithDefaultValues.Add(new AddedParameterViewModel(this, addedParameter));
        }

        UpdateNameConflictMarkers();
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
    }

    internal ParameterConfiguration GetParameterConfiguration()
    {
        return new ParameterConfiguration(
            _originalParameterConfiguration.ThisParameter,
            _parametersWithoutDefaultValues.SelectAsArray(p => !p.IsRemoved, p => p.Parameter),
            _parametersWithDefaultValues.SelectAsArray(p => !p.IsRemoved, p => p.Parameter),
            (_paramsParameter == null || _paramsParameter.IsRemoved) ? null : (ExistingParameter)_paramsParameter.Parameter,
            selectedIndex: -1);
    }

    private static readonly SymbolDisplayFormat s_symbolDeclarationDisplayFormat = new(
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

    private static readonly SymbolDisplayFormat s_parameterDisplayFormat = new(
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
                    var languageService = _document.GetRequiredLanguageService<IChangeSignatureViewModelFactoryService>();
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

        var index = SelectedIndex!.Value;
        index = _thisParameter == null ? index : index - 1;
        Move(index < _parametersWithoutDefaultValues.Count ? _parametersWithoutDefaultValues : _parametersWithDefaultValues, index < _parametersWithoutDefaultValues.Count ? index : index - _parametersWithoutDefaultValues.Count, delta: -1);
    }

    internal void MoveDown()
    {
        Debug.Assert(CanMoveDown);

        var index = SelectedIndex!.Value;
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

    internal bool CanSubmit([NotNullWhen(false)] out string? message)
    {
        var canSubmit = AllParameters.Any(p => p.IsRemoved) ||
            AllParameters.Any(p => p is AddedParameterViewModel) ||
                !_parametersWithoutDefaultValues.OfType<ExistingParameterViewModel>().Select(p => p.ParameterSymbol).SequenceEqual(_originalParameterConfiguration.ParametersWithoutDefaultValues.Cast<ExistingParameter>().Select(p => p.Symbol)) ||
                !_parametersWithDefaultValues.OfType<ExistingParameterViewModel>().Select(p => p.ParameterSymbol).SequenceEqual(_originalParameterConfiguration.RemainingEditableParameters.Cast<ExistingParameter>().Select(p => p.Symbol));

        if (!canSubmit)
        {
            message = ServicesVSResources.You_must_change_the_signature;
            return false;
        }

        message = null;
        return true;
    }

    internal bool TrySubmit()
    {
        if (!CanSubmit(out var message))
        {
            _notificationService.SendNotification(message, severity: NotificationSeverity.Information);
            return false;
        }

        return true;
    }

    private bool IsDisabled(ParameterViewModel parameterViewModel)
    {
        return _disabledParameters.Contains(parameterViewModel);
    }

    public int? SelectedIndex
    {
        get;

        set
        {
            var newSelectedIndex = value == -1 ? null : value;
            if (newSelectedIndex == field)
            {
                return;
            }

            field = newSelectedIndex;

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

            return string.Format(ServicesVSResources.Move_0_above_1, AllParameters[SelectedIndex!.Value].ShortAutomationText, AllParameters[SelectedIndex!.Value - 1].ShortAutomationText);
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

            return string.Format(ServicesVSResources.Move_0_below_1, AllParameters[SelectedIndex!.Value].ShortAutomationText, AllParameters[SelectedIndex!.Value + 1].ShortAutomationText);
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

            return string.Format(ServicesVSResources.Remove_0, AllParameters[SelectedIndex!.Value].ShortAutomationText);
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

            return string.Format(ServicesVSResources.Restore_0, AllParameters[SelectedIndex!.Value].ShortAutomationText);
        }
    }
}
