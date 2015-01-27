// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation
{
    internal class Parameter : IParameter
    {
        public string Documentation { get; internal set; }
        public string Name { get; internal set; }
        public Span Locus { get; internal set; }
        public Span PrettyPrintedLocus { get; internal set; }
        public ISignature Signature { get; internal set; }

        public Parameter(
            Signature signature,
            SignatureHelpParameter parameter,
            string content,
            int index,
            int prettyPrintedIndex)
        {
            this.Signature = signature;
            this.Name = parameter.Name;
            this.Documentation = parameter.Documentation.GetFullText();

            this.Locus = new Span(index, content.Length);
            this.PrettyPrintedLocus = new Span(prettyPrintedIndex, content.Length);
        }
    }
}
