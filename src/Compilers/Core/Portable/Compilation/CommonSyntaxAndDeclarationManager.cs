// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal abstract class CommonSyntaxAndDeclarationManager
    {
        internal readonly ImmutableArray<SyntaxTree> ExternalSyntaxTrees;
        internal readonly string ScriptClassName;
        internal readonly SourceReferenceResolver Resolver;
        internal readonly CommonMessageProvider MessageProvider;
        internal readonly bool IsSubmission;

        private ImmutableDictionary<SyntaxTree, ImmutableArray<Diagnostic>> _syntaxTreeLoadDirectiveMap;
        // This ImmutableDictionary will use default (case-sensitive) comparison
        // for its keys.  It is the responsibility of the SourceReferenceResolver
        // to normalize the paths it resolves in a way that is appropriate for the
        // platforms that the host supports.
        private ImmutableDictionary<string, SyntaxTree> _resolvedFilePathSyntaxTreeMap;

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
