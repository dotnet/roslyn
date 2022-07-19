// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    public abstract class AbstractAutomationObject
    {
        private readonly Workspace _workspace;
        private readonly string _languageName;

        protected AbstractAutomationObject(Workspace workspace, string languageName)
            => (_workspace, _languageName) = (workspace, languageName);

        private protected T GetOption<T>(PerLanguageValuedOption2<T> key)
            => _workspace.Options.GetOption(key, _languageName);

        private protected void SetOption<T>(PerLanguageValuedOption2<T> key, T value)
            => _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                .WithChangedOption(key, _languageName, value)));

        private protected T GetOption<T>(SingleValuedOption2<T> key)
            => _workspace.Options.GetOption(key);

        private protected void SetOption<T>(SingleValuedOption2<T> key, T value)
            => _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                .WithChangedOption(key, value)));

        private protected int GetBooleanOption(PerLanguageValuedOption2<bool?> key)
            => NullableBooleanToInteger(GetOption(key));

        private protected void SetBooleanOption(PerLanguageValuedOption2<bool?> key, int value)
            => SetOption(key, IntegerToNullableBoolean(value));

        private protected int GetBooleanOption(SingleValuedOption2<bool?> key)
            => NullableBooleanToInteger(GetOption(key));

        private protected void SetBooleanOption(SingleValuedOption2<bool?> key, int value)
            => SetOption(key, IntegerToNullableBoolean(value));

        private protected string GetXmlOption<T>(SingleValuedOption2<CodeStyleOption2<T>> option)
            => GetOption(option).ToXElement().ToString();

        private protected string GetXmlOption<T>(PerLanguageValuedOption2<CodeStyleOption2<T>> option)
            => GetOption(option).ToXElement().ToString();

        private protected void SetXmlOption<T>(SingleValuedOption2<CodeStyleOption2<T>> option, string value)
        {
            var convertedValue = CodeStyleOption2<T>.FromXElement(XElement.Parse(value));
            SetOption(option, convertedValue);
        }

        private protected void SetXmlOption<T>(PerLanguageValuedOption2<CodeStyleOption2<T>> option, string value)
        {
            var convertedValue = CodeStyleOption2<T>.FromXElement(XElement.Parse(value));
            SetOption(option, convertedValue);
        }

        private static int NullableBooleanToInteger(bool? value)
        {
            if (!value.HasValue)
            {
                return -1;
            }

            return value.Value ? 1 : 0;
        }

        private static bool? IntegerToNullableBoolean(int value)
            => (value < 0) ? null : (value > 0);
    }
}
