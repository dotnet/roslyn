// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class KeywordCompletionProvider : AbstractKeywordCompletionProvider<CSharpSyntaxContext>
    {
        public KeywordCompletionProvider()
            : base(GetKeywordRecommenders())
        {
        }

        private static ImmutableArray<IKeywordRecommender<CSharpSyntaxContext>> GetKeywordRecommenders()
        {
            return new IKeywordRecommender<CSharpSyntaxContext>[]
            {
                new AbstractKeywordRecommender(),
                new AddKeywordRecommender(),
                new AliasKeywordRecommender(),
                new AnnotationsKeywordRecommender(),
                new AscendingKeywordRecommender(),
                new AsKeywordRecommender(),
                new AssemblyKeywordRecommender(),
                new AsyncKeywordRecommender(),
                new AwaitKeywordRecommender(),
                new BaseKeywordRecommender(),
                new BoolKeywordRecommender(),
                new BreakKeywordRecommender(),
                new ByKeywordRecommender(),
                new ByteKeywordRecommender(),
                new CaseKeywordRecommender(),
                new CatchKeywordRecommender(),
                new CharKeywordRecommender(),
                new CheckedKeywordRecommender(),
                new ChecksumKeywordRecommender(),
                new ClassKeywordRecommender(),
                new ConstKeywordRecommender(),
                new ContinueKeywordRecommender(),
                new DecimalKeywordRecommender(),
                new DefaultKeywordRecommender(),
                new DefineKeywordRecommender(),
                new DelegateKeywordRecommender(),
                new DescendingKeywordRecommender(),
                new DisableKeywordRecommender(),
                new DoKeywordRecommender(),
                new DoubleKeywordRecommender(),
                new DynamicKeywordRecommender(),
                new ElifKeywordRecommender(),
                new ElseKeywordRecommender(),
                new EnableKeywordRecommender(),
                new EndIfKeywordRecommender(),
                new EndRegionKeywordRecommender(),
                new EnumKeywordRecommender(),
                new EqualsKeywordRecommender(),
                new ErrorKeywordRecommender(),
                new EventKeywordRecommender(),
                new ExplicitKeywordRecommender(),
                new ExternKeywordRecommender(),
                new FalseKeywordRecommender(),
                new FieldKeywordRecommender(),
                new FinallyKeywordRecommender(),
                new FixedKeywordRecommender(),
                new FloatKeywordRecommender(),
                new ForEachKeywordRecommender(),
                new ForKeywordRecommender(),
                new FromKeywordRecommender(),
                new GetKeywordRecommender(),
                new GlobalKeywordRecommender(),
                new GotoKeywordRecommender(),
                new GroupKeywordRecommender(),
                new HiddenKeywordRecommender(),
                new IfKeywordRecommender(),
                new ImplicitKeywordRecommender(),
                new InKeywordRecommender(),
                new InterfaceKeywordRecommender(),
                new InternalKeywordRecommender(),
                new IntKeywordRecommender(),
                new IntoKeywordRecommender(),
                new IsKeywordRecommender(),
                new JoinKeywordRecommender(),
                new LetKeywordRecommender(),
                new LineKeywordRecommender(),
                new LoadKeywordRecommender(),
                new LockKeywordRecommender(),
                new LongKeywordRecommender(),
                new MethodKeywordRecommender(),
                new ModuleKeywordRecommender(),
                new NameOfKeywordRecommender(),
                new NamespaceKeywordRecommender(),
                new NewKeywordRecommender(),
                new NotNullKeywordRecommender(),
                new NullableKeywordRecommender(),
                new NullKeywordRecommender(),
                new ObjectKeywordRecommender(),
                new OnKeywordRecommender(),
                new OperatorKeywordRecommender(),
                new OrderByKeywordRecommender(),
                new OutKeywordRecommender(),
                new OverrideKeywordRecommender(),
                new ParamKeywordRecommender(),
                new ParamsKeywordRecommender(),
                new PartialKeywordRecommender(),
                new PragmaKeywordRecommender(),
                new PrivateKeywordRecommender(),
                new PropertyKeywordRecommender(),
                new ProtectedKeywordRecommender(),
                new PublicKeywordRecommender(),
                new ReadOnlyKeywordRecommender(),
                new ReferenceKeywordRecommender(),
                new RefKeywordRecommender(),
                new RegionKeywordRecommender(),
                new RemoveKeywordRecommender(),
                new RestoreKeywordRecommender(),
                new ReturnKeywordRecommender(),
                new SByteKeywordRecommender(),
                new SealedKeywordRecommender(),
                new SelectKeywordRecommender(),
                new SetKeywordRecommender(),
                new ShortKeywordRecommender(),
                new SizeOfKeywordRecommender(),
                new StackAllocKeywordRecommender(),
                new StaticKeywordRecommender(),
                new StringKeywordRecommender(),
                new StructKeywordRecommender(),
                new SwitchKeywordRecommender(),
                new ThisKeywordRecommender(),
                new ThrowKeywordRecommender(),
                new TrueKeywordRecommender(),
                new TryKeywordRecommender(),
                new TypeKeywordRecommender(),
                new TypeOfKeywordRecommender(),
                new TypeVarKeywordRecommender(),
                new UIntKeywordRecommender(),
                new ULongKeywordRecommender(),
                new UncheckedKeywordRecommender(),
                new UndefKeywordRecommender(),
                new UnmanagedKeywordRecommender(),
                new UnsafeKeywordRecommender(),
                new UShortKeywordRecommender(),
                new UsingKeywordRecommender(),
                new VarKeywordRecommender(),
                new VirtualKeywordRecommender(),
                new VoidKeywordRecommender(),
                new VolatileKeywordRecommender(),
                new WarningKeywordRecommender(),
                new WarningsKeywordRecommender(),
                new WhenKeywordRecommender(),
                new WhereKeywordRecommender(),
                new WhileKeywordRecommender(),
                new YieldKeywordRecommender(),
            }.ToImmutableArray();
        }

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        protected override async Task<CSharpSyntaxContext> CreateContextAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var span = new TextSpan(position, length: 0);
            var semanticModel = await document.GetSemanticModelForSpanAsync(span, cancellationToken).ConfigureAwait(false);
            return CSharpSyntaxContext.CreateContext(document.Project.Solution.Workspace, semanticModel, position, cancellationToken);
        }

        private static readonly CompletionItemRules s_tupleRules = CompletionItemRules.Default.
           WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ':'));

        protected override CompletionItem CreateItem(RecommendedKeyword keyword, CSharpSyntaxContext context)
        {
            var rules = context.IsPossibleTupleContext ? s_tupleRules : CompletionItemRules.Default;

            return CommonCompletionItem.Create(
                displayText: keyword.Keyword,
                displayTextSuffix: "",
                description: keyword.DescriptionFactory(CancellationToken.None),
                glyph: Glyph.Keyword,
                rules: rules.WithMatchPriority(keyword.MatchPriority)
                            .WithFormatOnCommit(keyword.ShouldFormatOnCommit));
        }

        internal override TextSpan GetCurrentSpan(TextSpan span, SourceText text)
        {
            return CompletionUtilities.GetCompletionItemSpan(text, span.End);
        }
    }
}
