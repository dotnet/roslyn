// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
#pragma warning disable CA1200 // Avoid using cref tags with a prefix - The prefix is required since this file is referenced in projects that can't access syntax nodes
    // DO NOT CHANGE NUMBERS ASSIGNED TO EXISTING KINDS OR YOU WILL BREAK BINARY COMPATIBILITY
    public enum SyntaxKind : ushort
    {
        None = 0,
        List = GreenNode.ListKind,

        // punctuation
        /// <summary>Represents <c>~</c> token.</summary>
        TildeToken = 8193,
        /// <summary>Represents <c>!</c> token.</summary>
        ExclamationToken = 8194,
        /// <summary>Represents <c>$</c> token.</summary>
        /// <remarks>This is a debugger special punctuation and not related to string interpolation.</remarks>
        DollarToken = 8195,
        /// <summary>Represents <c>%</c> token.</summary>
        PercentToken = 8196,
        /// <summary>Represents <c>^</c> token.</summary>
        CaretToken = 8197,
        /// <summary>Represents <c>&amp;</c> token.</summary>
        AmpersandToken = 8198,
        /// <summary>Represents <c>*</c> token.</summary>
        AsteriskToken = 8199,
        /// <summary>Represents <c>(</c> token.</summary>
        OpenParenToken = 8200,
        /// <summary>Represents <c>)</c> token.</summary>
        CloseParenToken = 8201,
        /// <summary>Represents <c>-</c> token.</summary>
        MinusToken = 8202,
        /// <summary>Represents <c>+</c> token.</summary>
        PlusToken = 8203,
        /// <summary>Represents <c>=</c> token.</summary>
        EqualsToken = 8204,
        /// <summary>Represents <c>{</c> token.</summary>
        OpenBraceToken = 8205,
        /// <summary>Represents <c>}</c> token.</summary>
        CloseBraceToken = 8206,
        /// <summary>Represents <c>[</c> token.</summary>
        OpenBracketToken = 8207,
        /// <summary>Represents <c>]</c> token.</summary>
        CloseBracketToken = 8208,
        /// <summary>Represents <c>|</c> token.</summary>
        BarToken = 8209,
        /// <summary>Represents <c>\</c> token.</summary>
        BackslashToken = 8210,
        /// <summary>Represents <c>:</c> token.</summary>
        ColonToken = 8211,
        /// <summary>Represents <c>;</c> token.</summary>
        SemicolonToken = 8212,
        /// <summary>Represents <c>"</c> token.</summary>
        DoubleQuoteToken = 8213,
        /// <summary>Represents <c>'</c> token.</summary>
        SingleQuoteToken = 8214,
        /// <summary>Represents <c>&lt;</c> token.</summary>
        LessThanToken = 8215,
        /// <summary>Represents <c>,</c> token.</summary>
        CommaToken = 8216,
        /// <summary>Represents <c>&gt;</c> token.</summary>
        GreaterThanToken = 8217,
        /// <summary>Represents <c>.</c> token.</summary>
        DotToken = 8218,
        /// <summary>Represents <c>?</c> token.</summary>
        QuestionToken = 8219,
        /// <summary>Represents <c>#</c> token.</summary>
        HashToken = 8220,
        /// <summary>Represents <c>/</c> token.</summary>
        SlashToken = 8221,
        /// <summary>Represents <c>..</c> token.</summary>
        DotDotToken = 8222,

        // additional xml tokens
        /// <summary>Represents <c>/&gt;</c> token.</summary>
        SlashGreaterThanToken = 8232, // xml empty element end
        /// <summary>Represents <c>&lt;/</c> token.</summary>
        LessThanSlashToken = 8233, // element end tag start token
        /// <summary>Represents <c>&lt;!--</c> token.</summary>
        XmlCommentStartToken = 8234, // <!--
        /// <summary>Represents <c>--&gt;</c> token.</summary>
        XmlCommentEndToken = 8235, // -->
        /// <summary>Represents <c>&lt;![CDATA[</c> token.</summary>
        XmlCDataStartToken = 8236, // <![CDATA[
        /// <summary>Represents <c>]]&gt;</c> token.</summary>
        XmlCDataEndToken = 8237, // ]]>
        /// <summary>Represents <c>&lt;?</c> token.</summary>
        XmlProcessingInstructionStartToken = 8238, // <?
        /// <summary>Represents <c>?&gt;</c> token.</summary>
        XmlProcessingInstructionEndToken = 8239, // ?>

        // compound punctuation
        /// <summary>Represents <c>||</c> token.</summary>
        BarBarToken = 8260,
        /// <summary>Represents <c>&amp;&amp;</c> token.</summary>
        AmpersandAmpersandToken = 8261,
        /// <summary>Represents <c>--</c> token.</summary>
        MinusMinusToken = 8262,
        /// <summary>Represents <c>++</c> token.</summary>
        PlusPlusToken = 8263,
        /// <summary>Represents <c>::</c> token.</summary>
        ColonColonToken = 8264,
        /// <summary>Represents <c>??</c> token.</summary>
        QuestionQuestionToken = 8265,
        /// <summary>Represents <c>-&gt;</c> token.</summary>
        MinusGreaterThanToken = 8266,
        /// <summary>Represents <c>!=</c> token.</summary>
        ExclamationEqualsToken = 8267,
        /// <summary>Represents <c>==</c> token.</summary>
        EqualsEqualsToken = 8268,
        /// <summary>Represents <c>=&gt;</c> token.</summary>
        EqualsGreaterThanToken = 8269,
        /// <summary>Represents <c>&lt;=</c> token.</summary>
        LessThanEqualsToken = 8270,
        /// <summary>Represents <c>&lt;&lt;</c> token.</summary>
        LessThanLessThanToken = 8271,
        /// <summary>Represents <c>&lt;&lt;=</c> token.</summary>
        LessThanLessThanEqualsToken = 8272,
        /// <summary>Represents <c>&gt;=</c> token.</summary>
        GreaterThanEqualsToken = 8273,
        /// <summary>Represents <c>&gt;&gt;</c> token.</summary>
        GreaterThanGreaterThanToken = 8274,
        /// <summary>Represents <c>&gt;&gt;=</c> token.</summary>
        GreaterThanGreaterThanEqualsToken = 8275,
        /// <summary>Represents <c>/=</c> token.</summary>
        SlashEqualsToken = 8276,
        /// <summary>Represents <c>*=</c> token.</summary>
        AsteriskEqualsToken = 8277,
        /// <summary>Represents <c>|=</c> token.</summary>
        BarEqualsToken = 8278,
        /// <summary>Represents <c>&amp;=</c> token.</summary>
        AmpersandEqualsToken = 8279,
        /// <summary>Represents <c>+=</c> token.</summary>
        PlusEqualsToken = 8280,
        /// <summary>Represents <c>-=</c> token.</summary>
        MinusEqualsToken = 8281,
        /// <summary>Represents <c>^=</c> token.</summary>
        CaretEqualsToken = 8282,
        /// <summary>Represents <c>%=</c> token.</summary>
        PercentEqualsToken = 8283,
        /// <summary>Represents <c>??=</c> token.</summary>
        QuestionQuestionEqualsToken = 8284,
        /// <summary>Represents <c>!!</c> token.</summary>
        ExclamationExclamationToken = 8285,

        // Keywords
        /// <summary>Represents <see langword="bool"/>.</summary>
        BoolKeyword = 8304,
        /// <summary>Represents <see langword="byte"/>.</summary>
        ByteKeyword = 8305,
        /// <summary>Represents <see langword="sbyte"/>.</summary>
        SByteKeyword = 8306,
        /// <summary>Represents <see langword="short"/>.</summary>
        ShortKeyword = 8307,
        /// <summary>Represents <see langword="ushort"/>.</summary>
        UShortKeyword = 8308,
        /// <summary>Represents <see langword="int"/>.</summary>
        IntKeyword = 8309,
        /// <summary>Represents <see langword="uint"/>.</summary>
        UIntKeyword = 8310,
        /// <summary>Represents <see langword="long"/>.</summary>
        LongKeyword = 8311,
        /// <summary>Represents <see langword="ulong"/>.</summary>
        ULongKeyword = 8312,
        /// <summary>Represents <see langword="double"/>.</summary>
        DoubleKeyword = 8313,
        /// <summary>Represents <see langword="float"/>.</summary>
        FloatKeyword = 8314,
        /// <summary>Represents <see langword="decimal"/>.</summary>
        DecimalKeyword = 8315,
        /// <summary>Represents <see langword="string"/>.</summary>
        StringKeyword = 8316,
        /// <summary>Represents <see langword="char"/>.</summary>
        CharKeyword = 8317,
        /// <summary>Represents <see langword="void"/>.</summary>
        VoidKeyword = 8318,
        /// <summary>Represents <see langword="object"/>.</summary>
        ObjectKeyword = 8319,
        /// <summary>Represents <see langword="typeof"/>.</summary>
        TypeOfKeyword = 8320,
        /// <summary>Represents <see langword="sizeof"/>.</summary>
        SizeOfKeyword = 8321,
        /// <summary>Represents <see langword="null"/>.</summary>
        NullKeyword = 8322,
        /// <summary>Represents <see langword="true"/>.</summary>
        TrueKeyword = 8323,
        /// <summary>Represents <see langword="false"/>.</summary>
        FalseKeyword = 8324,
        /// <summary>Represents <see langword="if"/>.</summary>
        IfKeyword = 8325,
        /// <summary>Represents <see langword="else"/>.</summary>
        ElseKeyword = 8326,
        /// <summary>Represents <see langword="while"/>.</summary>
        WhileKeyword = 8327,
        /// <summary>Represents <see langword="for"/>.</summary>
        ForKeyword = 8328,
        /// <summary>Represents <see langword="foreach"/>.</summary>
        ForEachKeyword = 8329,
        /// <summary>Represents <see langword="do"/>.</summary>
        DoKeyword = 8330,
        /// <summary>Represents <see langword="switch"/>.</summary>
        SwitchKeyword = 8331,
        /// <summary>Represents <see langword="case"/>.</summary>
        CaseKeyword = 8332,
        /// <summary>Represents <see langword="default"/>.</summary>
        DefaultKeyword = 8333,
        /// <summary>Represents <see langword="try"/>.</summary>
        TryKeyword = 8334,
        /// <summary>Represents <see langword="catch"/>.</summary>
        CatchKeyword = 8335,
        /// <summary>Represents <see langword="finally"/>.</summary>
        FinallyKeyword = 8336,
        /// <summary>Represents <see langword="lock"/>.</summary>
        LockKeyword = 8337,
        /// <summary>Represents <see langword="goto"/>.</summary>
        GotoKeyword = 8338,
        /// <summary>Represents <see langword="break"/>.</summary>
        BreakKeyword = 8339,
        /// <summary>Represents <see langword="continue"/>.</summary>
        ContinueKeyword = 8340,
        /// <summary>Represents <see langword="return"/>.</summary>
        ReturnKeyword = 8341,
        /// <summary>Represents <see langword="throw"/>.</summary>
        ThrowKeyword = 8342,
        /// <summary>Represents <see langword="public"/>.</summary>
        PublicKeyword = 8343,
        /// <summary>Represents <see langword="private"/>.</summary>
        PrivateKeyword = 8344,
        /// <summary>Represents <see langword="internal"/>.</summary>
        InternalKeyword = 8345,
        /// <summary>Represents <see langword="protected"/>.</summary>
        ProtectedKeyword = 8346,
        /// <summary>Represents <see langword="static"/>.</summary>
        StaticKeyword = 8347,
        /// <summary>Represents <see langword="readonly"/>.</summary>
        ReadOnlyKeyword = 8348,
        /// <summary>Represents <see langword="sealed"/>.</summary>
        SealedKeyword = 8349,
        /// <summary>Represents <see langword="const"/>.</summary>
        ConstKeyword = 8350,
        /// <summary>Represents <see langword="fixed"/>.</summary>
        FixedKeyword = 8351,
        /// <summary>Represents <see langword="stackalloc"/>.</summary>
        StackAllocKeyword = 8352,
        /// <summary>Represents <see langword="volatile"/>.</summary>
        VolatileKeyword = 8353,
        /// <summary>Represents <see langword="new"/>.</summary>
        NewKeyword = 8354,
        /// <summary>Represents <see langword="override"/>.</summary>
        OverrideKeyword = 8355,
        /// <summary>Represents <see langword="abstract"/>.</summary>
        AbstractKeyword = 8356,
        /// <summary>Represents <see langword="virtual"/>.</summary>
        VirtualKeyword = 8357,
        /// <summary>Represents <see langword="event"/>.</summary>
        EventKeyword = 8358,
        /// <summary>Represents <see langword="extern"/>.</summary>
        ExternKeyword = 8359,
        /// <summary>Represents <see langword="ref"/>.</summary>
        RefKeyword = 8360,
        /// <summary>Represents <see langword="out"/>.</summary>
        OutKeyword = 8361,
        /// <summary>Represents <see langword="in"/>.</summary>
        InKeyword = 8362,
        /// <summary>Represents <see langword="is"/>.</summary>
        IsKeyword = 8363,
        /// <summary>Represents <see langword="as"/>.</summary>
        AsKeyword = 8364,
        /// <summary>Represents <see langword="params"/>.</summary>
        ParamsKeyword = 8365,
        /// <summary>Represents <see langword="__arglist"/>.</summary>
        ArgListKeyword = 8366,
        /// <summary>Represents <see langword="__makeref"/>.</summary>
        MakeRefKeyword = 8367,
        /// <summary>Represents <see langword="__reftype"/>.</summary>
        RefTypeKeyword = 8368,
        /// <summary>Represents <see langword="__refvalue"/>.</summary>
        RefValueKeyword = 8369,
        /// <summary>Represents <see langword="this"/>.</summary>
        ThisKeyword = 8370,
        /// <summary>Represents <see langword="base"/>.</summary>
        BaseKeyword = 8371,
        /// <summary>Represents <see langword="namespace"/>.</summary>
        NamespaceKeyword = 8372,
        /// <summary>Represents <see langword="using"/>.</summary>
        UsingKeyword = 8373,
        /// <summary>Represents <see langword="class"/>.</summary>
        ClassKeyword = 8374,
        /// <summary>Represents <see langword="struct"/>.</summary>
        StructKeyword = 8375,
        /// <summary>Represents <see langword="interface"/>.</summary>
        InterfaceKeyword = 8376,
        /// <summary>Represents <see langword="enum"/>.</summary>
        EnumKeyword = 8377,
        /// <summary>Represents <see langword="delegate"/>.</summary>
        DelegateKeyword = 8378,
        /// <summary>Represents <see langword="checked"/>.</summary>
        CheckedKeyword = 8379,
        /// <summary>Represents <see langword="unchecked"/>.</summary>
        UncheckedKeyword = 8380,
        /// <summary>Represents <see langword="unsafe"/>.</summary>
        UnsafeKeyword = 8381,
        /// <summary>Represents <see langword="operator"/>.</summary>
        OperatorKeyword = 8382,
        /// <summary>Represents <see langword="explicit"/>.</summary>
        ExplicitKeyword = 8383,
        /// <summary>Represents <see langword="implicit"/>.</summary>
        ImplicitKeyword = 8384,

        // contextual keywords
        /// <summary>Represents <see langword="yield"/>.</summary>
        YieldKeyword = 8405,
        /// <summary>Represents <see langword="partial"/>.</summary>
        PartialKeyword = 8406,
        /// <summary>Represents <see langword="alias"/>.</summary>
        AliasKeyword = 8407,
        /// <summary>Represents <see langword="global"/>.</summary>
        GlobalKeyword = 8408,
        /// <summary>Represents <see langword="assembly"/>.</summary>
        AssemblyKeyword = 8409,
        /// <summary>Represents <see langword="module"/>.</summary>
        ModuleKeyword = 8410,
        /// <summary>Represents <see langword="type"/>.</summary>
        TypeKeyword = 8411,
        /// <summary>Represents <see langword="field"/>.</summary>
        FieldKeyword = 8412,
        /// <summary>Represents <see langword="method"/>.</summary>
        MethodKeyword = 8413,
        /// <summary>Represents <see langword="param"/>.</summary>
        ParamKeyword = 8414,
        /// <summary>Represents <see langword="property"/>.</summary>
        PropertyKeyword = 8415,
        /// <summary>Represents <see langword="typevar"/>.</summary>
        TypeVarKeyword = 8416,
        /// <summary>Represents <see langword="get"/>.</summary>
        GetKeyword = 8417,
        /// <summary>Represents <see langword="set"/>.</summary>
        SetKeyword = 8418,
        /// <summary>Represents <see langword="add"/>.</summary>
        AddKeyword = 8419,
        /// <summary>Represents <see langword="remove"/>.</summary>
        RemoveKeyword = 8420,
        /// <summary>Represents <see langword="where"/>.</summary>
        WhereKeyword = 8421,
        /// <summary>Represents <see langword="from"/>.</summary>
        FromKeyword = 8422,
        /// <summary>Represents <see langword="group"/>.</summary>
        GroupKeyword = 8423,
        /// <summary>Represents <see langword="join"/>.</summary>
        JoinKeyword = 8424,
        /// <summary>Represents <see langword="into"/>.</summary>
        IntoKeyword = 8425,
        /// <summary>Represents <see langword="let"/>.</summary>
        LetKeyword = 8426,
        /// <summary>Represents <see langword="by"/>.</summary>
        ByKeyword = 8427,
        /// <summary>Represents <see langword="select"/>.</summary>
        SelectKeyword = 8428,
        /// <summary>Represents <see langword="orderby"/>.</summary>
        OrderByKeyword = 8429,
        /// <summary>Represents <see langword="on"/>.</summary>
        OnKeyword = 8430,
        /// <summary>Represents <see langword="equals"/>.</summary>
        EqualsKeyword = 8431,
        /// <summary>Represents <see langword="ascending"/>.</summary>
        AscendingKeyword = 8432,
        /// <summary>Represents <see langword="descending"/>.</summary>
        DescendingKeyword = 8433,
        /// <summary>Represents <see langword="nameof"/>.</summary>
        NameOfKeyword = 8434,
        /// <summary>Represents <see langword="async"/>.</summary>
        AsyncKeyword = 8435,
        /// <summary>Represents <see langword="await"/>.</summary>
        AwaitKeyword = 8436,
        /// <summary>Represents <see langword="when"/>.</summary>
        WhenKeyword = 8437,
        /// <summary>Represents <see langword="or"/>.</summary>
        OrKeyword = 8438,
        /// <summary>Represents <see langword="and"/>.</summary>
        AndKeyword = 8439,
        /// <summary>Represents <see langword="not"/>.</summary>
        NotKeyword = 8440,

        // Don't use 8441. It corresponds to a deleted kind (DataKeyword) that was previously shipped.

        /// <summary>Represents <see langword="with"/>.</summary>
        WithKeyword = 8442,
        /// <summary>Represents <see langword="init"/>.</summary>
        InitKeyword = 8443,
        /// <summary>Represents <see langword="record"/>.</summary>
        RecordKeyword = 8444,
        /// <summary>Represents <see langword="managed"/>.</summary>
        ManagedKeyword = 8445,
        /// <summary>Represents <see langword="unmanaged"/>.</summary>
        UnmanagedKeyword = 8446,

        // when adding a contextual keyword following functions must be adapted:
        // <see cref="SyntaxFacts.GetContextualKeywordKinds"/>
        // <see cref="SyntaxFacts.IsContextualKeyword(SyntaxKind)"/>
        // <see cref="SyntaxFacts.GetContextualKeywordKind(string)"/>
        // <see cref="SyntaxFacts.GetText(SyntaxKind)"/>

        // keywords with an enum value less than ElifKeyword are considered i.a. contextual keywords
        // additional preprocessor keywords
        /// <summary>Represents <see langword="elif"/>.</summary>
        ElifKeyword = 8467,
        /// <summary>Represents <see langword="endif"/>.</summary>
        EndIfKeyword = 8468,
        /// <summary>Represents <see langword="region"/>.</summary>
        RegionKeyword = 8469,
        /// <summary>Represents <see langword="endregion"/>.</summary>
        EndRegionKeyword = 8470,
        /// <summary>Represents <see langword="define"/>.</summary>
        DefineKeyword = 8471,
        /// <summary>Represents <see langword="undef"/>.</summary>
        UndefKeyword = 8472,
        /// <summary>Represents <see langword="warning"/>.</summary>
        WarningKeyword = 8473,
        /// <summary>Represents <see langword="error"/>.</summary>
        ErrorKeyword = 8474,
        /// <summary>Represents <see langword="line"/>.</summary>
        LineKeyword = 8475,
        /// <summary>Represents <see langword="pragma"/>.</summary>
        PragmaKeyword = 8476,
        /// <summary>Represents <see langword="hidden"/>.</summary>
        HiddenKeyword = 8477,
        /// <summary>Represents <see langword="checksum"/>.</summary>
        ChecksumKeyword = 8478,
        /// <summary>Represents <see langword="disable"/>.</summary>
        DisableKeyword = 8479,
        /// <summary>Represents <see langword="restore"/>.</summary>
        RestoreKeyword = 8480,
        /// <summary>Represents <see langword="r"/>.</summary>
        ReferenceKeyword = 8481,

        /// <summary>Represents <c>$"</c> token.</summary>
        InterpolatedStringStartToken = 8482,            // $"
        /// <summary>Represents <c>"</c> token that is closing <c>$"</c>.</summary>
        InterpolatedStringEndToken = 8483,              // "
        /// <summary>Represents <c>$@</c> or <c>@$</c> token.</summary>
        InterpolatedVerbatimStringStartToken = 8484,    // $@" or @$"

        // additional preprocessor keywords (continued)
        /// <summary>Represents <see langword="load"/>.</summary>
        LoadKeyword = 8485,
        /// <summary>Represents <see langword="nullable"/>.</summary>
        NullableKeyword = 8486,
        /// <summary>Represents <see langword="enable"/>.</summary>
        EnableKeyword = 8487,

        // targets for #nullable directive
        /// <summary>Represents <see langword="warnings"/>.</summary>
        WarningsKeyword = 8488,
        /// <summary>Represents <see langword="annotations"/>.</summary>
        AnnotationsKeyword = 8489,

        // Other
        /// <summary>Represents <see langword="var"/>.</summary>
        VarKeyword = 8490,
        /// <summary>Represents <c>_</c> token.</summary>
        UnderscoreToken = 8491,
        /// <summary>Represents that nothing was specified as a type argument.</summary>
        /// <remarks>For example <c>Dictionary&lt;,&gt;</c> which has <see cref="OmittedTypeArgumentToken"/> as a child of <see cref="T:Microsoft.CodeAnalysis.CSharp.Syntax.OmittedTypeArgumentSyntax"/> before and after the <see cref="CommaToken"/>.</remarks>
        OmittedTypeArgumentToken = 8492,
        /// <summary>Represents that nothing was specified as an array size.</summary>
        /// <remarks>For example <c>int[,]</c> which has <see cref="OmittedArraySizeExpressionToken"/> as a child of <see cref="T:Microsoft.CodeAnalysis.CSharp.Syntax.OmittedArraySizeExpressionSyntax"/> before and after the <see cref="CommaToken"/>.</remarks>
        OmittedArraySizeExpressionToken = 8493,
        /// <summary>Represents a token that comes after the end of a directive such as <c>#endif</c>.</summary>
        EndOfDirectiveToken = 8494,
        /// <summary>Represents the end of a triple-slash documentation comment.</summary>
        EndOfDocumentationCommentToken = 8495,
        /// <summary>Represents the end of a file.</summary>
        EndOfFileToken = 8496, //NB: this is assumed to be the last textless token

        // tokens with text
        BadToken = 8507,
        IdentifierToken = 8508,
        NumericLiteralToken = 8509,
        CharacterLiteralToken = 8510,
        StringLiteralToken = 8511,
        XmlEntityLiteralToken = 8512,  // &lt; &gt; &quot; &amp; &apos; or &name; or &#nnnn; or &#xhhhh;
        XmlTextLiteralToken = 8513,    // xml text node text
        XmlTextLiteralNewLineToken = 8514,

        /// <summary>
        /// Token for a whole interpolated string <c>$""" ... { expr } ..."""</c>. This only exists in transient form during parsing.
        /// </summary>
        InterpolatedStringToken = 8515,
        InterpolatedStringTextToken = 8517,             // literal text that is part of an interpolated string

        SingleLineRawStringLiteralToken = 8518,
        MultiLineRawStringLiteralToken = 8519,

        // trivia
        EndOfLineTrivia = 8539,
        WhitespaceTrivia = 8540,
        SingleLineCommentTrivia = 8541,
        MultiLineCommentTrivia = 8542,
        DocumentationCommentExteriorTrivia = 8543,
        SingleLineDocumentationCommentTrivia = 8544,
        MultiLineDocumentationCommentTrivia = 8545,
        DisabledTextTrivia = 8546,
        PreprocessingMessageTrivia = 8547,
        IfDirectiveTrivia = 8548,
        ElifDirectiveTrivia = 8549,
        ElseDirectiveTrivia = 8550,
        EndIfDirectiveTrivia = 8551,
        RegionDirectiveTrivia = 8552,
        EndRegionDirectiveTrivia = 8553,
        DefineDirectiveTrivia = 8554,
        UndefDirectiveTrivia = 8555,
        ErrorDirectiveTrivia = 8556,
        WarningDirectiveTrivia = 8557,
        LineDirectiveTrivia = 8558,
        PragmaWarningDirectiveTrivia = 8559,
        PragmaChecksumDirectiveTrivia = 8560,
        ReferenceDirectiveTrivia = 8561,
        BadDirectiveTrivia = 8562,
        SkippedTokensTrivia = 8563,
        ConflictMarkerTrivia = 8564,

        // xml nodes (for xml doc comment structure)
        XmlElement = 8574,
        XmlElementStartTag = 8575,
        XmlElementEndTag = 8576,
        XmlEmptyElement = 8577,
        XmlTextAttribute = 8578,
        XmlCrefAttribute = 8579,
        XmlNameAttribute = 8580,
        XmlName = 8581,
        XmlPrefix = 8582,
        XmlText = 8583,
        XmlCDataSection = 8584,
        XmlComment = 8585,
        XmlProcessingInstruction = 8586,

        // documentation comment nodes (structure inside DocumentationCommentTrivia)
        TypeCref = 8597,
        QualifiedCref = 8598,
        NameMemberCref = 8599,
        IndexerMemberCref = 8600,
        OperatorMemberCref = 8601,
        ConversionOperatorMemberCref = 8602,
        CrefParameterList = 8603,
        CrefBracketedParameterList = 8604,
        CrefParameter = 8605,

        // names & type-names
        IdentifierName = 8616,
        QualifiedName = 8617,
        GenericName = 8618,
        TypeArgumentList = 8619,
        AliasQualifiedName = 8620,
        PredefinedType = 8621,
        ArrayType = 8622,
        ArrayRankSpecifier = 8623,
        PointerType = 8624,
        NullableType = 8625,
        OmittedTypeArgument = 8626,

        // expressions
        ParenthesizedExpression = 8632,
        ConditionalExpression = 8633,
        InvocationExpression = 8634,
        ElementAccessExpression = 8635,
        ArgumentList = 8636,
        BracketedArgumentList = 8637,
        Argument = 8638,
        NameColon = 8639,
        CastExpression = 8640,
        AnonymousMethodExpression = 8641,
        SimpleLambdaExpression = 8642,
        ParenthesizedLambdaExpression = 8643,
        ObjectInitializerExpression = 8644,
        CollectionInitializerExpression = 8645,
        ArrayInitializerExpression = 8646,
        AnonymousObjectMemberDeclarator = 8647,
        ComplexElementInitializerExpression = 8648,
        ObjectCreationExpression = 8649,
        AnonymousObjectCreationExpression = 8650,
        ArrayCreationExpression = 8651,
        ImplicitArrayCreationExpression = 8652,
        StackAllocArrayCreationExpression = 8653,
        OmittedArraySizeExpression = 8654,
        InterpolatedStringExpression = 8655,
        ImplicitElementAccess = 8656,
        IsPatternExpression = 8657,
        RangeExpression = 8658,
        ImplicitObjectCreationExpression = 8659,

        // binary expressions
        AddExpression = 8668,
        SubtractExpression = 8669,
        MultiplyExpression = 8670,
        DivideExpression = 8671,
        ModuloExpression = 8672,
        LeftShiftExpression = 8673,
        RightShiftExpression = 8674,
        LogicalOrExpression = 8675,
        LogicalAndExpression = 8676,
        BitwiseOrExpression = 8677,
        BitwiseAndExpression = 8678,
        ExclusiveOrExpression = 8679,
        EqualsExpression = 8680,
        NotEqualsExpression = 8681,
        LessThanExpression = 8682,
        LessThanOrEqualExpression = 8683,
        GreaterThanExpression = 8684,
        GreaterThanOrEqualExpression = 8685,
        IsExpression = 8686,
        AsExpression = 8687,
        CoalesceExpression = 8688,
        SimpleMemberAccessExpression = 8689,  // dot access:   a.b
        PointerMemberAccessExpression = 8690,  // arrow access:   a->b
        ConditionalAccessExpression = 8691,    // question mark access:   a?.b , a?[1]

        // binding expressions
        MemberBindingExpression = 8707,
        ElementBindingExpression = 8708,

        // binary assignment expressions
        SimpleAssignmentExpression = 8714,
        AddAssignmentExpression = 8715,
        SubtractAssignmentExpression = 8716,
        MultiplyAssignmentExpression = 8717,
        DivideAssignmentExpression = 8718,
        ModuloAssignmentExpression = 8719,
        AndAssignmentExpression = 8720,
        ExclusiveOrAssignmentExpression = 8721,
        OrAssignmentExpression = 8722,
        LeftShiftAssignmentExpression = 8723,
        RightShiftAssignmentExpression = 8724,
        CoalesceAssignmentExpression = 8725,

        // unary expressions
        UnaryPlusExpression = 8730,
        UnaryMinusExpression = 8731,
        BitwiseNotExpression = 8732,
        LogicalNotExpression = 8733,
        PreIncrementExpression = 8734,
        PreDecrementExpression = 8735,
        PointerIndirectionExpression = 8736,
        AddressOfExpression = 8737,
        PostIncrementExpression = 8738,
        PostDecrementExpression = 8739,
        AwaitExpression = 8740,
        IndexExpression = 8741,

        // primary expression
        ThisExpression = 8746,
        BaseExpression = 8747,
        ArgListExpression = 8748,
        NumericLiteralExpression = 8749,
        StringLiteralExpression = 8750,
        CharacterLiteralExpression = 8751,
        TrueLiteralExpression = 8752,
        FalseLiteralExpression = 8753,
        NullLiteralExpression = 8754,
        DefaultLiteralExpression = 8755,

        // primary function expressions
        TypeOfExpression = 8760,
        SizeOfExpression = 8761,
        CheckedExpression = 8762,
        UncheckedExpression = 8763,
        DefaultExpression = 8764,
        MakeRefExpression = 8765,
        RefValueExpression = 8766,
        RefTypeExpression = 8767,
        // NameOfExpression = 8768, // we represent nameof(x) as an invocation expression

        // query expressions
        QueryExpression = 8774,
        QueryBody = 8775,
        FromClause = 8776,
        LetClause = 8777,
        JoinClause = 8778,
        JoinIntoClause = 8779,
        WhereClause = 8780,
        OrderByClause = 8781,
        AscendingOrdering = 8782,
        DescendingOrdering = 8783,
        SelectClause = 8784,
        GroupClause = 8785,
        QueryContinuation = 8786,

        // statements
        Block = 8792,
        LocalDeclarationStatement = 8793,
        VariableDeclaration = 8794,
        VariableDeclarator = 8795,
        EqualsValueClause = 8796,
        ExpressionStatement = 8797,
        EmptyStatement = 8798,
        LabeledStatement = 8799,

        // jump statements
        GotoStatement = 8800,
        GotoCaseStatement = 8801,
        GotoDefaultStatement = 8802,
        BreakStatement = 8803,
        ContinueStatement = 8804,
        ReturnStatement = 8805,
        YieldReturnStatement = 8806,
        YieldBreakStatement = 8807,
        ThrowStatement = 8808,

        WhileStatement = 8809,
        DoStatement = 8810,
        ForStatement = 8811,
        ForEachStatement = 8812,
        UsingStatement = 8813,
        FixedStatement = 8814,

        // checked statements
        CheckedStatement = 8815,
        UncheckedStatement = 8816,

        UnsafeStatement = 8817,
        LockStatement = 8818,
        IfStatement = 8819,
        ElseClause = 8820,
        SwitchStatement = 8821,
        SwitchSection = 8822,
        CaseSwitchLabel = 8823,
        DefaultSwitchLabel = 8824,
        TryStatement = 8825,
        CatchClause = 8826,
        CatchDeclaration = 8827,
        CatchFilterClause = 8828,
        FinallyClause = 8829,

        // statements that didn't fit above
        LocalFunctionStatement = 8830,

        // declarations
        CompilationUnit = 8840,
        GlobalStatement = 8841,
        NamespaceDeclaration = 8842,
        UsingDirective = 8843,
        ExternAliasDirective = 8844,
        FileScopedNamespaceDeclaration = 8845,

        // attributes
        AttributeList = 8847,
        AttributeTargetSpecifier = 8848,
        Attribute = 8849,
        AttributeArgumentList = 8850,
        AttributeArgument = 8851,
        NameEquals = 8852,

        // type declarations
        ClassDeclaration = 8855,
        StructDeclaration = 8856,
        InterfaceDeclaration = 8857,
        EnumDeclaration = 8858,
        DelegateDeclaration = 8859,

        BaseList = 8864,
        SimpleBaseType = 8865,
        TypeParameterConstraintClause = 8866,
        ConstructorConstraint = 8867,
        ClassConstraint = 8868,
        StructConstraint = 8869,
        TypeConstraint = 8870,
        ExplicitInterfaceSpecifier = 8871,
        EnumMemberDeclaration = 8872,
        FieldDeclaration = 8873,
        EventFieldDeclaration = 8874,
        MethodDeclaration = 8875,
        OperatorDeclaration = 8876,
        ConversionOperatorDeclaration = 8877,
        ConstructorDeclaration = 8878,

        BaseConstructorInitializer = 8889,
        ThisConstructorInitializer = 8890,
        DestructorDeclaration = 8891,
        PropertyDeclaration = 8892,
        EventDeclaration = 8893,
        IndexerDeclaration = 8894,
        AccessorList = 8895,
        GetAccessorDeclaration = 8896,
        SetAccessorDeclaration = 8897,
        AddAccessorDeclaration = 8898,
        RemoveAccessorDeclaration = 8899,
        UnknownAccessorDeclaration = 8900,
        ParameterList = 8906,
        BracketedParameterList = 8907,
        Parameter = 8908,
        TypeParameterList = 8909,
        TypeParameter = 8910,
        IncompleteMember = 8916,
        ArrowExpressionClause = 8917,
        Interpolation = 8918, // part of an interpolated string
        InterpolatedStringText = 8919,
        InterpolationAlignmentClause = 8920,
        InterpolationFormatClause = 8921,

        ShebangDirectiveTrivia = 8922,
        LoadDirectiveTrivia = 8923,
        // Changes after C# 6

        // tuples
        TupleType = 8924,
        TupleElement = 8925,
        TupleExpression = 8926,
        SingleVariableDesignation = 8927,
        ParenthesizedVariableDesignation = 8928,
        ForEachVariableStatement = 8929,

        // patterns (for pattern-matching)
        DeclarationPattern = 9000,
        ConstantPattern = 9002,
        CasePatternSwitchLabel = 9009,
        WhenClause = 9013,
        DiscardDesignation = 9014,

        // added along with recursive patterns
        RecursivePattern = 9020,
        PropertyPatternClause = 9021,
        Subpattern = 9022,
        PositionalPatternClause = 9023,
        DiscardPattern = 9024,
        SwitchExpression = 9025,
        SwitchExpressionArm = 9026,
        VarPattern = 9027,

        // new patterns added in C# 9.0
        ParenthesizedPattern = 9028,
        RelationalPattern = 9029,
        TypePattern = 9030,
        OrPattern = 9031,
        AndPattern = 9032,
        NotPattern = 9033,

        // new patterns added in C# 11.0
        SlicePattern = 9034,
        ListPattern = 9035,

        // Kinds between 9000 and 9039 are "reserved" for pattern matching.

        DeclarationExpression = 9040,
        RefExpression = 9050,
        RefType = 9051,
        ThrowExpression = 9052,
        ImplicitStackAllocArrayCreationExpression = 9053,
        SuppressNullableWarningExpression = 9054,
        NullableDirectiveTrivia = 9055,

        FunctionPointerType = 9056,
        FunctionPointerParameter = 9057,
        FunctionPointerParameterList = 9058,
        FunctionPointerCallingConvention = 9059,

        InitAccessorDeclaration = 9060,

        WithExpression = 9061,
        WithInitializerExpression = 9062,
        RecordDeclaration = 9063,

        DefaultConstraint = 9064,

        PrimaryConstructorBaseType = 9065,

        FunctionPointerUnmanagedCallingConventionList = 9066,
        FunctionPointerUnmanagedCallingConvention = 9067,

        RecordStructDeclaration = 9068,

        ExpressionColon = 9069,
        LineDirectivePosition = 9070,
        LineSpanDirectiveTrivia = 9071,

        InterpolatedSingleLineRawStringStartToken = 9072,   // $"""
        InterpolatedMultiLineRawStringStartToken = 9073,    // $""" (whitespace and newline are included in the Text for this token)
        InterpolatedRawStringEndToken = 9074,               // """ (preceding whitespace and newline are included in the Text for this token)
    }
}
