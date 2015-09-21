// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System.Collections;
using System.Collections.Immutable;
using System.Text;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using static Microsoft.VisualStudio.Testing.ProjectTreeProvider;
using static Microsoft.VisualStudio.Testing.Tokenizer;

namespace Microsoft.VisualStudio.Testing
{
    internal class ProjectTreeParser
    {
        private static readonly ImmutableArray<TokenType> Delimitors = ImmutableArray.Create(TokenType.CarriageReturn);
        private static readonly ImmutableArray<TokenType> CaptionDelimitors = ImmutableArray.Create(TokenType.LeftParenthesis);

        private readonly Tokenizer _tokenizer;

        public ProjectTreeParser(string value)
        {
            Requires.NotNullOrEmpty(value, nameof(value));

            _tokenizer = new Tokenizer(new StringReader(value), Delimitors);
        }

        public IProjectTree Parse()
        {
            while (TryReadProjectItemRow())
            {

            }

        }

        private MutableProjectTree ReadProjectItemRow(ref MutableProjectTree parent)
        {   // Parse "Root (capabilities: {ProjectRoot})"

            string caption = _tokenizer.ReadId();




        }
        
        private void ReadProjectItemProperties()
        {   // Parses "(capabilities: {ProjectRoot})"

            _tokenizer.Skip(TokenType.LeftParenthesis);

            string capabilities = _tokenizer.ReadId();

            _tokenizer.Skip(TokenType.Colon);
            _tokenizer.Skip(TokenType.LeftBrace);

            

            _tokenizer.Skip(TokenType.RightBrace);
            _tokenizer.Skip(TokenType.RightParenthesis);
        }
    }
}
