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
    // TODO: Dedupe against ParameterViewModel
    internal class ParameterDetailsDialogViewModel : AbstractNotifyPropertyChanged
    {
        private readonly INotificationService _notificationService;
        private readonly IParameterSymbol _parameterSymbol;

        private RefKind _refKind;

        internal ParameterDetailsDialogViewModel(INotificationService notificationService, IParameterSymbol parameterSymbol)
        {
            _notificationService = notificationService;

            _parameterSymbol = parameterSymbol;
            _refKind = parameterSymbol.RefKind;
        }

        public string Modifier
        {
            get
            {
                switch (_parameterSymbol.Language)
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
                    switch (_refKind)
                    {
                        case RefKind.Out:
                            return @out ?? string.Empty;
                        case RefKind.Ref:
                            return @ref ?? string.Empty;
                        case RefKind.In:
                            return @in ?? string.Empty;
                    }

                    return string.Empty;
                }
            }
        }

        public string Type => _parameterSymbol.Type.ToDisplayString(s_parameterDisplayFormat);

        public string Name => _parameterSymbol.Name;

        public string Default
        {
            get
            {
                if (!_parameterSymbol.HasExplicitDefaultValue)
                {
                    return string.Empty;
                }
                switch (_parameterSymbol.Language)
                {
                    case LanguageNames.CSharp:
                        return NullText("null");
                    case LanguageNames.VisualBasic:
                        return NullText("Nothing");
                }
                return string.Empty;

                string NullText(string @null)
                {
                    return _parameterSymbol.ExplicitDefaultValue == null ? @null :
                           _parameterSymbol.ExplicitDefaultValue is string ? "\"" + _parameterSymbol.ExplicitDefaultValue.ToString() + "\"" :
                           _parameterSymbol.ExplicitDefaultValue.ToString();
                }

            }
        }
        internal bool TrySubmit()
        {
            return IsOkButtonEnabled;
        }

        public bool IsOkButtonEnabled
        {
            get
            {
                // TODO
                return true;
            }
        }

        private static SymbolDisplayFormat s_parameterDisplayFormat = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
            parameterOptions:
                SymbolDisplayParameterOptions.IncludeType |
                SymbolDisplayParameterOptions.IncludeParamsRefOut |
                SymbolDisplayParameterOptions.IncludeDefaultValue |
                SymbolDisplayParameterOptions.IncludeExtensionThis |
                SymbolDisplayParameterOptions.IncludeName);

    }
}
