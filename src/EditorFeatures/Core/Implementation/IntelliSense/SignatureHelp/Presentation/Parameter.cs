// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation
{
    internal class Parameter : IParameter
    {
        private readonly SignatureHelpParameter _parameter;
        private string _documentation;
        private readonly int _contentLength;
        private readonly int _index;
        private readonly int _prettyPrintedIndex;

        public string Documentation => _documentation ?? (_documentation = _parameter.DocumentationFactory(CancellationToken.None).GetFullText());
        public string Name => _parameter.Name;
        public Span Locus => new Span(_index, _contentLength);
        public Span PrettyPrintedLocus => new Span(_prettyPrintedIndex, _contentLength);
        public ISignature Signature { get; }

        public Parameter(
            Signature signature,
            SignatureHelpParameter parameter,
            string content,
            int index,
            int prettyPrintedIndex)
        {
            _parameter = parameter;
            this.Signature = signature;
            _contentLength = content.Length;
            _index = index;
            _prettyPrintedIndex = prettyPrintedIndex;
        }
    }
}
