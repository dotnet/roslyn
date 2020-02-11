// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal class AddParameterDialogViewModel : AbstractNotifyPropertyChanged
    {
        private readonly INotificationService _notificationService;

        public readonly Document Document;
        public readonly int InsertPosition;

        private readonly SemanticModel _semanticModel;

        public AddParameterDialogViewModel(Document document, int insertPosition)
        {
            _notificationService = document.Project.Solution.Workspace.Services.GetService<INotificationService>();
            _semanticModel = document.GetRequiredSemanticModelAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);

            Document = document;
            InsertPosition = insertPosition;
            ParameterName = string.Empty;
            CallSiteValue = string.Empty;
        }

        public string ParameterName { get; set; }

        public string CallSiteValue { get; set; }

        private string TypeNameWithoutErrorIndicator
        {
            get
            {
                return TypeSymbol!.ToDisplayString();
            }
        }

        private SymbolDisplayFormat _symbolDisplayFormat = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public ITypeSymbol TypeSymbol { get; set; }

        public string TypeName
        {
            get
            {
                return (TypeSymbol!.IsErrorType() ? "(x) " : "") + TypeSymbol!.ToDisplayString(_symbolDisplayFormat);
            }
        }

        public bool IsRequired { get; internal set; }
        public string DefaultValue { get; internal set; }
        public bool IsCallsiteError { get; internal set; }
        public bool IsCallsiteOmitted { get; internal set; }

        internal void UpdateTypeSymbol(string typeName)
        {
            var languageService = Document.GetRequiredLanguageService<IChangeSignatureViewModelFactoryService>();
            TypeSymbol = _semanticModel.GetSpeculativeTypeInfo(InsertPosition, languageService.GetTypeNode(typeName), SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
        }

        internal bool TrySubmit(Document document)
        {
            if (string.IsNullOrEmpty(ParameterName) || string.IsNullOrEmpty(TypeNameWithoutErrorIndicator))
            {
                SendFailureNotification(ServicesVSResources.A_type_and_name_must_be_provided);
                return false;
            }

            if (!IsParameterTypeValid(TypeNameWithoutErrorIndicator, document))
            {
                SendFailureNotification(ServicesVSResources.Parameter_type_contains_invalid_characters);
                return false;
            }

            if (!IsParameterNameValid(ParameterName, document))
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

        private bool IsParameterTypeValid(string typeName, Document document)
        {
            var languageService = document.GetRequiredLanguageService<IChangeSignatureViewModelFactoryService>();
            return languageService.IsTypeNameValid(typeName);
        }

        private bool IsParameterNameValid(string identifierName, Document document)
        {
            var languageService = document.GetRequiredLanguageService<ISyntaxFactsService>();
            return languageService.IsValidIdentifier(identifierName);
        }
    }
}
