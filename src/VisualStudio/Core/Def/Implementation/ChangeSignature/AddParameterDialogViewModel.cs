// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Utilities.Internal;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal class AddParameterDialogViewModel : AbstractNotifyPropertyChanged
    {
        private readonly INotificationService? _notificationService;

        public readonly Document Document;
        public readonly int InsertPosition;

        private readonly SemanticModel _semanticModel;

        public AddParameterDialogViewModel(Document document, int insertPosition)
        {
            _notificationService = document.Project.Solution.Workspace.Services.GetService<INotificationService>();
            _semanticModel = document.GetRequiredSemanticModelAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);

            TypeIsEmptyImage = Visibility.Visible;
            TypeBindsImage = Visibility.Collapsed;
            TypeDoesNotParseImage = Visibility.Collapsed;
            TypeDoesNotBindImage = Visibility.Collapsed;
            TypeBindsDynamicStatus = ServicesVSResources.Please_enter_a_type_name;

            Document = document;
            InsertPosition = insertPosition;
            ParameterName = string.Empty;
            CallSiteValue = string.Empty;
        }

        public string ParameterName { get; set; }

        public string CallSiteValue { get; set; }

        private SymbolDisplayFormat _symbolDisplayFormat = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public ITypeSymbol? TypeSymbol { get; set; }

        public string? TypeName
        {
            get
            {
                return TypeSymbol?.ToDisplayString(_symbolDisplayFormat);
            }
        }

        public bool TypeBinds => !TypeSymbol!.IsErrorType();

        public bool IsRequired { get; internal set; }
        public string? DefaultValue { get; internal set; }
        public bool IsCallsiteError { get; internal set; }
        public bool IsCallsiteOmitted { get; internal set; }

        public bool UseNamedArguments { get; set; }

        private string _verbatimTypeName = string.Empty;

        internal bool TrySubmit()
        {
            if (string.IsNullOrEmpty(_verbatimTypeName) || string.IsNullOrEmpty(ParameterName))
            {
                SendFailureNotification(ServicesVSResources.A_type_and_name_must_be_provided);
                return false;
            }

            if (!IsParameterTypeSyntacticallyValid(_verbatimTypeName))
            {
                SendFailureNotification(ServicesVSResources.Parameter_type_contains_invalid_characters);
                return false;
            }

            if (!IsParameterNameValid(ParameterName))
            {
                SendFailureNotification(ServicesVSResources.Parameter_name_contains_invalid_characters);
                return false;
            }

            return true;
        }

        private void SendFailureNotification(string message)
        {
            _notificationService?.SendNotification(message, severity: NotificationSeverity.Information);
        }

        internal void SetCurrentTypeTextAndUpdateBindingStatus(string typeName)
        {
            _verbatimTypeName = typeName;

            if (typeName.IsNullOrWhiteSpace())
            {
                TypeIsEmptyImage = Visibility.Visible;
                TypeDoesNotParseImage = Visibility.Collapsed;
                TypeDoesNotBindImage = Visibility.Collapsed;
                TypeBindsImage = Visibility.Collapsed;
                TypeBindsDynamicStatus = ServicesVSResources.Please_enter_a_type_name;

                TypeSymbol = null;
            }
            else
            {
                TypeIsEmptyImage = Visibility.Collapsed;

                var languageService = Document.GetRequiredLanguageService<IChangeSignatureViewModelFactoryService>();
                TypeSymbol = _semanticModel.GetSpeculativeTypeInfo(InsertPosition, languageService.GetTypeNode(typeName), SpeculativeBindingOption.BindAsTypeOrNamespace).Type;

                var typeParses = IsParameterTypeSyntacticallyValid(typeName);
                if (!typeParses)
                {
                    TypeDoesNotParseImage = Visibility.Visible;
                    TypeDoesNotBindImage = Visibility.Collapsed;
                    TypeBindsImage = Visibility.Collapsed;
                    TypeBindsDynamicStatus = ServicesVSResources.Type_name_does_not_parse_correctly;
                }
                else
                {
                    var parameterTypeBinds = DoesTypeFullyBind(TypeSymbol);
                    TypeDoesNotParseImage = Visibility.Collapsed;

                    TypeBindsImage = parameterTypeBinds ? Visibility.Visible : Visibility.Collapsed;
                    TypeDoesNotBindImage = !parameterTypeBinds ? Visibility.Visible : Visibility.Collapsed;
                    TypeBindsDynamicStatus = parameterTypeBinds
                        ? ServicesVSResources.Type_name_parses_correctly_and_is_recognized
                        : ServicesVSResources.Type_name_parses_correctly_but_is_not_recognized;
                }
            }

            NotifyPropertyChanged(nameof(TypeBindsDynamicStatus));
            NotifyPropertyChanged(nameof(TypeBindsImage));
            NotifyPropertyChanged(nameof(TypeDoesNotBindImage));
            NotifyPropertyChanged(nameof(TypeDoesNotParseImage));
            NotifyPropertyChanged(nameof(TypeIsEmptyImage));
        }

        private bool IsParameterTypeSyntacticallyValid(string typeName)
        {
            var languageService = Document.GetRequiredLanguageService<IChangeSignatureViewModelFactoryService>();
            return languageService.IsTypeNameValid(typeName);
        }

        private bool DoesTypeFullyBind(ITypeSymbol? type)
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

        public string? TypeBindsDynamicStatus { get; set; }
        public Visibility TypeBindsImage { get; set; }
        public Visibility TypeDoesNotBindImage { get; set; }
        public Visibility TypeDoesNotParseImage { get; set; }
        public Visibility TypeIsEmptyImage { get; set; }

        private bool IsParameterNameValid(string identifierName)
        {
            var languageService = Document.GetRequiredLanguageService<ISyntaxFactsService>();
            return languageService.IsValidIdentifier(identifierName);
        }
    }
}
