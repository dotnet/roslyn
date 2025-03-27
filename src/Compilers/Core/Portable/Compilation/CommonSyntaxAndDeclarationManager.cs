// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
