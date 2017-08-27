﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    internal static class NoCompilationContentTypeDefinitions
    {
        /// <summary>
        /// Definition of the primary C# content type.
        /// </summary>
        [System.ComponentModel.Composition.Export]
        [Name(NoCompilationConstants.LanguageName)]
        [BaseDefinition(ContentTypeNames.RoslynContentType)]
        public static readonly ContentTypeDefinition NoCompilationContentTypeDefinition;
    }
}
