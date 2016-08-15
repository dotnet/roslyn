// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.SolutionExplorer;
using System;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    /// <summary>
    /// The attribute used for adding localized display names to properties
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class BrowseObjectDisplayNameAttribute : DisplayNameAttribute
    {
        private string _key;
        private bool _initialized;

        public BrowseObjectDisplayNameAttribute(string key)
        {
            _key = key;
        }

        public override string DisplayName
        {
            get
            {
                if (!_initialized)
                {
                    base.DisplayNameValue = SolutionExplorerShim.ResourceManager.GetString(_key, CultureInfo.CurrentUICulture);
                    _initialized = true;
                }

                return base.DisplayName;
            }
        }
    }
}
