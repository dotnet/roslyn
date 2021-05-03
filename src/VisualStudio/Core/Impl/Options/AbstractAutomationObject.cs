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

        private protected T GetOption<T>(PerLanguageOption2<T> key)
            => _workspace.Options.GetOption(key, _languageName);

        private protected void SetOption<T>(PerLanguageOption2<T> key, T value)
            => _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                .WithChangedOption(key, _languageName, value)));

        private protected T GetOption<T>(Option2<T> key)
            => _workspace.Options.GetOption(key);

        private protected void SetOption<T>(Option2<T> key, T value)
            => _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                .WithChangedOption(key, value)));

        private protected string GetXmlOption<T>(Option2<CodeStyleOption2<T>> option)
            => GetOption(option).ToXElement().ToString();

        private protected string GetXmlOption(PerLanguageOption2<CodeStyleOption2<bool>> option)
            => GetOption(option).ToXElement().ToString();

        private protected void SetXmlOption<T>(Option2<CodeStyleOption2<T>> option, string value)
        {
            var convertedValue = CodeStyleOption2<T>.FromXElement(XElement.Parse(value));
            SetOption(option, convertedValue);
        }

        private protected void SetXmlOption(PerLanguageOption2<CodeStyleOption2<bool>> option, string value)
        {
            var convertedValue = CodeStyleOption2<bool>.FromXElement(XElement.Parse(value));
            SetOption(option, convertedValue);
        }

        private protected static int NullableBooleanToInteger(bool? value)
        {
            if (!value.HasValue)
            {
                return -1;
            }

            return value.Value ? 1 : 0;
        }

        private protected static bool? IntegerToNullableBoolean(int value)
            => (value < 0) ? null : (value > 0);
    }
}
