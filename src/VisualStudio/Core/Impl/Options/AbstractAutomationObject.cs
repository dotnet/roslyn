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
        private readonly ILegacyGlobalOptionService _legacyGlobalOptions;
        private readonly string _languageName;

        internal AbstractAutomationObject(ILegacyGlobalOptionService legacyGlobalOptions, string languageName)
            => (_legacyGlobalOptions, _languageName) = (legacyGlobalOptions, languageName);

        private protected T GetOption<T>(PerLanguageOption2<T> option)
            => _legacyGlobalOptions.GlobalOptions.GetOption(option, _languageName);

        private protected void SetOption<T>(PerLanguageOption2<T> option, T value)
        {
            _legacyGlobalOptions.GlobalOptions.SetGlobalOption(option, _languageName, value);

            // May be updating an internally-defined public option stored in solution snapshots:
            _legacyGlobalOptions.UpdateRegisteredWorkspaces();
        }

        private protected T GetOption<T>(Option2<T> option)
            => _legacyGlobalOptions.GlobalOptions.GetOption(option);

        private protected void SetOption<T>(Option2<T> option, T value)
        {
            _legacyGlobalOptions.GlobalOptions.SetGlobalOption(option, value);

            // May be updating an internally-defined public option stored in solution snapshots:
            _legacyGlobalOptions.UpdateRegisteredWorkspaces();
        }

        private protected int GetBooleanOption(PerLanguageOption2<bool?> key)
            => NullableBooleanToInteger(GetOption(key));

        private protected void SetBooleanOption(PerLanguageOption2<bool?> key, int value)
            => SetOption(key, IntegerToNullableBoolean(value));

        private protected int GetBooleanOption(Option2<bool?> key)
            => NullableBooleanToInteger(GetOption(key));

        private protected void SetBooleanOption(Option2<bool?> key, int value)
            => SetOption(key, IntegerToNullableBoolean(value));

        private protected string GetXmlOption<T>(Option2<CodeStyleOption2<T>> option)
            => GetOption(option).ToXElement().ToString();

        private protected string GetXmlOption<T>(PerLanguageOption2<CodeStyleOption2<T>> option)
            => GetOption(option).ToXElement().ToString();

        private protected void SetXmlOption<T>(Option2<CodeStyleOption2<T>> option, string value)
        {
            var convertedValue = CodeStyleOption2<T>.FromXElement(XElement.Parse(value));
            SetOption(option, convertedValue);
        }

        private protected void SetXmlOption<T>(PerLanguageOption2<CodeStyleOption2<T>> option, string value)
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
