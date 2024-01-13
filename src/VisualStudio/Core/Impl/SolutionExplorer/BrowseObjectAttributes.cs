// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private readonly string m_key;
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
