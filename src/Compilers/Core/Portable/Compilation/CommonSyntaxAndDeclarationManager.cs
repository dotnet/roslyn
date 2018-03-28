// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal abstract class CommonSyntaxAndDeclarationManager
    {
        internal readonly ImmutableArray<SyntaxTree> ExternalSyntaxTrees;
        internal readonly string ScriptClassName;
        internal readonly SourceReferenceResolver Resolver;
        internal readonly CommonMessageProvider MessageProvider;
        internal readonly bool IsSubmission;

        public CommonSyntaxAndDeclarationManager(
            ImmutableArray<SyntaxTree> externalSyntaxTrees,
            string scriptClassName,
            SourceReferenceResolver resolver,
            CommonMessageProvider messageProvider,
            bool isSubmission)
        {
            this.ExternalSyntaxTrees = externalSyntaxTrees;
            this.ScriptClassName = scriptClassName ?? "";
            this.Resolver = resolver;
            this.MessageProvider = messageProvider;
            this.IsSubmission = isSubmission;
        }
    }
}
