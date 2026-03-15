' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Completion
Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Tags

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(KeywordCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(FirstBuiltInCompletionProvider))>
    <[Shared]>
    Friend Class KeywordCompletionProvider
        Inherits AbstractKeywordCompletionProvider(Of VisualBasicSyntaxContext)

        Private Shared ReadOnly s_tags As ImmutableArray(Of String) = ImmutableArray.Create(WellKnownTags.Intrinsic)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(ImmutableArray.Create(Of IKeywordRecommender(Of VisualBasicSyntaxContext))(
                New KeywordRecommenders.ArrayStatements.EraseKeywordRecommender(),
                New KeywordRecommenders.ArrayStatements.PreserveKeywordRecommender(),
                New KeywordRecommenders.ArrayStatements.ReDimKeywordRecommender(),
                New KeywordRecommenders.Declarations.AliasKeywordRecommender(),
                New KeywordRecommenders.Declarations.AsKeywordRecommender(),
                New KeywordRecommenders.Declarations.AsyncKeywordRecommender(),
                New KeywordRecommenders.Declarations.AttributeScopesKeywordRecommender(),
                New KeywordRecommenders.Declarations.CharsetModifierKeywordRecommender(),
                New KeywordRecommenders.Declarations.ClassKeywordRecommender(),
                New KeywordRecommenders.Declarations.ConstKeywordRecommender(),
                New KeywordRecommenders.Declarations.CovarianceModifiersKeywordRecommender(),
                New KeywordRecommenders.Declarations.CustomEventKeywordRecommender(),
                New KeywordRecommenders.Declarations.DeclareKeywordRecommender(),
                New KeywordRecommenders.Declarations.DelegateKeywordRecommender(),
                New KeywordRecommenders.Declarations.DelegateSubFunctionKeywordRecommender(),
                New KeywordRecommenders.Declarations.DimKeywordRecommender(),
                New KeywordRecommenders.Declarations.EndBlockKeywordRecommender(),
                New KeywordRecommenders.Declarations.EnumKeywordRecommender(),
                New KeywordRecommenders.Declarations.EventKeywordRecommender(),
                New KeywordRecommenders.Declarations.ExternalSubFunctionKeywordRecommender(),
                New KeywordRecommenders.Declarations.FunctionKeywordRecommender(),
                New KeywordRecommenders.Declarations.GenericConstraintsKeywordRecommender(),
                New KeywordRecommenders.Declarations.GetSetKeywordRecommender(),
                New KeywordRecommenders.Declarations.ImplementsKeywordRecommender(),
                New KeywordRecommenders.Declarations.ImportsKeywordRecommender(),
                New KeywordRecommenders.Declarations.InheritsKeywordRecommender(),
                New KeywordRecommenders.Declarations.InKeywordRecommender(),
                New KeywordRecommenders.Declarations.InterfaceKeywordRecommender(),
                New KeywordRecommenders.Declarations.IteratorKeywordRecommender(),
                New KeywordRecommenders.Declarations.LibKeywordRecommender(),
                New KeywordRecommenders.Declarations.ModifierKeywordsRecommender(),
                New KeywordRecommenders.Declarations.ModuleKeywordRecommender(),
                New KeywordRecommenders.Declarations.NamespaceKeywordRecommender(),
                New KeywordRecommenders.Declarations.OfKeywordRecommender(),
                New KeywordRecommenders.Declarations.OperatorKeywordRecommender(),
                New KeywordRecommenders.Declarations.OverloadableOperatorRecommender(),
                New KeywordRecommenders.Declarations.ParameterModifiersKeywordRecommender(),
                New KeywordRecommenders.Declarations.PropertyKeywordRecommender(),
                New KeywordRecommenders.Declarations.StaticKeywordRecommender(),
                New KeywordRecommenders.Declarations.StructureKeywordRecommender(),
                New KeywordRecommenders.Declarations.SubKeywordRecommender(),
                New KeywordRecommenders.Declarations.ToKeywordRecommender(),
                New KeywordRecommenders.EventHandling.AddHandlerKeywordRecommender(),
                New KeywordRecommenders.EventHandling.HandlesKeywordRecommender(),
                New KeywordRecommenders.EventHandling.RaiseEventKeywordRecommender(),
                New KeywordRecommenders.EventHandling.RemoveHandlerKeywordRecommender(),
                New KeywordRecommenders.Expressions.AddressOfKeywordRecommender(),
                New KeywordRecommenders.Expressions.BinaryOperatorKeywordRecommender(),
                New KeywordRecommenders.Expressions.CastOperatorsKeywordRecommender(),
                New KeywordRecommenders.Expressions.FromKeywordRecommender(),
                New KeywordRecommenders.Expressions.GetTypeKeywordRecommender(),
                New KeywordRecommenders.Expressions.GetXmlNamespaceKeywordRecommender(),
                New KeywordRecommenders.Expressions.GlobalKeywordRecommender(),
                New KeywordRecommenders.Expressions.IfKeywordRecommender(),
                New KeywordRecommenders.Expressions.KeyKeywordRecommender(),
                New KeywordRecommenders.Expressions.MeKeywordRecommender(),
                New KeywordRecommenders.Expressions.MyBaseKeywordRecommender(),
                New KeywordRecommenders.Expressions.MyClassKeywordRecommender(),
                New KeywordRecommenders.Expressions.NameOfKeywordRecommender(),
                New KeywordRecommenders.Expressions.NewKeywordRecommender(),
                New KeywordRecommenders.Expressions.NothingKeywordRecommender(),
                New KeywordRecommenders.Expressions.NotKeywordRecommender(),
                New KeywordRecommenders.Expressions.LambdaKeywordRecommender(),
                New KeywordRecommenders.Expressions.TrueFalseKeywordRecommender(),
                New KeywordRecommenders.Expressions.TypeOfKeywordRecommender(),
                New KeywordRecommenders.Expressions.WithKeywordRecommender(),
                New KeywordRecommenders.OnErrorStatements.ErrorKeywordRecommender(),
                New KeywordRecommenders.OnErrorStatements.GoToDestinationsRecommender(),
                New KeywordRecommenders.OnErrorStatements.GoToKeywordRecommender(),
                New KeywordRecommenders.OnErrorStatements.NextKeywordRecommender(),
                New KeywordRecommenders.OnErrorStatements.OnErrorKeywordRecommender(),
                New KeywordRecommenders.OnErrorStatements.ResumeKeywordRecommender(),
                New KeywordRecommenders.OptionStatements.CompareBinaryTextRecommender(),
                New KeywordRecommenders.OptionStatements.ExplicitOptionsRecommender(),
                New KeywordRecommenders.OptionStatements.InferOptionsRecommender(),
                New KeywordRecommenders.OptionStatements.OptionKeywordRecommender(),
                New KeywordRecommenders.OptionStatements.OptionNamesRecommender(),
                New KeywordRecommenders.OptionStatements.StrictOptionsRecommender(),
                New KeywordRecommenders.PreprocessorDirectives.ConstDirectiveKeywordRecommender(),
                New KeywordRecommenders.PreprocessorDirectives.ElseDirectiveKeywordRecommender(),
                New KeywordRecommenders.PreprocessorDirectives.ElseIfDirectiveKeywordRecommender(),
                New KeywordRecommenders.PreprocessorDirectives.EndIfDirectiveKeywordRecommender(),
                New KeywordRecommenders.PreprocessorDirectives.EndRegionDirectiveKeywordRecommender(),
                New KeywordRecommenders.PreprocessorDirectives.IfDirectiveKeywordRecommender(),
                New KeywordRecommenders.PreprocessorDirectives.ReferenceDirectiveKeywordRecommender(),
                New KeywordRecommenders.PreprocessorDirectives.RegionDirectiveKeywordRecommender(),
                New KeywordRecommenders.PreprocessorDirectives.WarningDirectiveKeywordRecommender(),
                New KeywordRecommenders.Queries.AggregateKeywordRecommender(),
                New KeywordRecommenders.Queries.AscendingDescendingKeywordRecommender(),
                New KeywordRecommenders.Queries.DistinctKeywordRecommender(),
                New KeywordRecommenders.Queries.EqualsKeywordRecommender(),
                New KeywordRecommenders.Queries.FromKeywordRecommender(),
                New KeywordRecommenders.Queries.GroupByKeywordRecommender(),
                New KeywordRecommenders.Queries.GroupJoinKeywordRecommender(),
                New KeywordRecommenders.Queries.GroupKeywordRecommender(),
                New KeywordRecommenders.Queries.IntoKeywordRecommender(),
                New KeywordRecommenders.Queries.JoinKeywordRecommender(),
                New KeywordRecommenders.Queries.LetKeywordRecommender(),
                New KeywordRecommenders.Queries.OnKeywordRecommender(),
                New KeywordRecommenders.Queries.OrderByKeywordRecommender(),
                New KeywordRecommenders.Queries.SelectKeywordRecommender(),
                New KeywordRecommenders.Queries.SkipKeywordRecommender(),
                New KeywordRecommenders.Queries.TakeKeywordRecommender(),
                New KeywordRecommenders.Queries.WhereKeywordRecommender(),
                New KeywordRecommenders.Queries.WhileKeywordRecommender(),
                New KeywordRecommenders.Statements.CallKeywordRecommender(),
                New KeywordRecommenders.Statements.CaseKeywordRecommender(),
                New KeywordRecommenders.Statements.CatchKeywordRecommender(),
                New KeywordRecommenders.Statements.ContinueKeywordRecommender(),
                New KeywordRecommenders.Statements.DoKeywordRecommender(),
                New KeywordRecommenders.Statements.EachKeywordRecommender(),
                New KeywordRecommenders.Statements.ElseIfKeywordRecommender(),
                New KeywordRecommenders.Statements.ElseKeywordRecommender(),
                New KeywordRecommenders.Statements.EndKeywordRecommender(),
                New KeywordRecommenders.Statements.ExitKeywordRecommender(),
                New KeywordRecommenders.Statements.FinallyKeywordRecommender(),
                New KeywordRecommenders.Statements.ForKeywordRecommender(),
                New KeywordRecommenders.Statements.GotoKeywordRecommender(),
                New KeywordRecommenders.Statements.IfKeywordRecommender(),
                New KeywordRecommenders.Statements.IsKeywordRecommender(),
                New KeywordRecommenders.Statements.LoopKeywordRecommender(),
                New KeywordRecommenders.Statements.MidKeywordRecommender(),
                New KeywordRecommenders.Statements.NextKeywordRecommender(),
                New KeywordRecommenders.Statements.ReturnKeywordRecommender(),
                New KeywordRecommenders.Statements.SelectKeywordRecommender(),
                New KeywordRecommenders.Statements.StepKeywordRecommender(),
                New KeywordRecommenders.Statements.StopKeywordRecommender(),
                New KeywordRecommenders.Statements.SyncLockKeywordRecommender(),
                New KeywordRecommenders.Statements.ThenKeywordRecommender(),
                New KeywordRecommenders.Statements.ThrowKeywordRecommender(),
                New KeywordRecommenders.Statements.ToKeywordRecommender(),
                New KeywordRecommenders.Statements.TryKeywordRecommender(),
                New KeywordRecommenders.Statements.UntilAndWhileKeywordRecommender(),
                New KeywordRecommenders.Statements.UsingKeywordRecommender(),
                New KeywordRecommenders.Statements.WhenKeywordRecommender(),
                New KeywordRecommenders.Statements.WhileLoopKeywordRecommender(),
                New KeywordRecommenders.Statements.WithKeywordRecommender(),
                New KeywordRecommenders.Types.BuiltInTypesKeywordRecommender()))
        End Sub

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            ' We show 'Of' after dim x as new list(
            Return CompletionUtilities.IsDefaultTriggerCharacterOrParen(text, characterPosition, options)
        End Function

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = CompletionUtilities.CommonTriggerCharsAndParen

        Private Shared ReadOnly s_tupleRules As CompletionItemRules = CompletionItemRules.Default.
            WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ":"c))

        Protected Overrides Function CreateItem(keyword As RecommendedKeyword, context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As CompletionItem
            Dim rules = If(context.IsPossibleTupleContext, s_tupleRules, CompletionItemRules.Default)

            Return CommonCompletionItem.Create(
                displayText:=keyword.Keyword,
                displayTextSuffix:="",
                description:=keyword.DescriptionFactory(cancellationToken),
                glyph:=Glyph.Keyword,
                tags:=s_tags,
                rules:=rules.WithMatchPriority(keyword.MatchPriority))
        End Function
    End Class
End Namespace
