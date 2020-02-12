// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal partial class ChangeSignatureDialogViewModel
    {
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
            public abstract bool IsRequired { get; }
            public abstract string DefaultValue { get; }

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

            public virtual string CallSiteValue => string.Empty;
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

            public override string ParameterName => _addedParameter.Name;

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

            public override string CallSite
            {
                get
                {
                    return _addedParameter.UseNamedArguments
                        ? _addedParameter.Name + ": " + _addedParameter.CallSiteValue
                        : _addedParameter.CallSiteValue;
                }
            }

            public override string CallSiteValue => _addedParameter.CallSiteValue;

            public override string InitialIndex => "+";

            // Newly added parameters cannot have modifiers yet
            public override string Modifier => string.Empty;

            // Only required parameters are supported currently
            public override string Default => _addedParameter.DefaultValue;

            public override bool IsRequired => _addedParameter.IsRequired;
            public override string DefaultValue => _addedParameter.DefaultValue;
        }

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

            public override string Modifier
            {
                get
                {
                    switch (ParameterSymbol.Language)
                    {
                        case LanguageNames.CSharp:
                            return ModifierText("out", "ref", "in", "params", "this");
                        case LanguageNames.VisualBasic:
                            return ModifierText(@out: null, "ByRef", @in: null, "ParamArray", "Me");
                        default:
                            return string.Empty;
                    }

                    string ModifierText(string? @out, string? @ref, string? @in, string? @params, string? @this)
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

            public override string ParameterName => ParameterSymbol.Name;

            public override string Default
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

            public override bool IsRemoved { get; set; }

            public override bool IsRequired => !ParameterSymbol.HasExplicitDefaultValue;
            public override string DefaultValue => Default;
        }
    }
}
