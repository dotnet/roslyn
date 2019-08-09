// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using static System.String;

namespace CSharpSyntaxGenerator.Grammar
{
    internal class GrammarGenerator
    {
        private readonly ImmutableDictionary<string, TreeType> _nameToElement;

        public GrammarGenerator(Tree tree)
        {
            // Syntax refers to a special pseudo-element 'Modifier'.  Just synthesize that since
            // it's useful in the g4 grammar.
            tree.Types.Add(new Node
            {
                Name = "Modifier",
                Children =
                {
                    new Field
                    {
                        Type = SyntaxToken,
                        Kinds = Modifiers.Select(k => new Kind { Name = k.ToString() }).ToList()
                    }
                }
            });

            _nameToElement = tree.Types.Where(c => c is AbstractNode || c is Node).ToImmutableDictionary(n => n.Name);
        }

        public string Run()
        {
            var nameToProductions = _nameToElement.Values.ToDictionary(n => n.Name, _ => new List<Production>());

            // Synthesize this so we have a special node that can act as the parent production for
            // all structured trivia rules.
            nameToProductions.Add("StructuredTriviaSyntax", new List<Production>());

            foreach (var node in _nameToElement.Values)
            {
                if (node.Base is string nodeBase && nameToProductions.TryGetValue(nodeBase, out var baseProductions))
                {
                    // If this node has a base-type, then have the base-type point to this node as a
                    // valid production for itself.
                    baseProductions.Add(RuleReference(node.Name));
                }

                if (node is Node)
                {
                    var children = node.Children;
                    if (children.Count == 0)
                    {
                        throw new InvalidOperationException(node.Name + " had no children");
                    }

                    // Some productions can be split into multiple productions that will read
                    // better.  Look for those patterns and break this up.  Then convert each
                    // production that we split out into the actual rule to emit into the grammer.
                    var splitProductions = from current1 in SplitTokenChoice(children)
                                           from current2 in SplitQuotes(current1)
                                           from current3 in SplitPairedOptionalBraces(current2)
                                           select current3;

                    foreach (var split in splitProductions)
                    {
                        // Otherwise, process all the children, making a production out of them for
                        // this node.
                        nameToProductions[node.Name].Add(ProcessChildren(split, delim: " "));
                    }
                }
            }

            // The grammar will bottom out with certain lexical productions.  Just emit a few empty
            // productions in the grammar file indicating what's going on, and making it so that the
            // g4 file is considered legal (i.e. no rule references names of rules that don't exist).

            var lexicalProductions = new List<Production> { new Production("/* see lexical specification */") };
            nameToProductions.Add("Token", lexicalProductions);

            foreach (var kind in LexicalTokens)
            {
                nameToProductions.Add(kind.ToString(), lexicalProductions);
            }

            return GenerateResult(nameToProductions);
        }

        private IEnumerable<List<TreeTypeChild>> SplitTokenChoice(List<TreeTypeChild> children)
        {
            // If a node has many token children, emit it as:
            //
            //  predefined_type
            //      : 'int'
            //      | 'bool'
            //      | etc.
            //
            // Not:
            //
            //  predefined_type
            //      : ('int' | 'bool' | etc... )

            if (children.Count == 1 && children[0] is Field field && field.IsToken)
            {
                return field.Kinds.Select(k => new List<TreeTypeChild>
                {
                    new Field { Type = "SyntaxToken", Kinds = new List<Kind> { k } }
                });
            }

            return new[] { children };
        }

        private IEnumerable<List<TreeTypeChild>> SplitQuotes(List<TreeTypeChild> children)
        {
            // look for rules of the form: ('"' | ''') a ('"' | ''')
            // and convert to            : ('"' a '"') | (''' a ''')

            var fields = children.OfType<Field>();
            var matches = fields.Where(
                f => f.IsToken &&
                     f.Kinds.Count >= 2 &&
                     f.Kinds.Select(GetTokenKind).Contains(SyntaxKind.DoubleQuoteToken)).ToList();
            if (matches.Count < 2)
            {
                yield return children;
                yield break;
            }

            var firstMatchKinds = matches[0].Kinds;
            if (matches.All(m => m.Kinds.SequenceEqual(firstMatchKinds)))
            {
                // found a match.  Split it into separate productions.
                foreach (var kind in firstMatchKinds)
                {
                    // Take the existing production and swap the instance of `('"' | ''')`
                    // with just `'"'`  or  `'''`.
                    yield return children.Select(n => n is Field field && matches.Contains(field)
                        ? new Field { Type = field.Type, Optional = field.Optional, Kinds = new List<Kind> { kind } }
                        : n).ToList();
                }
            }
        }

        private IEnumerable<List<TreeTypeChild>> SplitPairedOptionalBraces(List<TreeTypeChild> children)
        {
            // look for rules of the form: '('? a ')'?
            // and convert to            : a | '(' a ')'

            var fields = children.OfType<Field>();
            var matches = fields.Where(
                f => f.IsToken &&
                     f.Optional != null &&
                     f.Kinds.Select(GetTokenKind).Any(OpenCloseTokens.Contains)).ToList();
            if (matches.Count < 2)
            {
                yield return children;
                yield break;
            }

            // First, return the production where the paired braces are removed.
            yield return children.Where(n => !matches.Contains(n)).ToList();

            // Then return the production where the paired braces are there, but are no longer
            // optional.
            yield return children.Select(n => n is Field field && matches.Contains(field)
                ? new Field { Type = field.Type, Kinds = field.Kinds, Optional = null }
                : n).ToList();
        }

        private string GenerateResult(Dictionary<string, List<Production>> nameToProductions)
        {
            // Keep track of the rules we've emitted.  Once we've emitted a rule once, no need to do
            // it again, even if it's referenced by another rule.
            var seen = new HashSet<string>();
            var normalizedRules = new List<(string name, ImmutableArray<string> productions)>();

            // Process each major section.
            foreach (var section in s_majorSections)
            {
                AddNormalizedRules(section);
            }

            // Now go through the entire list and print out any other rules not hit transitively
            // from those sections.
            foreach (var name in nameToProductions.Keys.OrderBy(a => a, StringComparer.Ordinal))
            {
                AddNormalizedRules(name);
            }

            return
@"// <auto-generated />
grammar csharp;" + Join("", normalizedRules.Select(t => Generate(t.name, t.productions)));

            void AddNormalizedRules(string name)
            {
                // Only consider the rule if it's the first time we're seeing it.
                if (seen.Add(name))
                {
                    // Order the productions alphabetically for consistency and to keep us independent
                    // from whatever ordering changes happen in Syntax.xml.
                    var sorted = nameToProductions[name].OrderBy(v => v.Text, StringComparer.Ordinal);

                    normalizedRules.Add((Normalize(name), sorted.Select(s => s.Text).ToImmutableArray()));

                    // Now proceed in depth-first fashion through the rules the productions of this rule
                    // reference.  This helps keep related rules of these productions close by.
                    //
                    // Note: if we hit a major-section node, we don't recurse in.  This keeps us from
                    // travelling too far away, and keeps the major sections relatively cohesive.
                    var references = sorted.SelectMany(t => t.RuleReferences).Where(r => !s_majorSections.Contains(r));
                    foreach (var reference in references)
                    {
                        AddNormalizedRules(reference);
                    }
                }
            }

            static string Generate(string name, ImmutableArray<string> productions)
            {
                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine(name);
                sb.Append("  : ");

                if (productions.Length == 0)
                {
                    throw new InvalidOperationException("Rule didn't have any productions: " + name);
                }

                sb.AppendJoin(Environment.NewLine + "  | ", productions);
                sb.AppendLine();
                sb.Append("  ;");

                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns the g4 production string for this rule based on the children it has. Also
        /// returns all the names of other rules this particular production references.
        /// </summary>
        private Production ProcessChildren(List<TreeTypeChild> children, string delim)
        {
            var result = children.Select(child => child switch
            {
                Choice c => ProcessChoice(c),
                Sequence s => ProcessChildren(s.Children, " ").Parenthesize(),
                _ => ProcessField((Field)child),
            }).Where(p => p.Text.Length > 0);

            return new Production(
                Join(delim, result.Select(t => t.Text)),
                result.SelectMany(t => t.RuleReferences));
        }

        private Production ProcessChoice(Choice choice)
        {
            // Convert `(a? | b?)` to `(a | b)?`
            var allChildrenAreOptional = choice.Children.All(c => c is Field field && field.Optional != null);
            if (allChildrenAreOptional)
            {
                foreach (var field in choice.Children.OfType<Field>())
                {
                    field.Optional = null;
                }
            }

            return ProcessChildren(choice.Children, " | ").Parenthesize().WithSuffix(allChildrenAreOptional ? "?" : "");
        }

        private Production ProcessField(Field field)
            => GetFieldUnderlyingType(field).WithSuffix(field.Optional == "true" ? "?" : "");

        private Production GetFieldUnderlyingType(Field field)
            => field.Type switch
            {
                // 'bool' fields are for the few boolean properties we generate on DirectiveTrivia.
                // They're not relevant to the grammar, so we just return an empty production here
                // which will be filtered out by the caller.
                "bool" => new Production(""),
                "CSharpSyntaxNode" => HandleCSharpSyntaxNodeField(field),
                _ when field.IsToken => HandleSyntaxTokenField(field),
                _ when field.Type.StartsWith("SeparatedSyntaxList") => HandleSeparatedSyntaxListField(field),
                _ when field.Type.StartsWith("SyntaxList") => HandleSyntaxListField(field),
                _ => RuleReference(field.Type),
            };

        private static Production HandleSyntaxTokenField(Field field)
        {
            var production = new Production(field.Kinds.Count == 0
                ? GetTokenText(GetTokenKind(field.Name))
                : Join(" | ", GetTokenKindStrings(field)));
            return field.Kinds.Count > 1 ? production.Parenthesize() : production;
        }

        private Production HandleCSharpSyntaxNodeField(Field field)
            => RuleReference(field.Kinds.Single().Name + Syntax);

        private Production HandleSeparatedSyntaxListField(Field field)
        {
            var production = RuleReference(field.Type[("SeparatedSyntaxList".Length + 1)..^1]);

            var result = production.WithSuffix(" (',' " + production + ")*");
            result = field.AllowTrailingSeparator != null ? result.WithSuffix(" ','?") : result;
            return field.MinCount != null ? result : result.Parenthesize().WithSuffix("?");
        }

        private Production HandleSyntaxListField(Field field)
            => GetSyntaxListUnderlyingType(field).WithSuffix(field.MinCount != null ? "+" : "*");

        private Production GetSyntaxListUnderlyingType(Field field)
            => field.Name switch
            {
                // Specialized token lists that we want the grammar to be more precise about. i.e.
                // we don't want `Commas` to be in the grammar as `token*` (implying that it could
                // be virtually any token.
                "Commas" => new Production("','"),
                "Modifiers" => RuleReference("Modifier"),
                "Tokens" => new Production(Normalize("Token")),
                "TextTokens" => new Production(Normalize("XmlTextLiteralToken")),
                _ => RuleReference(field.Type[("SyntaxList".Length + 1)..^1])
            };

        private static IEnumerable<string> GetTokenKindStrings(Field field)
            => field.Kinds.Select(k => GetTokenText(GetTokenKind(k.Name))).OrderBy(a => a, StringComparer.Ordinal);

        private static string GetTokenText(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.EndOfFileToken:
                    // Emit the special antlr EOF token indicating this production should consume
                    // the entire file.
                    return "EOF";
                case SyntaxKind.EndOfDocumentationCommentToken:
                case SyntaxKind.EndOfDirectiveToken:
                    // Don't emit anything in the production for these.
                    return null;
                case SyntaxKind.OmittedTypeArgumentToken:
                case SyntaxKind.OmittedArraySizeExpressionToken:
                    // Indicate that these productions are explicitly empty.
                    return "/* epsilon */";
            }

            if (LexicalTokens.Contains(kind))
            {
                // Map these token kinds to just a synthesized rule that we state is
                // declared elsewhere.
                return Normalize(kind.ToString());
            }

            var result = SyntaxFacts.GetText(kind);
            if (result == "")
            {
                throw new NotImplementedException("Unexpected SyntaxKind: " + kind);
            }

            return result == "'"
                ? @"'\''"
                : "'" + result + "'";
        }

        private static SyntaxKind GetTokenKind(Kind kind)
            => GetTokenKind(kind.Name);

        private static SyntaxKind GetTokenKind(string tokenName)
        {
            foreach (var field in typeof(SyntaxKind).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.Name == tokenName)
                {
                    return (SyntaxKind)field.GetValue(null);
                }
            }

            // Slight special case.  Syntax.xml references IdentifierTokens as Identifier.
            return tokenName == "Identifier"
                ? SyntaxKind.IdentifierToken
                : throw new NotImplementedException("Could not find SyntaxKind for: " + tokenName);
        }

        private Production RuleReference(string ruleName)
            => _nameToElement.ContainsKey(ruleName)
                ? new Production(Normalize(ruleName), ImmutableArray.Create(ruleName))
                : throw new InvalidOperationException("No rule found with name: " + ruleName);

        /// <summary>
        /// Converts a <c>PascalCased</c> name into <c>snake_cased</c> name.
        /// </summary>
        private static string Normalize(string name)
            => _normalizationRegex.Replace(name.EndsWith(Syntax) ? name[..^Syntax.Length] : name, "_").ToLower();

        private static readonly Regex _normalizationRegex = new Regex(@"
            (?<=[A-Z])(?=[A-Z][a-z]) |
            (?<=[^A-Z])(?=[A-Z]) |
            (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        // Special constants we use in a few places.

        private const string Syntax = "Syntax";
        private const string SyntaxToken = "SyntaxToken";

        private static readonly ImmutableArray<SyntaxKind> LexicalTokens = ImmutableArray.Create(
            SyntaxKind.IdentifierToken,
            SyntaxKind.CharacterLiteralToken,
            SyntaxKind.StringLiteralToken,
            SyntaxKind.NumericLiteralToken,
            SyntaxKind.InterpolatedStringTextToken,
            SyntaxKind.XmlTextLiteralToken);

        private static readonly ImmutableArray<SyntaxKind> OpenCloseTokens = ImmutableArray.Create(
            SyntaxKind.OpenBraceToken,
            SyntaxKind.OpenBracketToken,
            SyntaxKind.OpenParenToken,
            SyntaxKind.CloseBraceToken,
            SyntaxKind.CloseBracketToken,
            SyntaxKind.CloseParenToken);

        private static readonly ImmutableArray<SyntaxKind> Modifiers = ImmutableArray.Create(
            SyntaxKind.AbstractKeyword,
            SyntaxKind.SealedKeyword,
            SyntaxKind.StaticKeyword,
            SyntaxKind.NewKeyword,
            SyntaxKind.PublicKeyword,
            SyntaxKind.ProtectedKeyword,
            SyntaxKind.InternalKeyword,
            SyntaxKind.PrivateKeyword,
            SyntaxKind.ReadOnlyKeyword,
            SyntaxKind.ConstKeyword,
            SyntaxKind.VolatileKeyword,
            SyntaxKind.ExternKeyword,
            SyntaxKind.PartialKeyword,
            SyntaxKind.UnsafeKeyword,
            SyntaxKind.FixedKeyword,
            SyntaxKind.VirtualKeyword,
            SyntaxKind.OverrideKeyword,
            SyntaxKind.AsyncKeyword,
            SyntaxKind.RefKeyword);

        // This is optional, but makes the emitted g4 file a bit nicer.  Basically, we define a few
        // major sections (generally, corresponding to base nodes that have a lot of derived nodes).
        // If we hit these nodes while recursing through another node, we won't just print them out
        // then.  Instead, we'll wait till we're done with the previous nodes, then start emitting
        // these.  This helps organize the final g4 document into reasonable sections.  i.e. you
        // generally see all the member declarations together, and all the statements together and
        // all the expressions together.  Without this, just processing CompilationUnit will cause
        // expressions to print early because of things like:
        // CompilationUnit->AttributeList->AttributeArg->Expression.

        private static readonly ImmutableArray<string> s_majorSections = ImmutableArray.Create(
            "CompilationUnitSyntax",
            "MemberDeclarationSyntax",
            "StatementSyntax",
            "ExpressionSyntax",
            "TypeSyntax",
            "XmlNodeSyntax",
            "StructuredTriviaSyntax");
    }

    internal struct Production
    {
        /// <summary>
        /// The line of text to include in the grammar file.  i.e. everything after
        /// <c>:</c> in <c>: 'extern' 'alias' identifier_token ';'</c>.
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// The names of other rules that are referenced by this rule.  Used purely as an aid to
        /// help order productions when emitting.  In general, we want to keep referenced rules
        /// close to the rule that references them.
        /// </summary>
        public readonly ImmutableArray<string> RuleReferences;

        public Production(string text)
            : this(text, ImmutableArray<string>.Empty)
        {
        }

        public Production(string text, IEnumerable<string> ruleReferences)
        {
            Text = text;
            RuleReferences = ruleReferences.ToImmutableArray();
        }

        public Production WithPrefix(string prefix) => new Production(prefix + this, RuleReferences);
        public Production WithSuffix(string suffix) => new Production(this + suffix, RuleReferences);
        public Production Parenthesize() => WithPrefix("(").WithSuffix(")");
        public override string ToString() => Text;
    }
}

namespace Microsoft.CodeAnalysis
{
    internal static class GreenNode
    {
        internal const int ListKind = 1; // See SyntaxKind.
    }
}
