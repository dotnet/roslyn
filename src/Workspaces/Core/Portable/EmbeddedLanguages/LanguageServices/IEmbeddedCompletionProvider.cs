//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.CodeAnalysis.Options;
//using Microsoft.CodeAnalysis.Text;

//namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
//{
//    internal interface IEmbeddedCompletionProvider
//    {
//        bool ShouldTriggerCompletion(SourceText text, int caretPosition, EmbeddedCompletionTrigger trigger, OptionSet options);
//        Task ProvideCompletionsAsync(EmbeddedCompletionContext embeddedContext);
//    }

//    internal struct EmbeddedCompletionChange
//    {
//        public readonly TextChange TextChange;
//        public readonly int? NewPosition;

//        public EmbeddedCompletionChange(
//            TextChange textChange, int? newPosition)
//        {
//            TextChange = textChange;
//            NewPosition = newPosition;
//        }
//    }

//    internal class EmbeddedCompletionContext
//    {
//        public readonly Document Document;
//        public readonly int Position;
//        public readonly EmbeddedCompletionTrigger Trigger;
//        public readonly OptionSet Options;
//        public readonly CancellationToken CancellationToken;

//        public TextSpan CompletionListSpan;
//        public readonly List<EmbeddedCompletionItem> Items = new List<EmbeddedCompletionItem>();
//        public readonly HashSet<string> Names = new HashSet<string>();

//        public EmbeddedCompletionContext(
//            Document document, int position, TextSpan completionListSpan,
//            EmbeddedCompletionTrigger trigger, OptionSet options, CancellationToken cancellationToken)
//        {
//            Document = document;
//            Position = position;
//            Trigger = trigger;
//            Options = options;
//            CancellationToken = cancellationToken;
//            CompletionListSpan = completionListSpan;
//        }
//    }

//    internal class EmbeddedCompletionItem
//    {
//        public readonly string DisplayText;
//        public readonly string Description;
//        public readonly EmbeddedCompletionChange Change;

//        public EmbeddedCompletionItem(
//            string displayText, string description, EmbeddedCompletionChange change)
//        {
//            DisplayText = displayText;
//            Description = description;
//            Change = change;
//        }
//    }

//    internal struct EmbeddedCompletionTrigger
//    {
//        public EmbeddedCompletionTrigger(EmbeddedCompletionTriggerKind kind, char character) : this()
//        {
//            Kind = kind;
//            Character = character;
//        }

//        public EmbeddedCompletionTriggerKind Kind { get; }
//        public char Character { get; }
//    }

//    internal enum EmbeddedCompletionTriggerKind
//    {
//        Invoke = 0,
//        Insertion = 1,
//        Deletion = 2,
//        Snippets = 3,
//        InvokeAndCommitIfUnique = 4
//    }
//}
