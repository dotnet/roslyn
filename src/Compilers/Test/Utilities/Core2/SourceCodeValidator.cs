// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class SourceCodeValidator
    {
        protected const int ParserTimeout = 1 * 600000;
        protected const int TreeValidatorTimeout = 1 * 600000;
        protected readonly Rule ExceptionRule = new Rule()
        {
            Name = "ExceptionThrown"
        };

        private readonly string[] m_searchPatterns;
        private readonly IParser m_parser;
        private readonly TreeValidator m_treeValidator;
        protected readonly List<Failure> m_failures;

        public event Action<string> FileFound;
        public event Action<StringBuilder> TransformCode;

        public SourceCodeValidator(IParser parser, string[] searchPatterns, ISyntaxNodeKindProvider nodeKindProvider, bool enableAllRules)
        {
            m_parser = parser;
            m_searchPatterns = searchPatterns;
            m_treeValidator = new TreeValidator(nodeKindProvider);
            m_failures = new List<Failure>();
            if (!enableAllRules)
            {
                m_treeValidator.UnregisterAllRules();
            }
        }

        public ReadOnlyCollection<Failure> Failures
        {
            get
            {
                return new ReadOnlyCollection<Failure>(m_failures);
            }
        }

        public void RegisterRule(TreeRuleDelegate test, string name, string group)
        {
            m_treeValidator.RegisterRule(test, name, group);
        }

        public void RegisterRule(NonTerminalRuleDelegate test, string name, string group)
        {
            m_treeValidator.RegisterRule(test, name, group);
        }

        public void RegisterRule(TokenRuleDelegate test, string name, string group)
        {
            m_treeValidator.RegisterRule(test, name, group);
        }

        public void RegisterRule(TriviaRuleDelegate test, string name, string group)
        {
            m_treeValidator.RegisterRule(test, name, group);
        }

        public void UnRegisterRule(string name)
        {
            m_treeValidator.UnregisterRule(name);
        }

        protected bool ValidateCode(string code, ref SyntaxTree outTree, string filePath = "")
        {
            var isValid = false;
            ParameterizedThreadStart exceptionWrapper = (object obj) =>
            {
                ThreadStart wrapped = (ThreadStart)obj;
                try
                {
                    wrapped.Invoke();
                }
                catch (Exception ex)
                {
                    isValid = false;
                    m_failures.Add(new Failure(filePath, ExceptionRule, null, ex.ToString(), null));
                }
            }

            ;
            var codeBuilder = new StringBuilder(code);
            TransformCode(codeBuilder);
            code = codeBuilder.ToString();
            SyntaxTree tree = null;
            var parserThread = new Thread(exceptionWrapper);
            parserThread.Start((ThreadStart)(() =>
            {
                tree = m_parser.Parse(code);
            }));
            if (!parserThread.Join(ParserTimeout))
            {
                throw new TimeoutException("Parsing timed out.  Current timeout is " + ParserTimeout / 1000 + " seconds.");
            }

            outTree = tree;
            if (outTree != null)
            {
                var treeValidatorThread = new Thread(exceptionWrapper);
                treeValidatorThread.Start((ThreadStart)(() =>
                {
                    isValid = m_treeValidator.Validate(tree, code, filePath, m_failures);
                }));
                if (!treeValidatorThread.Join(TreeValidatorTimeout))
                {
                    throw new TimeoutException("Tree validation timed out.  Current timeout is " + TreeValidatorTimeout / 1000 + " seconds.");
                }
            }

            return isValid;
        }

        public virtual bool ValidateFile(string filePath)
        {
            SyntaxTree tree = null;
            return ValidateCode(File.ReadAllText(filePath), ref tree, filePath);
        }

        public bool ValidateAllFiles(string dirPath)
        {
            var isValid = true;
            var dirHelper = new DirectoryHelper(dirPath);
            dirHelper.FileFound += (string filePath) =>
            {
                FileFound(filePath);
                isValid = isValid & ValidateFile(filePath);
            }

            ;
            dirHelper.IterateFiles(m_searchPatterns);
            return isValid;
        }

        public bool ValidateAllFiles(IEnumerable<string> fileList)
        {
            var isValid = true;
            foreach (var f in fileList)
            {
                FileFound(f);
                isValid = isValid & ValidateFile(f);
            }

            return isValid;
        }
    }
}
