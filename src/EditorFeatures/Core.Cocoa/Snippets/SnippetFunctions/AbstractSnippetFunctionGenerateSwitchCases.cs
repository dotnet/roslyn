// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets.SnippetFunctions
{
    internal abstract class AbstractSnippetFunctionGenerateSwitchCases : AbstractSnippetFunction
    {
        protected readonly string CaseGenerationLocationField;
        protected readonly string SwitchExpressionField;

        protected abstract string CaseFormat { get; }
        protected abstract string DefaultCase { get; }

        public AbstractSnippetFunctionGenerateSwitchCases(AbstractSnippetExpansionClient snippetExpansionClient, ITextBuffer subjectBuffer, string caseGenerationLocationField, string switchExpressionField)
            : base(snippetExpansionClient, subjectBuffer)
        {
            this.CaseGenerationLocationField = caseGenerationLocationField;
            this.SwitchExpressionField = (switchExpressionField.Length >= 2 && switchExpressionField[0] == '$' && switchExpressionField[^1] == '$')
                ? switchExpressionField[1..^1] : switchExpressionField;
        }

        protected abstract bool TryGetEnumTypeSymbol(CancellationToken cancellationToken, [NotNullWhen(returnValue: true)] out ITypeSymbol? typeSymbol);
        protected abstract bool TryGetSimplifiedTypeNameInCaseContext(Document document, string fullyQualifiedTypeName, string firstEnumMemberName, int startPosition, int endPosition, CancellationToken cancellationToken, out string simplifiedTypeName);

        protected override bool FieldChanged(string field)
        {
            return SwitchExpressionField == field;
        }

        protected override void GetCurrentValue(CancellationToken cancellationToken, out string value, out bool hasCurrentValue)
        {
            // If the switch expression is invalid, still show the default case
            value = DefaultCase;
            hasCurrentValue = true;
            if (!TryGetEnumTypeSymbol(cancellationToken, out var typeSymbol) || typeSymbol.TypeKind != TypeKind.Enum)
            {
                return;
            }

            var enumFields = typeSymbol.GetMembers().Where(m => m.Kind == SymbolKind.Field && m.IsStatic);

            // Find and use the most simplified legal version of the enum type name in this context
            var simplifiedTypeName = string.Empty;
            if (!enumFields.Any() ||
                !TryGetSimplifiedTypeName(
                    typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    enumFields.First().Name,
                    cancellationToken,
                    out simplifiedTypeName))
            {
                return;
            }

            var casesBuilder = new StringBuilder();
            foreach (var member in enumFields)
            {
                casesBuilder.AppendFormat(CaseFormat, simplifiedTypeName, member.Name);
            }

            casesBuilder.Append(DefaultCase);
            value = casesBuilder.ToString();
        }

        private bool TryGetSimplifiedTypeName(string fullyQualifiedTypeName, string firstEnumMemberName, CancellationToken cancellationToken, out string simplifiedTypeName)
        {
            simplifiedTypeName = string.Empty;
            if (!TryGetDocument(out var document))
            {
                return false;
            }

            Contract.ThrowIfNull(_snippetExpansionClient.ExpansionSession);

            var surfaceBufferFieldSpan = _snippetExpansionClient.ExpansionSession.GetFieldSpan(CaseGenerationLocationField);

            if (!_snippetExpansionClient.TryGetSubjectBufferSpan(surfaceBufferFieldSpan, out var subjectBufferFieldSpan))
            {
                return false;
            }

            return TryGetSimplifiedTypeNameInCaseContext(document, fullyQualifiedTypeName, firstEnumMemberName, subjectBufferFieldSpan.Start.Position, subjectBufferFieldSpan.End.Position, cancellationToken, out simplifiedTypeName);
        }
    }
}
