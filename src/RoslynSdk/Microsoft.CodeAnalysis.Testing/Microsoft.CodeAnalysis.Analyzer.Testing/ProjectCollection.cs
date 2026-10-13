// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Testing
{
    public class ProjectCollection : Dictionary<string, ProjectState>
    {
        private readonly string _defaultLanguage;
        private readonly string _defaultExtension;

        public ProjectCollection(string defaultLanguage, string defaultExtension)
        {
            _defaultLanguage = defaultLanguage;
            _defaultExtension = defaultExtension;
        }

        public new ProjectState this[string projectName]
        {
            get
            {
                if (TryGetValue(projectName, out var project))
                {
                    return project;
                }

                return this[projectName, _defaultLanguage];
            }
        }

        public ProjectState this[string projectName, string language]
        {
            get
            {
                string extension;
                if (language == _defaultLanguage)
                {
                    extension = _defaultExtension;
                }
                else
                {
                    extension = language switch
                    {
                        LanguageNames.CSharp => "cs",
                        LanguageNames.VisualBasic => "vb",
                        _ => throw new ArgumentOutOfRangeException(nameof(language)),
                    };
                }

                var project = this.GetOrAdd(projectName, () => new ProjectState(projectName, language, $"/{projectName}/Test", extension));
                if (project.Language != language)
                {
                    throw new InvalidOperationException();
                }

                return project;
            }
        }
    }
}
