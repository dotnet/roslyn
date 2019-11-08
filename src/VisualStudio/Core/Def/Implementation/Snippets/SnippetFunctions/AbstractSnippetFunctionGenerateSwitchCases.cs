// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets.SnippetFunctions
{
    internal abstract class AbstractSnippetFunctionGenerateSwitchCases : AbstractSnippetFunction
    {
        protected readonly string CaseGenerationLocationField;
        protected readonly string SwitchExpressionField;

        protected abstract string CaseFormat { get; }
        protected abstract string DefaultCase { get; }

        public AbstractSnippetFunctionGenerateSwitchCases(AbstractSnippetExpansionClient snippetExpansionClient, ITextView textView, ITextBuffer subjectBuffer, string caseGenerationLocationField, string switchExpressionField)
            : base(snippetExpansionClient, textView, subjectBuffer)
        {
            this.CaseGenerationLocationField = caseGenerationLocationField;
            this.SwitchExpressionField = (switchExpressionField.Length >= 2 && switchExpressionField[0] == '$' && switchExpressionField[switchExpressionField.Length - 1] == '$')
                ? switchExpressionField.Substring(1, switchExpressionField.Length - 2) : switchExpressionField;
        }

        protected abstract bool TryGetEnumTypeSymbol(CancellationToken cancellationToken, out ITypeSymbol typeSymbol);
        protected abstract bool TryGetSimplifiedTypeNameInCaseContext(Document document, string fullyQualifiedTypeName, string firstEnumMemberName, int startPosition, int endPosition, CancellationToken cancellationToken, out string simplifiedTypeName);

        protected override int FieldChanged(string field, out int requeryFunction)
        {
            requeryFunction = (SwitchExpressionField == field) ? 1 : 0;
            return VSConstants.S_OK;
        }

        protected override int GetCurrentValue(CancellationToken cancellationToken, out string value, out int hasCurrentValue)
        {
            // If the switch expression is invalid, still show the default case
            value = DefaultCase;
            hasCurrentValue = 1;
            if (!TryGetEnumTypeSymbol(cancellationToken, out var typeSymbol) || typeSymbol.TypeKind != TypeKind.Enum)
            {
                return VSConstants.S_OK;
            }

            var enumFields = typeSymbol.GetMembers().Where(m => m is { Kind: SymbolKind.Field, IsStatic: true });

            // Find and use the most simplified legal version of the enum type name in this context
            var simplifiedTypeName = string.Empty;
            if (!enumFields.Any() ||
                !TryGetSimplifiedTypeName(
                    typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    enumFields.First().Name,
                    cancellationToken,
                    out simplifiedTypeName))
            {
                return VSConstants.S_OK;
            }

            var casesBuilder = new StringBuilder();
            foreach (var member in enumFields)
            {
                casesBuilder.AppendFormat(CaseFormat, simplifiedTypeName, member.Name);
            }

            casesBuilder.Append(DefaultCase);
            value = casesBuilder.ToString();

            return VSConstants.S_OK;
        }

        private bool TryGetSimplifiedTypeName(string fullyQualifiedTypeName, string firstEnumMemberName, CancellationToken cancellationToken, out string simplifiedTypeName)
        {
            simplifiedTypeName = string.Empty;
            if (!TryGetDocument(out var document))
            {
                return false;
            }

            // Add the first switch case using the fully qualified type name, then simplify it in
            // that context
            var surfaceBufferFieldSpan = new VsTextSpan[1];
            if (snippetExpansionClient.ExpansionSession.GetFieldSpan(CaseGenerationLocationField, surfaceBufferFieldSpan) != VSConstants.S_OK)
            {
                return false;
            }

            if (!snippetExpansionClient.TryGetSubjectBufferSpan(surfaceBufferFieldSpan[0], out var subjectBufferFieldSpan))
            {
                return false;
            }

            return TryGetSimplifiedTypeNameInCaseContext(document, fullyQualifiedTypeName, firstEnumMemberName, subjectBufferFieldSpan.Start.Position, subjectBufferFieldSpan.End.Position, cancellationToken, out simplifiedTypeName);
        }
    }
}
