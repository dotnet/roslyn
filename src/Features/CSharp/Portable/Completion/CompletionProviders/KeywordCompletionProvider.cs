// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(KeywordCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(NamedParameterCompletionProvider))]
    [Shared]
    internal class KeywordCompletionProvider : AbstractKeywordCompletionProvider<CSharpSyntaxContext>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public KeywordCompletionProvider()
            : base(ImmutableArray.Create<IKeywordRecommender<CSharpSyntaxContext>>(
                new AbstractKeywordRecommender(),
                new AddKeywordRecommender(),
                new AliasKeywordRecommender(),
                new AndKeywordRecommender(),
                new AnnotationsKeywordRecommender(),
                new AscendingKeywordRecommender(),
                new AsKeywordRecommender(),
                new AssemblyKeywordRecommender(),
                new AsyncKeywordRecommender(),
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
                new InitKeywordRecommender(),
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
                new ManagedKeywordRecommender(),
                new MethodKeywordRecommender(),
                new ModuleKeywordRecommender(),
                new NameOfKeywordRecommender(),
                new NamespaceKeywordRecommender(),
                new NewKeywordRecommender(),
                new NintKeywordRecommender(),
                new NotKeywordRecommender(),
                new NotNullKeywordRecommender(),
                new NuintKeywordRecommender(),
                new NullableKeywordRecommender(),
                new NullKeywordRecommender(),
                new ObjectKeywordRecommender(),
                new OnKeywordRecommender(),
                new OperatorKeywordRecommender(),
                new OrderByKeywordRecommender(),
                new OrKeywordRecommender(),
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
                new RecordKeywordRecommender(),
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
                new WithKeywordRecommender(),
                new YieldKeywordRecommender()))
        {
        }

        internal override string Language => LanguageNames.CSharp;

        public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
            => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);

        public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharacters;

        private static readonly CompletionItemRules s_tupleRules = CompletionItemRules.Default.
           WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ':'));

        protected override CompletionItem CreateItem(RecommendedKeyword keyword, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var rules = context.IsPossibleTupleContext ? s_tupleRules : CompletionItemRules.Default;

            return CommonCompletionItem.Create(
                displayText: keyword.Keyword,
                displayTextSuffix: "",
                description: keyword.DescriptionFactory(cancellationToken),
                glyph: Glyph.Keyword,
                rules: rules.WithMatchPriority(keyword.MatchPriority)
                            .WithFormatOnCommit(keyword.ShouldFormatOnCommit));
        }
    }
}
