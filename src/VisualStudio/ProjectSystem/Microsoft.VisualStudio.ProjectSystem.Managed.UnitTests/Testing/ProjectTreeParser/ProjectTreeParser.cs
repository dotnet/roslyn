// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using static Microsoft.VisualStudio.Testing.ProjectTreeProvider;
using static Microsoft.VisualStudio.Testing.Tokenizer;

namespace Microsoft.VisualStudio.Testing
{
    internal class ProjectTreeParser
    {
        private static readonly ImmutableArray<TokenType> Delimitors = ImmutableArray.Create(TokenType.CarriageReturn, TokenType.LeftParenthesis, TokenType.RightParenthesis, TokenType.LeftBrace, TokenType.RightBrace, TokenType.Colon, TokenType.Comma, TokenType.Quote);
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
            tree.Caption = _tokenizer.ReadLiteral(LiteralParseOptions.AllowWhiteSpace | LiteralParseOptions.Required);
        }

        private void ReadProperties(MutableProjectTree tree)
        {   // Parses "(visibility: visible, capabilities: {ProjectRoot})"

            // Properties section is optional
            if (!_tokenizer.SkipIf(TokenType.LeftParenthesis))
                return;

            if (_tokenizer.Peek() != TokenType.RightParenthesis)
            {
                do
                {
                    ReadProperty(tree);                    
                }
                while (_tokenizer.SkipIf(TokenType.Comma) && _tokenizer.SkipIf(TokenType.WhiteSpace));
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
                    throw new FormatException($"Unrecognized property name: {propertyName}");
            }
        }

        private void ReadVisibility(MutableProjectTree tree)
        {   // Parse 'visible|invisible' in 'visibility:visible|visibility:invisible"

            string visibility = _tokenizer.ReadLiteral(LiteralParseOptions.Required);

            switch (visibility)
            {
                case "visible":
                    tree.Visible = true;
                    break;
                case "invisible":
                    tree.Visible = false;
                    break;

                default:
                    throw new FormatException($"Unrecognized visiblity: '{visibility}'");
            }
        }

        private void ReadCapabilities(MutableProjectTree tree)
        {   // Parse '{ProjectRoot Folder}'

            _tokenizer.Skip(TokenType.LeftBrace);

            if (_tokenizer.Peek() != TokenType.RightBrace)
            {
                do
                {
                    string capability = _tokenizer.ReadLiteral(LiteralParseOptions.Required);
                    tree.Capabilities.Add(capability);
                }
                while (_tokenizer.SkipIf(TokenType.WhiteSpace));
            }

            _tokenizer.Skip(TokenType.RightBrace);
        }

        private void ReadFilePath(MutableProjectTree tree)
        {   // Parses 'FilePath:"C:\Temp\Foo"'

            // FilePath section is optional
            if (!_tokenizer.SkipIf(TokenType.Comma))
                return;

            _tokenizer.Skip(TokenType.WhiteSpace);

            string filePath = ReadPropertyName();
            if (!StringComparer.Ordinal.Equals(filePath, "FilePath"))
                throw new FormatException($"Unrecognized property name: {filePath}");

            _tokenizer.Skip(TokenType.Quote);

            tree.FilePath = _tokenizer.ReadLiteral(LiteralParseOptions.AllowWhiteSpace);

            _tokenizer.Skip(TokenType.Quote);
        }

        private string ReadPropertyName()
        {
            string propertyName = _tokenizer.ReadLiteral(LiteralParseOptions.Required);
            _tokenizer.Skip(TokenType.Colon);
            _tokenizer.Skip(TokenType.WhiteSpace);

            return propertyName;
        }
    }
}
