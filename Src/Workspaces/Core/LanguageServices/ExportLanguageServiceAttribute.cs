// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    /// <summary>
    /// Specifies the exact type of the service exported by the ILanguageService.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportLanguageServiceAttribute : ExportAttribute
    {
        public string ServiceTypeAssemblyQualifiedName { get; private set; }
        public string Language { get; private set; }
        public string WorkspaceKind { get; private set; }

        public ExportLanguageServiceAttribute(Type type, string language, string workspaceKind = Microsoft.CodeAnalysis.WorkspaceKind.Any)
            : base(typeof(ILanguageService))
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (language == null)
            {
                throw new ArgumentNullException("language");
            }

            this.ServiceTypeAssemblyQualifiedName = type.AssemblyQualifiedName;
            this.Language = language;
            this.WorkspaceKind = workspaceKind;
        }
    }
}