// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SyntaxDifferencing
{
    internal abstract class SyntaxDifferenceService : ILanguageService
    {
        public abstract string Language { get; }

        public static SyntaxDifferenceService GetService(Document document)
        {
            return GetService(document.Project.Solution.Workspace, document.Project.Language);
        }

        public static SyntaxDifferenceService GetService(Workspace workspace, string language)
        {
            return workspace.Services.GetLanguageServices(language).GetService<SyntaxDifferenceService>();
        }

        public abstract SyntaxMatch ComputeTopLevelMatch(SyntaxNode oldRoot, SyntaxNode newRoot);

        public abstract SyntaxMatch ComputeBodyLevelMatch(SyntaxNode oldBody, SyntaxNode newBody);
    }
}