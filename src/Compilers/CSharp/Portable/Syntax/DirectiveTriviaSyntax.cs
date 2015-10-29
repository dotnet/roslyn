// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class DirectiveTriviaSyntax
    {
        public SyntaxToken DirectiveNameToken
        {
            get
            {
                switch (this.Kind())
                {
                    case SyntaxKind.IfDirectiveTrivia:
                        return ((IfDirectiveTriviaSyntax)this).IfKeyword;
                    case SyntaxKind.ElifDirectiveTrivia:
                        return ((ElifDirectiveTriviaSyntax)this).ElifKeyword;
                    case SyntaxKind.ElseDirectiveTrivia:
                        return ((ElseDirectiveTriviaSyntax)this).ElseKeyword;
                    case SyntaxKind.EndIfDirectiveTrivia:
                        return ((EndIfDirectiveTriviaSyntax)this).EndIfKeyword;
                    case SyntaxKind.RegionDirectiveTrivia:
                        return ((RegionDirectiveTriviaSyntax)this).RegionKeyword;
                    case SyntaxKind.EndRegionDirectiveTrivia:
                        return ((EndRegionDirectiveTriviaSyntax)this).EndRegionKeyword;
                    case SyntaxKind.ErrorDirectiveTrivia:
                        return ((ErrorDirectiveTriviaSyntax)this).ErrorKeyword;
                    case SyntaxKind.WarningDirectiveTrivia:
                        return ((WarningDirectiveTriviaSyntax)this).WarningKeyword;
                    case SyntaxKind.BadDirectiveTrivia:
                        return ((BadDirectiveTriviaSyntax)this).Identifier;
                    case SyntaxKind.DefineDirectiveTrivia:
                        return ((DefineDirectiveTriviaSyntax)this).DefineKeyword;
                    case SyntaxKind.UndefDirectiveTrivia:
                        return ((UndefDirectiveTriviaSyntax)this).UndefKeyword;
                    case SyntaxKind.LineDirectiveTrivia:
                        return ((LineDirectiveTriviaSyntax)this).LineKeyword;
                    case SyntaxKind.PragmaWarningDirectiveTrivia:
                        return ((PragmaWarningDirectiveTriviaSyntax)this).PragmaKeyword;
                    case SyntaxKind.PragmaChecksumDirectiveTrivia:
                        return ((PragmaChecksumDirectiveTriviaSyntax)this).PragmaKeyword;
                    case SyntaxKind.ReferenceDirectiveTrivia:
                        return ((ReferenceDirectiveTriviaSyntax)this).ReferenceKeyword;
                    case SyntaxKind.LoadDirectiveTrivia:
                        return ((LoadDirectiveTriviaSyntax)this).LoadKeyword;
                    case SyntaxKind.ShebangDirectiveTrivia:
                        return ((ShebangDirectiveTriviaSyntax)this).ExclamationToken;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(this.Kind());
                }
            }
        }

        public DirectiveTriviaSyntax GetNextDirective(Func<DirectiveTriviaSyntax, bool> predicate = null)
        {
            var token = (SyntaxToken)this.ParentTrivia.Token;
            bool next = false;
            while (token.Kind() != SyntaxKind.None)
            {
                foreach (var tr in token.LeadingTrivia)
                {
                    if (next)
                    {
                        if (tr.IsDirective)
                        {
                            var d = (DirectiveTriviaSyntax)tr.GetStructure();
                            if (predicate == null || predicate(d))
                            {
                                return d;
                            }
                        }
                    }
                    else if (tr.UnderlyingNode == this.Green)
                    {
                        next = true;
                    }
                }

                token = token.GetNextToken(s_hasDirectivesFunction);
            }

            return null;
        }

        public DirectiveTriviaSyntax GetPreviousDirective(Func<DirectiveTriviaSyntax, bool> predicate = null)
        {
            var token = (SyntaxToken)this.ParentTrivia.Token;
            bool next = false;
            while (token.Kind() != SyntaxKind.None)
            {
                foreach (var tr in token.LeadingTrivia.Reverse())
                {
                    if (next)
                    {
                        if (tr.IsDirective)
                        {
                            var d = (DirectiveTriviaSyntax)tr.GetStructure();
                            if (predicate == null || predicate(d))
                            {
                                return d;
                            }
                        }
                    }
                    else if (tr.UnderlyingNode == this.Green)
                    {
                        next = true;
                    }
                }

                token = token.GetPreviousToken(s_hasDirectivesFunction);
            }

            return null;
        }

        public List<DirectiveTriviaSyntax> GetRelatedDirectives()
        {
            var list = new List<DirectiveTriviaSyntax>();
            this.GetRelatedDirectives(list);
            return list;
        }

        private void GetRelatedDirectives(List<DirectiveTriviaSyntax> list)
        {
            list.Clear();
            var p = this.GetPreviousRelatedDirective();
            while (p != null)
            {
                list.Add(p);
                p = p.GetPreviousRelatedDirective();
            }

            list.Reverse();
            list.Add(this);
            var n = this.GetNextRelatedDirective();
            while (n != null)
            {
                list.Add(n);
                n = n.GetNextRelatedDirective();
            }
        }

        private DirectiveTriviaSyntax GetNextRelatedDirective()
        {
            DirectiveTriviaSyntax d = this;
            switch (d.Kind())
            {
                case SyntaxKind.IfDirectiveTrivia:
                    while (d != null)
                    {
                        switch (d.Kind())
                        {
                            case SyntaxKind.ElifDirectiveTrivia:
                            case SyntaxKind.ElseDirectiveTrivia:
                            case SyntaxKind.EndIfDirectiveTrivia:
                                return d;
                        }

                        d = d.GetNextPossiblyRelatedDirective();
                    }

                    break;
                case SyntaxKind.ElifDirectiveTrivia:
                    d = d.GetNextPossiblyRelatedDirective();

                    while (d != null)
                    {
                        switch (d.Kind())
                        {
                            case SyntaxKind.ElifDirectiveTrivia:
                            case SyntaxKind.ElseDirectiveTrivia:
                            case SyntaxKind.EndIfDirectiveTrivia:
                                return d;
                        }

                        d = d.GetNextPossiblyRelatedDirective();
                    }

                    break;
                case SyntaxKind.ElseDirectiveTrivia:
                    while (d != null)
                    {
                        if (d.Kind() == SyntaxKind.EndIfDirectiveTrivia)
                        {
                            return d;
                        }

                        d = d.GetNextPossiblyRelatedDirective();
                    }

                    break;
                case SyntaxKind.RegionDirectiveTrivia:
                    while (d != null)
                    {
                        if (d.Kind() == SyntaxKind.EndRegionDirectiveTrivia)
                        {
                            return d;
                        }

                        d = d.GetNextPossiblyRelatedDirective();
                    }

                    break;
            }

            return null;
        }

        private DirectiveTriviaSyntax GetNextPossiblyRelatedDirective()
        {
            DirectiveTriviaSyntax d = this;
            while (d != null)
            {
                d = d.GetNextDirective();
                if (d != null)
                {
                    // skip matched sets
                    switch (d.Kind())
                    {
                        case SyntaxKind.IfDirectiveTrivia:
                            while (d != null && d.Kind() != SyntaxKind.EndIfDirectiveTrivia)
                            {
                                d = d.GetNextRelatedDirective();
                            }

                            continue;
                        case SyntaxKind.RegionDirectiveTrivia:
                            while (d != null && d.Kind() != SyntaxKind.EndRegionDirectiveTrivia)
                            {
                                d = d.GetNextRelatedDirective();
                            }

                            continue;
                    }
                }

                return d;
            }

            return null;
        }

        private DirectiveTriviaSyntax GetPreviousRelatedDirective()
        {
            DirectiveTriviaSyntax d = this;
            switch (d.Kind())
            {
                case SyntaxKind.EndIfDirectiveTrivia:
                    while (d != null)
                    {
                        switch (d.Kind())
                        {
                            case SyntaxKind.IfDirectiveTrivia:
                            case SyntaxKind.ElifDirectiveTrivia:
                            case SyntaxKind.ElseDirectiveTrivia:
                                return d;
                        }

                        d = d.GetPreviousPossiblyRelatedDirective();
                    }

                    break;
                case SyntaxKind.ElifDirectiveTrivia:
                    d = d.GetPreviousPossiblyRelatedDirective();

                    while (d != null)
                    {
                        switch (d.Kind())
                        {
                            case SyntaxKind.IfDirectiveTrivia:
                            case SyntaxKind.ElifDirectiveTrivia:
                                return d;
                        }

                        d = d.GetPreviousPossiblyRelatedDirective();
                    }

                    break;
                case SyntaxKind.ElseDirectiveTrivia:
                    while (d != null)
                    {
                        switch (d.Kind())
                        {
                            case SyntaxKind.IfDirectiveTrivia:
                            case SyntaxKind.ElifDirectiveTrivia:
                                return d;
                        }

                        d = d.GetPreviousPossiblyRelatedDirective();
                    }

                    break;
                case SyntaxKind.EndRegionDirectiveTrivia:
                    while (d != null)
                    {
                        if (d.Kind() == SyntaxKind.RegionDirectiveTrivia)
                        {
                            return d;
                        }

                        d = d.GetPreviousPossiblyRelatedDirective();
                    }

                    break;
            }

            return null;
        }

        private DirectiveTriviaSyntax GetPreviousPossiblyRelatedDirective()
        {
            DirectiveTriviaSyntax d = this;
            while (d != null)
            {
                d = d.GetPreviousDirective();
                if (d != null)
                {
                    // skip matched sets
                    switch (d.Kind())
                    {
                        case SyntaxKind.EndIfDirectiveTrivia:
                            while (d != null && d.Kind() != SyntaxKind.IfDirectiveTrivia)
                            {
                                d = d.GetPreviousRelatedDirective();
                            }

                            continue;
                        case SyntaxKind.EndRegionDirectiveTrivia:
                            while (d != null && d.Kind() != SyntaxKind.RegionDirectiveTrivia)
                            {
                                d = d.GetPreviousRelatedDirective();
                            }

                            continue;
                    }
                }

                return d;
            }

            return null;
        }

        private static readonly Func<SyntaxToken, bool> s_hasDirectivesFunction = t => t.ContainsDirectives;
    }
}
