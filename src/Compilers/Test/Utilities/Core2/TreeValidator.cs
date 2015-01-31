// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public delegate bool TreeRuleDelegate(SyntaxTree tree, string codeText, string filename, ref string errorText);
    public delegate bool NonTerminalRuleDelegate(SyntaxNode nonTerminal, SyntaxTree tree, ref string errorText);
    public delegate bool TokenRuleDelegate(SyntaxToken token, SyntaxTree tree, ref string errorText);
    public delegate bool TriviaRuleDelegate(SyntaxTrivia trivia, SyntaxTree tree, ref string errorText);
    public delegate void ErrorHandlerDelegate(string error, Rule rule);

    public class Failure
    {
        private string _file;
        public string File
        {
            get
            {
                if (_file == null)
                {
                    _file = string.Empty;
                }

                return _file;
            }
        }

        private readonly Rule _rule;
        public Rule Rule
        {
            get
            {
                return _rule;
            }
        }

        private string _errorText;
        public string ErrorText
        {
            get
            {
                if (_errorText == null)
                {
                    _errorText = string.Empty;
                }

                return _errorText;
            }
        }

        private Location _location;
        public Location Location
        {
            get
            {
                if (_location == null)
                {
                    _location = FailureLocation.Default;
                }

                return _location;
            }
        }

        private string _nodeKind;
        public string NodeKind
        {
            get
            {
                if (_nodeKind == null)
                {
                    _nodeKind = string.Empty;
                }

                return _nodeKind;
            }
        }

        public override string ToString()
        {
            var retVal = !string.IsNullOrEmpty(this.File) ? this.File + " " : null;
            if (this.Location.IsInSource)
            {
                retVal = this.Location.SourceSpan.ToString() + ": ";
            }

            retVal = this.Rule.Name + ": " + this.NodeKind + ": " + this.ErrorText;
            return retVal;
        }

        public Failure(string file, Rule rule, string nodeKind, string errorText, Location location)
        {
            _file = file;
            _rule = rule;
            _nodeKind = nodeKind;
            _errorText = errorText;
            _location = location;
        }
    }

    /* #End Region
     */
    /* #Region "Rule"
     */
    public class Rule
    {
        public string Name
        {
            get;
            set;
        }

        public string Group
        {
            get;
            set;
        }
    }

    /* #End Region
     */
    public class TreeValidator
    {
        /* #Region "Rules"
     */
        private class TreeRule : Rule
        {
            public TreeRuleDelegate Test
            {
                get;
                set;
            }
        }

        private class NonTerminalRule : Rule
        {
            public NonTerminalRuleDelegate Test
            {
                get;
                set;
            }
        }

        private class TokenRule : Rule
        {
            public TokenRuleDelegate Test
            {
                get;
                set;
            }
        }

        private class TriviaRule : Rule
        {
            public TriviaRuleDelegate Test
            {
                get;
                set;
            }
        }

        private readonly Dictionary<string, TreeRule> _treeRules = new Dictionary<string, TreeRule>();
        private readonly Dictionary<string, NonTerminalRule> _nonTerminalRules = new Dictionary<string, NonTerminalRule>();
        private readonly Dictionary<string, TokenRule> _tokenRules = new Dictionary<string, TokenRule>();
        private readonly Dictionary<string, TriviaRule> _triviaRules = new Dictionary<string, TriviaRule>();
        private event ErrorHandlerDelegate ValidationFailed;

        public TreeValidator(ISyntaxNodeKindProvider nodeKindProvider)
        {
            NodeHelpers.KindProvider = nodeKindProvider;
            RegisterRules();
        }

        /* #Region "Register"
     */
        private void RegisterRules()
        {
            RegisterRules(Assembly.GetExecutingAssembly());
        }

        private void RegisterRules(Assembly assembly)
        {
            if (assembly != null)
            {
                foreach (var t in assembly.GetTypes())
                {
                    foreach (var m in t.GetMethods())
                    {
                        if (m.IsStatic && !m.IsAbstract && !m.IsConstructor && !m.IsGenericMethod && m.ReturnType == typeof(bool) &&
                            (m.GetParameters()[0].ParameterType == typeof(SyntaxTree) ||
                            m.GetParameters()[0].ParameterType == typeof(SyntaxNode) ||
                            m.GetParameters()[0].ParameterType == typeof(SyntaxToken) ||
                            m.GetParameters()[0].ParameterType == typeof(SyntaxTrivia)))
                        {
                            var method = m;
                            var attrs = method.GetCustomAttributes(false);
                            if (attrs != null && attrs.Length == 1)
                            {
                                if (attrs[0] is TreeRuleAttribute)
                                {
                                    var attr = (TreeRuleAttribute)attrs[0];
                                    RegisterRule(new TreeRule()
                                    {
                                        Test = (TreeRuleDelegate)Delegate.CreateDelegate(typeof(TreeRuleDelegate), method),
                                        Name = attr.Name,
                                        Group = attr.Group
                                    }

                                    );
                                }
                                else if (attrs[0] is NonTerminalRuleAttribute)
                                {
                                    var attr = (NonTerminalRuleAttribute)attrs[0];
                                    RegisterRule(new NonTerminalRule()
                                    {
                                        Test = (NonTerminalRuleDelegate)Delegate.CreateDelegate(typeof(NonTerminalRuleDelegate), method),
                                        Name = attr.Name,
                                        Group = attr.Group
                                    }

                                    );
                                }
                                else if (attrs[0] is TokenRuleAttribute)
                                {
                                    var attr = (TokenRuleAttribute)attrs[0];
                                    RegisterRule(new TokenRule()
                                    {
                                        Test = (TokenRuleDelegate)Delegate.CreateDelegate(typeof(TokenRuleDelegate), method),
                                        Name = attr.Name,
                                        Group = attr.Group
                                    }

                                    );
                                }
                                else if (attrs[0] is TriviaRuleAttribute)
                                {
                                    var attr = (TriviaRuleAttribute)attrs[0];
                                    RegisterRule(new TriviaRule()
                                    {
                                        Test = (TriviaRuleDelegate)Delegate.CreateDelegate(typeof(TriviaRuleDelegate), method),
                                        Name = attr.Name,
                                        Group = attr.Group
                                    }

                                    );
                                }
                            }
                        }
                    }
                }
            }
        }

        public void RegisterRules(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                RegisterRules(Assembly.LoadFrom(path));
            }
        }

        private void RegisterRule(TreeRule rule)
        {
            if (rule != null)
            {
                _treeRules[rule.Name] = rule;
            }
        }

        private void RegisterRule(NonTerminalRule rule)
        {
            if (rule != null)
            {
                _nonTerminalRules[rule.Name] = rule;
            }
        }

        private void RegisterRule(TokenRule rule)
        {
            if (rule != null)
            {
                _tokenRules[rule.Name] = rule;
            }
        }

        private void RegisterRule(TriviaRule rule)
        {
            if (rule != null)
            {
                _triviaRules[rule.Name] = rule;
            }
        }

        public void RegisterErrorHandler(ErrorHandlerDelegate errorHandler)
        {
            if (errorHandler != null)
            {
                ValidationFailed += errorHandler;
            }
        }

        public void RegisterRule(TreeRuleDelegate test, string name, string group)
        {
            if (test != null && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(group))
            {
                RegisterRule(new TreeRule()
                {
                    Test = test,
                    Name = name,
                    Group = group
                }

                );
            }
        }

        public void RegisterRule(NonTerminalRuleDelegate test, string name, string group)
        {
            if (test != null && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(group))
            {
                RegisterRule(new NonTerminalRule()
                {
                    Test = test,
                    Name = name,
                    Group = group
                }

                );
            }
        }

        public void RegisterRule(TokenRuleDelegate test, string name, string group)
        {
            if (test != null && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(group))
            {
                RegisterRule(new TokenRule()
                {
                    Test = test,
                    Name = name,
                    Group = group
                }

                );
            }
        }

        public void RegisterRule(TriviaRuleDelegate test, string name, string group)
        {
            if (test != null && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(group))
            {
                RegisterRule(new TriviaRule()
                {
                    Test = test,
                    Name = name,
                    Group = group
                }

                );
            }
        }

        /* #End Region
     */
        /* #Region "UnRegister"
     */
        public void UnregisterRule(string ruleName)
        {
            if (!string.IsNullOrEmpty(ruleName))
            {
                if (_treeRules.ContainsKey(ruleName))
                {
                    _treeRules.Remove(ruleName);
                }

                if (_nonTerminalRules.ContainsKey(ruleName))
                {
                    _nonTerminalRules.Remove(ruleName);
                }

                if (_tokenRules.ContainsKey(ruleName))
                {
                    _tokenRules.Remove(ruleName);
                }

                if (_triviaRules.ContainsKey(ruleName))
                {
                    _triviaRules.Remove(ruleName);
                }
            }
        }

        public void UnregisterRuleGroup(string group)
        {
            if (!string.IsNullOrEmpty(group))
            {
                foreach (var r in _treeRules)
                {
                    if (r.Value.Group == group)
                    {
                        _treeRules.Remove(r.Value.Name);
                    }
                }

                foreach (var r in _nonTerminalRules)
                {
                    if (r.Value.Group == group)
                    {
                        _nonTerminalRules.Remove(r.Value.Name);
                    }
                }

                foreach (var r in _tokenRules)
                {
                    if (r.Value.Group == group)
                    {
                        _tokenRules.Remove(r.Value.Name);
                    }
                }

                foreach (var r in _triviaRules)
                {
                    if (r.Value.Group == group)
                    {
                        _triviaRules.Remove(r.Value.Name);
                    }
                }
            }
        }

        public void UnregisterAllTreeRules()
        {
            _treeRules.Clear();
        }

        public void UnregisterAllNonTerminalRules()
        {
            _nonTerminalRules.Clear();
        }

        public void UnregisterAllTokenRules()
        {
            _tokenRules.Clear();
        }

        public void UnregisterAllTriviaRules()
        {
            _triviaRules.Clear();
        }

        public void UnregisterAllRules()
        {
            UnregisterAllTreeRules();
            UnregisterAllNonTerminalRules();
            UnregisterAllTokenRules();
            UnregisterAllTriviaRules();
        }

        /* #End Region
     */
        /* #Region "Validate"
     */
        private bool ValidateTree(SyntaxTree tree, string codeText, string filename, List<Failure> failures = null)
        {
            var retVal = true;
            if (!string.IsNullOrEmpty(filename))
            {
                filename = Path.GetFullPath(filename);
            }
            else if (filename == null)
            {
                filename = string.Empty;
            }

            bool pass = false;
            if (tree != null)
            {
                foreach (var rule in _treeRules.Values)
                {
                    var errorText = string.Empty;
                    pass = rule.Test(tree, codeText, filename, ref errorText);
                    if (!pass)
                    {
                        if (failures != null)
                        {
                            failures.Add(new Failure(filename, rule, tree.GetRoot().GetKind(), errorText, new FailureLocation(tree.GetRoot().Span, tree)));
                        }

                        ValidationFailed(errorText, rule);
                    }

                    retVal = retVal & pass;
                }
            }

            return retVal;
        }

        private bool ValidateNodeOrToken(SyntaxNodeOrToken nodeOrtoken, SyntaxTree tree, string filename = "", List<Failure> failures = null)
        {
            var retVal = true;
            if (nodeOrtoken.IsNode)
            {
                retVal = ValidateNonTerminal(nodeOrtoken.AsNode(), tree, filename, failures);
            }
            else
            {
                retVal = ValidateToken(nodeOrtoken.AsToken(), tree, filename, failures);
            }

            return retVal;
        }

        private bool ValidateNonTerminal(SyntaxNode nonTerminal, SyntaxTree tree, string filename = "", List<Failure> failures = null)
        {
            var retVal = true;
            if (!string.IsNullOrEmpty(filename))
            {
                filename = Path.GetFullPath(filename);
            }
            else if (filename == null)
            {
                filename = string.Empty;
            }

            if (nonTerminal != null)
            {
                foreach (var child in nonTerminal.ChildNodesAndTokens())
                {
                    retVal = retVal & ValidateNodeOrToken(child, tree, filename, failures);
                }

                bool pass = false;
                foreach (var rule in _nonTerminalRules.Values)
                {
                    var errorText = string.Empty;
                    pass = rule.Test(nonTerminal, tree, ref errorText);
                    if (!pass)
                    {
                        if (failures != null)
                        {
                            failures.Add(new Failure(filename, rule, nonTerminal.GetKind(), errorText, new FailureLocation(nonTerminal.Span, tree)));
                        }

                        ValidationFailed(errorText, rule);
                    }

                    retVal = retVal & pass;
                }
            }

            return retVal;
        }

        private bool ValidateToken(SyntaxToken token, SyntaxTree tree, string filename = "", List<Failure> failures = null)
        {
            var retVal = true;
            foreach (var leadingTrivia in token.LeadingTrivia)
            {
                retVal = retVal & ValidateTrivia(leadingTrivia, tree, filename, failures);
            }

            foreach (var trailingTrivia in token.TrailingTrivia)
            {
                retVal = retVal & ValidateTrivia(trailingTrivia, tree, filename, failures);
            }

            bool pass = false;
            foreach (var rule in _tokenRules.Values)
            {
                var errorText = string.Empty;
                pass = rule.Test(token, tree, ref errorText);
                if (!pass)
                {
                    if (failures != null)
                    {
                        failures.Add(new Failure(filename, rule, token.GetKind(), errorText, new FailureLocation(token.Span, tree)));
                    }

                    ValidationFailed(errorText, rule);
                }

                retVal = retVal & pass;
            }

            return retVal;
        }

        private bool ValidateTrivia(SyntaxTrivia trivia, SyntaxTree tree, string filename = "", List<Failure> failures = null)
        {
            var retVal = true;
            if (trivia.HasStructure)
            {
                retVal = retVal & ValidateNonTerminal(trivia.GetStructure(), tree, filename, failures);
            }

            bool pass = false;
            foreach (var rule in _triviaRules.Values)
            {
                var errorText = string.Empty;
                pass = rule.Test(trivia, tree, ref errorText);
                if (!pass)
                {
                    if (failures != null)
                    {
                        failures.Add(new Failure(filename, rule, trivia.GetKind(), errorText, new FailureLocation(trivia.Span, tree)));
                    }

                    ValidationFailed(errorText, rule);
                }

                retVal = retVal & pass;
            }

            return retVal;
        }

        public bool Validate(SyntaxTree tree, string codeText, string filename, List<Failure> failures = null)
        {
            var retVal = true;
            if (!string.IsNullOrEmpty(filename))
            {
                filename = Path.GetFullPath(filename);
            }
            else if (filename == null)
            {
                filename = string.Empty;
            }

            if (_treeRules.Count > 0)
            {
                retVal = retVal & ValidateTree(tree, codeText, filename, failures);
            }

            if (_nonTerminalRules.Count > 0)
            {
                retVal = retVal & ValidateNonTerminal(tree.GetRoot(), tree, filename, failures);
            }

            return retVal;
        }
    }
}
