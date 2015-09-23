// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using static Microsoft.VisualStudio.Testing.ProjectTreeProvider;
using static Microsoft.VisualStudio.Testing.Tokenizer;

namespace Microsoft.VisualStudio.Testing
{
    internal class ProjectTreeParser
    {
        private static readonly ImmutableArray<TokenType> Delimitors = ImmutableArray.Create(TokenType.Comma, TokenType.LeftParenthesis, TokenType.RightParenthesis, TokenType.WhiteSpace);
        private readonly Tokenizer _tokenizer;

        public ProjectTreeParser(string value)
        {
            Requires.NotNullOrEmpty(value, nameof(value));

            _tokenizer = new Tokenizer(new StringReader(value), Delimitors);
        }

        public IProjectTree Parse()
        {
            MutableProjectTree tree = new MutableProjectTree();

            ReadProjectItem(tree);

            return tree;
        }

        private void ReadProjectItem(MutableProjectTree tree)
        {   // Parse "Root (visibility: visible, capabilities: {ProjectRoot}), FilePath: "C:\My Project\MyFile.txt"

            ReadCaption(tree);
            ReadProperties(tree);
            ReadFilePath(tree);
        }

        private void ReadCaption(MutableProjectTree tree)
        {
            Tokenizer tokenizer = Tokenizer(Delimiters.Caption);

            tree.Caption = tokenizer.ReadIdentifier(IdentifierParseOptions.Required);
        }

        private void ReadProperties(MutableProjectTree tree)
        {   // Parses "(visibility: visible, capabilities: {ProjectRoot})"

            // Properties section is optional
            if (!_tokenizer.SkipIf(TokenType.LeftParenthesis))
                return;

            // Empty properties
            if (_tokenizer.SkipIf(TokenType.RightParenthesis))
                return;

            ReadProperty(tree);

            while (_tokenizer.SkipIf(TokenType.Comma))
            {
                _tokenizer.Skip(TokenType.WhiteSpace);
                ReadProperty(tree);
            }

            _tokenizer.Skip(TokenType.RightParenthesis);
        }

        private void ReadProperty(MutableProjectTree tree)
        {
            string propertyName = ReadPropertyName();

            switch (propertyName)
            {
                case "visibility":
                    ReadVisibility(tree);
                    break;

                case "capabilities":
                    ReadCapabilities(tree);
                    break;

                default:
                    throw _tokenizer.FormatException(ProjectTreeFormatError.UnrecognizedPropertyName, "Expected 'visibility' or 'capabilities', but encountered '{propertyName}'.");
            }
        }

        private void ReadVisibility(MutableProjectTree tree)
        {   // Parse 'visible|invisible' in 'visibility:visible|visibility:invisible"

            Tokenizer tokenizer = Tokenizer(Delimiters.PropertyValue);

            string visibility = tokenizer.ReadIdentifier(IdentifierParseOptions.Required);

            switch (visibility)
            {
                case "visible":
                    tree.Visible = true;
                    break;
                case "invisible":
                    tree.Visible = false;
                    break;

                default:
                    throw _tokenizer.FormatException(ProjectTreeFormatError.UnrecognizedPropertyValue, "Expected 'visible' or 'invisible', but encountered '{visibility}'.");
            }
        }

        private void ReadCapabilities(MutableProjectTree tree)
        {   // Parse '{ProjectRoot Folder}'

            Tokenizer tokenizer = Tokenizer(Delimiters.BracedPropertyValueBlock);
            tokenizer.Skip(TokenType.LeftBrace);

            // Empty capabilities
            if (tokenizer.SkipIf(TokenType.RightBrace))
                return;

            do
            {
                ReadCapability(tree);
            }
            while (tokenizer.SkipIf(TokenType.WhiteSpace));
            tokenizer.Skip(TokenType.RightBrace);
        }

        private void ReadCapability(MutableProjectTree tree)
        {   // Parses 'AppDesigner' in '{AppDesigner Folder}'

            Tokenizer tokenizer = Tokenizer(Delimiters.BracedPropertyValue);

            string capability = tokenizer.ReadIdentifier(IdentifierParseOptions.Required);
            tree.Capabilities.Add(capability);
        }

        private void ReadFilePath(MutableProjectTree tree)
        {   // Parses 'FilePath: "C:\Temp\Foo"'

            // FilePath section is optional
            if (_tokenizer.SkipIf(TokenType.Comma))
            {
                _tokenizer.Skip(TokenType.WhiteSpace);

                ReadFilePathPropertyName();

                tree.FilePath = ReadQuotedPropertyValue();
            }
        }

        private void ReadFilePathPropertyName()
        {
            string filePath = ReadPropertyName();
            if (!StringComparer.Ordinal.Equals(filePath, "FilePath"))
                throw _tokenizer.FormatException(ProjectTreeFormatError.UnrecognizedPropertyName, $"Expected 'FilePath', but encountered '{filePath}'.");
        }

        private string ReadQuotedPropertyValue()
        {
            Tokenizer tokenizer = Tokenizer(Delimiters.QuotedPropertyValue);

            tokenizer.Skip(TokenType.Quote);

            string value = tokenizer.ReadIdentifier(IdentifierParseOptions.None);

            tokenizer.Skip(TokenType.Quote);

            return value;
        }

        private string ReadPropertyName()
        {
            Tokenizer tokenizer = Tokenizer(Delimiters.PropertyName);

            string propertyName = tokenizer.ReadIdentifier(IdentifierParseOptions.Required);
            tokenizer.Skip(TokenType.Colon);
            tokenizer.Skip(TokenType.WhiteSpace);

            return propertyName;
        }

        private Tokenizer Tokenizer(ImmutableArray<TokenType> delimiters)
        {
            return new Tokenizer(_tokenizer.UnderlyingReader, delimiters);
        }
    }
}
