// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ReplaceDefaultLiteral
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ReplaceDefaultLiteral), Shared]
    internal sealed class CSharpReplaceDefaultLiteralCodeFixProvider : CodeFixProvider
    {
        private const string CS8313 = nameof(CS8313); // A default literal 'default' is not valid as a case constant. Use another literal (e.g. '0' or 'null') as appropriate. If you intended to write the default label, use 'default:' without 'case'.
        private const string CS8505 = nameof(CS8505); // A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern 'var _'.

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpReplaceDefaultLiteralCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(CS8313, CS8505);

        public override FixAllProvider? GetFixAllProvider()
        {
            // This code fix addresses very specific compiler errors. It's unlikely there will be more than 1 of them at a time.
            return null;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var token = syntaxRoot.FindToken(context.Span.Start);

            if (token.Span == context.Span &&
                token.IsKind(SyntaxKind.DefaultKeyword) &&
                token.Parent is LiteralExpressionSyntax(SyntaxKind.DefaultLiteralExpression) defaultLiteral)
            {
                var semanticModel = await context.Document.GetRequiredSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

                var (newExpression, displayText) = GetReplacementExpressionAndText(
                    context.Document, semanticModel, defaultLiteral, context.CancellationToken);

                if (newExpression != null)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            string.Format(CSharpCodeFixesResources.Use_0, displayText),
                            c => ReplaceAsync(context.Document, context.Span, newExpression, c),
                            nameof(CSharpCodeFixesResources.Use_0)),
                        context.Diagnostics);
                }
            }
        }

        private static async Task<Document> ReplaceAsync(
            Document document, TextSpan span, SyntaxNode newExpression, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var defaultToken = syntaxRoot.FindToken(span.Start);
            var defaultLiteral = (LiteralExpressionSyntax)defaultToken.GetRequiredParent();

            var newRoot = syntaxRoot.ReplaceNode(defaultLiteral, newExpression.WithTriviaFrom(defaultLiteral));
            return document.WithSyntaxRoot(newRoot);
        }

        private static (SyntaxNode newExpression, string displayText) GetReplacementExpressionAndText(
            Document document,
            SemanticModel semanticModel,
            LiteralExpressionSyntax defaultLiteral,
            CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);

            var type = semanticModel.GetTypeInfo(defaultLiteral, cancellationToken).ConvertedType;
            if (type != null && type.TypeKind != TypeKind.Error)
            {
                if (IsFlagsEnum(type, semanticModel.Compilation) &&
                    type.GetMembers("None").FirstOrDefault() is IFieldSymbol field && IsZero(field.ConstantValue))
                {
                    return GenerateMemberAccess("None");
                }
                else if (type.Equals(semanticModel.Compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!)))
                {
                    return GenerateMemberAccess(nameof(CancellationToken.None));
                }
                else if (type.SpecialType is SpecialType.System_IntPtr or SpecialType.System_UIntPtr)
                {
                    return GenerateMemberAccess(nameof(IntPtr.Zero));
                }
                else if (semanticModel.GetConstantValue(defaultLiteral, cancellationToken) is var constant && constant.HasValue)
                {
                    var newLiteral = generator.LiteralExpression(constant.Value);
                    return (newLiteral, newLiteral.ToString());
                }
                else if (!type.ContainsAnonymousType())
                {
                    var defaultExpression = generator.DefaultExpression(type);
                    return (defaultExpression, $"default({type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)})");
                }
            }

            return default;

            (SyntaxNode newExpression, string displayText) GenerateMemberAccess(string memberName)
            {
                var memberAccess = generator.MemberAccessExpression(generator.TypeExpression(type), memberName);
                return (memberAccess, $"{type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}.{memberName}");
            }
        }

        private static bool IsFlagsEnum(ITypeSymbol type, Compilation compilation)
        {
            var flagsAttribute = compilation.GetTypeByMetadataName(typeof(FlagsAttribute).FullName!);
            return type.TypeKind == TypeKind.Enum &&
                   flagsAttribute != null &&
                   type.GetAttributes().Any(static (attribute, flagsAttribute) => flagsAttribute.Equals(attribute.AttributeClass), flagsAttribute);
        }

        private static bool IsZero(object? o)
        {
            switch (o)
            {
                case default(int):
                case default(uint):
                case default(byte):
                case default(sbyte):
                case default(short):
                case default(ushort):
                case default(long):
                case default(ulong):
                    return true;
                default:
                    return false;
            }
        }
    }
}
