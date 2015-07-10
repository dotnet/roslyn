' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.OperatorPrecedence

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Friend Class KeywordTable

        Shared Sub New()

            '// Note: "CanFollowExpr" says whether this token can come after an expression.
            '// e.g. "Dim x = From i In <expression> Select i" is valid, therefore "Select" can come follow an expression.
            '// The complete list was discovered through the tool in vb\Language\Tools\VBGrammarAnalyzer\vbgrammar.html
            '// If you add keywords, then make sure that they're added to the official language grammar, and re-run the tool.

            Const New7to8 As UShort = 1US << 8
            Const QueryClause As UShort = 1US << 9
            Const CanFollowExpr As UShort = 1US << 10
            Const None As UShort = 0US

            '// Kind  New7to8 Precedence QueryClause CanFollowExpr
            Dim keywordInitData As UShort() = New UShort() {
                SyntaxKind.AddHandlerKeyword, None,
                SyntaxKind.AddressOfKeyword, None,
                SyntaxKind.AliasKeyword, None,
                SyntaxKind.AndKeyword, PrecedenceAnd Or CanFollowExpr,
                SyntaxKind.AndAlsoKeyword, PrecedenceAnd Or CanFollowExpr,
                SyntaxKind.AsKeyword, None,
                SyntaxKind.BooleanKeyword, None,
                SyntaxKind.ByRefKeyword, None,
                SyntaxKind.ByteKeyword, None,
                SyntaxKind.ByValKeyword, None,
                SyntaxKind.CallKeyword, None,
                SyntaxKind.CaseKeyword, None,
                SyntaxKind.CatchKeyword, None,
                SyntaxKind.CBoolKeyword, None,
                SyntaxKind.CByteKeyword, None,
                SyntaxKind.CCharKeyword, None,
                SyntaxKind.CDateKeyword, None,
                SyntaxKind.CDecKeyword, None,
                SyntaxKind.CDblKeyword, None,
                SyntaxKind.CharKeyword, None,
                SyntaxKind.CIntKeyword, None,
                SyntaxKind.ClassKeyword, None,
                SyntaxKind.CLngKeyword, None,
                SyntaxKind.CObjKeyword, None,
                SyntaxKind.ConstKeyword, None,
                SyntaxKind.ContinueKeyword, New7to8,
                SyntaxKind.CSByteKeyword, New7to8,
                SyntaxKind.CShortKeyword, None,
                SyntaxKind.CSngKeyword, None,
                SyntaxKind.CStrKeyword, None,
                SyntaxKind.CTypeKeyword, None,
                SyntaxKind.CUIntKeyword, New7to8,
                SyntaxKind.CULngKeyword, New7to8,
                SyntaxKind.CUShortKeyword, New7to8,
                SyntaxKind.DateKeyword, None,
                SyntaxKind.DecimalKeyword, None,
                SyntaxKind.DeclareKeyword, None,
                SyntaxKind.DefaultKeyword, None,
                SyntaxKind.DelegateKeyword, None,
                SyntaxKind.DimKeyword, None,
                SyntaxKind.DirectCastKeyword, None,
                SyntaxKind.DoKeyword, None,
                SyntaxKind.DoubleKeyword, None,
                SyntaxKind.EachKeyword, None,
                SyntaxKind.ElseKeyword, CanFollowExpr,
                SyntaxKind.ElseIfKeyword, None,
                SyntaxKind.EndKeyword, None,
                SyntaxKind.EnumKeyword, None,
                SyntaxKind.EraseKeyword, None,
                SyntaxKind.ErrorKeyword, None,
                SyntaxKind.EventKeyword, None,
                SyntaxKind.ExitKeyword, None,
                SyntaxKind.FalseKeyword, None,
                SyntaxKind.FinallyKeyword, None,
                SyntaxKind.ForKeyword, None,
                SyntaxKind.FriendKeyword, None,
                SyntaxKind.FunctionKeyword, None,
                SyntaxKind.GetKeyword, None,
                SyntaxKind.GetTypeKeyword, None,
                SyntaxKind.GetXmlNamespaceKeyword, None,
                SyntaxKind.GlobalKeyword, New7to8,
                SyntaxKind.GoToKeyword, None,
                SyntaxKind.HandlesKeyword, None,
                SyntaxKind.IfKeyword, None,
                SyntaxKind.ImplementsKeyword, CanFollowExpr,
                SyntaxKind.ImportsKeyword, None,
                SyntaxKind.InKeyword, CanFollowExpr,
                SyntaxKind.InheritsKeyword, None,
                SyntaxKind.IntegerKeyword, None,
                SyntaxKind.InterfaceKeyword, None,
                SyntaxKind.IsKeyword, PrecedenceRelational Or CanFollowExpr,
                SyntaxKind.IsNotKeyword, PrecedenceRelational Or New7to8 Or CanFollowExpr,
                SyntaxKind.LetKeyword, QueryClause Or CanFollowExpr,
                SyntaxKind.LibKeyword, None,
                SyntaxKind.LikeKeyword, PrecedenceRelational Or CanFollowExpr,
                SyntaxKind.LongKeyword, None,
                SyntaxKind.LoopKeyword, None,
                SyntaxKind.MeKeyword, None,
                SyntaxKind.ModKeyword, PrecedenceModulus Or CanFollowExpr,
                SyntaxKind.ModuleKeyword, None,
                SyntaxKind.MustInheritKeyword, None,
                SyntaxKind.MustOverrideKeyword, None,
                SyntaxKind.MyBaseKeyword, None,
                SyntaxKind.MyClassKeyword, None,
                SyntaxKind.NameOfKeyword, None,
                SyntaxKind.NamespaceKeyword, None,
                SyntaxKind.NarrowingKeyword, New7to8,
                SyntaxKind.NextKeyword, None,
                SyntaxKind.NewKeyword, None,
                SyntaxKind.NotKeyword, PrecedenceNot,
                SyntaxKind.NothingKeyword, None,
                SyntaxKind.NotInheritableKeyword, None,
                SyntaxKind.NotOverridableKeyword, None,
                SyntaxKind.ObjectKeyword, None,
                SyntaxKind.OfKeyword, New7to8,
                SyntaxKind.OnKeyword, CanFollowExpr,
                SyntaxKind.OperatorKeyword, New7to8,
                SyntaxKind.OptionKeyword, None,
                SyntaxKind.OptionalKeyword, None,
                SyntaxKind.OrKeyword, PrecedenceOr Or CanFollowExpr,
                SyntaxKind.OrElseKeyword, PrecedenceOr Or CanFollowExpr,
                SyntaxKind.OverloadsKeyword, None,
                SyntaxKind.OverridableKeyword, None,
                SyntaxKind.OverridesKeyword, None,
                SyntaxKind.ParamArrayKeyword, None,
                SyntaxKind.PartialKeyword, New7to8,
                SyntaxKind.PrivateKeyword, None,
                SyntaxKind.PropertyKeyword, None,
                SyntaxKind.ProtectedKeyword, None,
                SyntaxKind.PublicKeyword, None,
                SyntaxKind.RaiseEventKeyword, None,
                SyntaxKind.ReadOnlyKeyword, None,
                SyntaxKind.ReferenceKeyword, None,
                SyntaxKind.ReDimKeyword, None,
                SyntaxKind.REMKeyword, CanFollowExpr,
                SyntaxKind.RemoveHandlerKeyword, None,
                SyntaxKind.ResumeKeyword, None,
                SyntaxKind.ReturnKeyword, None,
                SyntaxKind.SByteKeyword, New7to8,
                SyntaxKind.SelectKeyword, QueryClause Or CanFollowExpr,
                SyntaxKind.SetKeyword, None,
                SyntaxKind.ShadowsKeyword, None,
                SyntaxKind.SharedKeyword, None,
                SyntaxKind.ShortKeyword, None,
                SyntaxKind.SingleKeyword, None,
                SyntaxKind.StaticKeyword, None,
                SyntaxKind.StepKeyword, CanFollowExpr,
                SyntaxKind.StopKeyword, None,
                SyntaxKind.StringKeyword, None,
                SyntaxKind.StructureKeyword, None,
                SyntaxKind.SubKeyword, None,
                SyntaxKind.SyncLockKeyword, None,
                SyntaxKind.ThenKeyword, CanFollowExpr,
                SyntaxKind.ThrowKeyword, None,
                SyntaxKind.ToKeyword, CanFollowExpr,
                SyntaxKind.TrueKeyword, None,
                SyntaxKind.TryKeyword, None,
                SyntaxKind.TryCastKeyword, New7to8,
                SyntaxKind.TypeOfKeyword, None,
                SyntaxKind.UIntegerKeyword, New7to8,
                SyntaxKind.ULongKeyword, New7to8,
                SyntaxKind.UShortKeyword, New7to8,
                SyntaxKind.UsingKeyword, New7to8,
                SyntaxKind.WhenKeyword, None,
                SyntaxKind.WhileKeyword, None,
                SyntaxKind.WideningKeyword, New7to8,
                SyntaxKind.WithKeyword, None,
                SyntaxKind.WithEventsKeyword, None,
                SyntaxKind.WriteOnlyKeyword, None,
                SyntaxKind.XorKeyword, PrecedenceXor Or CanFollowExpr,
                SyntaxKind.AggregateKeyword, QueryClause Or CanFollowExpr,
                SyntaxKind.AllKeyword, None,
                SyntaxKind.AnsiKeyword, None,
                SyntaxKind.AscendingKeyword, CanFollowExpr,
                SyntaxKind.AssemblyKeyword, None,
                SyntaxKind.AutoKeyword, None,
                SyntaxKind.BinaryKeyword, None,
                SyntaxKind.ByKeyword, CanFollowExpr,
                SyntaxKind.CompareKeyword, None,
                SyntaxKind.CustomKeyword, None,
                SyntaxKind.DescendingKeyword, CanFollowExpr,
                SyntaxKind.DisableKeyword, None,
                SyntaxKind.DistinctKeyword, QueryClause Or CanFollowExpr,
                SyntaxKind.EnableKeyword, None,
                SyntaxKind.EqualsKeyword, CanFollowExpr,
                SyntaxKind.ExplicitKeyword, None,
                SyntaxKind.ExternalSourceKeyword, None,
                SyntaxKind.ExternalChecksumKeyword, None,
                SyntaxKind.FromKeyword, QueryClause Or CanFollowExpr,
                SyntaxKind.GroupKeyword, QueryClause Or CanFollowExpr,
                SyntaxKind.InferKeyword, None,
                SyntaxKind.IntoKeyword, CanFollowExpr,
                SyntaxKind.IsFalseKeyword, None,
                SyntaxKind.IsTrueKeyword, None,
                SyntaxKind.JoinKeyword, QueryClause Or CanFollowExpr,
                SyntaxKind.KeyKeyword, None,
                SyntaxKind.MidKeyword, None,
                SyntaxKind.OffKeyword, None,
                SyntaxKind.OrderKeyword, QueryClause Or CanFollowExpr,
                SyntaxKind.OutKeyword, None,
                SyntaxKind.PreserveKeyword, None,
                SyntaxKind.RegionKeyword, None,
                SyntaxKind.SkipKeyword, QueryClause Or CanFollowExpr,
                SyntaxKind.StrictKeyword, None,
                SyntaxKind.TextKeyword, None,
                SyntaxKind.TakeKeyword, QueryClause Or CanFollowExpr,
                SyntaxKind.UnicodeKeyword, None,
                SyntaxKind.UntilKeyword, None,
                SyntaxKind.WarningKeyword, None,
                SyntaxKind.WhereKeyword, QueryClause Or CanFollowExpr,
                SyntaxKind.AsyncKeyword, None,
                SyntaxKind.AwaitKeyword, PrecedenceAwait,
                SyntaxKind.IteratorKeyword, None,
                SyntaxKind.YieldKeyword, None,
                SyntaxKind.EndIfKeyword, None,
                SyntaxKind.GosubKeyword, None,
                SyntaxKind.TypeKeyword, None,
                SyntaxKind.VariantKeyword, None,
                SyntaxKind.WendKeyword, None,
                SyntaxKind.CommaToken, CanFollowExpr,
                SyntaxKind.AmpersandToken, PrecedenceConcatenate Or CanFollowExpr,
                SyntaxKind.SingleQuoteToken, None,
                SyntaxKind.OpenParenToken, CanFollowExpr,
                SyntaxKind.CloseParenToken, CanFollowExpr,
                SyntaxKind.OpenBraceToken, None,
                SyntaxKind.CloseBraceToken, CanFollowExpr,
                SyntaxKind.AsteriskToken, PrecedenceMultiply Or CanFollowExpr,
                SyntaxKind.PlusToken, PrecedenceAdd Or CanFollowExpr,
                SyntaxKind.MinusToken, PrecedenceAdd Or CanFollowExpr,
                SyntaxKind.SlashToken, PrecedenceMultiply Or CanFollowExpr,
                SyntaxKind.LessThanToken, PrecedenceRelational Or CanFollowExpr,
                SyntaxKind.LessThanEqualsToken, PrecedenceRelational Or CanFollowExpr,
                SyntaxKind.LessThanGreaterThanToken, PrecedenceRelational Or CanFollowExpr,
                SyntaxKind.EqualsToken, PrecedenceRelational Or CanFollowExpr,
                SyntaxKind.GreaterThanToken, PrecedenceRelational Or CanFollowExpr,
                SyntaxKind.GreaterThanEqualsToken, PrecedenceRelational,
                SyntaxKind.BackslashToken, PrecedenceIntegerDivide Or CanFollowExpr,
                SyntaxKind.CaretToken, PrecedenceExponentiate Or CanFollowExpr,
                SyntaxKind.ColonEqualsToken, None,
                SyntaxKind.AmpersandEqualsToken, PrecedenceConcatenate,
                SyntaxKind.AsteriskEqualsToken, PrecedenceMultiply,
                SyntaxKind.PlusEqualsToken, PrecedenceAdd,
                SyntaxKind.MinusEqualsToken, PrecedenceAdd,
                SyntaxKind.SlashEqualsToken, PrecedenceMultiply,
                SyntaxKind.BackslashEqualsToken, PrecedenceIntegerDivide,
                SyntaxKind.CaretEqualsToken, PrecedenceExponentiate,
                SyntaxKind.LessThanLessThanToken, PrecedenceShift,
                SyntaxKind.GreaterThanGreaterThanToken, PrecedenceShift,
                SyntaxKind.LessThanLessThanEqualsToken, PrecedenceShift,
                SyntaxKind.GreaterThanGreaterThanEqualsToken, PrecedenceShift,
                SyntaxKind.PercentGreaterThanToken, CanFollowExpr
                }

            For i As Integer = 0 To keywordInitData.Length - 1 Step 2
                Dim bits = keywordInitData(i + 1)
                AddKeyword(
                    Token:=DirectCast(keywordInitData(i), SyntaxKind),
                    New7To8:=(bits And New7to8) <> 0,
                    Precedence:=CType(bits And &HFF, OperatorPrecedence),
                    isQueryClause:=(bits And QueryClause) <> 0,
                    canFollowExpr:=(bits And CanFollowExpr) <> 0
                    )
            Next
        End Sub

        Public Structure KeywordDescription
            Public kdOperPrec As OperatorPrecedence
            Public kdNew7To8kwd As Boolean
            Public kdIsQueryClause As Boolean

            '// Note: "CanFollowExpr" says whether this token can come after an expression.
            '// e.g. "Dim x = From i In <expression> Select i" is valid, therefore "Select" can come follow an expression.           
            '// The complete list was discovered through the tool in vb\Language\Tools\VBGrammarAnalyzer\vbgrammar.html
            '// If you add keywords, then make sure that they're added to the official language grammar, and re-run the tool.
            Public kdCanFollowExpr As Boolean

            Public Sub New(
                New7To8 As Boolean,
                Precedence As OperatorPrecedence,
                isQueryClause As Boolean,
                canFollowExpr As Boolean)

                Me.kdNew7To8kwd = New7To8
                Me.kdOperPrec = Precedence
                Me.kdIsQueryClause = isQueryClause
                Me.kdCanFollowExpr = canFollowExpr
            End Sub
        End Structure

        Private Shared ReadOnly s_keywords As New Dictionary(Of String, SyntaxKind)(IdentifierComparison.Comparer)
        Private Shared ReadOnly s_keywordProperties As New Dictionary(Of UShort, KeywordDescription)

        Friend Shared Function TokenOfString(tokenName As String) As SyntaxKind
            Debug.Assert(tokenName IsNot Nothing)

            tokenName = EnsureHalfWidth(tokenName)

            Dim kind As SyntaxKind
            If Not s_keywords.TryGetValue(tokenName, kind) Then
                ' s_keywords includes preprocessor keywords, but only one of
                ' "r" and "reference".  GetProcessorKeywordKind supports both.
                Dim preprocessorKind = SyntaxFacts.GetPreprocessorKeywordKind(tokenName)
                Debug.Assert(preprocessorKind = SyntaxKind.None OrElse preprocessorKind = SyntaxKind.ReferenceKeyword)
                kind = If(preprocessorKind <> SyntaxKind.None, preprocessorKind, SyntaxKind.IdentifierToken)
            End If
            Return kind
        End Function

        Private Shared Function EnsureHalfWidth(s As String) As String
            Dim result As Char() = Nothing

            For i As Integer = 0 To s.Length - 1
                Dim ch = s(i)

                If SyntaxFacts.IsFullWidth(ch) Then
                    ch = SyntaxFacts.MakeHalfWidth(ch)

                    If result Is Nothing Then
                        result = New Char(s.Length - 1) {}
                        For j As Integer = 0 To i - 1
                            result(j) = s(j)
                        Next
                    End If

                    result(i) = ch
                Else
                    If result IsNot Nothing Then
                        result(i) = ch
                    End If
                End If
            Next

            If result IsNot Nothing Then
                Return New String(result)
            Else
                Return s
            End If
        End Function

        Friend Shared Function CanFollowExpression(kind As SyntaxKind) As Boolean
            Dim description As KeywordDescription = Nothing
            If (s_keywordProperties.TryGetValue(kind, description)) Then
                Return description.kdCanFollowExpr
            End If
            Return False
        End Function

        Friend Shared Function IsQueryClause(kind As SyntaxKind) As Boolean
            Dim description As KeywordDescription = Nothing
            If (s_keywordProperties.TryGetValue(kind, description)) Then
                Return description.kdIsQueryClause
            End If
            Return False
        End Function

        Friend Shared Function TokenOpPrec(kind As SyntaxKind) As OperatorPrecedence
            Dim description As KeywordDescription = Nothing
            If (s_keywordProperties.TryGetValue(kind, description)) Then
                Return description.kdOperPrec
            End If

            Debug.Assert(False, "the kind is not found")

            Return PrecedenceNone
        End Function

        Private Shared Sub AddKeyword(
            Token As SyntaxKind,
            New7To8 As Boolean,
            Precedence As OperatorPrecedence,
            isQueryClause As Boolean,
            canFollowExpr As Boolean)

            Dim keyword As New KeywordDescription(New7To8, Precedence, isQueryClause, canFollowExpr)
            s_keywordProperties.Add(Token, keyword)

            Dim Name = SyntaxFacts.GetText(Token)
            Debug.Assert(Name IsNot Nothing)
            s_keywords.Add(Name, Token)
        End Sub


    End Class

    '//-------------------------------------------------------------------------------------------------
    '//
    '// OperatorPrecedence.Precedence levels of unary and binary operators.
    '//
    Friend Enum OperatorPrecedence As Byte
        PrecedenceNone = 0
        PrecedenceXor
        PrecedenceOr
        PrecedenceAnd
        PrecedenceNot
        PrecedenceRelational
        PrecedenceShift
        PrecedenceConcatenate
        PrecedenceAdd
        PrecedenceModulus
        PrecedenceIntegerDivide
        PrecedenceMultiply
        PrecedenceNegate
        PrecedenceExponentiate
        PrecedenceAwait
    End Enum

End Namespace
