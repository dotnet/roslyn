// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal sealed class SpanEditHandlerBuilder
{
    private static readonly Func<string, IEnumerable<SyntaxToken>> DefaultTokenizer = SpanEditHandler.NoTokenizer;
    private static readonly SpanEditHandler DefaultEditHandler = SpanEditHandler.GetDefault(AcceptedCharactersInternal.Any);

    private readonly Func<string, IEnumerable<SyntaxToken>>? _defaultLanguageTokenizer;
    private readonly SpanEditHandler? _defaultLanguageEditHandler;

    private SpanEditHandler? _currentInstance;
    private Func<string, IEnumerable<SyntaxToken>>? _tokenizer;
    private Func<AcceptedCharactersInternal, Func<string, IEnumerable<SyntaxToken>>, SpanEditHandler>? _factory;

    public SpanEditHandlerBuilder(Func<string, IEnumerable<SyntaxToken>>? defaultLanguageTokenizer)
    {
        _defaultLanguageTokenizer = defaultLanguageTokenizer;
        if (_defaultLanguageTokenizer is not null)
        {
            _defaultLanguageEditHandler = SpanEditHandler.CreateDefault(_defaultLanguageTokenizer, AcceptedCharactersInternal.Any);
        }
        Reset();
    }

    public Func<string, IEnumerable<SyntaxToken>>? Tokenizer
    {
        get => _tokenizer;
        set
        {
            if (_tokenizer != value)
            {
                _tokenizer = value;
                _currentInstance = null;
            }
        }
    }

    public Func<AcceptedCharactersInternal, Func<string, IEnumerable<SyntaxToken>>, SpanEditHandler>? Factory
    {
        get => _factory;
        set
        {
            if (_factory != value)
            {
                _factory = value;
                _currentInstance = null;
            }
        }
    }

    public SpanEditHandler Build(AcceptedCharactersInternal acceptedCharacters)
    {
        Debug.Assert(_currentInstance is null || _currentInstance.Tokenizer == Tokenizer);
        return _currentInstance ??= CreateHandler();

        SpanEditHandler CreateHandler()
        {
            if (Factory is not null)
            {
                return Factory(acceptedCharacters, Tokenizer ?? DefaultTokenizer);
            }

            if (acceptedCharacters == AcceptedCharactersInternal.Any)
            {
                if (Tokenizer is null)
                {
                    return DefaultEditHandler;
                }
                else if (Tokenizer == _defaultLanguageTokenizer)
                {
                    Debug.Assert(_defaultLanguageEditHandler is not null);
                    return _defaultLanguageEditHandler!;
                }
            }

            return new SpanEditHandler
            {
                AcceptedCharacters = acceptedCharacters,
                Tokenizer = Tokenizer ?? DefaultTokenizer
            };
        }
    }

    public void Reset()
    {
        Tokenizer = null;
        Factory = null;
        _currentInstance = null;
    }
}
