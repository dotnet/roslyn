// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal readonly struct Directive
    {
        private readonly DirectiveTriviaSyntax _node;

        internal Directive(DirectiveTriviaSyntax node)
        {
            _node = node;
        }

        public SyntaxKind Kind
        {
            get
            {
                return _node.Kind;
            }
        }

        public bool IncrementallyEquivalent(Directive other)
        {
            if (this.Kind != other.Kind)
            {
                return false;
            }

            bool isActive = this.IsActive;
            bool otherIsActive = other.IsActive;

            // states of inactive directives don't matter
            if (!isActive && !otherIsActive)
            {
                return true;
            }

            if (isActive != otherIsActive)
            {
                return false;
            }

            switch (this.Kind)
            {
                case SyntaxKind.DefineDirectiveTrivia:
                case SyntaxKind.UndefDirectiveTrivia:
                    return this.GetIdentifier() == other.GetIdentifier();
                case SyntaxKind.IfDirectiveTrivia:
                case SyntaxKind.ElifDirectiveTrivia:
                case SyntaxKind.ElseDirectiveTrivia:
                    return this.BranchTaken == other.BranchTaken;
                default:
                    return true;
            }
        }

        // Can't be private as it's called by DirectiveStack in its GetDebuggerDisplay()
        internal string GetDebuggerDisplay()
        {
            var writer = new System.IO.StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            _node.WriteTo(writer, false, false);
            return writer.ToString();
        }

        internal string? GetIdentifier()
        {
            switch (_node.Kind)
            {
                case SyntaxKind.DefineDirectiveTrivia:
                    return ((DefineDirectiveTriviaSyntax)_node).Name.ValueText;
                case SyntaxKind.UndefDirectiveTrivia:
                    return ((UndefDirectiveTriviaSyntax)_node).Name.ValueText;
                default:
                    return null;
            }
        }

        internal bool IsActive
        {
            get { return _node.IsActive; }
        }

        internal bool BranchTaken
        {
            get
            {
                var branching = _node as BranchingDirectiveTriviaSyntax;
                if (branching != null)
                {
                    return branching.BranchTaken;
                }

                return false;
            }
        }
    }

    internal enum DefineState
    {
        Defined,
        Undefined,
        Unspecified
    }

    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal readonly struct DirectiveStack
    {
        public static readonly DirectiveStack Empty = new DirectiveStack(ConsList<Directive>.Empty);

        private readonly ConsList<Directive>? _directives;

        private DirectiveStack(ConsList<Directive>? directives)
        {
            _directives = directives;
        }

        public static void InterlockedInitialize(ref DirectiveStack location, DirectiveStack value)
            => Interlocked.CompareExchange(ref Unsafe.AsRef(in location._directives), value._directives, null);

        public bool IsNull
        {
            get
            {
                return _directives == null;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return _directives == ConsList<Directive>.Empty;
            }
        }

        public DefineState IsDefined(string id)
        {
            for (var current = _directives; current != null && current.Any(); current = current.Tail)
            {
                switch (current.Head.Kind)
                {
                    case SyntaxKind.DefineDirectiveTrivia:
                        if (current.Head.GetIdentifier() == id)
                        {
                            return DefineState.Defined;
                        }

                        break;
                    case SyntaxKind.UndefDirectiveTrivia:
                        if (current.Head.GetIdentifier() == id)
                        {
                            return DefineState.Undefined;
                        }

                        break;

                    case SyntaxKind.ElifDirectiveTrivia:
                    case SyntaxKind.ElseDirectiveTrivia:
                        // Skip directives from previous branches of the same #if.
                        do
                        {
                            current = current.Tail;

                            if (current == null || !current.Any())
                            {
                                return DefineState.Unspecified;
                            }
                        }
                        while (current.Head.Kind != SyntaxKind.IfDirectiveTrivia);

                        break;
                }
            }

            return DefineState.Unspecified;
        }

        // true if any previous section of the closest #if has its branch taken
        public bool PreviousBranchTaken()
        {
            for (var current = _directives; current != null && current.Any(); current = current.Tail)
            {
                if (current.Head.BranchTaken)
                {
                    return true;
                }
                else if (current.Head.Kind == SyntaxKind.IfDirectiveTrivia)
                {
                    return false;
                }
            }

            return false;
        }

        public bool HasUnfinishedIf()
        {
            var prev = GetPreviousIfElifElseOrRegion(_directives);
            return prev != null && prev.Any() && prev.Head.Kind != SyntaxKind.RegionDirectiveTrivia;
        }

        public bool HasPreviousIfOrElif()
        {
            var prev = GetPreviousIfElifElseOrRegion(_directives);
            return prev != null && prev.Any() && (prev.Head.Kind == SyntaxKind.IfDirectiveTrivia || prev.Head.Kind == SyntaxKind.ElifDirectiveTrivia);
        }

        public bool HasUnfinishedRegion()
        {
            var prev = GetPreviousIfElifElseOrRegion(_directives);
            return prev != null && prev.Any() && prev.Head.Kind == SyntaxKind.RegionDirectiveTrivia;
        }

        public DirectiveStack Add(Directive directive)
        {
            switch (directive.Kind)
            {
                case SyntaxKind.EndIfDirectiveTrivia:
                    var prevIf = GetPreviousIf(_directives);
                    if (prevIf == null || !prevIf.Any())
                    {
                        goto default; // no matching if directive !! leave directive alone
                    }

                    RoslynDebug.AssertNotNull(_directives); // If 'prevIf' isn't null, then '_directives' wasn't null.
                    return new DirectiveStack(CompleteIf(_directives, out _));
                case SyntaxKind.EndRegionDirectiveTrivia:
                    var prevRegion = GetPreviousRegion(_directives);
                    if (prevRegion == null || !prevRegion.Any())
                    {
                        goto default; // no matching region directive !! leave directive alone
                    }

                    RoslynDebug.AssertNotNull(_directives); // If 'prevRegion' isn't null, then '_directives' wasn't null.
                    return new DirectiveStack(CompleteRegion(_directives)); // remove region directives from stack but leave everything else
                default:
                    return new DirectiveStack(new ConsList<Directive>(directive, _directives ?? ConsList<Directive>.Empty));
            }
        }

        // removes unfinished if & related directives from stack and leaves active branch directives
        private static ConsList<Directive> CompleteIf(ConsList<Directive> stack, out bool include)
        {
            // if we get to the top, the default rule is to include anything that follows
            if (!stack.Any())
            {
                include = true;
                return stack;
            }

            // if we reach the #if directive, then we stop unwinding and start
            // rebuilding the stack w/o the #if/#elif/#else/#endif directives
            // only including content from sections that are considered included
            if (stack.Head.Kind == SyntaxKind.IfDirectiveTrivia)
            {
                include = stack.Head.BranchTaken;
                return stack.Tail;
            }

            var newStack = CompleteIf(stack.Tail, out include);
            switch (stack.Head.Kind)
            {
                case SyntaxKind.ElifDirectiveTrivia:
                case SyntaxKind.ElseDirectiveTrivia:
                    include = stack.Head.BranchTaken;
                    break;
                default:
                    if (include)
                    {
                        newStack = new ConsList<Directive>(stack.Head, newStack);
                    }

                    break;
            }

            return newStack;
        }

        // removes region directives from stack but leaves everything else
        private static ConsList<Directive> CompleteRegion(ConsList<Directive> stack)
        {
            // if we get to the top, the default rule is to include anything that follows
            if (!stack.Any())
            {
                return stack;
            }

            if (stack.Head.Kind == SyntaxKind.RegionDirectiveTrivia)
            {
                return stack.Tail;
            }

            var newStack = CompleteRegion(stack.Tail);
            newStack = new ConsList<Directive>(stack.Head, newStack);
            return newStack;
        }

        private static ConsList<Directive>? GetPreviousIf(ConsList<Directive>? directives)
        {
            var current = directives;
            while (current != null && current.Any())
            {
                switch (current.Head.Kind)
                {
                    case SyntaxKind.IfDirectiveTrivia:
                        return current;
                }

                current = current.Tail;
            }

            return current;
        }

        private static ConsList<Directive>? GetPreviousIfElifElseOrRegion(ConsList<Directive>? directives)
        {
            var current = directives;
            while (current != null && current.Any())
            {
                switch (current.Head.Kind)
                {
                    case SyntaxKind.IfDirectiveTrivia:
                    case SyntaxKind.ElifDirectiveTrivia:
                    case SyntaxKind.ElseDirectiveTrivia:
                    case SyntaxKind.RegionDirectiveTrivia:
                        return current;
                }

                current = current.Tail;
            }

            return current;
        }

        private static ConsList<Directive>? GetPreviousRegion(ConsList<Directive>? directives)
        {
            var current = directives;
            while (current != null && current.Any() && current.Head.Kind != SyntaxKind.RegionDirectiveTrivia)
            {
                current = current.Tail;
            }

            return current;
        }

        internal string GetDebuggerDisplay()
        {
            if (IsNull)
            {
                return "<null>";
            }

            if (IsEmpty)
            {
                return "[]";
            }

            var sb = new StringBuilder();
            for (var current = _directives; current != null && current.Any(); current = current.Tail)
            {
                if (sb.Length > 0)
                {
                    sb.Insert(0, " | ");
                }

                sb.Insert(0, current.Head.GetDebuggerDisplay());
            }

            return sb.ToString();
        }

        public bool IncrementallyEquivalent(DirectiveStack other)
        {
            var mine = SkipInsignificantDirectives(_directives);
            var theirs = SkipInsignificantDirectives(other._directives);
            bool mineHasAny = mine != null && mine.Any();
            bool theirsHasAny = theirs != null && theirs.Any();
            while (mineHasAny && theirsHasAny)
            {
                if (!mine!.Head.IncrementallyEquivalent(theirs!.Head))
                {
                    return false;
                }

                mine = SkipInsignificantDirectives(mine.Tail);
                theirs = SkipInsignificantDirectives(theirs.Tail);
                mineHasAny = mine != null && mine.Any();
                theirsHasAny = theirs != null && theirs.Any();
            }

            return mineHasAny == theirsHasAny;
        }

        private static ConsList<Directive>? SkipInsignificantDirectives(ConsList<Directive>? directives)
        {
            for (; directives != null && directives.Any(); directives = directives.Tail)
            {
                switch (directives.Head.Kind)
                {
                    case SyntaxKind.IfDirectiveTrivia:
                    case SyntaxKind.ElifDirectiveTrivia:
                    case SyntaxKind.ElseDirectiveTrivia:
                    case SyntaxKind.EndIfDirectiveTrivia:
                    case SyntaxKind.DefineDirectiveTrivia:
                    case SyntaxKind.UndefDirectiveTrivia:
                    case SyntaxKind.RegionDirectiveTrivia:
                    case SyntaxKind.EndRegionDirectiveTrivia:
                        return directives;
                }
            }

            return directives;
        }
    }
}
