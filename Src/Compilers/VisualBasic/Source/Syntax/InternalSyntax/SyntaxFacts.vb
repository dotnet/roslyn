Imports System.Runtime.CompilerServices

'-----------------------------------------------------------------------------------------------------------
'
'  Copyright (c) Microsoft Corporation.  All rights reserved.
'
'-----------------------------------------------------------------------------------------------------------

Namespace Roslyn.Compilers.VisualBasic.InternalSyntax

    Friend Module SyntaxFacts

        ''' <summary>
        ''' Determine if the SyntaxNode instance represents a Keyword
        ''' </summary>
        Friend Function IsKeyword(ByVal node As SyntaxNode) As Boolean
            If node Is Nothing Then
                Throw New ArgumentNullException("node")
            End If

            Return TypeOf node Is KeywordSyntax
        End Function

        <Extension()>
        Public Function IsKeyword(ByVal token As SyntaxToken) As Boolean
            Select Case token.Kind
                Case SyntaxKind.AddHandlerKeyword,
                    SyntaxKind.AddressOfKeyword,
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
                    SyntaxKind.WendKeyword,
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
                    SyntaxKind.DistinctKeyword,
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
                    SyntaxKind.SkipKeyword,
                    SyntaxKind.StrictKeyword,
                    SyntaxKind.TakeKeyword,
                    SyntaxKind.TextKeyword,
                    SyntaxKind.UnicodeKeyword,
                    SyntaxKind.UntilKeyword,
                    SyntaxKind.WhereKeyword,
                    SyntaxKind.TypeKeyword,
                    SyntaxKind.XmlKeyword
                    Return True
            End Select

            Return False
        End Function

        ''' <summary>
        ''' Determine if the SyntaxNode instance represents a Token
        ''' </summary>
        Friend Function IsToken(ByVal node As SyntaxNode) As Boolean
            If node Is Nothing Then
                Throw New ArgumentNullException("node")
            End If

            Return TypeOf node Is SyntaxToken
        End Function


        Friend Function IsPredefinedTypeKeyword(ByVal kind As SyntaxKind) As Boolean

            Select Case (kind)
                Case _
                    SyntaxKind.ShortKeyword,
                    SyntaxKind.UShortKeyword,
                    SyntaxKind.IntegerKeyword,
                    SyntaxKind.UIntegerKeyword,
                    SyntaxKind.LongKeyword,
                    SyntaxKind.ULongKeyword,
                    SyntaxKind.DecimalKeyword,
                    SyntaxKind.SingleKeyword,
                    SyntaxKind.DoubleKeyword,
                    SyntaxKind.SByteKeyword,
                    SyntaxKind.ByteKeyword,
                    SyntaxKind.BooleanKeyword,
                    SyntaxKind.CharKeyword,
                    SyntaxKind.DateKeyword,
                    SyntaxKind.StringKeyword,
                    SyntaxKind.ObjectKeyword
                    Return True
            End Select

            Return False
        End Function

        Friend Function IsCastTypeKeyword(ByVal kind As SyntaxKind) As Boolean
            Select Case (kind)

                Case _
                    SyntaxKind.CBoolKeyword,
                    SyntaxKind.CCharKeyword,
                    SyntaxKind.CDateKeyword,
                    SyntaxKind.CDblKeyword,
                    SyntaxKind.CSByteKeyword,
                    SyntaxKind.CByteKeyword,
                    SyntaxKind.CShortKeyword,
                    SyntaxKind.CUShortKeyword,
                    SyntaxKind.CIntKeyword,
                    SyntaxKind.CUIntKeyword,
                    SyntaxKind.CLngKeyword,
                    SyntaxKind.CULngKeyword,
                    SyntaxKind.CSngKeyword,
                    SyntaxKind.CStrKeyword,
                    SyntaxKind.CDecKeyword,
                    SyntaxKind.CObjKeyword
                    Return True
            End Select

            Return False
        End Function

        Friend Function SupportsContinueStatement(ByVal kind As SyntaxKind) As Boolean
            Select Case kind
                Case _
                    SyntaxKind.WhileBlock,
                    SyntaxKind.ForBlock,
                    SyntaxKind.ForEachBlock,
                    SyntaxKind.DoLoopTopTestBlock,
                    SyntaxKind.DoLoopForeverBlock,
                    SyntaxKind.DoLoopTopTestBlock
                    Return True
            End Select
            Return False
        End Function


        Friend Function SupportsExitStatement(ByVal kind As SyntaxKind) As Boolean
            Select Case kind
                Case _
                    SyntaxKind.WhileBlock,
                    SyntaxKind.ForBlock,
                    SyntaxKind.ForEachBlock,
                    SyntaxKind.DoLoopTopTestBlock,
                    SyntaxKind.DoLoopForeverBlock,
                    SyntaxKind.DoLoopTopTestBlock,
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

        Friend Function IsEndBlockStatement(ByVal kind As SyntaxKind) As Boolean
            Select Case (kind)
                Case SyntaxKind.EndIfStatement,
                SyntaxKind.EndWithStatement,
                SyntaxKind.EndSelectStatement,
                SyntaxKind.EndWhileStatement,
                SyntaxKind.LoopStatement,
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

        <Extension()>
        Friend Function IsXmlSyntax(ByVal kind As SyntaxKind) As Boolean

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

    End Module
End Namespace

