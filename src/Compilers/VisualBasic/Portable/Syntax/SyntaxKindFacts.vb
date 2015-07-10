' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Public Class SyntaxFacts

        ''' <summary>
        ''' Determine if the kind represents a reserved keyword
        ''' </summary>
        Public Shared Function IsReservedKeyword(kind As SyntaxKind) As Boolean
            Return kind - SyntaxKind.AddHandlerKeyword <=
                    SyntaxKind.WendKeyword - SyntaxKind.AddHandlerKeyword OrElse kind = SyntaxKind.NameOfKeyword
        End Function

        ''' <summary>
        ''' Determine if the kind represents a contextual keyword
        ''' </summary>
        Public Shared Function IsContextualKeyword(kind As SyntaxKind) As Boolean
            Return kind = SyntaxKind.ReferenceKeyword OrElse
                (SyntaxKind.AggregateKeyword <= kind AndAlso kind <= SyntaxKind.YieldKeyword)
        End Function

        ''' <summary>
        ''' Determine if the token instance represents 'Me', 'MyBase' or 'MyClass' keywords
        ''' </summary>
        Public Shared Function IsInstanceExpression(kind As SyntaxKind) As Boolean
            Select Case kind
                Case SyntaxKind.MeKeyword,
                     SyntaxKind.MyClassKeyword,
                     SyntaxKind.MyBaseKeyword
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        ''' <summary>
        ''' Return correspondent expression syntax for 'Me', 'MyBase' and 'MyClass' 
        ''' keywords or SyntaxKind.None for other syntax kinds
        ''' </summary>
        Public Shared Function GetInstanceExpression(kind As SyntaxKind) As SyntaxKind
            Select Case kind
                Case SyntaxKind.MeKeyword
                    Return SyntaxKind.MeExpression
                Case SyntaxKind.MyClassKeyword
                    Return SyntaxKind.MyClassExpression
                Case SyntaxKind.MyBaseKeyword
                    Return SyntaxKind.MyBaseExpression
                Case Else
                    Return SyntaxKind.None
            End Select
        End Function

        ''' <summary>
        ''' Determine if the token instance represents a preprocessor keyword
        ''' </summary>
        Public Shared Function IsPreprocessorKeyword(kind As SyntaxKind) As Boolean
            Select Case kind
                Case SyntaxKind.IfKeyword,
                    SyntaxKind.ElseIfKeyword,
                    SyntaxKind.ElseKeyword,
                    SyntaxKind.EndIfKeyword,
                    SyntaxKind.RegionKeyword,
                    SyntaxKind.EndKeyword,
                    SyntaxKind.ConstKeyword,
                    SyntaxKind.ReferenceKeyword,
                    SyntaxKind.EnableKeyword,
                    SyntaxKind.DisableKeyword,
                    SyntaxKind.ExternalSourceKeyword,
                    SyntaxKind.ExternalChecksumKeyword
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Private Shared ReadOnly s_reservedKeywords As SyntaxKind() = New SyntaxKind() {
            SyntaxKind.AddressOfKeyword,
            SyntaxKind.AddHandlerKeyword,
            SyntaxKind.AliasKeyword,
            SyntaxKind.AndKeyword,
            SyntaxKind.AndAlsoKeyword,
            SyntaxKind.AsKeyword,
            SyntaxKind.BooleanKeyword,
            SyntaxKind.ByRefKeyword,
            SyntaxKind.ByteKeyword,
            SyntaxKind.ByValKeyword,
            SyntaxKind.CallKeyword,
            SyntaxKind.CaseKeyword,
            SyntaxKind.CatchKeyword,
            SyntaxKind.CBoolKeyword,
            SyntaxKind.CByteKeyword,
            SyntaxKind.CCharKeyword,
            SyntaxKind.CDateKeyword,
            SyntaxKind.CDecKeyword,
            SyntaxKind.CDblKeyword,
            SyntaxKind.CharKeyword,
            SyntaxKind.CIntKeyword,
            SyntaxKind.ClassKeyword,
            SyntaxKind.CLngKeyword,
            SyntaxKind.CObjKeyword,
            SyntaxKind.ConstKeyword,
            SyntaxKind.ReferenceKeyword,
            SyntaxKind.ContinueKeyword,
            SyntaxKind.CSByteKeyword,
            SyntaxKind.CShortKeyword,
            SyntaxKind.CSngKeyword,
            SyntaxKind.CStrKeyword,
            SyntaxKind.CTypeKeyword,
            SyntaxKind.CUIntKeyword,
            SyntaxKind.CULngKeyword,
            SyntaxKind.CUShortKeyword,
            SyntaxKind.DateKeyword,
            SyntaxKind.DecimalKeyword,
            SyntaxKind.DeclareKeyword,
            SyntaxKind.DefaultKeyword,
            SyntaxKind.DelegateKeyword,
            SyntaxKind.DimKeyword,
            SyntaxKind.DirectCastKeyword,
            SyntaxKind.DoKeyword,
            SyntaxKind.DoubleKeyword,
            SyntaxKind.EachKeyword,
            SyntaxKind.ElseKeyword,
            SyntaxKind.ElseIfKeyword,
            SyntaxKind.EndKeyword,
            SyntaxKind.EnumKeyword,
            SyntaxKind.EraseKeyword,
            SyntaxKind.ErrorKeyword,
            SyntaxKind.EventKeyword,
            SyntaxKind.ExitKeyword,
            SyntaxKind.FalseKeyword,
            SyntaxKind.FinallyKeyword,
            SyntaxKind.ForKeyword,
            SyntaxKind.FriendKeyword,
            SyntaxKind.FunctionKeyword,
            SyntaxKind.GetKeyword,
            SyntaxKind.GetTypeKeyword,
            SyntaxKind.GetXmlNamespaceKeyword,
            SyntaxKind.GlobalKeyword,
            SyntaxKind.GoToKeyword,
            SyntaxKind.HandlesKeyword,
            SyntaxKind.IfKeyword,
            SyntaxKind.ImplementsKeyword,
            SyntaxKind.ImportsKeyword,
            SyntaxKind.InKeyword,
            SyntaxKind.InheritsKeyword,
            SyntaxKind.IntegerKeyword,
            SyntaxKind.InterfaceKeyword,
            SyntaxKind.IsKeyword,
            SyntaxKind.IsNotKeyword,
            SyntaxKind.LetKeyword,
            SyntaxKind.LibKeyword,
            SyntaxKind.LikeKeyword,
            SyntaxKind.LongKeyword,
            SyntaxKind.LoopKeyword,
            SyntaxKind.MeKeyword,
            SyntaxKind.ModKeyword,
            SyntaxKind.ModuleKeyword,
            SyntaxKind.MustInheritKeyword,
            SyntaxKind.MustOverrideKeyword,
            SyntaxKind.MyBaseKeyword,
            SyntaxKind.MyClassKeyword,
            SyntaxKind.NameOfKeyword,
            SyntaxKind.NamespaceKeyword,
            SyntaxKind.NarrowingKeyword,
            SyntaxKind.NextKeyword,
            SyntaxKind.NewKeyword,
            SyntaxKind.NotKeyword,
            SyntaxKind.NothingKeyword,
            SyntaxKind.NotInheritableKeyword,
            SyntaxKind.NotOverridableKeyword,
            SyntaxKind.ObjectKeyword,
            SyntaxKind.OfKeyword,
            SyntaxKind.OnKeyword,
            SyntaxKind.OperatorKeyword,
            SyntaxKind.OptionKeyword,
            SyntaxKind.OptionalKeyword,
            SyntaxKind.OrKeyword,
            SyntaxKind.OrElseKeyword,
            SyntaxKind.OverloadsKeyword,
            SyntaxKind.OverridableKeyword,
            SyntaxKind.OverridesKeyword,
            SyntaxKind.ParamArrayKeyword,
            SyntaxKind.PartialKeyword,
            SyntaxKind.PrivateKeyword,
            SyntaxKind.PropertyKeyword,
            SyntaxKind.ProtectedKeyword,
            SyntaxKind.PublicKeyword,
            SyntaxKind.RaiseEventKeyword,
            SyntaxKind.ReadOnlyKeyword,
            SyntaxKind.ReDimKeyword,
            SyntaxKind.REMKeyword,
            SyntaxKind.RemoveHandlerKeyword,
            SyntaxKind.ResumeKeyword,
            SyntaxKind.ReturnKeyword,
            SyntaxKind.SByteKeyword,
            SyntaxKind.SelectKeyword,
            SyntaxKind.SetKeyword,
            SyntaxKind.ShadowsKeyword,
            SyntaxKind.SharedKeyword,
            SyntaxKind.ShortKeyword,
            SyntaxKind.SingleKeyword,
            SyntaxKind.StaticKeyword,
            SyntaxKind.StepKeyword,
            SyntaxKind.StopKeyword,
            SyntaxKind.StringKeyword,
            SyntaxKind.StructureKeyword,
            SyntaxKind.SubKeyword,
            SyntaxKind.SyncLockKeyword,
            SyntaxKind.ThenKeyword,
            SyntaxKind.ThrowKeyword,
            SyntaxKind.ToKeyword,
            SyntaxKind.TrueKeyword,
            SyntaxKind.TryKeyword,
            SyntaxKind.TryCastKeyword,
            SyntaxKind.TypeOfKeyword,
            SyntaxKind.UIntegerKeyword,
            SyntaxKind.ULongKeyword,
            SyntaxKind.UShortKeyword,
            SyntaxKind.UsingKeyword,
            SyntaxKind.WhenKeyword,
            SyntaxKind.WhileKeyword,
            SyntaxKind.WideningKeyword,
            SyntaxKind.WithKeyword,
            SyntaxKind.WithEventsKeyword,
            SyntaxKind.WriteOnlyKeyword,
            SyntaxKind.XorKeyword,
            SyntaxKind.EndIfKeyword,
            SyntaxKind.GosubKeyword,
            SyntaxKind.VariantKeyword,
            SyntaxKind.WendKeyword
            }
        ''' <summary>
        ''' Get all reserved keywords
        ''' </summary>
        Public Shared Function GetReservedKeywordKinds() As IEnumerable(Of SyntaxKind)
            Return s_reservedKeywords
        End Function

        Private Shared ReadOnly s_contextualKeywords As SyntaxKind() = New SyntaxKind() {
            SyntaxKind.AggregateKeyword,
            SyntaxKind.AllKeyword,
            SyntaxKind.AnsiKeyword,
            SyntaxKind.AscendingKeyword,
            SyntaxKind.AssemblyKeyword,
            SyntaxKind.AutoKeyword,
            SyntaxKind.BinaryKeyword,
            SyntaxKind.ByKeyword,
            SyntaxKind.CompareKeyword,
            SyntaxKind.CustomKeyword,
            SyntaxKind.DescendingKeyword,
            SyntaxKind.DisableKeyword,
            SyntaxKind.DistinctKeyword,
            SyntaxKind.EnableKeyword,
            SyntaxKind.EqualsKeyword,
            SyntaxKind.ExplicitKeyword,
            SyntaxKind.ExternalSourceKeyword,
            SyntaxKind.ExternalChecksumKeyword,
            SyntaxKind.FromKeyword,
            SyntaxKind.GroupKeyword,
            SyntaxKind.InferKeyword,
            SyntaxKind.IntoKeyword,
            SyntaxKind.IsFalseKeyword,
            SyntaxKind.IsTrueKeyword,
            SyntaxKind.JoinKeyword,
            SyntaxKind.KeyKeyword,
            SyntaxKind.MidKeyword,
            SyntaxKind.OffKeyword,
            SyntaxKind.OrderKeyword,
            SyntaxKind.OutKeyword,
            SyntaxKind.PreserveKeyword,
            SyntaxKind.RegionKeyword,
            SyntaxKind.ReferenceKeyword,
            SyntaxKind.SkipKeyword,
            SyntaxKind.StrictKeyword,
            SyntaxKind.TakeKeyword,
            SyntaxKind.TextKeyword,
            SyntaxKind.UnicodeKeyword,
            SyntaxKind.UntilKeyword,
            SyntaxKind.WarningKeyword,
            SyntaxKind.WhereKeyword,
            SyntaxKind.TypeKeyword,
            SyntaxKind.XmlKeyword,
            SyntaxKind.AsyncKeyword,
            SyntaxKind.AwaitKeyword,
            SyntaxKind.IteratorKeyword,
            SyntaxKind.YieldKeyword
            }
        ''' <summary>
        ''' Get contextual keywords
        ''' </summary>
        Public Shared Function GetContextualKeywordKinds() As IEnumerable(Of SyntaxKind)
            Return s_contextualKeywords
        End Function

        Private Shared ReadOnly s_punctuationKinds As SyntaxKind() = New SyntaxKind() {
            SyntaxKind.ExclamationToken,
            SyntaxKind.AtToken,
            SyntaxKind.CommaToken,
            SyntaxKind.HashToken,
            SyntaxKind.AmpersandToken,
            SyntaxKind.SingleQuoteToken,
            SyntaxKind.OpenParenToken,
            SyntaxKind.CloseParenToken,
            SyntaxKind.OpenBraceToken,
            SyntaxKind.CloseBraceToken,
            SyntaxKind.SemicolonToken,
            SyntaxKind.AsteriskToken,
            SyntaxKind.PlusToken,
            SyntaxKind.MinusToken,
            SyntaxKind.DotToken,
            SyntaxKind.SlashToken,
            SyntaxKind.ColonToken,
            SyntaxKind.LessThanToken,
            SyntaxKind.LessThanEqualsToken,
            SyntaxKind.LessThanGreaterThanToken,
            SyntaxKind.EqualsToken,
            SyntaxKind.GreaterThanToken,
            SyntaxKind.GreaterThanEqualsToken,
            SyntaxKind.BackslashToken,
            SyntaxKind.CaretToken,
            SyntaxKind.ColonEqualsToken,
            SyntaxKind.AmpersandEqualsToken,
            SyntaxKind.AsteriskEqualsToken,
            SyntaxKind.PlusEqualsToken,
            SyntaxKind.MinusEqualsToken,
            SyntaxKind.SlashEqualsToken,
            SyntaxKind.BackslashEqualsToken,
            SyntaxKind.CaretEqualsToken,
            SyntaxKind.LessThanLessThanToken,
            SyntaxKind.GreaterThanGreaterThanToken,
            SyntaxKind.LessThanLessThanEqualsToken,
            SyntaxKind.GreaterThanGreaterThanEqualsToken,
            SyntaxKind.QuestionToken,
            SyntaxKind.DoubleQuoteToken,
            SyntaxKind.StatementTerminatorToken,
            SyntaxKind.EndOfFileToken,
            SyntaxKind.EmptyToken
            }
        ''' <summary>
        ''' Get punctuations
        ''' </summary>
        Public Shared Function GetPunctuationKinds() As IEnumerable(Of SyntaxKind)
            Return s_punctuationKinds
        End Function

        Private Shared ReadOnly s_preprocessorKeywords As SyntaxKind() = New SyntaxKind() {
                                                            SyntaxKind.IfKeyword,
                                                            SyntaxKind.ThenKeyword,
                                                            SyntaxKind.ElseIfKeyword,
                                                            SyntaxKind.ElseKeyword,
                                                            SyntaxKind.EndIfKeyword,
                                                            SyntaxKind.EndKeyword,
                                                            SyntaxKind.RegionKeyword,
                                                            SyntaxKind.ConstKeyword,
                                                            SyntaxKind.ReferenceKeyword,
                                                            SyntaxKind.EnableKeyword,
                                                            SyntaxKind.DisableKeyword,
                                                            SyntaxKind.WarningKeyword,
                                                            SyntaxKind.ExternalSourceKeyword,
                                                            SyntaxKind.ExternalChecksumKeyword}

        ''' <summary>
        ''' Get preprocessor keywords
        ''' </summary>
        Public Shared Function GetPreprocessorKeywordKinds() As IEnumerable(Of SyntaxKind)
            Return s_preprocessorKeywords
        End Function

        Friend Shared Function IsSpecifier(kind As SyntaxKind) As Boolean
            Select Case kind
                Case SyntaxKind.PublicKeyword,
                    SyntaxKind.PrivateKeyword,
                    SyntaxKind.ProtectedKeyword,
                    SyntaxKind.FriendKeyword,
                    SyntaxKind.StaticKeyword,
                    SyntaxKind.SharedKeyword,
                    SyntaxKind.ShadowsKeyword,
                    SyntaxKind.MustInheritKeyword,
                    SyntaxKind.OverloadsKeyword,
                    SyntaxKind.NotInheritableKeyword,
                    SyntaxKind.OverridesKeyword,
                    SyntaxKind.PartialKeyword,
                    SyntaxKind.NotOverridableKeyword,
                    SyntaxKind.OverridableKeyword,
                    SyntaxKind.MustOverrideKeyword,
                    SyntaxKind.ReadOnlyKeyword,
                    SyntaxKind.WriteOnlyKeyword,
                    SyntaxKind.DimKeyword,
                    SyntaxKind.ConstKeyword,
                    SyntaxKind.DefaultKeyword,
                    SyntaxKind.WithEventsKeyword,
                    SyntaxKind.WideningKeyword,
                    SyntaxKind.NarrowingKeyword,
                    SyntaxKind.CustomKeyword,
                    SyntaxKind.AsyncKeyword,
                    SyntaxKind.IteratorKeyword
                    Return True
            End Select

            Return False
        End Function

        Friend Shared Function CanStartSpecifierDeclaration(kind As SyntaxKind) As Boolean

            Select Case kind

                Case SyntaxKind.PropertyKeyword,
                   SyntaxKind.IdentifierToken,
                   SyntaxKind.EnumKeyword,
                   SyntaxKind.ModuleKeyword,
                   SyntaxKind.StructureKeyword,
                   SyntaxKind.InterfaceKeyword,
                   SyntaxKind.ClassKeyword,
                   SyntaxKind.DeclareKeyword,
                   SyntaxKind.EventKeyword,
                   SyntaxKind.SubKeyword,
                   SyntaxKind.FunctionKeyword,
                   SyntaxKind.OperatorKeyword,
                   SyntaxKind.DelegateKeyword

                    Return True
            End Select

            Return False
        End Function

        ' Test whether a given token is a relational operator.

        Public Shared Function IsRelationalOperator(kind As SyntaxKind) As Boolean
            Select Case kind
                Case _
                    SyntaxKind.LessThanToken,
                    SyntaxKind.LessThanEqualsToken,
                    SyntaxKind.EqualsToken,
                    SyntaxKind.GreaterThanToken,
                    SyntaxKind.GreaterThanEqualsToken,
                    SyntaxKind.LessThanGreaterThanToken
                    Return True

                Case Else
                    Return False
            End Select
        End Function

        Public Shared Function IsOperator(kind As SyntaxKind) As Boolean
            Select Case (kind)

                Case SyntaxKind.AndKeyword,
                    SyntaxKind.AndAlsoKeyword,
                    SyntaxKind.CBoolKeyword,
                    SyntaxKind.CByteKeyword,
                    SyntaxKind.CCharKeyword,
                    SyntaxKind.CDateKeyword,
                    SyntaxKind.CDecKeyword,
                    SyntaxKind.CDblKeyword,
                    SyntaxKind.CIntKeyword,
                    SyntaxKind.CLngKeyword,
                    SyntaxKind.CObjKeyword,
                    SyntaxKind.CSByteKeyword,
                    SyntaxKind.CShortKeyword,
                    SyntaxKind.CSngKeyword,
                    SyntaxKind.CStrKeyword,
                    SyntaxKind.CTypeKeyword,
                    SyntaxKind.CUIntKeyword,
                    SyntaxKind.CULngKeyword,
                    SyntaxKind.CUShortKeyword,
                    SyntaxKind.DirectCastKeyword,
                    SyntaxKind.GetTypeKeyword,
                    SyntaxKind.NameOfKeyword,
                    SyntaxKind.IsKeyword,
                    SyntaxKind.IsFalseKeyword,
                    SyntaxKind.IsNotKeyword,
                    SyntaxKind.IsTrueKeyword,
                    SyntaxKind.LikeKeyword,
                    SyntaxKind.ModKeyword,
                    SyntaxKind.NewKeyword,
                    SyntaxKind.NotKeyword,
                    SyntaxKind.OrKeyword,
                    SyntaxKind.OrElseKeyword,
                    SyntaxKind.TryCastKeyword,
                    SyntaxKind.TypeOfKeyword,
                    SyntaxKind.XorKeyword,
                    SyntaxKind.PlusToken,
                    SyntaxKind.MinusToken,
                    SyntaxKind.AsteriskToken,
                    SyntaxKind.SlashToken,
                    SyntaxKind.CaretToken,
                    SyntaxKind.BackslashToken,
                    SyntaxKind.AmpersandToken,
                    SyntaxKind.LessThanLessThanToken,
                    SyntaxKind.GreaterThanGreaterThanToken,
                    SyntaxKind.EqualsToken,
                    SyntaxKind.LessThanGreaterThanToken,
                    SyntaxKind.LessThanToken,
                    SyntaxKind.LessThanEqualsToken,
                    SyntaxKind.GreaterThanEqualsToken,
                    SyntaxKind.GreaterThanToken,
                    SyntaxKind.ExclamationToken,
                    SyntaxKind.DotToken,
                    SyntaxKind.AmpersandEqualsToken,
                    SyntaxKind.AsteriskEqualsToken,
                    SyntaxKind.PlusEqualsToken,
                    SyntaxKind.MinusEqualsToken,
                    SyntaxKind.SlashEqualsToken,
                    SyntaxKind.BackslashEqualsToken,
                    SyntaxKind.CaretEqualsToken,
                    SyntaxKind.LessThanLessThanEqualsToken,
                    SyntaxKind.GreaterThanGreaterThanEqualsToken
                    Return True

                Case Else
                    Return False
            End Select
        End Function

        Public Shared Function IsPreprocessorDirective(kind As SyntaxKind) As Boolean
            Select Case kind
                Case SyntaxKind.IfDirectiveTrivia,
                    SyntaxKind.ElseIfDirectiveTrivia,
                    SyntaxKind.ElseDirectiveTrivia,
                    SyntaxKind.EndIfDirectiveTrivia,
                    SyntaxKind.RegionDirectiveTrivia,
                    SyntaxKind.EndRegionDirectiveTrivia,
                    SyntaxKind.ConstDirectiveTrivia,
                    SyntaxKind.ExternalSourceDirectiveTrivia,
                    SyntaxKind.EndExternalSourceDirectiveTrivia,
                    SyntaxKind.ExternalChecksumDirectiveTrivia,
                    SyntaxKind.EnableWarningDirectiveTrivia,
                    SyntaxKind.DisableWarningDirectiveTrivia,
                    SyntaxKind.ReferenceDirectiveTrivia,
                    SyntaxKind.BadDirectiveTrivia
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Friend Shared Function SupportsContinueStatement(kind As SyntaxKind) As Boolean
            Select Case kind
                Case SyntaxKind.WhileBlock,
                     SyntaxKind.ForBlock,
                     SyntaxKind.ForEachBlock,
                     SyntaxKind.SimpleDoLoopBlock,
                     SyntaxKind.DoWhileLoopBlock,
                     SyntaxKind.DoUntilLoopBlock

                    Return True
            End Select
            Return False
        End Function

        Friend Shared Function SupportsExitStatement(kind As SyntaxKind) As Boolean
            Select Case kind
                Case SyntaxKind.WhileBlock,
                     SyntaxKind.ForBlock,
                     SyntaxKind.ForEachBlock,
                     SyntaxKind.SimpleDoLoopBlock,
                     SyntaxKind.DoWhileLoopBlock,
                     SyntaxKind.DoUntilLoopBlock,
                     SyntaxKind.SelectBlock,
                     SyntaxKind.SubBlock,
                     SyntaxKind.SingleLineSubLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.PropertyBlock,
                     SyntaxKind.TryBlock
                    Return True
            End Select
            Return False
        End Function

        Friend Shared Function IsEndBlockLoopOrNextStatement(kind As SyntaxKind) As Boolean
            Select Case (kind)
                Case SyntaxKind.EndIfStatement,
                     SyntaxKind.EndWithStatement,
                     SyntaxKind.EndSelectStatement,
                     SyntaxKind.EndWhileStatement,
                     SyntaxKind.SimpleLoopStatement, SyntaxKind.LoopWhileStatement, SyntaxKind.LoopUntilStatement,
                     SyntaxKind.NextStatement,
                     SyntaxKind.EndStructureStatement,
                     SyntaxKind.EndEnumStatement,
                     SyntaxKind.EndPropertyStatement,
                     SyntaxKind.EndEventStatement,
                     SyntaxKind.EndInterfaceStatement,
                     SyntaxKind.EndTryStatement,
                     SyntaxKind.EndClassStatement,
                     SyntaxKind.EndModuleStatement,
                     SyntaxKind.EndNamespaceStatement,
                     SyntaxKind.EndUsingStatement,
                     SyntaxKind.EndSyncLockStatement,
                     SyntaxKind.EndSubStatement,
                     SyntaxKind.EndFunctionStatement,
                     SyntaxKind.EndOperatorStatement,
                     SyntaxKind.EndGetStatement,
                     SyntaxKind.EndSetStatement,
                     SyntaxKind.EndAddHandlerStatement,
                     SyntaxKind.EndRemoveHandlerStatement,
                     SyntaxKind.EndRaiseEventStatement
                    Return True

                Case Else
                    Return False
            End Select
        End Function

        Friend Shared Function IsXmlSyntax(kind As SyntaxKind) As Boolean
            Select Case kind
                Case SyntaxKind.XmlDocument,
                    SyntaxKind.XmlDeclaration,
                    SyntaxKind.XmlDeclarationOption,
                    SyntaxKind.XmlElement,
                    SyntaxKind.XmlText,
                    SyntaxKind.XmlElementStartTag,
                    SyntaxKind.XmlElementEndTag,
                    SyntaxKind.XmlEmptyElement,
                    SyntaxKind.XmlAttribute,
                    SyntaxKind.XmlString,
                    SyntaxKind.XmlName,
                    SyntaxKind.XmlBracketedName,
                    SyntaxKind.XmlPrefix,
                    SyntaxKind.XmlComment,
                    SyntaxKind.XmlCDataSection,
                    SyntaxKind.XmlEmbeddedExpression
                    Return True
            End Select
            Return False
        End Function

        Public Shared Function GetKeywordKind(text As String) As SyntaxKind
            text = MakeHalfWidthIdentifier(text)
            Dim result As SyntaxKind = KeywordTable.TokenOfString(text)

            If result <> SyntaxKind.IdentifierToken AndAlso Not IsContextualKeyword(result) Then
                Return result
            End If

            Return SyntaxKind.None
        End Function

        Public Shared Function GetAccessorStatementKind(keyword As SyntaxKind) As SyntaxKind
            Select Case keyword

                Case SyntaxKind.GetKeyword
                    Return SyntaxKind.GetAccessorStatement

                Case SyntaxKind.SetKeyword
                    Return SyntaxKind.SetAccessorStatement

                Case SyntaxKind.AddHandlerKeyword
                    Return SyntaxKind.AddHandlerStatement

                Case SyntaxKind.RemoveHandlerKeyword
                    Return SyntaxKind.RemoveHandlerStatement

                Case SyntaxKind.RaiseEventKeyword
                    Return SyntaxKind.RaiseEventAccessorStatement

                Case Else
                    Return SyntaxKind.None
            End Select
        End Function

        Public Shared Function GetBaseTypeStatementKind(keyword As SyntaxKind) As SyntaxKind
            Return If(keyword = SyntaxKind.EnumKeyword, SyntaxKind.EnumStatement, GetTypeStatementKind(keyword))
        End Function

        Public Shared Function GetTypeStatementKind(keyword As SyntaxKind) As SyntaxKind
            Select Case keyword

                Case SyntaxKind.ClassKeyword
                    Return SyntaxKind.ClassStatement

                Case SyntaxKind.InterfaceKeyword
                    Return SyntaxKind.InterfaceStatement

                Case SyntaxKind.StructureKeyword
                    Return SyntaxKind.StructureStatement

                Case Else
                    Return SyntaxKind.None
            End Select
        End Function

        Public Shared Function GetBinaryExpression(keyword As SyntaxKind) As SyntaxKind
            Select Case (keyword)

                Case SyntaxKind.IsKeyword
                    Return SyntaxKind.IsExpression

                Case SyntaxKind.IsNotKeyword
                    Return SyntaxKind.IsNotExpression

                Case SyntaxKind.LikeKeyword
                    Return SyntaxKind.LikeExpression

                Case SyntaxKind.AndKeyword
                    Return SyntaxKind.AndExpression

                Case SyntaxKind.AndAlsoKeyword
                    Return SyntaxKind.AndAlsoExpression
                Case SyntaxKind.OrKeyword
                    Return SyntaxKind.OrExpression

                Case SyntaxKind.OrElseKeyword
                    Return SyntaxKind.OrElseExpression

                Case SyntaxKind.XorKeyword
                    Return SyntaxKind.ExclusiveOrExpression

                Case SyntaxKind.AmpersandToken
                    Return SyntaxKind.ConcatenateExpression

                Case SyntaxKind.AsteriskToken
                    Return SyntaxKind.MultiplyExpression

                Case SyntaxKind.PlusToken
                    Return SyntaxKind.AddExpression

                Case SyntaxKind.MinusToken
                    Return SyntaxKind.SubtractExpression

                Case SyntaxKind.SlashToken
                    Return SyntaxKind.DivideExpression

                Case SyntaxKind.BackslashToken
                    Return SyntaxKind.IntegerDivideExpression

                Case SyntaxKind.ModKeyword
                    Return SyntaxKind.ModuloExpression

                Case SyntaxKind.CaretToken
                    Return SyntaxKind.ExponentiateExpression

                Case SyntaxKind.LessThanToken
                    Return SyntaxKind.LessThanExpression

                Case SyntaxKind.LessThanEqualsToken
                    Return SyntaxKind.LessThanOrEqualExpression

                Case SyntaxKind.LessThanGreaterThanToken
                    Return SyntaxKind.NotEqualsExpression

                Case SyntaxKind.EqualsToken
                    Return SyntaxKind.EqualsExpression

                Case SyntaxKind.GreaterThanToken
                    Return SyntaxKind.GreaterThanExpression

                Case SyntaxKind.GreaterThanEqualsToken
                    Return SyntaxKind.GreaterThanOrEqualExpression

                Case SyntaxKind.LessThanLessThanToken
                    Return SyntaxKind.LeftShiftExpression

                Case SyntaxKind.GreaterThanGreaterThanToken
                    Return SyntaxKind.RightShiftExpression

                Case Else
                    Return SyntaxKind.None
            End Select
        End Function

        Private Shared ReadOnly s_contextualKeywordToSyntaxKindMap As Dictionary(Of String, SyntaxKind) =
            New Dictionary(Of String, SyntaxKind)(IdentifierComparison.Comparer) From
            {
                   {"aggregate", SyntaxKind.AggregateKeyword},
                   {"all", SyntaxKind.AllKeyword},
                   {"ansi", SyntaxKind.AnsiKeyword},
                   {"ascending", SyntaxKind.AscendingKeyword},
                   {"assembly", SyntaxKind.AssemblyKeyword},
                   {"auto", SyntaxKind.AutoKeyword},
                   {"binary", SyntaxKind.BinaryKeyword},
                   {"by", SyntaxKind.ByKeyword},
                   {"compare", SyntaxKind.CompareKeyword},
                   {"custom", SyntaxKind.CustomKeyword},
                   {"descending", SyntaxKind.DescendingKeyword},
                   {"disable", SyntaxKind.DisableKeyword},
                   {"distinct", SyntaxKind.DistinctKeyword},
                   {"enable", SyntaxKind.EnableKeyword},
                   {"equals", SyntaxKind.EqualsKeyword},
                   {"explicit", SyntaxKind.ExplicitKeyword},
                   {"externalsource", SyntaxKind.ExternalSourceKeyword},
                   {"externalchecksum", SyntaxKind.ExternalChecksumKeyword},
                   {"from", SyntaxKind.FromKeyword},
                   {"group", SyntaxKind.GroupKeyword},
                   {"infer", SyntaxKind.InferKeyword},
                   {"into", SyntaxKind.IntoKeyword},
                   {"isfalse", SyntaxKind.IsFalseKeyword},
                   {"istrue", SyntaxKind.IsTrueKeyword},
                   {"join", SyntaxKind.JoinKeyword},
                   {"key", SyntaxKind.KeyKeyword},
                   {"mid", SyntaxKind.MidKeyword},
                   {"off", SyntaxKind.OffKeyword},
                   {"order", SyntaxKind.OrderKeyword},
                   {"out", SyntaxKind.OutKeyword},
                   {"preserve", SyntaxKind.PreserveKeyword},
                   {"region", SyntaxKind.RegionKeyword},
                   {"reference", SyntaxKind.ReferenceKeyword},
                   {"r", SyntaxKind.ReferenceKeyword},
                   {"skip", SyntaxKind.SkipKeyword},
                   {"strict", SyntaxKind.StrictKeyword},
                   {"take", SyntaxKind.TakeKeyword},
                   {"text", SyntaxKind.TextKeyword},
                   {"unicode", SyntaxKind.UnicodeKeyword},
                   {"until", SyntaxKind.UntilKeyword},
                   {"warning", SyntaxKind.WarningKeyword},
                   {"where", SyntaxKind.WhereKeyword},
                   {"type", SyntaxKind.TypeKeyword},
                   {"xml", SyntaxKind.XmlKeyword},
                   {"async", SyntaxKind.AsyncKeyword},
                   {"await", SyntaxKind.AwaitKeyword},
                   {"iterator", SyntaxKind.IteratorKeyword},
                   {"yield", SyntaxKind.YieldKeyword}
            }

        Public Shared Function GetContextualKeywordKind(text As String) As SyntaxKind
            text = MakeHalfWidthIdentifier(text)
            Dim kind As SyntaxKind = SyntaxKind.None
            Return If(s_contextualKeywordToSyntaxKindMap.TryGetValue(text, kind), kind, SyntaxKind.None)
        End Function

        Private Shared ReadOnly s_preprocessorKeywordToSyntaxKindMap As Dictionary(Of String, SyntaxKind) =
            New Dictionary(Of String, SyntaxKind)(IdentifierComparison.Comparer) From
            {
                   {"if", SyntaxKind.IfKeyword},
                   {"elseif", SyntaxKind.ElseIfKeyword},
                   {"else", SyntaxKind.ElseKeyword},
                   {"endif", SyntaxKind.EndIfKeyword},
                   {"region", SyntaxKind.RegionKeyword},
                   {"end", SyntaxKind.EndKeyword},
                   {"const", SyntaxKind.ConstKeyword},
                   {"externalsource", SyntaxKind.ExternalSourceKeyword},
                   {"externalchecksum", SyntaxKind.ExternalChecksumKeyword},
                   {"reference", SyntaxKind.ReferenceKeyword},
                   {"r", SyntaxKind.ReferenceKeyword},
                   {"enable", SyntaxKind.EnableKeyword},
                   {"disable", SyntaxKind.DisableKeyword}
            }

        Public Shared Function GetPreprocessorKeywordKind(text As String) As SyntaxKind
            text = MakeHalfWidthIdentifier(text)
            Dim kind As SyntaxKind = SyntaxKind.None
            Return If(s_preprocessorKeywordToSyntaxKindMap.TryGetValue(text, kind), kind, SyntaxKind.None)
        End Function

        Public Shared Function GetLiteralExpression(token As SyntaxKind) As SyntaxKind
            Select Case token

                Case SyntaxKind.IntegerLiteralToken, SyntaxKind.DecimalLiteralToken, SyntaxKind.FloatingLiteralToken
                    Return SyntaxKind.NumericLiteralExpression

                Case SyntaxKind.CharacterLiteralToken
                    Return SyntaxKind.CharacterLiteralExpression

                Case SyntaxKind.DateLiteralToken
                    Return SyntaxKind.DateLiteralExpression

                Case SyntaxKind.StringLiteralToken
                    Return SyntaxKind.StringLiteralExpression

                Case SyntaxKind.TrueKeyword
                    Return SyntaxKind.TrueLiteralExpression

                Case SyntaxKind.FalseKeyword
                    Return SyntaxKind.FalseLiteralExpression

                Case SyntaxKind.NothingKeyword
                    Return SyntaxKind.NothingLiteralExpression

                Case Else
                    Return SyntaxKind.None
            End Select

        End Function

    End Class

End Namespace
