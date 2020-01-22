// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptDocumentationCommentWrapper
    {
        private readonly DocumentationComment _underlyingObject;

        public VSTypeScriptDocumentationCommentWrapper(DocumentationComment underlyingObject)
        {
            _underlyingObject = underlyingObject;
        }

        public static VSTypeScriptDocumentationCommentWrapper FromXmlFragment(string xml)
            => new VSTypeScriptDocumentationCommentWrapper(DocumentationComment.FromXmlFragment(xml));

        public bool IsDefault
            => _underlyingObject == null;

        public string? SummaryTextOpt
            => _underlyingObject?.SummaryText;

        public string? GetParameterTextOpt(string parameterName)
            => _underlyingObject?.GetParameterText(parameterName);

    }
}
