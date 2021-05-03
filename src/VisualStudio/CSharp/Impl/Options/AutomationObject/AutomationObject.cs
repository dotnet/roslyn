// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    [ComVisible(true)]
    public partial class AutomationObject
    {
        private readonly Workspace _workspace;

        internal AutomationObject(Workspace workspace)
            => _workspace = workspace;

        private int GetBooleanOption(Option2<bool> key)
            => GetOption(key) ? 1 : 0;

        private int GetBooleanOption(PerLanguageOption2<bool> key)
            => GetOption(key) ? 1 : 0;

        private T GetOption<T>(PerLanguageOption2<T> key)
            => _workspace.Options.GetOption(key, LanguageNames.CSharp);

        private T GetOption<T>(Option2<T> key)
            => _workspace.Options.GetOption(key);

        private void SetBooleanOption(Option2<bool> key, int value)
            => SetOption(key, value != 0);

        private void SetBooleanOption(PerLanguageOption2<bool> key, int value)
            => SetOption(key, value != 0);

        private void SetOption<T>(PerLanguageOption2<T> key, T value)
        {
            _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                .WithChangedOption(key, LanguageNames.CSharp, value)));
        }

        private void SetOption<T>(Option2<T> key, T value)
        {
            _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                .WithChangedOption(key, value)));
        }

        private int GetBooleanOption(PerLanguageOption2<bool?> key)
        {
            var option = GetOption(key);
            if (!option.HasValue)
            {
                return -1;
            }

            return option.Value ? 1 : 0;
        }

        private int GetBooleanOption(Option2<bool?> key)
        {
            var option = GetOption(key);
            if (!option.HasValue)
            {
                return -1;
            }

            return option.Value ? 1 : 0;
        }

        private string GetXmlOption<T>(Option2<CodeStyleOption2<T>> option)
            => GetOption(option).ToXElement().ToString();

        private void SetBooleanOption(PerLanguageOption2<bool?> key, int value)
        {
            var boolValue = (value < 0) ? (bool?)null : (value > 0);
            SetOption(key, boolValue);
        }

        private void SetBooleanOption(Option2<bool?> key, int value)
        {
            var boolValue = (value < 0) ? (bool?)null : (value > 0);
            SetOption(key, boolValue);
        }

        private string GetXmlOption(PerLanguageOption2<CodeStyleOption2<bool>> option)
            => GetOption(option).ToXElement().ToString();

        private void SetXmlOption<T>(Option2<CodeStyleOption2<T>> option, string value)
        {
            var convertedValue = CodeStyleOption2<T>.FromXElement(XElement.Parse(value));
            SetOption(option, convertedValue);
        }

        private void SetXmlOption(PerLanguageOption2<CodeStyleOption2<bool>> option, string value)
        {
            var convertedValue = CodeStyleOption2<bool>.FromXElement(XElement.Parse(value));
            SetOption(option, convertedValue);
        }
    }
}
