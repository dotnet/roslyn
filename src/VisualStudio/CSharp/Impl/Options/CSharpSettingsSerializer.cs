// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Compilers;
using Microsoft.CodeAnalysis.Services.CSharp.Formatting;
using Microsoft.CodeAnalysis.Services.Formatting;
using Microsoft.CodeAnalysis.Services.OptionService;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualStudio.Services;
using Microsoft.CodeAnalysis.VisualStudio.Services.Implementation.Options;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.VisualStudio.CSharp.Options
{
    [ExportOptionSerializer(AllLanguageOptionReferences.TabFeatureName)]
    internal sealed class CSharpSettingsSerializer : AbstractLanguageSettingsSerializer
    {
        [ImportingConstructor]
        public CSharpSettingsSerializer(SVsServiceProvider serviceProvider)
            : base(Guids.CSharpLanguageServiceId, serviceProvider)
        {
        }

        public override bool TryFetch(OptionInfo option, string language, out object value)
        {
            if (language != LanguageNames.CSharp)
            {
                value = null;
                return false;
            }

            if (option.Key.Equals(AllLanguageOptionReferences.UseTab))
            {
                value = languageSetting.fInsertTabs != 0;
                return true;
            }

            if (option.Key == AllLanguageOptionReferences.TabSize)
            {
                value = (int)languageSetting.uTabSize;
                return true;
            }

            if (option.Key == AllLanguageOptionReferences.IndentationSize)
            {
                value = (int)languageSetting.uIndentSize;
                return true;
            }

            if (option.Key == AllLanguageOptionReferences.DebugMode)
            {
                value = option.DefaultValue;
                return true;
            }

            value = null;
            return false;
        }

        public override bool TryPersist(OptionInfo option, string language, object value)
        {
            if (language != LanguageNames.CSharp)
            {
                value = null;
                return false;
            }

            if (option.Key == AllLanguageOptionReferences.UseTab)
            {
                languageSetting.fInsertTabs = (uint)((bool)value ? 1 : 0);
                SetUserPreferences();
                return true;
            }

            if (option.Key == AllLanguageOptionReferences.TabSize)
            {
                languageSetting.uTabSize = (uint)value;
                SetUserPreferences();
                return true;
            }

            if (option.Key == AllLanguageOptionReferences.IndentationSize)
            {
                languageSetting.uIndentSize = (uint)value;
                SetUserPreferences();
                return true;
            }

            if (option.Key == AllLanguageOptionReferences.DebugMode)
            {
                // Currently we don't store it.
                return true;
            }

            return false;
        }
    }
}
