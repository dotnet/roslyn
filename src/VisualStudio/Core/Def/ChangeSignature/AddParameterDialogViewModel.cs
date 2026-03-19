// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature;

internal sealed class AddParameterDialogViewModel : AbstractNotifyPropertyChanged
{
    private readonly INotificationService? _notificationService;

    public readonly SemanticDocument Document;
    public readonly int PositionForTypeBinding;

    public AddParameterDialogViewModel(
        SemanticDocument document, int positionForTypeBinding)
    {
        _notificationService = document.Project.Solution.Services.GetService<INotificationService>();

        TypeIsEmptyImage = Visibility.Visible;
        TypeBindsImage = Visibility.Collapsed;
        TypeDoesNotParseOrInvalidTypeImage = Visibility.Collapsed;
        TypeDoesNotBindImage = Visibility.Collapsed;
        TypeBindsDynamicStatus = ServicesVSResources.Please_enter_a_type_name;

        Document = document;
        PositionForTypeBinding = positionForTypeBinding;

        IsRequired = true;
        IsCallsiteRegularValue = true;

        ParameterName = string.Empty;
        CallSiteValue = string.Empty;
        DefaultValue = string.Empty;
    }

    public string ParameterName { get; set; }

    public string CallSiteValue { get; set; }

    private static readonly SymbolDisplayFormat s_symbolDisplayFormat = new(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public ITypeSymbol? TypeSymbol { get; set; }

    public string? TypeName => TypeSymbol?.ToDisplayString(s_symbolDisplayFormat);

    public bool TypeBinds => !TypeSymbol.IsErrorType();

    public bool IsRequired
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                NotifyPropertyChanged(nameof(IsOptional));

                if (IsCallsiteOmitted)
                {
                    IsCallsiteOmitted = false;
                    IsCallsiteRegularValue = true;

                    NotifyPropertyChanged(nameof(IsCallsiteOmitted));
                    NotifyPropertyChanged(nameof(IsCallsiteRegularValue));
                }
            }
        }
    }

    public bool IsOptional
    {
        get => !IsRequired;
        set
        {
            if (IsRequired == value)
            {
                IsRequired = !value;
            }
        }
    }

    public string DefaultValue { get; set; }
    public bool IsCallsiteTodo { get; set; }
    public bool IsCallsiteOmitted { get; set; }
    public bool IsCallsiteInferred { get; set; }
    public bool IsCallsiteRegularValue { get; set; } = true;

    public bool UseNamedArguments { get; set; }

    public string TypeBindsDynamicStatus { get; set; }
    public Visibility TypeBindsImage { get; set; }
    public Visibility TypeDoesNotBindImage { get; set; }
    public Visibility TypeDoesNotParseOrInvalidTypeImage { get; set; }
    public Visibility TypeIsEmptyImage { get; set; }

    public string VerbatimTypeName
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                SetCurrentTypeTextAndUpdateBindingStatus(value);
            }
        }
    } = string.Empty;

    private bool _isVoidParameterType;

    internal bool CanSubmit([NotNullWhen(false)] out string? message)
    {
        if (string.IsNullOrEmpty(VerbatimTypeName) || string.IsNullOrEmpty(ParameterName))
        {
            message = ServicesVSResources.A_type_and_name_must_be_provided;
            return false;
        }

        if (_isVoidParameterType)
        {
            message = ServicesVSResources.SystemVoid_is_not_a_valid_type_for_a_parameter;
            return false;
        }

        if (TypeSymbol == null || !IsParameterTypeSyntacticallyValid(VerbatimTypeName))
        {
            message = ServicesVSResources.Parameter_type_contains_invalid_characters;
            return false;
        }

        if (!IsParameterNameValid(ParameterName))
        {
            message = ServicesVSResources.Parameter_name_contains_invalid_characters;
            return false;
        }

        if (IsCallsiteRegularValue && string.IsNullOrWhiteSpace(CallSiteValue))
        {
            message = ServicesVSResources.Enter_a_call_site_value_or_choose_a_different_value_injection_kind;
            return false;
        }

        if (IsOptional && string.IsNullOrWhiteSpace(DefaultValue))
        {
            message = ServicesVSResources.Optional_parameters_must_provide_a_default_value;
            return false;
        }

        message = null;
        return true;
    }

    internal bool TrySubmit()
    {
        if (!CanSubmit(out var message))
        {
            SendFailureNotification(message);
            return false;
        }

        return true;
    }

    private void SendFailureNotification(string message)
    {
        _notificationService?.SendNotification(message, severity: NotificationSeverity.Information);
    }

    private void SetCurrentTypeTextAndUpdateBindingStatus(string typeName)
    {
        VerbatimTypeName = typeName;
        _isVoidParameterType = false;

        if (string.IsNullOrWhiteSpace(typeName))
        {
            TypeIsEmptyImage = Visibility.Visible;
            TypeDoesNotParseOrInvalidTypeImage = Visibility.Collapsed;
            TypeDoesNotBindImage = Visibility.Collapsed;
            TypeBindsImage = Visibility.Collapsed;
            TypeBindsDynamicStatus = ServicesVSResources.Please_enter_a_type_name;

            TypeSymbol = null;
        }
        else
        {
            TypeIsEmptyImage = Visibility.Collapsed;

            var languageService = Document.GetRequiredLanguageService<IChangeSignatureViewModelFactoryService>();
            TypeSymbol = Document.SemanticModel.GetSpeculativeTypeInfo(PositionForTypeBinding, languageService.GetTypeNode(typeName), SpeculativeBindingOption.BindAsTypeOrNamespace).Type;

            if (TypeSymbol is { SpecialType: SpecialType.System_Void })
            {
                _isVoidParameterType = true;
                TypeDoesNotParseOrInvalidTypeImage = Visibility.Visible;
                TypeDoesNotBindImage = Visibility.Collapsed;
                TypeBindsImage = Visibility.Collapsed;
                TypeBindsDynamicStatus = ServicesVSResources.SystemVoid_is_not_a_valid_type_for_a_parameter;
            }
            else if (!IsParameterTypeSyntacticallyValid(typeName) || TypeSymbol == null)
            {
                TypeDoesNotParseOrInvalidTypeImage = Visibility.Visible;
                TypeDoesNotBindImage = Visibility.Collapsed;
                TypeBindsImage = Visibility.Collapsed;
                TypeBindsDynamicStatus = ServicesVSResources.Type_name_has_a_syntax_error;
            }
            else
            {
                var parameterTypeBinds = DoesTypeFullyBind(TypeSymbol);
                TypeDoesNotParseOrInvalidTypeImage = Visibility.Collapsed;

                TypeBindsImage = parameterTypeBinds ? Visibility.Visible : Visibility.Collapsed;
                TypeDoesNotBindImage = !parameterTypeBinds ? Visibility.Visible : Visibility.Collapsed;
                TypeBindsDynamicStatus = parameterTypeBinds
                    ? ServicesVSResources.Type_name_is_recognized
                    : ServicesVSResources.Type_name_is_not_recognized;
            }
        }

        NotifyPropertyChanged(nameof(TypeBindsDynamicStatus));
        NotifyPropertyChanged(nameof(TypeBindsImage));
        NotifyPropertyChanged(nameof(TypeDoesNotBindImage));
        NotifyPropertyChanged(nameof(TypeDoesNotParseOrInvalidTypeImage));
        NotifyPropertyChanged(nameof(TypeIsEmptyImage));
    }

    private bool IsParameterTypeSyntacticallyValid(string typeName)
    {
        var languageService = Document.GetRequiredLanguageService<IChangeSignatureViewModelFactoryService>();
        return languageService.IsTypeNameValid(typeName);
    }

    private static bool DoesTypeFullyBind(ITypeSymbol? type)
    {
        if (type == null || type.IsErrorType())
        {
            return false;
        }

        foreach (var typeArgument in type.GetTypeArguments())
        {
            if (typeArgument is ITypeParameterSymbol)
            {
                return false;
            }

            if (!DoesTypeFullyBind(typeArgument))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsParameterNameValid(string identifierName)
    {
        var languageService = Document.GetRequiredLanguageService<ISyntaxFactsService>();
        return languageService.IsValidIdentifier(identifierName);
    }
}
