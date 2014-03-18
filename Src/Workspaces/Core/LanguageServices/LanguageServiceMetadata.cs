// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal class LanguageServiceMetadata : LanguageMetadata
    {
        public string ServiceTypeAssemblyQualifiedName { get; private set; }
        public string WorkspaceKind { get; private set; }

        public LanguageServiceMetadata(string language, Type serviceType, string workspaceKind) : this(language, serviceType.AssemblyQualifiedName, workspaceKind)
        {
        }

        public LanguageServiceMetadata(IDictionary<string, object> data)
            : base(data)
        {
            this.ServiceTypeAssemblyQualifiedName = (string)data.GetValueOrDefault("ServiceTypeAssemblyQualifiedName");
            this.WorkspaceKind = (string)data.GetValueOrDefault("WorkspaceKind");
        }

        public LanguageServiceMetadata(string language, string serviceTypeAssemblyQualifiedName, string workspaceKind)
            : base(language)
        {
            this.ServiceTypeAssemblyQualifiedName = serviceTypeAssemblyQualifiedName;
            this.WorkspaceKind = workspaceKind;
        }
    }
}