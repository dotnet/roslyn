// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class LoopBinder : LocalScopeBinder
    {
        private readonly GeneratedLabelSymbol _breakLabel;
        private readonly GeneratedLabelSymbol _continueLabel;
        private readonly string? _labelName;

        protected LoopBinder(Binder enclosing, SyntaxNode loopSyntax)
            : base(enclosing)
        {
            _breakLabel = new GeneratedLabelSymbol("break");
            _continueLabel = new GeneratedLabelSymbol("continue");
            _labelName = loopSyntax.Parent is LabeledStatementSyntax labeled ? labeled.Identifier.ValueText : null;
        }

        internal override GeneratedLabelSymbol? GetBreakLabel(string? labelName)
            => (labelName is null || labelName == _labelName) ? _breakLabel : NextRequired.GetBreakLabel(labelName);

        internal override GeneratedLabelSymbol? GetContinueLabel(string? labelName)
            => (labelName is null || labelName == _labelName) ? _continueLabel : NextRequired.GetContinueLabel(labelName);
    }
}
