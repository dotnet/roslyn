// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
<<<<<<< HEAD
<<<<<<< HEAD
=======
using System.Reflection;
<<<<<<< HEAD
<<<<<<< HEAD
>>>>>>> Simplify
=======
using System.Runtime.InteropServices;
>>>>>>> Simplify
=======
>>>>>>> Simplify
=======
>>>>>>> Simplify
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpSyntaxGenerator.Grammar
{
    internal static class GrammarGenerator
    {
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
        public static string Run(List<TreeType> types)
=======
        private readonly ImmutableDictionary<string, TreeType> _nameToElement;
=======
        private readonly ImmutableArray<TreeType> _nodes;
<<<<<<< HEAD
>>>>>>> Simplify
        private readonly Dictionary<string, List<Production>> _nameToProductions;
=======
>>>>>>> Simplify

        public GrammarGenerator(Tree tree)
=======
        public static string Run(Tree tree)
>>>>>>> Simplify
=======
        public static string Run(List<TreeType> types)
>>>>>>> Simplify
        {
            // Syntax.xml refers to a special pseudo-element 'Modifier'.  Synthesize that for the grammar.
            var modifiers = GetMembers<DeclarationModifiers>()
                .Select(m => m + "Keyword").Where(n => GetSyntaxKind(n) != SyntaxKind.None)
                .Select(n => new Kind { Name = n }).ToList();

<<<<<<< HEAD
            tree.Types.Add(new Node
            {
                Name = "Modifier",
                Children = { new Field { Type = "SyntaxToken", Kinds = modifiers.ToList() } }
            });

<<<<<<< HEAD
            _nodes = tree.Types.Where(t => t.Name != "CSharpSyntaxNode").ToImmutableArray();
        }

        public string Run()
>>>>>>> Simplify
        {
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
            // Syntax.xml refers to a special pseudo-element 'Modifier'.  Synthesize that for the grammar.
            var modifiers = GetMembers<DeclarationModifiers>()
                .Select(m => m + "Keyword").Where(n => GetSyntaxKind(n) != SyntaxKind.None)
                .Select(n => new Kind { Name = n }).ToList();

            types.Add(new Node { Name = "Modifier", Children = { new Field { Type = "SyntaxToken", Kinds = modifiers } } });
=======
            // Synthesize this so we have a special node that can act as the parent production for
            // all structured trivia rules.
            _nameToProductions.Add("StructuredTriviaSyntax", new List<Production>());
>>>>>>> Simplify

<<<<<<< HEAD
            var rules = types.ToDictionary(n => n.Name, _ => new List<Production>());
            foreach (var type in types)
=======
=======
>>>>>>> Simplify
=======
            var nameToProductions = _nodes.ToDictionary(n => n.Name, _ => new List<Production>());

>>>>>>> Simplify
            foreach (var node in _nodes)
>>>>>>> Simplify
=======
            var nameToProductions = tree.Types.ToDictionary(n => n.Name, _ => new List<Production>());

            foreach (var node in tree.Types)
>>>>>>> Simplify
=======
            types.Add(new Node { Name = "Modifier", Children = { new Field { Type = "SyntaxToken", Kinds = modifiers } } });

            var nameToProductions = types.ToDictionary(n => n.Name, _ => new List<Production>());
            foreach (var type in types)
>>>>>>> Simplify
            {
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
                if (type.Base != null && rules.TryGetValue(type.Base, out var productions))
                    productions.Add(RuleReference(type.Name));
=======
=======
                // If this node has a base-type, then have the base-type point to this node as a
                // valid production for itself.
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
>>>>>>> Simplify
                if (node.Base is string nodeBase && _nameToProductions.TryGetValue(nodeBase, out var baseProductions))
<<<<<<< HEAD
                    baseProductions.Add(RuleReference(node.Name));
<<<<<<< HEAD
                }
>>>>>>> Simplify
=======
>>>>>>> Simplify
=======
=======
                if (node.Base is string nodeBase && nameToProductions.TryGetValue(nodeBase, out var baseProductions))
<<<<<<< HEAD
>>>>>>> Simplify
                    baseProductions.Add(CreateProductionForRuleReference(node.Name));
>>>>>>> Simplify

<<<<<<< HEAD
                if (type is Node && type.Children.Count > 0)
                {
<<<<<<< HEAD
                    // Convert rules like `a: (x | y) ...` into:
                    // a: x ...
                    //  | y ...;
                    if (type.Children[0] is Field field && field.Kinds.Count > 0)
                    {
                        foreach (var kind in field.Kinds)
                        {
                            field.Kinds = new List<Kind> { kind };
                            rules[type.Name].Add(HandleChildren(type.Children));
                        }
                    }
<<<<<<< HEAD
                    else
                    {
                        rules[type.Name].Add(HandleChildren(type.Children));
                    }
                }
            }
=======
=======
                if (node.Base is string nodeBase && nodeBase != "CSharpSyntaxNode" && nameToProductions.TryGetValue(nodeBase, out var baseProductions))
>>>>>>> Simplify
                    baseProductions.Add(RuleReference(node.Name));
>>>>>>> Simplify

            // The grammar will bottom out with certain lexical productions. Create rules for these.
            var lexicalRules = rules.Values.SelectMany(ps => ps).SelectMany(p => p.ReferencedRules)
                .Where(r => !rules.TryGetValue(r, out var productions) || productions.Count == 0).ToArray();
            foreach (var name in lexicalRules)
                rules[name] = new List<Production> { new Production("/* see lexical specification */") };

=======
=======
                    if (node.Children.Count == 0)
<<<<<<< HEAD
                        throw new InvalidOperationException(node.Name + " had no children");
>>>>>>> Simplify
=======
                        continue;
>>>>>>> Simplify

=======
                if (node is Node && node.Children.Count > 0)
=======
                if (type.Base is string nodeBase && nodeBase != "CSharpSyntaxNode" && nameToProductions.TryGetValue(nodeBase, out var baseProductions))
=======
=======
                // If this node has a base-type, then have the base-type point to this node as one of its productions
>>>>>>> Simplify
                if (type.Base is string nodeBase && nameToProductions.TryGetValue(nodeBase, out var baseProductions))
>>>>>>> Simplify
                    baseProductions.Add(RuleReference(type.Name));

                if (type is Node && type.Children.Count > 0)
>>>>>>> Simplify
                {
<<<<<<< HEAD
>>>>>>> Simplify
                    // Convert a rule of `a: (x | y | z)` into:
=======
                    // Convert rules like `a: (x | y)` into:
>>>>>>> Simplify
                    // a: x
                    //  | y;
                    if (type.Children.Count == 1 && type.Children[0] is Field field && field.IsToken)
                    {
                        nameToProductions[type.Name].AddRange(field.Kinds.Select(k =>
                            HandleChildren(new List<TreeTypeChild> { new Field { Type = "SyntaxToken", Kinds = { k } } })));
                        continue;
                    }

                    nameToProductions[type.Name].Add(HandleChildren(type.Children));
                }
            }

            // The grammar will bottom out with certain lexical productions. Create rules for these.
            var lexicalRules = nameToProductions.Values.SelectMany(ps => ps).SelectMany(p => p.ReferencedRules)
                                                       .Where(r => !nameToProductions.ContainsKey(r)).ToArray();
            foreach (var name in lexicalRules)
                nameToProductions[name] = new List<Production> { new Production("/* see lexical specification */") };

<<<<<<< HEAD
            var result = @"// <auto-generated />
grammar csharp;";

<<<<<<< HEAD
            void processNodeChildren(TreeType node, List<TreeTypeChild> children)
                => _nameToProductions[node.Name].Add(CreateProductionFromNodeChildren(children, delim: " "));
        }

        private string GenerateResult()
        {
<<<<<<< HEAD
            // Keep track of the rules we've emitted.  Once we've emitted a rule once, no need to do
            // it again, even if it's referenced by another rule.
>>>>>>> Simplify
=======
>>>>>>> Simplify
=======
>>>>>>> Simplify
=======
>>>>>>> Simplify
            var seen = new HashSet<string>();

<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
            // Define a few major sections to help keep the grammar file naturally grouped.
            var majorRules = ImmutableArray.Create(
                "CompilationUnitSyntax", "MemberDeclarationSyntax", "TypeSyntax", "StatementSyntax", "ExpressionSyntax", "XmlNodeSyntax", "StructuredTriviaSyntax");
=======
            // Process each major section.
            foreach (var section in s_majorSections)
                AddNormalizedRules(section);
>>>>>>> Simplify

<<<<<<< HEAD
            var result = "// <auto-generated />" + Environment.NewLine + "grammar csharp;" + Environment.NewLine;
=======
            // Now go through the entire list and print out any other rules not hit transitively
            // from those sections.
            foreach (var name in _nameToProductions.Keys.OrderBy(a => a, StringComparer.Ordinal))
                AddNormalizedRules(name);
<<<<<<< HEAD
            }
>>>>>>> Simplify

            // Handle each major section first and then walk any rules not hit transitively from them.
            foreach (var rule in majorRules.Concat(rules.Keys.OrderBy(a => a)))
                processRule(rule, ref result);
=======
>>>>>>> Simplify
=======
=======
            // Define a few major sections that generally correspond to base nodes that have a lot
            // of derived nodes. If we hit these nodes while recursing through another node, we
            // won't just print them out then. Instead, we'll wait till we're done with the previous
            // nodes, then start emitting these. This helps keep the grammar file naturally grouped.
=======
            // Define a few major sections to help keep the grammar file naturally grouped.
>>>>>>> Simplify
            var majorRules = ImmutableArray.Create(
                "CompilationUnitSyntax", "MemberDeclarationSyntax", "TypeSyntax", "StatementSyntax", "ExpressionSyntax", "XmlNodeSyntax", "StructuredTriviaSyntax");

<<<<<<< HEAD
<<<<<<< HEAD
>>>>>>> Simplify
=======
            var result = @"// <auto-generated />
grammar csharp;";
=======
            var result = "// <auto-generated />" + Environment.NewLine + "grammar csharp;" + Environment.NewLine;
>>>>>>> Simplify

<<<<<<< HEAD
<<<<<<< HEAD
>>>>>>> Simplify
            // Process each major section first, followed by the entire list. That way we process
=======
            // Handle each major section first, followed by the entire list. That way we process
>>>>>>> Simplify
            // any rules not hit transitively from those sections.
            var orderedRules = majorRules.Concat(nameToProductions.Keys.OrderBy(a => a));
            foreach (var rule in orderedRules)
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
                AddNormalizedRules(rule);
>>>>>>> Simplify
=======
                addNormalizedRules(rule);
>>>>>>> Compiler namings

<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
            return result;
=======
            return
@"// <auto-generated />
<<<<<<< HEAD
<<<<<<< HEAD
grammar csharp;" + Concat(normalizedRules.Select(t => GenerateRule(t.name, t.productions)));
>>>>>>> Simplify

            void processRule(string name, ref string result)
            {
                if (name != "CSharpSyntaxNode" && seen.Add(name))
                {
                    // Order the productions to keep us independent from whatever changes happen in Syntax.xml.
                    var sorted = rules[name].OrderBy(v => v);
                    result += Environment.NewLine + RuleReference(name).Text + Environment.NewLine + "  : " +
                                string.Join(Environment.NewLine + "  | ", sorted) + Environment.NewLine + "  ;" + Environment.NewLine;

                    // Now proceed in depth-first fashion through the referenced rules to keep related rules
                    // close by. Don't recurse into major-sections to help keep them separated in grammar file.
                    foreach (var production in sorted)
                        foreach (var referencedRule in production.ReferencedRules)
                            if (!majorRules.Concat(lexicalRules).Contains(referencedRule))
                                processRule(referencedRule, ref result);
                }
            }
<<<<<<< HEAD
=======

            return ProcessChildren(choice.Children, " | ").Parenthesize().WithSuffix(allChildrenAreOptional ? "?" : "");
=======
            void AddNormalizedRules(string name)
=======
grammar csharp;" + Concat(normalizedRules.Select(t => generateRule(t.name, t.productions)));
=======
grammar csharp;" + string.Concat(normalizedRules.Select(t => generateRule(t.name, t.productions)));
>>>>>>> Simplify

<<<<<<< HEAD
            void addNormalizedRules(string name)
>>>>>>> Compiler namings
=======
                processRule(rule);

<<<<<<< HEAD
            return result;

            void processRule(string name)
>>>>>>> Simplify
=======
=======
            // Handle each major section first and then walk any missed rules.
=======
            // Handle each major section first and then walk any rules not hit from them.
>>>>>>> Simplify
=======
            // Handle each major section first and then walk any rules not hit transitively from them.
>>>>>>> Simplify
            foreach (var rule in majorRules.Concat(nameToProductions.Keys.OrderBy(a => a)))
>>>>>>> Simplify
                processRule(rule, ref result);

            return result;

            void processRule(string name, ref string result)
>>>>>>> Simplify
            {
                if (name != "CSharpSyntaxNode" && seen.Add(name))
                {
                    // Order the productions to keep us independent from whatever changes happen in Syntax.xml.
                    var sorted = nameToProductions[name].OrderBy(v => v);
                    if (sorted.Any())
                    {
                        result += Environment.NewLine + RuleReference(name).Text + Environment.NewLine + "  : " +
                                  string.Join(Environment.NewLine + "  | ", sorted) + Environment.NewLine + "  ;" + Environment.NewLine;

                        // Now proceed in depth-first fashion through the referenced rules to keep related rules
                        // close by. Don't recurse into major-sections to help keep them separated in grammar file.
                        foreach (var production in sorted)
                            foreach (var referencedRule in production.ReferencedRules.Where(r => !majorRules.Concat(lexicalRules).Contains(r)))
                                processRule(referencedRule, ref result);
                    }
                }
            }
<<<<<<< HEAD

<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
        private static string Generate(string name, ImmutableArray<string> productions)
<<<<<<< HEAD
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
>>>>>>> Simplify
        }
=======
            => Environment.NewLine + Environment.NewLine + name + Environment.NewLine + "  : " +
               Join(Environment.NewLine + "  | ", productions) + Environment.NewLine + "  ;";
>>>>>>> Simplify
=======
            static string GenerateRule(string name, ImmutableArray<string> productions)
=======
            static string generateRule(string name, ImmutableArray<string> productions)
>>>>>>> Compiler namings
=======
            void processNodeChildren(TreeType node, List<TreeTypeChild> children)
                => nameToProductions[node.Name].Add(CreateProductionFromNodeChildren(children, delim: " "));
<<<<<<< HEAD

            static string generateRule(string name, ImmutableArray<Production> productions)
>>>>>>> Simplify
                => Environment.NewLine + Environment.NewLine + name + Environment.NewLine + "  : " +
                   string.Join(Environment.NewLine + "  | ", productions) + Environment.NewLine + "  ;";
=======
>>>>>>> Simplify
=======
>>>>>>> Simplify
        }
>>>>>>> Simplify

<<<<<<< HEAD
<<<<<<< HEAD
        private Production ProcessField(Field field)
            => GetFieldUnderlyingType(field).WithSuffix(field.Optional == "true" ? "?" : "");
=======
        /// <summary>
        /// Returns the g4 production string for this rule based on the children it has. Also
        /// returns all the names of other rules this particular production references.
        /// </summary>
=======
>>>>>>> Simplify
=======
        private static Production Join(string delim, IEnumerable<Production> productions)
            => new Production(string.Join(delim, productions.Where(p => p.Text.Length > 0)), productions.SelectMany(p => p.ReferencedRules));

<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
>>>>>>> Simplify
        private Production CreateProductionFromNodeChildren(TreeTypeChild[] children, string delim = " ")
=======
        private Production ProcessChildren(TreeTypeChild[] children, string delim = " ")
>>>>>>> Simplify
=======
        private Production HandleChildren(TreeTypeChild[] children, string delim = " ")
>>>>>>> Simplify
=======
        private static Production HandleChildren(TreeTypeChild[] children, string delim = " ")
>>>>>>> Simplify
=======
        private static Production HandleChildren(List<TreeTypeChild> children, string delim = " ")
>>>>>>> Simplify
            => Join(delim, children.Select(child =>
                child is Choice c ? HandleChildren(c.Children, delim: " | ").Parenthesize() :
                child is Sequence s ? HandleChildren(s.Children).Parenthesize() :
                child is Field f ? HandleField(f).Suffix("?", when: f.IsOptional) : throw new InvalidOperationException()));

<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
        private Production ProcessField(Field field, bool elideParentheses)
            => GetFieldUnderlyingType(field, elideParentheses).WithSuffix(field.IsOptional ? "?" : "");
>>>>>>> Simplify
=======
        private Production ProcessField(Field field)
            => GetFieldUnderlyingType(field).WithSuffix(field.IsOptional ? "?" : "");
>>>>>>> Simplify

        private Production GetFieldUnderlyingType(Field field)
=======
        private Production GetFieldType(Field field)
>>>>>>> Simplify
=======
        private Production CreateProductionForField(Field field)
>>>>>>> Simplify
=======
        private static Production CreateProductionForField(Field field)
<<<<<<< HEAD
>>>>>>> Simplify
            // 'bool' fields are for the few boolean properties we generate on DirectiveTrivia.
            // They're not relevant to the grammar, so we just return an empty production here
            // which will be filtered out by the caller.
=======
=======
        private static Production ProcessField(Field field)
>>>>>>> Simplify
=======
        private static Production HandleField(Field field)
>>>>>>> Simplify
            // 'bool' fields are for a few properties we generate on DirectiveTrivia. They're not
            // relevant to the grammar, so we just return an empty production to ignore them.
>>>>>>> Simplify
            => field.Type == "bool" ? new Production("") :
               field.Type == "CSharpSyntaxNode" ? RuleReference(field.Kinds.Single().Name + "Syntax") :
               field.Type.StartsWith("SeparatedSyntaxList") ? HandleSeparatedList(field, field.Type[("SeparatedSyntaxList".Length + 1)..^1]) :
               field.Type.StartsWith("SyntaxList") ? HandleList(field, field.Type[("SyntaxList".Length + 1)..^1]) :
               field.IsToken ? HandleTokenField(field) : RuleReference(field.Type);

        private static Production HandleSeparatedList(Field field, string elementType)
<<<<<<< HEAD
<<<<<<< HEAD
        {
            var result = RuleReference(elementType).WithSuffix(" (',' " + RuleReference(elementType) + ")*").WithSuffixIf(field.AllowTrailingSeparator != null, " ','?");
            return field.MinCount != null ? result : result.Parenthesize().WithSuffix("?");
>>>>>>> Simplify
        }
=======
            => RuleReference(elementType).WithSuffix(" (',' " + RuleReference(elementType) + ")*")
<<<<<<< HEAD
<<<<<<< HEAD
                .WithSuffixIf(field.AllowTrailingSeparator != null, " ','?")
                .ParenthesizeIf(field.MinCount == null).WithSuffixIf(field.MinCount == null, "?");
>>>>>>> Simplify
=======
                .WithSuffix(" ','?", when: field.AllowTrailingSeparator != null)
=======
                .WithSuffix(" ','?", when: field.AllowTrailingSeparator)
<<<<<<< HEAD
>>>>>>> Simplify
                .Parenthesize(when: field.MinCount == null).WithSuffix("?", when: field.MinCount == null);
>>>>>>> Simplify
=======
                .Parenthesize(when: field.MinCount == 0).WithSuffix("?", when: field.MinCount == 0);
>>>>>>> Simplify
=======
            => RuleReference(elementType).Suffix(" (',' " + RuleReference(elementType) + ")*")
                .Suffix(" ','?", when: field.AllowTrailingSeparator)
                .Parenthesize(when: field.MinCount == 0).Suffix("?", when: field.MinCount == 0);
>>>>>>> Simplify

<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
        private static Production Join(string delim, IEnumerable<Production> productions)
            => new Production(string.Join(delim, productions.Where(p => p.Text.Length > 0)), productions.SelectMany(p => p.ReferencedRules));
=======
        private Production HandleSyntaxListField(Field field)
=======
        private Production CreateProductionForSyntaxListField(Field field)
>>>>>>> Simplify
=======
        private static Production CreateProductionForSyntaxListField(Field field)
>>>>>>> Simplify
=======
        private static Production CreateProductionForSyntaxList(Field field, string elementType)
>>>>>>> Simplify
=======
        private static Production ProcessSyntaxList(Field field, string elementType)
<<<<<<< HEAD
>>>>>>> Simplify
        {
            var result = elementType != "SyntaxToken" ? RuleReference(elementType) :
                         field.Name == "Commas" ? new Production("','") :
                         field.Name == "Modifiers" ? RuleReference("Modifier") :
                         field.Name == "TextTokens" ? RuleReference(nameof(SyntaxKind.XmlTextLiteralToken)) : RuleReference("Token");
            return result.WithSuffix(field.MinCount != null ? "+" : "*");
        }
=======
=======
        private static Production HandleSyntaxList(Field field, string elementType)
>>>>>>> Simplify
=======
        private static Production HandleList(Field field, string elementType)
>>>>>>> Simplify
            => (elementType != "SyntaxToken" ? RuleReference(elementType) :
                field.Name == "Commas" ? new Production("','") :
                field.Name == "Modifiers" ? RuleReference("Modifier") :
                field.Name == "TextTokens" ? RuleReference(nameof(SyntaxKind.XmlTextLiteralToken)) : RuleReference("Token"))
<<<<<<< HEAD
<<<<<<< HEAD
                    .WithSuffix(field.MinCount != null ? "+" : "*");
>>>>>>> Simplify
=======
                    .WithSuffix(field.MinCount == 0 ? "*" : "+");
>>>>>>> Simplify
=======
                    .Suffix(field.MinCount == 0 ? "*" : "+");
>>>>>>> Simplify

        private static Production HandleTokenField(Field field)
            => field.Kinds.Count == 0 ? HandleTokenName(field.Name) : Join(" | ", field.Kinds.Select(
                k => HandleTokenName(k.Name))).Parenthesize(when: field.Kinds.Count >= 2);

        private static Production HandleTokenName(string tokenName)
            => GetSyntaxKind(tokenName) is var kind && kind == SyntaxKind.None ? RuleReference("Token") :
               SyntaxFacts.GetText(kind) is var text && text != "" ? new Production(text == "'" ? "'\\''" : $"'{text}'") :
               tokenName.StartsWith("EndOf") ? new Production("") :
               tokenName.StartsWith("Omitted") ? new Production("/* epsilon */") : RuleReference(tokenName);

        private static SyntaxKind GetSyntaxKind(string name)
            => GetMembers<SyntaxKind>().Where(k => k.ToString() == name).SingleOrDefault();

        private static IEnumerable<TEnum> GetMembers<TEnum>() where TEnum : struct, Enum
            => (IEnumerable<TEnum>)Enum.GetValues(typeof(TEnum));

<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
        private static IEnumerable<SyntaxKind> SyntaxKinds => GetKinds<SyntaxKind>();

<<<<<<< HEAD
        private static SyntaxKind GetTokenKind(string tokenName)
<<<<<<< HEAD
        {
            foreach (var field in typeof(SyntaxKind).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.Name == tokenName)
                {
                    return (SyntaxKind)field.GetValue(null);
                }
            }
>>>>>>> Simplify

        private static Production HandleChildren(IEnumerable<TreeTypeChild> children, string delim = " ")
            => Join(delim, children.Select(child =>
                child is Choice c ? HandleChildren(c.Children, delim: " | ").Parenthesize().Suffix("?", when: c.Optional) :
                child is Sequence s ? HandleChildren(s.Children).Parenthesize() :
                child is Field f ? HandleField(f).Suffix("?", when: f.IsOptional) : throw new InvalidOperationException()));

<<<<<<< HEAD
        private static Production HandleField(Field field)
            // 'bool' fields are for a few properties we generate on DirectiveTrivia. They're not
            // relevant to the grammar, so we just return an empty production to ignore them.
            => field.Type == "bool" ? new Production("") :
               field.Type == "CSharpSyntaxNode" ? RuleReference(field.Kinds.Single().Name + "Syntax") :
               field.Type.StartsWith("SeparatedSyntaxList") ? HandleSeparatedList(field, field.Type[("SeparatedSyntaxList".Length + 1)..^1]) :
               field.Type.StartsWith("SyntaxList") ? HandleList(field, field.Type[("SyntaxList".Length + 1)..^1]) :
               field.IsToken ? HandleTokenField(field) : RuleReference(field.Type);

        private static Production HandleSeparatedList(Field field, string elementType)
            => RuleReference(elementType).Suffix(" (',' " + RuleReference(elementType) + ")")
                .Suffix("*", when: field.MinCount < 2).Suffix("+", when: field.MinCount >= 2)
                .Suffix(" ','?", when: field.AllowTrailingSeparator)
                .Parenthesize(when: field.MinCount == 0).Suffix("?", when: field.MinCount == 0);

        private static Production HandleList(Field field, string elementType)
            => (elementType != "SyntaxToken" ? RuleReference(elementType) :
                field.Name == "Commas" ? new Production("','") :
                field.Name == "Modifiers" ? RuleReference("Modifier") :
                field.Name == "TextTokens" ? RuleReference(nameof(SyntaxKind.XmlTextLiteralToken)) : RuleReference(elementType))
                    .Suffix(field.MinCount == 0 ? "*" : "+");

        private static Production HandleTokenField(Field field)
            => field.Kinds.Count == 0
                ? HandleTokenName(field.Name)
                : Join(" | ", field.Kinds.Select(k => HandleTokenName(k.Name))).Parenthesize(when: field.Kinds.Count >= 2);

        private static Production HandleTokenName(string tokenName)
            => GetSyntaxKind(tokenName) is var kind && kind == SyntaxKind.None ? RuleReference("SyntaxToken") :
               SyntaxFacts.GetText(kind) is var text && text != "" ? new Production(text == "'" ? "'\\''" : $"'{text}'") :
               tokenName.StartsWith("EndOf") ? new Production("") :
               tokenName.StartsWith("Omitted") ? new Production("/* epsilon */") : RuleReference(tokenName);

        private static SyntaxKind GetSyntaxKind(string name)
            => GetMembers<SyntaxKind>().Where(k => k.ToString() == name).SingleOrDefault();

        private static IEnumerable<TEnum> GetMembers<TEnum>() where TEnum : struct, Enum
            => (IEnumerable<TEnum>)Enum.GetValues(typeof(TEnum));

        private static Production RuleReference(string name)
            => new Production(
                s_normalizationRegex.Replace(name.EndsWith("Syntax") ? name[..^"Syntax".Length] : name, "_").ToLower(),
                ImmutableArray.Create(name));

        // Converts a PascalCased name into snake_cased name.
        private static readonly Regex s_normalizationRegex = new Regex(
            "(?<=[A-Z])(?=[A-Z][a-z]) | (?<=[^A-Z])(?=[A-Z]) | (?<=[A-Za-z])(?=[^A-Za-z])",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
=======
=======
            => tokenName == "Identifier"
                ? SyntaxKind.IdentifierToken
                : SyntaxKinds.Where(k => k.ToString() == tokenName).Single();

>>>>>>> Simplify
        private Production RuleReference(string ruleName)
=======
=======
>>>>>>> Simplify
        private Production CreateProductionForRuleReference(string ruleName)
>>>>>>> Simplify
            => _nameToProductions.ContainsKey(ruleName)
                ? new Production(Normalize(ruleName), ImmutableArray.Create(ruleName))
                : throw new InvalidOperationException("No rule found with name: " + ruleName);
=======
        private static Production CreateProductionForRuleReference(string ruleName)
=======
        private static Production RuleReference(string ruleName)
<<<<<<< HEAD
>>>>>>> Simplify
            => new Production(Normalize(ruleName), ImmutableArray.Create(ruleName));
>>>>>>> Simplify
=======
            => new Production(
                s_normalizationRegex.Replace(ruleName.EndsWith("Syntax") ? ruleName[..^"Syntax".Length] : ruleName, "_").ToLower(),
                ImmutableArray.Create(ruleName));
>>>>>>> Simplify

<<<<<<< HEAD
        /// <summary>
        /// Converts a <c>PascalCased</c> name into <c>snake_cased</c> name.
        /// </summary>
        private static readonly Regex s_normalizationRegex = new Regex(@"
            (?<=[A-Z])(?=[A-Z][a-z]) |
            (?<=[^A-Z])(?=[A-Z]) |
            (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
<<<<<<< HEAD

        private static readonly ImmutableArray<string> s_lexicalTokens = ImmutableArray.Create(
<<<<<<< HEAD
            "Token",
            nameof(SyntaxKind.IdentifierToken),
            nameof(SyntaxKind.CharacterLiteralToken),
            nameof(SyntaxKind.StringLiteralToken),
            nameof(SyntaxKind.NumericLiteralToken),
            nameof(SyntaxKind.InterpolatedStringTextToken),
            nameof(SyntaxKind.XmlTextLiteralToken));
<<<<<<< HEAD

        // This is optional, but makes the emitted g4 file a bit nicer.  We define a few major
        // sections that generally correspond to base nodes that have a lot of derived nodes. If we
        // hit these nodes while recursing through another node, we won't just print them out then.
        // Instead, we'll wait till we're done with the previous nodes, then start emitting these.
        // Without this, just processing CompilationUnit will cause expressions to print early
        // because of things like: CompilationUnit->AttributeList->AttributeArg->Expression.
=======
        private static Production RuleReference(string name)
            => new Production(
                s_normalizationRegex.Replace(name.EndsWith("Syntax") ? name[..^"Syntax".Length] : name, "_").ToLower(),
                ImmutableArray.Create(name));
>>>>>>> Simplify

        private static readonly ImmutableArray<string> s_majorRules = ImmutableArray.Create(
            "CompilationUnitSyntax",
            "MemberDeclarationSyntax",
            "StatementSyntax",
            "ExpressionSyntax",
            "TypeSyntax",
            "XmlNodeSyntax",
            "StructuredTriviaSyntax");
>>>>>>> Simplify
=======
>>>>>>> Simplify
=======
            "Token", nameof(IdentifierToken), nameof(CharacterLiteralToken), nameof(StringLiteralToken),
            nameof(NumericLiteralToken), nameof(InterpolatedStringTextToken), nameof(XmlTextLiteralToken));
>>>>>>> Simplify
=======
>>>>>>> Simplify
=======
        // Converts a PascalCased name into snake_cased name.
        private static readonly Regex s_normalizationRegex = new Regex(
            "(?<=[A-Z])(?=[A-Z][a-z]) | (?<=[^A-Z])(?=[A-Z]) | (?<=[A-Za-z])(?=[^A-Za-z])",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
>>>>>>> Simplify
    }

    internal struct Production : IComparable<Production>
    {
<<<<<<< HEAD
<<<<<<< HEAD
=======
        /// <summary>
        /// The line of text to include in the grammar file.  i.e. everything after <c>:</c> in 
        /// <c>: 'extern' 'alias' identifier_token ';'</c>.
        /// </summary>
>>>>>>> Simplify
        public readonly string Text;
        public readonly ImmutableArray<string> ReferencedRules;

<<<<<<< HEAD
        public Production(string text, IEnumerable<string> referencedRules = null)
=======
        /// <summary>
        /// The names of other rules that are referenced by this rule.  Used purely as an aid to
        /// help order productions when emitting.  In general, we want to keep referenced rules
        /// close to the rule that references them.
        /// </summary>
        public readonly ImmutableArray<string> RuleReferences;
=======
        public readonly string Text;
        public readonly ImmutableArray<string> ReferencedRules;
>>>>>>> Simplify

<<<<<<< HEAD
        public Production(string text) : this(text, ImmutableArray<string>.Empty)
        {
        }

<<<<<<< HEAD
        public Production(string text, IEnumerable<string> ruleReferences)
>>>>>>> Simplify
        {
            Text = text;
            ReferencedRules = referencedRules?.ToImmutableArray() ?? ImmutableArray<string>.Empty;
        }

        public override string ToString() => Text;
<<<<<<< HEAD
        public int CompareTo(Production other) => StringComparer.Ordinal.Compare(this.Text, other.Text);
        public Production Prefix(string prefix) => new Production(prefix + this, ReferencedRules);
        public Production Suffix(string suffix, bool when = true) => when ? new Production(this + suffix, ReferencedRules) : this;
        public Production Parenthesize(bool when = true) => when ? Prefix("(").Suffix(")") : this;
=======
        public Production WithPrefix(string prefix) => new Production(prefix + this, RuleReferences);
        public Production WithSuffix(string suffix) => new Production(this + suffix, RuleReferences);
=======
        public Production(string text, IEnumerable<string> referencedRules)
=======
        public Production(string text, IEnumerable<string> referencedRules = null)
>>>>>>> Simplify
        {
            Text = text;
            ReferencedRules = referencedRules?.ToImmutableArray() ?? ImmutableArray<string>.Empty;
        }

        public override string ToString() => Text;
        public int CompareTo(Production other) => StringComparer.Ordinal.Compare(this.Text, other.Text);
<<<<<<< HEAD
        public Production WithPrefix(string prefix) => new Production(prefix + this, ReferencedRules);
<<<<<<< HEAD
        public Production WithSuffix(string suffix) => new Production(this + suffix, ReferencedRules);
>>>>>>> Simplify
        public Production WithSuffixIf(bool test, string suffix) => test ? WithSuffix(suffix) : this;
        public Production Parenthesize() => WithPrefix("(").WithSuffix(")");
<<<<<<< HEAD
>>>>>>> Simplify
=======
        public Production ParenthesizeIf(bool test) => test ? Parenthesize() : this;
>>>>>>> Simplify
=======
        public Production WithSuffix(string suffix, bool when = true) => when ? new Production(this + suffix, ReferencedRules) : this;
        public Production Parenthesize(bool when = true) => when ? WithPrefix("(").WithSuffix(")") : this;
>>>>>>> Simplify
=======
        public Production Prefix(string prefix) => new Production(prefix + this, ReferencedRules);
        public Production Suffix(string suffix, bool when = true) => when ? new Production(this + suffix, ReferencedRules) : this;
        public Production Parenthesize(bool when = true) => when ? Prefix("(").Suffix(")") : this;
>>>>>>> Simplify
    }
}

namespace Microsoft.CodeAnalysis
{
    internal static class GreenNode
    {
        internal const int ListKind = 1; // See SyntaxKind.
    }
}

