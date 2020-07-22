// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Classification
{
    [ExportLspMethod(LSP.SemanticTokensMethods.TextDocumentSemanticTokensName), Shared]
    internal class ClassificationHandler : AbstractRequestHandler<LSP.SemanticTokensParams, SemanticTokens>
    {
        private IProgress<SumType<SemanticTokens, SemanticTokensEdits>>? _progress;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ClassificationHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<SemanticTokens> HandleRequestAsync(
            SemanticTokensParams request,
            ClientCapabilities clientCapabilities,
            string? clientName,
            CancellationToken cancellationToken)
        {
            if (request.TextDocument == null)
            {
                return new SemanticTokens();
            }

            var document = SolutionProvider.GetDocument(request.TextDocument, clientName);
            if (document == null)
            {
                return new SemanticTokens();
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return new SemanticTokens();
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (text == null)
            {
                return new SemanticTokens();
            }

            var classifiedSpans = await Classifier.GetClassifiedSpansAsync(document, root.FullSpan, cancellationToken).ConfigureAwait(false);
            if (classifiedSpans == null)
            {
                return new SemanticTokens();
            }

            var groupedSpans = classifiedSpans.GroupBy(s => s.TextSpan);

            var lastLineNumber = 0;
            var lastStartCharacter = 0;

            if (request.PartialResultToken != null)
            {
                _progress = request.PartialResultToken;
                var workQueue = new AsyncBatchingWorkQueue<int>(
                    TimeSpan.FromMilliseconds(500), ReportTokensAsync, cancellationToken);

                ComputeTokensStreaming(ref lastLineNumber, ref lastStartCharacter, workQueue, groupedSpans, text.Lines);
                return new SemanticTokens();
            }
            else
            {
                var tokens = ComputeTokensNonStreaming(ref lastLineNumber, ref lastStartCharacter, groupedSpans, text.Lines);
                return new SemanticTokens { Data = tokens };
            }
        }

        private static void ComputeTokensStreaming(
            ref int lastLineNumber,
            ref int lastStartCharacter,
            AsyncBatchingWorkQueue<int> workQueue,
            IEnumerable<IGrouping<TextSpan, ClassifiedSpan>> groupedSpans,
            TextLineCollection lines)
        {
            foreach (var span in groupedSpans)
            {
                using var _ = ArrayBuilder<int>.GetInstance(out var data);
                ComputeNextToken(data, lines, ref lastLineNumber, ref lastStartCharacter, span);
                workQueue.AddWork(data.ToArray());
            }
        }

        private Task ReportTokensAsync(ImmutableArray<int> tokensToReport, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_progress);

            var semanticTokens = new SemanticTokens { Data = tokensToReport.ToArray() };
            _progress.Report(semanticTokens);
            return Task.CompletedTask;
        }

        private static int[] ComputeTokensNonStreaming(
            ref int lastLineNumber,
            ref int lastStartCharacter,
            IEnumerable<IGrouping<TextSpan, ClassifiedSpan>> groupedSpans,
            TextLineCollection lines)
        {
            using var _ = ArrayBuilder<int>.GetInstance(out var data);
            foreach (var span in groupedSpans)
            {
                ComputeNextToken(data, lines, ref lastLineNumber, ref lastStartCharacter, span);
            }

            return data.ToArray();
        }

        private static void ComputeNextToken(
            ArrayBuilder<int> data,
            TextLineCollection lines,
            ref int lastLineNumber,
            ref int lastStartCharacter,
            IGrouping<TextSpan, ClassifiedSpan> span)
        {
            var textSpan = span.Key;
            var linePosition = lines.GetLinePositionSpan(textSpan).Start;
            var lineNumber = linePosition.Line;
            var startCharacter = linePosition.Character;

            // 1. Token line number, relative to the previous token
            var deltaLine = lineNumber - lastLineNumber;
            data.Add(deltaLine);

            // 2. Token start character, relative to the previous token
            // (Relative to 0 or the previous token’s start if they're on the same line)
            if (lastLineNumber == lineNumber)
            {
                data.Add(startCharacter - lastStartCharacter);
            }
            else
            {
                data.Add(startCharacter);
            }

            // 3. Token length
            data.Add(textSpan.Length);

            var additiveResults = span.Where(s => ClassificationTypeNames.AdditiveTypeNames.Contains(s.ClassificationType));

            // 4. Token type - looked up in SemanticTokensLegend.tokenTypes.
            var tokenTypeClassifiedSpan = span.Except(additiveResults).Single();
            if (s_typeMap.TryGetValue(tokenTypeClassifiedSpan.ClassificationType, out var tokenType))
            {
                var index = TokenTypes.IndexOf(t => t == tokenType);
                if (index == -1)
                {
                    throw new ArgumentException($"Token type {tokenType} is not recognized.");
                }

                data.Add(index);
            }
            else
            {
                throw new NotSupportedException($"Classification type {tokenTypeClassifiedSpan.ClassificationType} is unsupported.");
            }

            // 5. Token modifiers - each set bit will be looked up in SemanticTokensLegend.tokenModifiers
            var modifiers = 0;
            foreach (var currentModifier in additiveResults)
            {
                if (s_modifierMap.TryGetValue(currentModifier.ClassificationType, out var modifier))
                {
                    var index = TokenModifiers.IndexOf(t => t == modifier);
                    if (index == -1)
                    {
                        throw new ArgumentException($"Token type {modifier} is not recognized.");
                    }

                    modifiers |= index + 1;
                }
                else
                {
                    throw new NotSupportedException($"Classification type {currentModifier.ClassificationType} is unsupported.");
                }
            }

            data.Add(modifiers);

            lastLineNumber = lineNumber;
            lastStartCharacter = startCharacter;
        }

        internal static readonly string[] TokenTypes =
            new string[]
            {
                SemanticTokenTypes.Class,
                SemanticTokenTypes.Comment,
                SemanticTokenTypes.Enum,
                SemanticTokenTypes.EnumMember,
                SemanticTokenTypes.Event,
                SemanticTokenTypes.Function,
                SemanticTokenTypes.Interface,
                SemanticTokenTypes.Keyword,
                SemanticTokenTypes.Macro,
                SemanticTokenTypes.Member,
                SemanticTokenTypes.Modifier,
                SemanticTokenTypes.Namespace,
                SemanticTokenTypes.Number,
                SemanticTokenTypes.Operator,
                SemanticTokenTypes.Parameter,
                SemanticTokenTypes.Property,
                SemanticTokenTypes.Regexp,
                SemanticTokenTypes.String,
                SemanticTokenTypes.Struct,
                SemanticTokenTypes.Type,
                SemanticTokenTypes.TypeParameter,
                SemanticTokenTypes.Variable
            };

        internal static readonly string[] TokenModifiers =
            new string[]
            {
                SemanticTokenModifiers.Static
            };

        private static readonly Dictionary<string, string> s_typeMap =
            new Dictionary<string, string>
            {
                [ClassificationTypeNames.ClassName] = SemanticTokenTypes.Class,
                [ClassificationTypeNames.Comment] = SemanticTokenTypes.Comment,
                [ClassificationTypeNames.ConstantName] = SemanticTokenTypes.Variable, // TO-DO: Change to custom type
                [ClassificationTypeNames.ControlKeyword] = SemanticTokenTypes.Keyword, // TO-DO: Change to custom type
                [ClassificationTypeNames.DelegateName] = SemanticTokenTypes.Member, // TO-DO: Change to custom type
                [ClassificationTypeNames.EnumMemberName] = SemanticTokenTypes.EnumMember,
                [ClassificationTypeNames.EnumName] = SemanticTokenTypes.Enum,
                [ClassificationTypeNames.EventName] = SemanticTokenTypes.Event,
                [ClassificationTypeNames.ExcludedCode] = SemanticTokenTypes.String, // TO-DO: Change to custom type
                [ClassificationTypeNames.ExtensionMethodName] = SemanticTokenTypes.Member, // TO-DO: Change to custom type
                [ClassificationTypeNames.FieldName] = SemanticTokenTypes.Property, // TO-DO: Change to custom type
                [ClassificationTypeNames.Identifier] = SemanticTokenTypes.Variable, // TO-DO: Change to custom type
                [ClassificationTypeNames.InterfaceName] = SemanticTokenTypes.Interface,
                [ClassificationTypeNames.Keyword] = SemanticTokenTypes.Keyword,
                [ClassificationTypeNames.LabelName] = SemanticTokenTypes.Variable, // TO-DO: Change to custom type
                [ClassificationTypeNames.LocalName] = SemanticTokenTypes.Member, // TO-DO: Change to custom type
                [ClassificationTypeNames.MethodName] = SemanticTokenTypes.Member, // TO-DO: Change to custom type ?
                [ClassificationTypeNames.ModuleName] = SemanticTokenTypes.Member, // TO-DO: Change to custom type
                [ClassificationTypeNames.NamespaceName] = SemanticTokenTypes.Namespace,
                [ClassificationTypeNames.NumericLiteral] = SemanticTokenTypes.Number,
                [ClassificationTypeNames.Operator] = SemanticTokenTypes.Operator,
                [ClassificationTypeNames.OperatorOverloaded] = SemanticTokenTypes.Operator, // TO-DO: Change to custom type
                [ClassificationTypeNames.ParameterName] = SemanticTokenTypes.Parameter,
                [ClassificationTypeNames.PreprocessorKeyword] = SemanticTokenTypes.Keyword, // TO-DO: Change to custom type
                [ClassificationTypeNames.PreprocessorText] = SemanticTokenTypes.String, // TO-DO: Change to custom type
                [ClassificationTypeNames.PropertyName] = SemanticTokenTypes.Property,
                [ClassificationTypeNames.Punctuation] = SemanticTokenTypes.Operator, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexAlternation] = SemanticTokenTypes.Regexp, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexAnchor] = SemanticTokenTypes.Regexp, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexCharacterClass] = SemanticTokenTypes.Regexp, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexComment] = SemanticTokenTypes.Regexp, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexGrouping] = SemanticTokenTypes.Regexp, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexOtherEscape] = SemanticTokenTypes.Regexp, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexQuantifier] = SemanticTokenTypes.Regexp, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexSelfEscapedCharacter] = SemanticTokenTypes.Regexp, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexText] = SemanticTokenTypes.Regexp, // TO-DO: Change to custom type
                [ClassificationTypeNames.StructName] = SemanticTokenTypes.Struct,
                [ClassificationTypeNames.Text] = SemanticTokenTypes.String, // TO-DO: Change to custom type
                [ClassificationTypeNames.TypeParameterName] = SemanticTokenTypes.TypeParameter,
                [ClassificationTypeNames.VerbatimStringLiteral] = SemanticTokenTypes.String, // TO-DO: Change to custom type
                [ClassificationTypeNames.WhiteSpace] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentAttributeName] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentAttributeQuotes] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentAttributeValue] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentCDataSection] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentComment] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentDelimiter] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentEntityReference] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentName] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentProcessingInstruction] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentText] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralAttributeName] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralAttributeQuotes] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralAttributeValue] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralCDataSection] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralComment] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralDelimiter] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralEmbeddedExpression] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralEntityReference] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralName] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralProcessingInstruction] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralText] = SemanticTokenTypes.Comment, // TO-DO: Change to custom type
            };

        private static readonly Dictionary<string, string> s_modifierMap =
            new Dictionary<string, string>
            {
                [ClassificationTypeNames.StaticSymbol] = SemanticTokenModifiers.Static
            };
    }
}
