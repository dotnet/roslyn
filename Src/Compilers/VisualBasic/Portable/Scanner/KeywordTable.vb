' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.OperatorPrecedence

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Friend Class KeywordTable
        Shared Sub New()

            '// Note: "CanFollowExpr" says whether this token can come after an expression.
            '// e.g. "Dim x = From i In <expression> Select i" is valid, therefore "Select" can come follow an expression.
            '// The complete list was discovered through the tool in vb\Language\Tools\VBGrammarAnalyzer\vbgrammar.html
            '// If you add keywords, then make sure that they're added to the official language grammar, and re-run the tool.

            '// Kind  New7to8 Precedence QueryClause CanFollowExpr
            AddKeyword(SyntaxKind.AddHandlerKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.AddressOfKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.AliasKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.AndKeyword, 0, PrecedenceAnd, 0, 1)
            AddKeyword(SyntaxKind.AndAlsoKeyword, 0, PrecedenceAnd, 0, 1)
            AddKeyword(SyntaxKind.AsKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.BooleanKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ByRefKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ByteKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ByValKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CallKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CaseKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CatchKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CBoolKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CByteKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CCharKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CDateKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CDecKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CDblKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CharKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CIntKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ClassKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CLngKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CObjKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ConstKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ContinueKeyword, 1, 0, 0, 0)
            AddKeyword(SyntaxKind.CSByteKeyword, 1, 0, 0, 0)
            AddKeyword(SyntaxKind.CShortKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CSngKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CStrKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CTypeKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CUIntKeyword, 1, 0, 0, 0)
            AddKeyword(SyntaxKind.CULngKeyword, 1, 0, 0, 0)
            AddKeyword(SyntaxKind.CUShortKeyword, 1, 0, 0, 0)
            AddKeyword(SyntaxKind.DateKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.DecimalKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.DeclareKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.DefaultKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.DelegateKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.DimKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.DirectCastKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.DoKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.DoubleKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.EachKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ElseKeyword, 0, 0, 0, 1)
            AddKeyword(SyntaxKind.ElseIfKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.EndKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.EnumKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.EraseKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ErrorKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.EventKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ExitKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.FalseKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.FinallyKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ForKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.FriendKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.FunctionKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.GetKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.GetTypeKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.GetXmlNamespaceKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.GlobalKeyword, 1, 0, 0, 0)
            AddKeyword(SyntaxKind.GoToKeyword, 0, 0, 0, 0)

            AddKeyword(SyntaxKind.HandlesKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.IfKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ImplementsKeyword, 0, 0, 0, 1)
            AddKeyword(SyntaxKind.ImportsKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.InKeyword, 0, 0, 0, 1)
            AddKeyword(SyntaxKind.InheritsKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.IntegerKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.InterfaceKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.IsKeyword, 0, PrecedenceRelational, 0, 1)
            AddKeyword(SyntaxKind.IsNotKeyword, 1, PrecedenceRelational, 0, 1)
            AddKeyword(SyntaxKind.LetKeyword, 0, 0, 1, 1)
            AddKeyword(SyntaxKind.LibKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.LikeKeyword, 0, PrecedenceRelational, 0, 1)
            AddKeyword(SyntaxKind.LongKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.LoopKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.MeKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ModKeyword, 0, PrecedenceModulus, 0, 1)
            AddKeyword(SyntaxKind.ModuleKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.MustInheritKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.MustOverrideKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.MyBaseKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.MyClassKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.NamespaceKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.NarrowingKeyword, 1, 0, 0, 0)
            AddKeyword(SyntaxKind.NextKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.NewKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.NotKeyword, 0, PrecedenceNot, 0, 0)
            AddKeyword(SyntaxKind.NothingKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.NotInheritableKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.NotOverridableKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ObjectKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.OfKeyword, 1, 0, 0, 0)
            AddKeyword(SyntaxKind.OnKeyword, 0, 0, 0, 1)
            AddKeyword(SyntaxKind.OperatorKeyword, 1, 0, 0, 0)
            AddKeyword(SyntaxKind.OptionKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.OptionalKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.OrKeyword, 0, PrecedenceOr, 0, 1)
            AddKeyword(SyntaxKind.OrElseKeyword, 0, PrecedenceOr, 0, 1)
            AddKeyword(SyntaxKind.OverloadsKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.OverridableKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.OverridesKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ParamArrayKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.PartialKeyword, 1, 0, 0, 0)
            AddKeyword(SyntaxKind.PrivateKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.PropertyKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ProtectedKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.PublicKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.RaiseEventKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ReadOnlyKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ReDimKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.REMKeyword, 0, 0, 0, 1)
            AddKeyword(SyntaxKind.RemoveHandlerKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ResumeKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ReturnKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.SByteKeyword, 1, 0, 0, 0)
            AddKeyword(SyntaxKind.SelectKeyword, 0, 0, 1, 1)
            AddKeyword(SyntaxKind.SetKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ShadowsKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.SharedKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ShortKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.SingleKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.StaticKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.StepKeyword, 0, 0, 0, 1)
            AddKeyword(SyntaxKind.StopKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.StringKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.StructureKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.SubKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.SyncLockKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ThenKeyword, 0, 0, 0, 1)
            AddKeyword(SyntaxKind.ThrowKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ToKeyword, 0, 0, 0, 1)
            AddKeyword(SyntaxKind.TrueKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.TryKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.TryCastKeyword, 1, 0, 0, 0)
            AddKeyword(SyntaxKind.TypeOfKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.UIntegerKeyword, 1, 0, 0, 0)
            AddKeyword(SyntaxKind.ULongKeyword, 1, 0, 0, 0)
            AddKeyword(SyntaxKind.UShortKeyword, 1, 0, 0, 0)
            AddKeyword(SyntaxKind.UsingKeyword, 1, 0, 0, 0)
            AddKeyword(SyntaxKind.WhenKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.WhileKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.WideningKeyword, 1, 0, 0, 0)
            AddKeyword(SyntaxKind.WithKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.WithEventsKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.WriteOnlyKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.XorKeyword, 0, PrecedenceXor, 0, 1)

            ' Contextual keywords
            AddKeyword(SyntaxKind.AggregateKeyword, 0, 0, 1, 1)
            AddKeyword(SyntaxKind.AllKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.AnsiKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.AscendingKeyword, 0, 0, 0, 1)
            AddKeyword(SyntaxKind.AssemblyKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.AutoKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.BinaryKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ByKeyword, 0, 0, 0, 1)
            AddKeyword(SyntaxKind.CompareKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CustomKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.DescendingKeyword, 0, 0, 0, 1)
            AddKeyword(SyntaxKind.DisableKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.DistinctKeyword, 0, 0, 1, 1)
            AddKeyword(SyntaxKind.EnableKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.EqualsKeyword, 0, 0, 0, 1)
            AddKeyword(SyntaxKind.ExplicitKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ExternalSourceKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.ExternalChecksumKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.FromKeyword, 0, 0, 1, 1)
            AddKeyword(SyntaxKind.GroupKeyword, 0, 0, 1, 1)
            AddKeyword(SyntaxKind.InferKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.IntoKeyword, 0, 0, 0, 1)
            AddKeyword(SyntaxKind.IsFalseKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.IsTrueKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.JoinKeyword, 0, 0, 1, 1)
            AddKeyword(SyntaxKind.KeyKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.MidKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.OffKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.OrderKeyword, 0, 0, 1, 1)
            AddKeyword(SyntaxKind.OutKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.PreserveKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.RegionKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.SkipKeyword, 0, 0, 1, 1)
            AddKeyword(SyntaxKind.StrictKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.TextKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.TakeKeyword, 0, 0, 1, 1)
            AddKeyword(SyntaxKind.UnicodeKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.UntilKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.WarningKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.WhereKeyword, 0, 0, 1, 1)

            ' Visual Basic 11.0 keywords
            AddKeyword(SyntaxKind.AsyncKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.AwaitKeyword, 0, PrecedenceAwait, 0, 0)
            AddKeyword(SyntaxKind.IteratorKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.YieldKeyword, 0, 0, 0, 0)

            'Obsolete keywords - They exist to give error messages.
            AddKeyword(SyntaxKind.EndIfKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.GosubKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.TypeKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.VariantKeyword, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.WendKeyword, 0, 0, 0, 0)

            AddKeyword(SyntaxKind.CommaToken, 0, 0, 0, 1)
            AddKeyword(SyntaxKind.AmpersandToken, 0, PrecedenceConcatenate, 0, 1)
            AddKeyword(SyntaxKind.SingleQuoteToken, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.OpenParenToken, 0, 0, 0, 1)
            AddKeyword(SyntaxKind.CloseParenToken, 0, 0, 0, 1)
            AddKeyword(SyntaxKind.OpenBraceToken, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.CloseBraceToken, 0, 0, 0, 1)
            AddKeyword(SyntaxKind.AsteriskToken, 0, PrecedenceMultiply, 0, 1)
            AddKeyword(SyntaxKind.PlusToken, 0, PrecedenceAdd, 0, 1)
            AddKeyword(SyntaxKind.MinusToken, 0, PrecedenceAdd, 0, 1)
            AddKeyword(SyntaxKind.SlashToken, 0, PrecedenceMultiply, 0, 1)
            AddKeyword(SyntaxKind.LessThanToken, 0, PrecedenceRelational, 0, 1)
            AddKeyword(SyntaxKind.LessThanEqualsToken, 0, PrecedenceRelational, 0, 0)
            AddKeyword(SyntaxKind.LessThanGreaterThanToken, 0, PrecedenceRelational, 0, 0)
            AddKeyword(SyntaxKind.EqualsToken, 0, PrecedenceRelational, 0, 1)
            AddKeyword(SyntaxKind.GreaterThanToken, 0, PrecedenceRelational, 0, 1)
            AddKeyword(SyntaxKind.GreaterThanEqualsToken, 0, PrecedenceRelational, 0, 0)
            AddKeyword(SyntaxKind.BackslashToken, 0, PrecedenceIntegerDivide, 0, 1)
            AddKeyword(SyntaxKind.CaretToken, 0, PrecedenceExponentiate, 0, 1)

            AddKeyword(SyntaxKind.ColonEqualsToken, 0, 0, 0, 0)
            AddKeyword(SyntaxKind.AmpersandEqualsToken, 0, PrecedenceConcatenate, 0, 0)
            AddKeyword(SyntaxKind.AsteriskEqualsToken, 0, PrecedenceMultiply, 0, 0)
            AddKeyword(SyntaxKind.PlusEqualsToken, 0, PrecedenceAdd, 0, 0)
            AddKeyword(SyntaxKind.MinusEqualsToken, 0, PrecedenceAdd, 0, 0)
            AddKeyword(SyntaxKind.SlashEqualsToken, 0, PrecedenceMultiply, 0, 0)
            AddKeyword(SyntaxKind.BackslashEqualsToken, 0, PrecedenceIntegerDivide, 0, 0)
            AddKeyword(SyntaxKind.CaretEqualsToken, 0, PrecedenceExponentiate, 0, 0)
            AddKeyword(SyntaxKind.LessThanLessThanToken, 0, PrecedenceShift, 0, 0)
            AddKeyword(SyntaxKind.GreaterThanGreaterThanToken, 0, PrecedenceShift, 0, 0)
            AddKeyword(SyntaxKind.LessThanLessThanEqualsToken, 0, PrecedenceShift, 0, 0)
            AddKeyword(SyntaxKind.GreaterThanGreaterThanEqualsToken, 0, PrecedenceShift, 0, 0)
            AddKeyword(SyntaxKind.PercentGreaterThanToken, 0, 0, 0, 1)
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
                New7To8 As Integer,
                Precedence As OperatorPrecedence,
                isQueryClause As Integer,
                canFollowExpr As Integer)

                Me.kdNew7To8kwd = New7To8 <> 0
                Me.kdOperPrec = Precedence
                Me.kdIsQueryClause = isQueryClause <> 0
                Me.kdCanFollowExpr = canFollowExpr <> 0
            End Sub
        End Structure

        Private Shared _Keywords As New Dictionary(Of String, SyntaxKind)(IdentifierComparison.Comparer)
        Private Shared _KeywordProperties As New Dictionary(Of UShort, KeywordDescription)

        Friend Shared Function TokenOfString(tokenName As String) As SyntaxKind
            Debug.Assert(tokenName IsNot Nothing)

            tokenName = EnsureHalfWidth(tokenName)

            Dim kind As SyntaxKind
            If Not _Keywords.TryGetValue(tokenName, kind) Then
                kind = SyntaxKind.IdentifierToken
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
            If (_KeywordProperties.TryGetValue(kind, description)) Then
                Return description.kdCanFollowExpr
            End If
            Return False
        End Function

        Friend Shared Function IsQueryClause(kind As SyntaxKind) As Boolean
            Dim description As KeywordDescription = Nothing
            If (_KeywordProperties.TryGetValue(kind, description)) Then
                Return description.kdIsQueryClause
            End If
            Return False
        End Function

        Friend Shared Function TokenOpPrec(kind As SyntaxKind) As OperatorPrecedence
            Dim description As KeywordDescription = Nothing
            If (_KeywordProperties.TryGetValue(kind, description)) Then
                Return description.kdOperPrec
            End If

            Debug.Assert(False, "the kind is not found")

            Return PrecedenceNone
        End Function

        Private Shared Sub AddKeyword(
            Token As SyntaxKind,
            New7To8 As Integer,
            Precedence As OperatorPrecedence,
            isQueryClause As Integer,
            canFollowExpr As Integer)

            Dim keyword As New KeywordDescription(New7To8, Precedence, isQueryClause, canFollowExpr)
            _KeywordProperties.Add(Token, keyword)

            Dim Name = SyntaxFacts.GetText(Token)
            Debug.Assert(Name IsNot Nothing)
            _Keywords.Add(Name, Token)
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
