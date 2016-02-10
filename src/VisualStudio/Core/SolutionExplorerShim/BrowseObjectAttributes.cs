// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.SolutionExplorer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    /// <summary>
    /// The attribute used for adding localized display names to properties
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class BrowseObjectDisplayNameAttribute : DisplayNameAttribute
    {
        private string m_key;
        private bool m_initialized;

        public BrowseObjectDisplayNameAttribute(string key)
        {
            m_key = key;
        }

        public override string DisplayName
        {
            get
            {
                if (!m_initialized)
                {
                    base.DisplayNameValue = SolutionExplorerShim.ResourceManager.GetString(m_key, CultureInfo.CurrentUICulture);
                    m_initialized = true;
                }

                return base.DisplayName;
            }
        }

    }
}
