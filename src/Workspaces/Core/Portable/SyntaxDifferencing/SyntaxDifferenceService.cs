// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SyntaxDifferencing
{
    public abstract class SyntaxDifferenceService : ILanguageService
    {
        public abstract string Language { get; }

        internal SyntaxDifferenceService()
        {
        }

        public static SyntaxDifferenceService GetService(Document document)
        {
            return GetService(document.Project.Solution.Workspace, document.Project.Language);
        }

        public static SyntaxDifferenceService GetService(Workspace workspace, string language)
        {
            return workspace.Services.GetLanguageServices(language).GetService<SyntaxDifferenceService>();
        }

        /// <summary>
        /// Computes the set of edits that would transform <paramref name="oldRoot"/> into
        /// <paramref name="newRoot"/>.  These roots should be the top level nodes of their
        /// respective documents.
        /// </summary>
        public abstract SyntaxMatch ComputeTopLevelMatch(SyntaxNode oldRoot, SyntaxNode newRoot);

        /// <summary>
        /// Computes the set of edits that would transform <paramref name="oldBody"/> into
        /// <paramref name="newBody"/>.  These roots should correspond to the executable code
        /// bodies of their respective declarations.  For example, a body would often be the
        /// Expression or Block body of a MethodDeclarationSyntax.  However, it could also be
        /// Expression or Block body of a LambdaSyntax.
        /// </summary>
        public abstract SyntaxMatch ComputeBodyLevelMatch(SyntaxNode oldBody, SyntaxNode newBody);
    }
}