#if false
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    internal static class SR
    {
        internal static readonly string QuantifyAfterNothing;
        internal static readonly string InternalError;
        internal static readonly string NotEnoughParens;
        internal static readonly string IllegalRange;
        internal static readonly string BadClassInCharRange;
        internal static readonly string TooManyParens;
        internal static readonly string NestedQuantify;
        internal static readonly string SubtractionMustBeLast;
        internal static readonly string ReversedCharRange;
        internal static readonly string UnterminatedBracket;
        internal static readonly string InvalidGroupName;
        internal static readonly string CapnumNotZero;
        internal static readonly string UndefinedBackref;
        internal static readonly string UndefinedNameRef;
        internal static readonly string UndefinedReference;
        internal static readonly string MalformedReference;
        internal static readonly string AlternationCantHaveComment;
        internal static readonly string AlternationCantCapture;
        internal static readonly string UnrecognizedGrouping;
        internal static readonly string UnterminatedComment;
        internal static readonly string IllegalEndEscape;
        internal static readonly string MalformedNameRef;
        internal static readonly string CaptureGroupOutOfRange;
        internal static readonly string TooFewHex;
        internal static readonly string UnrecognizedControl;
        internal static readonly string MissingControl;
        internal static readonly string UnrecognizedEscape;
        internal static readonly string MalformedSlashP;
        internal static readonly string IncompleteSlashP;
        internal static readonly string MakeException;
        internal static readonly string UnknownProperty;
        internal static readonly string IllegalCondition;

        internal static string Format(string format, object o1)
        {
            return string.Format(format, o1);
        }
        internal static string Format(string format, object o1, object o2)
        {
            return string.Format(format, o1);
        }
    }


    internal sealed class RegexParser
    {
        internal RegexNode _stack;
        internal RegexNode _group;
        internal RegexNode _alternation;
        internal RegexNode _concatenation;
        internal RegexNode _unit;

        private ImmutableArray<VirtualChar> _pattern;
        private int _currentPos;
        private CultureInfo _culture;

        private int _autocap;
        private int _capcount;
        private int _captop;
        private int _capsize;

        private Dictionary<int, int> _caps;
        private Dictionary<string, int> _capnames;
        private int[] _capnumlist;
        private List<string> _capnamelist;

        private RegexOptions _options;
        private List<RegexOptions> _optionsStack;

        private bool _ignoreNextParen = false;

        private const int MaxValueDiv10 = int.MaxValue / 10;
        private const int MaxValueMod10 = int.MaxValue % 10;
        
        /*
         * This static call constructs a RegexTree from a regular expression
         * pattern string and an option string.
         *
         * The method creates, drives, and drops a parser instance.
         */
        internal static RegexNode Parse(ImmutableArray<VirtualChar> re, RegexOptions op)
        {
            RegexParser p;
            RegexNode root;
            // string[] capnamelist;

            p = new RegexParser((op & RegexOptions.CultureInvariant) != 0 ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);

            p._options = op;

            p.SetPattern(re);
            p.CountCaptures();
            p.Reset(op);
            root = p.ScanRegex();

            //if (p._capnamelist == null)
            //    capnamelist = null;
            //else
            //    capnamelist = p._capnamelist.ToArray();

            //return new RegexTree(root, p._caps, p._capnumlist, p._captop, p._capnames, capnamelist, op);

            return root;
        }

#if false
        /*
         * This static call constructs a flat concatenation node given
         * a replacement pattern.
         */
        internal static RegexReplacement ParseReplacement(string rep, Hashtable caps, int capsize, Hashtable capnames, RegexOptions op)
        {
            RegexParser p;
            RegexNode root;

            p = new RegexParser((op & RegexOptions.CultureInvariant) != 0 ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);

            p._options = op;

            p.NoteCaptures(caps, capsize, capnames);
            p.SetPattern(rep);
            root = p.ScanReplacement();

            return new RegexReplacement(rep, root, caps);
        }

        /*
         * Escapes all metacharacters (including |,(,),[,{,|,^,$,*,+,?,\, spaces and #)
         */
        internal static string Escape(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (IsMetachar(input[i]))
                {
                    StringBuilder sb = StringBuilderCache.Acquire();
                    char ch = input[i];
                    int lastpos;

                    sb.Append(input, 0, i);
                    do
                    {
                        sb.Append('\\');
                        switch (ch)
                        {
                            case '\n':
                                ch = 'n';
                                break;
                            case '\r':
                                ch = 'r';
                                break;
                            case '\t':
                                ch = 't';
                                break;
                            case '\f':
                                ch = 'f';
                                break;
                        }
                        sb.Append(ch);
                        i++;
                        lastpos = i;

                        while (i < input.Length)
                        {
                            ch = input[i];
                            if (IsMetachar(ch))
                                break;

                            i++;
                        }

                        sb.Append(input, lastpos, i - lastpos);
                    } while (i < input.Length);

                    return StringBuilderCache.GetStringAndRelease(sb);
                }
            }

            return input;
        }

        /*
         * Escapes all metacharacters (including (,),[,],{,},|,^,$,*,+,?,\, spaces and #)
         */
        internal static string Unescape(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '\\')
                {
                    StringBuilder sb = StringBuilderCache.Acquire();
                    RegexParser p = new RegexParser(CultureInfo.InvariantCulture);
                    int lastpos;
                    p.SetPattern(input);

                    sb.Append(input, 0, i);
                    do
                    {
                        i++;
                        p.Textto(i);
                        if (i < input.Length)
                            sb.Append(p.ScanCharEscape());
                        i = p.Textpos();
                        lastpos = i;
                        while (i < input.Length && input[i] != '\\')
                            i++;
                        sb.Append(input, lastpos, i - lastpos);
                    } while (i < input.Length);

                    return StringBuilderCache.GetStringAndRelease(sb);
                }
            }

            return input;
        }
#endif

        /*
         * Private constructor.
         */
        private RegexParser(CultureInfo culture)
        {
            _culture = culture;
            _optionsStack = new List<RegexOptions>();
            _caps = new Dictionary<int, int>();
        }

        /*
         * Drops a string into the pattern buffer.
         */
        internal void SetPattern(ImmutableArray<VirtualChar> Re)
        {
            //if (Re == null)
            //    Re = string.Empty;
            _pattern = Re;
            _currentPos = 0;
        }

        /*
         * Resets parsing to the beginning of the pattern.
         */
        internal void Reset(RegexOptions topopts)
        {
            _currentPos = 0;
            _autocap = 1;
            _ignoreNextParen = false;

            if (_optionsStack.Count > 0)
                _optionsStack.RemoveRange(0, _optionsStack.Count - 1);

            _options = topopts;
            _stack = null;
        }

        /*
         * The main parsing function.
         */
        private RegexNode ScanRegex()
        {
            char ch = '@'; // nonspecial ch, means at beginning
            bool isQuantifier = false;

            StartGroup(new RegexNode(RegexNode.Capture, _options/*, 0, -1*/));

            while (CharsRight() > 0)
            {
                bool wasPrevQuantifier = isQuantifier;
                isQuantifier = false;

                ScanBlank();

                int startpos = Textpos();

                // move past all of the normal characters.  We'll stop when we hit some kind of control character,
                // or if IgnorePatternWhiteSpace is on, we'll stop when we see some whitespace.
                if (UseOptionX())
                    while (CharsRight() > 0 && (!IsStopperX(ch = RightChar()) || ch == '{' && !IsTrueQuantifier()))
                        MoveRight();
                else
                    while (CharsRight() > 0 && (!IsSpecial(ch = RightChar()) || ch == '{' && !IsTrueQuantifier()))
                        MoveRight();

                int endpos = Textpos();

                ScanBlank();

                if (CharsRight() == 0)
                    ch = '!'; // nonspecial, means at end
                else if (IsSpecial(ch = RightChar()))
                {
                    isQuantifier = IsQuantifier(ch);
                    MoveRight();
                }
                else
                    ch = ' '; // nonspecial, means at ordinary char

                if (startpos < endpos)
                {
                    int cchUnquantified = endpos - startpos - (isQuantifier ? 1 : 0);

                    wasPrevQuantifier = false;

                    if (cchUnquantified > 0)
                        AddConcatenate(startpos, cchUnquantified, false);

                    if (isQuantifier)
                        AddUnitOne(CharAt(endpos - 1));
                }

                switch (ch)
                {
                    case '!':
                        goto BreakOuterScan;

                    case ' ':
                        goto ContinueOuterScan;

                    case '[':
                        AddUnitSet(ScanCharClass(UseOptionI()).ToStringClass());
                        break;

                    case '(':
                        {
                            RegexNode grouper;
                            PushOptions();

                            if (null == (grouper = ScanGroupOpen()))
                            {
                                PopKeepOptions();
                            }
                            else
                            {
                                PushGroup();
                                StartGroup(grouper);
                            }
                        }
                        continue;

                    case '|':
                        AddAlternate();
                        goto ContinueOuterScan;

                    case ')':
                        if (EmptyStack())
                            throw MakeException(SR.TooManyParens);

                        AddGroup();
                        PopGroup();

                        PopOptions();

                        if (Unit() == null)
                            goto ContinueOuterScan;
                        break;

                    case '\\':
                        AddUnitNode(ScanBackslash());
                        break;

                    case '^':
                        AddUnitType(UseOptionM() ? RegexNode.Bol : RegexNode.Beginning);
                        break;

                    case '$':
                        AddUnitType(UseOptionM() ? RegexNode.Eol : RegexNode.EndZ);
                        break;

                    case '.':
                        if (UseOptionS())
                            AddUnitSet(RegexCharClass.AnyClass);
                        else
                            AddUnitNotone('\n');
                        break;

                    case '{':
                    case '*':
                    case '+':
                    case '?':
                        if (Unit() == null)
                            throw MakeException(wasPrevQuantifier ?
                                                SR.Format(SR.NestedQuantify, ch.ToString()) :
                                                SR.QuantifyAfterNothing);
                        MoveLeft();
                        break;

                    default:
                        throw MakeException(SR.InternalError);
                }

                ScanBlank();

                if (CharsRight() == 0 || !(isQuantifier = IsTrueQuantifier()))
                {
                    AddConcatenate();
                    goto ContinueOuterScan;
                }

                ch = MoveRightGetChar();

                // Handle quantifiers
                while (Unit() != null)
                {
                    int min;
                    int max;
                    bool lazy;

                    switch (ch)
                    {
                        case '*':
                            min = 0;
                            max = int.MaxValue;
                            break;

                        case '?':
                            min = 0;
                            max = 1;
                            break;

                        case '+':
                            min = 1;
                            max = int.MaxValue;
                            break;

                        case '{':
                            {
                                startpos = Textpos();
                                max = min = ScanDecimal();
                                if (startpos < Textpos())
                                {
                                    if (CharsRight() > 0 && RightChar() == ',')
                                    {
                                        MoveRight();
                                        if (CharsRight() == 0 || RightChar() == '}')
                                            max = int.MaxValue;
                                        else
                                            max = ScanDecimal();
                                    }
                                }

                                if (startpos == Textpos() || CharsRight() == 0 || MoveRightGetChar() != '}')
                                {
                                    AddConcatenate();
                                    Textto(startpos - 1);
                                    goto ContinueOuterScan;
                                }
                            }

                            break;

                        default:
                            throw MakeException(SR.InternalError);
                    }

                    ScanBlank();

                    if (CharsRight() == 0 || RightChar() != '?')
                        lazy = false;
                    else
                    {
                        MoveRight();
                        lazy = true;
                    }

                    if (min > max)
                        throw MakeException(SR.IllegalRange);

                    AddConcatenate(lazy, min, max);
                }

            ContinueOuterScan:
                ;
            }

        BreakOuterScan:
            ;

            if (!EmptyStack())
                throw MakeException(SR.NotEnoughParens);

            AddGroup();

            return Unit();
        }

#if false
#if false
        /*
         * Simple parsing for replacement patterns
         */
        internal RegexNode ScanReplacement()
        {
            int c;
            int startpos;

            _concatenation = new RegexNode(RegexNode.Concatenate, _options);

            for (; ;)
            {
                c = CharsRight();
                if (c == 0)
                    break;

                startpos = Textpos();

                while (c > 0 && RightChar() != '$')
                {
                    MoveRight();
                    c--;
                }

                AddConcatenate(startpos, Textpos() - startpos, true);

                if (c > 0)
                {
                    if (MoveRightGetChar() == '$')
                        AddUnitNode(ScanDollar());
                    AddConcatenate();
                }
            }

            return _concatenation;
        }
#endif
#endif
        /*
         * Scans contents of [] (not including []'s), and converts to a
         * RegexCharClass.
         */
        internal RegexCharClass ScanCharClass(bool caseInsensitive)
        {
            return ScanCharClass(caseInsensitive, false);
        }

        /*
         * Scans contents of [] (not including []'s), and converts to a
         * RegexCharClass.
         */
        internal RegexCharClass ScanCharClass(bool caseInsensitive, bool scanOnly)
        {
            char ch = '\0';
            char chPrev = '\0';
            bool inRange = false;
            bool firstChar = true;
            bool closed = false;

            RegexCharClass cc;

            cc = scanOnly ? null : new RegexCharClass();

            if (CharsRight() > 0 && RightChar() == '^')
            {
                MoveRight();
                if (!scanOnly)
                    cc.Negate = true;
            }

            for (; CharsRight() > 0; firstChar = false)
            {
                bool fTranslatedChar = false;
                ch = MoveRightGetChar();
                if (ch == ']')
                {
                    if (!firstChar)
                    {
                        closed = true;
                        break;
                    }
                }
                else if (ch == '\\' && CharsRight() > 0)
                {
                    switch (ch = MoveRightGetChar())
                    {
                        case 'D':
                        case 'd':
                            if (!scanOnly)
                            {
                                if (inRange)
                                    throw MakeException(SR.Format(SR.BadClassInCharRange, ch.ToString()));
                                cc.AddDigit(UseOptionE(), ch == 'D', _pattern);
                            }
                            continue;

                        case 'S':
                        case 's':
                            if (!scanOnly)
                            {
                                if (inRange)
                                    throw MakeException(SR.Format(SR.BadClassInCharRange, ch.ToString()));
                                cc.AddSpace(UseOptionE(), ch == 'S');
                            }
                            continue;

                        case 'W':
                        case 'w':
                            if (!scanOnly)
                            {
                                if (inRange)
                                    throw MakeException(SR.Format(SR.BadClassInCharRange, ch.ToString()));

                                cc.AddWord(UseOptionE(), ch == 'W');
                            }
                            continue;

                        case 'p':
                        case 'P':
                            if (!scanOnly)
                            {
                                if (inRange)
                                    throw MakeException(SR.Format(SR.BadClassInCharRange, ch.ToString()));
                                cc.AddCategoryFromName(ParseProperty(), (ch != 'p'), caseInsensitive, _pattern);
                            }
                            else
                                ParseProperty();

                            continue;

                        case '-':
                            if (!scanOnly)
                                cc.AddRange(ch, ch);
                            continue;

                        default:
                            MoveLeft();
                            ch = ScanCharEscape(); // non-literal character
                            fTranslatedChar = true;
                            break;          // this break will only break out of the switch
                    }
                }
                else if (ch == '[')
                {
                    // This is code for Posix style properties - [:Ll:] or [:IsTibetan:].
                    // It currently doesn't do anything other than skip the whole thing!
                    if (CharsRight() > 0 && RightChar() == ':' && !inRange)
                    {
                        string name;
                        int savePos = Textpos();

                        MoveRight();
                        name = ScanCapname();
                        if (CharsRight() < 2 || MoveRightGetChar() != ':' || MoveRightGetChar() != ']')
                            Textto(savePos);
                        // else lookup name (nyi)
                    }
                }


                if (inRange)
                {
                    inRange = false;
                    if (!scanOnly)
                    {
                        if (ch == '[' && !fTranslatedChar && !firstChar)
                        {
                            // We thought we were in a range, but we're actually starting a subtraction.
                            // In that case, we'll add chPrev to our char class, skip the opening [, and
                            // scan the new character class recursively.
                            cc.AddChar(chPrev);
                            cc.AddSubtraction(ScanCharClass(caseInsensitive, false));

                            if (CharsRight() > 0 && RightChar() != ']')
                                throw MakeException(SR.SubtractionMustBeLast);
                        }
                        else
                        {
                            // a regular range, like a-z
                            if (chPrev > ch)
                                throw MakeException(SR.ReversedCharRange);
                            cc.AddRange(chPrev, ch);
                        }
                    }
                }
                else if (CharsRight() >= 2 && RightChar() == '-' && RightChar(1) != ']')
                {
                    // this could be the start of a range
                    chPrev = ch;
                    inRange = true;
                    MoveRight();
                }
                else if (CharsRight() >= 1 && ch == '-' && !fTranslatedChar && RightChar() == '[' && !firstChar)
                {
                    // we aren't in a range, and now there is a subtraction.  Usually this happens
                    // only when a subtraction follows a range, like [a-z-[b]]
                    if (!scanOnly)
                    {
                        MoveRight(1);
                        cc.AddSubtraction(ScanCharClass(caseInsensitive, false));

                        if (CharsRight() > 0 && RightChar() != ']')
                            throw MakeException(SR.SubtractionMustBeLast);
                    }
                    else
                    {
                        MoveRight(1);
                        ScanCharClass(caseInsensitive, true);
                    }
                }
                else
                {
                    if (!scanOnly)
                        cc.AddRange(ch, ch);
                }
            }

            if (!closed)
                throw MakeException(SR.UnterminatedBracket);

            if (!scanOnly && caseInsensitive)
                cc.AddLowercase(_culture);

            return cc;
        }

#if false

        /*
         * Scans chars following a '(' (not counting the '('), and returns
         * a RegexNode for the type of group scanned, or null if the group
         * simply changed options (?cimsx-cimsx) or was a comment (#...).
         */
        internal RegexNode ScanGroupOpen()
        {
            char ch = '\0';
            int NodeType;
            char close = '>';


            // just return a RegexNode if we have:
            // 1. "(" followed by nothing
            // 2. "(x" where x != ?
            // 3. "(?)"
            if (CharsRight() == 0 || RightChar() != '?' || (RightChar() == '?' && (CharsRight() > 1 && RightChar(1) == ')')))
            {
                if (UseOptionN() || _ignoreNextParen)
                {
                    _ignoreNextParen = false;
                    return new RegexNode(RegexNode.Group, _options);
                }
                else
                    return new RegexNode(RegexNode.Capture, _options, _autocap++, -1);
            }

            MoveRight();

            for (; ;)
            {
                if (CharsRight() == 0)
                    break;

                switch (ch = MoveRightGetChar())
                {
                    case ':':
                        NodeType = RegexNode.Group;
                        break;

                    case '=':
                        _options &= ~(RegexOptions.RightToLeft);
                        NodeType = RegexNode.Require;
                        break;

                    case '!':
                        _options &= ~(RegexOptions.RightToLeft);
                        NodeType = RegexNode.Prevent;
                        break;

                    case '>':
                        NodeType = RegexNode.Greedy;
                        break;

                    case '\'':
                        close = '\'';
                        goto case '<';
                    // fallthrough

                    case '<':
                        if (CharsRight() == 0)
                            goto BreakRecognize;

                        switch (ch = MoveRightGetChar())
                        {
                            case '=':
                                if (close == '\'')
                                    goto BreakRecognize;

                                _options |= RegexOptions.RightToLeft;
                                NodeType = RegexNode.Require;
                                break;

                            case '!':
                                if (close == '\'')
                                    goto BreakRecognize;

                                _options |= RegexOptions.RightToLeft;
                                NodeType = RegexNode.Prevent;
                                break;

                            default:
                                MoveLeft();
                                int capnum = -1;
                                int uncapnum = -1;
                                bool proceed = false;

                                // grab part before -

                                if (ch >= '0' && ch <= '9')
                                {
                                    capnum = ScanDecimal();

                                    if (!IsCaptureSlot(capnum))
                                        capnum = -1;

                                    // check if we have bogus characters after the number
                                    if (CharsRight() > 0 && !(RightChar() == close || RightChar() == '-'))
                                        throw MakeException(SR.InvalidGroupName);
                                    if (capnum == 0)
                                        throw MakeException(SR.CapnumNotZero);
                                }
                                else if (RegexCharClass.IsWordChar(ch))
                                {
                                    string capname = ScanCapname();

                                    if (IsCaptureName(capname))
                                        capnum = CaptureSlotFromName(capname);

                                    // check if we have bogus character after the name
                                    if (CharsRight() > 0 && !(RightChar() == close || RightChar() == '-'))
                                        throw MakeException(SR.InvalidGroupName);
                                }
                                else if (ch == '-')
                                {
                                    proceed = true;
                                }
                                else
                                {
                                    // bad group name - starts with something other than a word character and isn't a number
                                    throw MakeException(SR.InvalidGroupName);
                                }

                                // grab part after - if any

                                if ((capnum != -1 || proceed == true) && CharsRight() > 0 && RightChar() == '-')
                                {
                                    MoveRight();
                                    ch = RightChar();

                                    if (ch >= '0' && ch <= '9')
                                    {
                                        uncapnum = ScanDecimal();

                                        if (!IsCaptureSlot(uncapnum))
                                            throw MakeException(SR.Format(SR.UndefinedBackref, uncapnum));

                                        // check if we have bogus characters after the number
                                        if (CharsRight() > 0 && RightChar() != close)
                                            throw MakeException(SR.InvalidGroupName);
                                    }
                                    else if (RegexCharClass.IsWordChar(ch))
                                    {
                                        string uncapname = ScanCapname();

                                        if (IsCaptureName(uncapname))
                                            uncapnum = CaptureSlotFromName(uncapname);
                                        else
                                            throw MakeException(SR.Format(SR.UndefinedNameRef, uncapname));

                                        // check if we have bogus character after the name
                                        if (CharsRight() > 0 && RightChar() != close)
                                            throw MakeException(SR.InvalidGroupName);
                                    }
                                    else
                                    {
                                        // bad group name - starts with something other than a word character and isn't a number
                                        throw MakeException(SR.InvalidGroupName);
                                    }
                                }

                                // actually make the node

                                if ((capnum != -1 || uncapnum != -1) && CharsRight() > 0 && MoveRightGetChar() == close)
                                {
                                    return new RegexNode(RegexNode.Capture, _options, capnum, uncapnum);
                                }
                                goto BreakRecognize;
                        }
                        break;

                    case '(':
                        // alternation construct (?(...) | )

                        int parenPos = Textpos();
                        if (CharsRight() > 0)
                        {
                            ch = RightChar();

                            // check if the alternation condition is a backref
                            if (ch >= '0' && ch <= '9')
                            {
                                int capnum = ScanDecimal();
                                if (CharsRight() > 0 && MoveRightGetChar() == ')')
                                {
                                    if (IsCaptureSlot(capnum))
                                        return new RegexNode(RegexNode.Testref, _options, capnum);
                                    else
                                        throw MakeException(SR.Format(SR.UndefinedReference, capnum.ToString(CultureInfo.CurrentCulture)));
                                }
                                else
                                    throw MakeException(SR.Format(SR.MalformedReference, capnum.ToString(CultureInfo.CurrentCulture)));
                            }
                            else if (RegexCharClass.IsWordChar(ch))
                            {
                                string capname = ScanCapname();

                                if (IsCaptureName(capname) && CharsRight() > 0 && MoveRightGetChar() == ')')
                                    return new RegexNode(RegexNode.Testref, _options, CaptureSlotFromName(capname));
                            }
                        }
                        // not a backref
                        NodeType = RegexNode.Testgroup;
                        Textto(parenPos - 1);       // jump to the start of the parentheses
                        _ignoreNextParen = true;    // but make sure we don't try to capture the insides

                        int charsRight = CharsRight();
                        if (charsRight >= 3 && RightChar(1) == '?')
                        {
                            char rightchar2 = RightChar(2);
                            // disallow comments in the condition
                            if (rightchar2 == '#')
                                throw MakeException(SR.AlternationCantHaveComment);

                            // disallow named capture group (?<..>..) in the condition
                            if (rightchar2 == '\'')
                                throw MakeException(SR.AlternationCantCapture);
                            else
                            {
                                if (charsRight >= 4 && (rightchar2 == '<' && RightChar(3) != '!' && RightChar(3) != '='))
                                    throw MakeException(SR.AlternationCantCapture);
                            }
                        }

                        break;


                    default:
                        MoveLeft();

                        NodeType = RegexNode.Group;
                        // Disallow options in the children of a testgroup node
                        if (_group._type != RegexNode.Testgroup)
                            ScanOptions();
                        if (CharsRight() == 0)
                            goto BreakRecognize;

                        if ((ch = MoveRightGetChar()) == ')')
                            return null;

                        if (ch != ':')
                            goto BreakRecognize;
                        break;
                }

                return new RegexNode(NodeType, _options);
            }

        BreakRecognize:
            ;
            // break Recognize comes here

            throw MakeException(SR.UnrecognizedGrouping);
        }
#endif

        /*
         * Scans whitespace or x-mode comments.
         */
        internal void ScanBlank()
        {
            if (UseOptionX())
            {
                for (; ;)
                {
                    while (CharsRight() > 0 && IsSpace(RightChar()))
                        MoveRight();

                    if (CharsRight() == 0)
                        break;

                    if (RightChar() == '#')
                    {
                        while (CharsRight() > 0 && RightChar() != '\n')
                            MoveRight();
                    }
                    else if (CharsRight() >= 3 && RightChar(2) == '#' &&
                             RightChar(1) == '?' && RightChar() == '(')
                    {
                        while (CharsRight() > 0 && RightChar() != ')')
                            MoveRight();
                        if (CharsRight() == 0)
                            throw MakeException(SR.UnterminatedComment);
                        MoveRight();
                    }
                    else
                        break;
                }
            }
            else
            {
                for (; ;)
                {
                    if (CharsRight() < 3 || RightChar(2) != '#' ||
                        RightChar(1) != '?' || RightChar() != '(')
                        return;

                    while (CharsRight() > 0 && RightChar() != ')')
                        MoveRight();
                    if (CharsRight() == 0)
                        throw MakeException(SR.UnterminatedComment);
                    MoveRight();
                }
            }
        }

#if false
        /*
         * Scans chars following a '\' (not counting the '\'), and returns
         * a RegexNode for the type of atom scanned.
         */
        internal RegexNode ScanBackslash()
        {
            char ch;
            RegexCharClass cc;

            if (CharsRight() == 0)
                throw MakeException(SR.IllegalEndEscape);

            switch (ch = RightChar())
            {
                case 'b':
                case 'B':
                case 'A':
                case 'G':
                case 'Z':
                case 'z':
                    MoveRight();
                    return new RegexNode(TypeFromCode(ch), _options);

                case 'w':
                    MoveRight();
                    if (UseOptionE())
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.ECMAWordClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.WordClass);

                case 'W':
                    MoveRight();
                    if (UseOptionE())
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotECMAWordClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotWordClass);

                case 's':
                    MoveRight();
                    if (UseOptionE())
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.ECMASpaceClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.SpaceClass);

                case 'S':
                    MoveRight();
                    if (UseOptionE())
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotECMASpaceClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotSpaceClass);

                case 'd':
                    MoveRight();
                    if (UseOptionE())
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.ECMADigitClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.DigitClass);

                case 'D':
                    MoveRight();
                    if (UseOptionE())
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotECMADigitClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotDigitClass);

                case 'p':
                case 'P':
                    MoveRight();
                    cc = new RegexCharClass();
                    cc.AddCategoryFromName(ParseProperty(), (ch != 'p'), UseOptionI(), _pattern);
                    if (UseOptionI())
                        cc.AddLowercase(_culture);

                    return new RegexNode(RegexNode.Set, _options, cc.ToStringClass());

                default:
                    return ScanBasicBackslash();
            }
        }

        /*
         * Scans \-style backreferences and character escapes
         */
        internal RegexNode ScanBasicBackslash()
        {
            if (CharsRight() == 0)
                throw MakeException(SR.IllegalEndEscape);

            char ch;
            bool angled = false;
            char close = '\0';
            int backpos;

            backpos = Textpos();
            ch = RightChar();

            // allow \k<foo> instead of \<foo>, which is now deprecated

            if (ch == 'k')
            {
                if (CharsRight() >= 2)
                {
                    MoveRight();
                    ch = MoveRightGetChar();

                    if (ch == '<' || ch == '\'')
                    {
                        angled = true;
                        close = (ch == '\'') ? '\'' : '>';
                    }
                }

                if (!angled || CharsRight() <= 0)
                    throw MakeException(SR.MalformedNameRef);

                ch = RightChar();
            }

            // Note angle without \g

            else if ((ch == '<' || ch == '\'') && CharsRight() > 1)
            {
                angled = true;
                close = (ch == '\'') ? '\'' : '>';

                MoveRight();
                ch = RightChar();
            }

            // Try to parse backreference: \<1> or \<cap>

            if (angled && ch >= '0' && ch <= '9')
            {
                int capnum = ScanDecimal();

                if (CharsRight() > 0 && MoveRightGetChar() == close)
                {
                    if (IsCaptureSlot(capnum))
                        return new RegexNode(RegexNode.Ref, _options, capnum);
                    else
                        throw MakeException(SR.Format(SR.UndefinedBackref, capnum.ToString(CultureInfo.CurrentCulture)));
                }
            }

            // Try to parse backreference or octal: \1

            else if (!angled && ch >= '1' && ch <= '9')
            {
                if (UseOptionE())
                {
                    int capnum = -1;
                    int newcapnum = (int)(ch - '0');
                    int pos = Textpos() - 1;
                    while (newcapnum <= _captop)
                    {
                        if (IsCaptureSlot(newcapnum) && (_caps == null || (int)_caps[newcapnum] < pos))
                            capnum = newcapnum;
                        MoveRight();
                        if (CharsRight() == 0 || (ch = RightChar()) < '0' || ch > '9')
                            break;
                        newcapnum = newcapnum * 10 + (int)(ch - '0');
                    }
                    if (capnum >= 0)
                        return new RegexNode(RegexNode.Ref, _options, capnum);
                }
                else
                {
                    int capnum = ScanDecimal();
                    if (IsCaptureSlot(capnum))
                        return new RegexNode(RegexNode.Ref, _options, capnum);
                    else if (capnum <= 9)
                        throw MakeException(SR.Format(SR.UndefinedBackref, capnum.ToString(CultureInfo.CurrentCulture)));
                }
            }

            else if (angled && RegexCharClass.IsWordChar(ch))
            {
                string capname = ScanCapname();

                if (CharsRight() > 0 && MoveRightGetChar() == close)
                {
                    if (IsCaptureName(capname))
                        return new RegexNode(RegexNode.Ref, _options, CaptureSlotFromName(capname));
                    else
                        throw MakeException(SR.Format(SR.UndefinedNameRef, capname));
                }
            }

            // Not backreference: must be char code

            Textto(backpos);
            ch = ScanCharEscape();

            if (UseOptionI())
                ch = _culture.TextInfo.ToLower(ch);

            return new RegexNode(RegexNode.One, _options, ch);
        }

#if false
        /*
         * Scans $ patterns recognized within replacement patterns
         */
        internal RegexNode ScanDollar()
        {
            if (CharsRight() == 0)
                return new RegexNode(RegexNode.One, _options, '$');

            char ch = RightChar();
            bool angled;
            int backpos = Textpos();
            int lastEndPos = backpos;

            // Note angle

            if (ch == '{' && CharsRight() > 1)
            {
                angled = true;
                MoveRight();
                ch = RightChar();
            }
            else
            {
                angled = false;
            }

            // Try to parse backreference: \1 or \{1} or \{cap}

            if (ch >= '0' && ch <= '9')
            {
                if (!angled && UseOptionE())
                {
                    int capnum = -1;
                    int newcapnum = (int)(ch - '0');
                    MoveRight();
                    if (IsCaptureSlot(newcapnum))
                    {
                        capnum = newcapnum;
                        lastEndPos = Textpos();
                    }

                    while (CharsRight() > 0 && (ch = RightChar()) >= '0' && ch <= '9')
                    {
                        int digit = (int)(ch - '0');
                        if (newcapnum > (MaxValueDiv10) || (newcapnum == (MaxValueDiv10) && digit > (MaxValueMod10)))
                            throw MakeException(SR.CaptureGroupOutOfRange);

                        newcapnum = newcapnum * 10 + digit;

                        MoveRight();
                        if (IsCaptureSlot(newcapnum))
                        {
                            capnum = newcapnum;
                            lastEndPos = Textpos();
                        }
                    }
                    Textto(lastEndPos);
                    if (capnum >= 0)
                        return new RegexNode(RegexNode.Ref, _options, capnum);
                }
                else
                {
                    int capnum = ScanDecimal();
                    if (!angled || CharsRight() > 0 && MoveRightGetChar() == '}')
                    {
                        if (IsCaptureSlot(capnum))
                            return new RegexNode(RegexNode.Ref, _options, capnum);
                    }
                }
            }
            else if (angled && RegexCharClass.IsWordChar(ch))
            {
                string capname = ScanCapname();

                if (CharsRight() > 0 && MoveRightGetChar() == '}')
                {
                    if (IsCaptureName(capname))
                        return new RegexNode(RegexNode.Ref, _options, CaptureSlotFromName(capname));
                }
            }
            else if (!angled)
            {
                int capnum = 1;

                switch (ch)
                {
                    case '$':
                        MoveRight();
                        return new RegexNode(RegexNode.One, _options, '$');

                    case '&':
                        capnum = 0;
                        break;

                    case '`':
                        capnum = RegexReplacement.LeftPortion;
                        break;

                    case '\'':
                        capnum = RegexReplacement.RightPortion;
                        break;

                    case '+':
                        capnum = RegexReplacement.LastGroup;
                        break;

                    case '_':
                        capnum = RegexReplacement.WholeString;
                        break;
                }

                if (capnum != 1)
                {
                    MoveRight();
                    return new RegexNode(RegexNode.Ref, _options, capnum);
                }
            }

            // unrecognized $: literalize

            Textto(backpos);
            return new RegexNode(RegexNode.One, _options, '$');
        }
#endif
#endif

        /*
         * Scans a capture name: consumes word chars
         */
        internal string ScanCapname()
        {
            int startpos = Textpos();

            while (CharsRight() > 0)
            {
                if (!RegexCharClass.IsWordChar(MoveRightGetChar()))
                {
                    MoveLeft();
                    break;
                }
            }

            return GetPatternSubstring(startpos, Textpos() - startpos);
        }

        /*
         * Scans up to three octal digits (stops before exceeding 0377).
         */
        internal char ScanOctal()
        {
            int d;
            int i;
            int c;

            // Consume octal chars only up to 3 digits and value 0377

            c = 3;

            if (c > CharsRight())
                c = CharsRight();

            for (i = 0; c > 0 && unchecked((uint)(d = RightChar() - '0')) <= 7; c -= 1)
            {
                MoveRight();
                i *= 8;
                i += d;
                if (UseOptionE() && i >= 0x20)
                    break;
            }

            // Octal codes only go up to 255.  Any larger and the behavior that Perl follows
            // is simply to truncate the high bits.
            i &= 0xFF;

            return (char)i;
        }

        /*
         * Scans any number of decimal digits (pegs value at 2^31-1 if too large)
         */
        internal int ScanDecimal()
        {
            int i = 0;
            int d;

            while (CharsRight() > 0 && unchecked((uint)(d = (char)(RightChar() - '0'))) <= 9)
            {
                MoveRight();

                if (i > (MaxValueDiv10) || (i == (MaxValueDiv10) && d > (MaxValueMod10)))
                    throw MakeException(SR.CaptureGroupOutOfRange);

                i *= 10;
                i += d;
            }

            return i;
        }

        /*
         * Scans exactly c hex digits (c=2 for \xFF, c=4 for \uFFFF)
         */
        internal char ScanHex(int c)
        {
            int i;
            int d;

            i = 0;

            if (CharsRight() >= c)
            {
                for (; c > 0 && ((d = HexDigit(MoveRightGetChar())) >= 0); c -= 1)
                {
                    i *= 0x10;
                    i += d;
                }
            }

            if (c > 0)
                throw MakeException(SR.TooFewHex);

            return (char)i;
        }

        /*
         * Returns n <= 0xF for a hex digit.
         */
        internal static int HexDigit(char ch)
        {
            int d;

            if ((uint)(d = ch - '0') <= 9)
                return d;

            if (unchecked((uint)(d = ch - 'a')) <= 5)
                return d + 0xa;

            if ((uint)(d = ch - 'A') <= 5)
                return d + 0xa;

            return -1;
        }

        /*
         * Grabs and converts an ASCII control character
         */
        internal char ScanControl()
        {
            char ch;

            if (CharsRight() <= 0)
                throw MakeException(SR.MissingControl);

            ch = MoveRightGetChar();

            // \ca interpreted as \cA

            if (ch >= 'a' && ch <= 'z')
                ch = (char)(ch - ('a' - 'A'));

            if (unchecked(ch = (char)(ch - '@')) < ' ')
                return ch;

            throw MakeException(SR.UnrecognizedControl);
        }

        /*
         * Returns true for options allowed only at the top level
         */
        internal bool IsOnlyTopOption(RegexOptions option)
        {
            return (option == RegexOptions.RightToLeft
                || option == RegexOptions.CultureInvariant
                || option == RegexOptions.ECMAScript
            );
        }

        /*
         * Scans cimsx-cimsx option string, stops at the first unrecognized char.
         */
        private void ScanOptions()
        {
            VirtualChar ch;
            bool off;
            RegexOptions option;

            for (off = false; CharsRight() > 0; MoveRight())
            {
                ch = RightChar();

                if (ch == '-')
                {
                    off = true;
                }
                else if (ch == '+')
                {
                    off = false;
                }
                else
                {
                    option = OptionFromCode(ch);
                    if (option == 0 || IsOnlyTopOption(option))
                        return;

                    if (off)
                        _options &= ~option;
                    else
                        _options |= option;
                }
            }
        }

        /*
         * Scans \ code for escape codes that map to single Unicode chars.
         */
        internal char ScanCharEscape()
        {
            char ch;

            ch = MoveRightGetChar();

            if (ch >= '0' && ch <= '7')
            {
                MoveLeft();
                return ScanOctal();
            }

            switch (ch)
            {
                case 'x':
                    return ScanHex(2);
                case 'u':
                    return ScanHex(4);
                case 'a':
                    return '\u0007';
                case 'b':
                    return '\b';
                case 'e':
                    return '\u001B';
                case 'f':
                    return '\f';
                case 'n':
                    return '\n';
                case 'r':
                    return '\r';
                case 't':
                    return '\t';
                case 'v':
                    return '\u000B';
                case 'c':
                    return ScanControl();
                default:
                    if (!UseOptionE() && RegexCharClass.IsWordChar(ch))
                        throw MakeException(SR.Format(SR.UnrecognizedEscape, ch.ToString()));
                    return ch;
            }
        }

        /*
         * Scans X for \p{X} or \P{X}
         */
        internal string ParseProperty()
        {
            if (CharsRight() < 3)
            {
                throw MakeException(SR.IncompleteSlashP);
            }
            char ch = MoveRightGetChar();
            if (ch != '{')
            {
                throw MakeException(SR.MalformedSlashP);
            }

            int startpos = Textpos();
            while (CharsRight() > 0)
            {
                ch = MoveRightGetChar();
                if (!(RegexCharClass.IsWordChar(ch) || ch == '-'))
                {
                    MoveLeft();
                    break;
                }
            }

            var capname = GetPatternSubstring(startpos, Textpos() - startpos);

            if (CharsRight() == 0 || MoveRightGetChar() != '}')
                throw MakeException(SR.IncompleteSlashP);

            return capname;
        }

        private ImmutableArray<VirtualChar> GetPatternSubstring(int start, int length)
        {
            //var builder = StringBuilderCache.Acquire(length);
            //for (int i = 0; i < length; i++)
            //{
            //    builder.Append(_pattern[start + i].Char);
            //}

            //return StringBuilderCache.GetStringAndRelease(builder);
            var builder = ArrayBuilder<VirtualChar>.GetInstance(length);
            for (int i = 0; i < length; i++)
            {
                builder.Add(_pattern[start + i]);
            }

            return builder.ToImmutableAndFree();
        }

#if false
        /*
         * Returns ReNode type for zero-length assertions with a \ code.
         */
        internal int TypeFromCode(char ch)
        {
            switch (ch)
            {
                case 'b':
                    return UseOptionE() ? RegexNode.ECMABoundary : RegexNode.Boundary;
                case 'B':
                    return UseOptionE() ? RegexNode.NonECMABoundary : RegexNode.Nonboundary;
                case 'A':
                    return RegexNode.Beginning;
                case 'G':
                    return RegexNode.Start;
                case 'Z':
                    return RegexNode.EndZ;
                case 'z':
                    return RegexNode.End;
                default:
                    return RegexNode.Nothing;
            }
        }

#endif

        /*
         * Returns option bit from single-char (?cimsx) code.
         */
        internal static RegexOptions OptionFromCode(char ch)
        {
            // case-insensitive
            if (ch >= 'A' && ch <= 'Z')
                ch += (char)('a' - 'A');

            switch (ch)
            {
                case 'i':
                    return RegexOptions.IgnoreCase;
                case 'r':
                    return RegexOptions.RightToLeft;
                case 'm':
                    return RegexOptions.Multiline;
                case 'n':
                    return RegexOptions.ExplicitCapture;
                case 's':
                    return RegexOptions.Singleline;
                case 'x':
                    return RegexOptions.IgnorePatternWhitespace;
//#if DEBUG
//                case 'd':
//                    return RegexOptions.Debug;
//#endif
                case 'e':
                    return RegexOptions.ECMAScript;
                default:
                    return 0;
            }
        }

        /*
         * a prescanner for deducing the slots used for
         * captures by doing a partial tokenization of the pattern.
         */
        internal void CountCaptures()
        {
            VirtualChar ch;

            NoteCaptureSlot(0, 0);

            _autocap = 1;

            while (CharsRight() > 0)
            {
                int pos = Textpos();
                ch = MoveRightGetChar();
                switch (ch)
                {
                    case '\\':
                        if (CharsRight() > 0)
                            MoveRight();
                        break;

                    case '#':
                        if (UseOptionX())
                        {
                            MoveLeft();
                            ScanBlank();
                        }
                        break;

                    case '[':
                        ScanCharClass(false, true);
                        break;

                    case ')':
                        if (!EmptyOptionsStack())
                            PopOptions();
                        break;

                    case '(':
                        if (CharsRight() >= 2 && RightChar(1) == '#' && RightChar() == '?')
                        {
                            MoveLeft();
                            ScanBlank();
                        }
                        else
                        {
                            PushOptions();
                            if (CharsRight() > 0 && RightChar() == '?')
                            {
                                // we have (?...
                                MoveRight();

                                if (CharsRight() > 1 && (RightChar() == '<' || RightChar() == '\''))
                                {
                                    // named group: (?<... or (?'...

                                    MoveRight();
                                    ch = RightChar();

                                    if (ch != '0' && RegexCharClass.IsWordChar(ch))
                                    {
                                        //if (_ignoreNextParen)
                                        //    throw MakeException(SR.AlternationCantCapture);
                                        if (ch >= '1' && ch <= '9')
                                            NoteCaptureSlot(ScanDecimal(), pos);
                                        else
                                            NoteCaptureName(ScanCapname(), pos);
                                    }
                                }
                                else
                                {
                                    // (?...

                                    // get the options if it's an option construct (?cimsx-cimsx...)
                                    ScanOptions();

                                    if (CharsRight() > 0)
                                    {
                                        if (RightChar() == ')')
                                        {
                                            // (?cimsx-cimsx)
                                            MoveRight();

                                            PopKeepOptions();
                                        }
                                        else if (RightChar() == '(')
                                        {
                                            // alternation construct: (?(foo)yes|no)
                                            // ignore the next paren so we don't capture the condition
                                            _ignoreNextParen = true;

                                            // break from here so we don't reset _ignoreNextParen
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (!UseOptionN() && !_ignoreNextParen)
                                    NoteCaptureSlot(_autocap++, pos);
                            }
                        }

                        _ignoreNextParen = false;
                        break;
                }
            }

            AssignNameSlots();
        }

        /*
         * Notes a used capture slot
         */
        internal void NoteCaptureSlot(int i, int pos)
        {
            if (!_caps.ContainsKey(i))
            {
                // the rhs of the hashtable isn't used in the parser

                _caps.Add(i, pos);
                _capcount++;

                if (_captop <= i)
                {
                    if (i == int.MaxValue)
                        _captop = i;
                    else
                        _captop = i + 1;
                }
            }
        }

        /*
         * Notes a used capture slot
         */
        internal void NoteCaptureName(string name, int pos)
        {
            if (_capnames == null)
            {
                _capnames = new Dictionary<string, int>();
                _capnamelist = new List<string>();
            }

            if (!_capnames.ContainsKey(name))
            {
                _capnames.Add(name, pos);
                _capnamelist.Add(name);
            }
        }

#if false

        /*
         * For when all the used captures are known: note them all at once
         */
        internal void NoteCaptures(Dictionary<int, int> caps, int capsize, Dictionary<string, int> capnames)
        {
            _caps = caps;
            _capsize = capsize;
            _capnames = capnames;
        }
#endif
        /*
         * Assigns unused slot numbers to the capture names
         */
        internal void AssignNameSlots()
        {
            if (_capnames != null)
            {
                for (int i = 0; i < _capnamelist.Count; i++)
                {
                    while (IsCaptureSlot(_autocap))
                        _autocap++;
                    string name = _capnamelist[i];
                    int pos = (int)_capnames[name];
                    _capnames[name] = _autocap;
                    NoteCaptureSlot(_autocap, pos);

                    _autocap++;
                }
            }

            // if the caps array has at least one gap, construct the list of used slots

            if (_capcount < _captop)
            {
                _capnumlist = new int[_capcount];
                int i = 0;

                // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
                IDictionaryEnumerator de = _caps.GetEnumerator();
                while (de.MoveNext())
                {
                    _capnumlist[i++] = (int)de.Key;
                }

                Array.Sort(_capnumlist, Comparer<int>.Default);
            }

            // merge capsnumlist into capnamelist

            if (_capnames != null || _capnumlist != null)
            {
                List<string> oldcapnamelist;
                int next;
                int k = 0;

                if (_capnames == null)
                {
                    oldcapnamelist = null;
                    _capnames = new Dictionary<string, int>();
                    _capnamelist = new List<string>();
                    next = -1;
                }
                else
                {
                    oldcapnamelist = _capnamelist;
                    _capnamelist = new List<string>();
                    next = (int)_capnames[oldcapnamelist[0]];
                }

                for (int i = 0; i < _capcount; i++)
                {
                    int j = (_capnumlist == null) ? i : (int)_capnumlist[i];

                    if (next == j)
                    {
                        _capnamelist.Add(oldcapnamelist[k++]);
                        next = (k == oldcapnamelist.Count) ? -1 : (int)_capnames[oldcapnamelist[k]];
                    }
                    else
                    {
                        string str = Convert.ToString(j, _culture);
                        _capnamelist.Add(str);
                        _capnames[str] = j;
                    }
                }
            }
        }

        /*
         * Looks up the slot number for a given name
         */
        internal int CaptureSlotFromName(string capname)
        {
            return (int)_capnames[capname];
        }

        /*
         * True if the capture slot was noted
         */
        internal bool IsCaptureSlot(int i)
        {
            if (_caps != null)
                return _caps.ContainsKey(i);

            return (i >= 0 && i < _capsize);
        }

        /*
         * Looks up the slot number for a given name
         */
        internal bool IsCaptureName(string capname)
        {
            if (_capnames == null)
                return false;

            return _capnames.ContainsKey(capname);
        }

        /*
         * True if N option disabling '(' autocapture is on.
         */
        internal bool UseOptionN()
        {
            return (_options & RegexOptions.ExplicitCapture) != 0;
        }

        /*
         * True if I option enabling case-insensitivity is on.
         */
        internal bool UseOptionI()
        {
            return (_options & RegexOptions.IgnoreCase) != 0;
        }

        /*
         * True if M option altering meaning of $ and ^ is on.
         */
        internal bool UseOptionM()
        {
            return (_options & RegexOptions.Multiline) != 0;
        }

        /*
         * True if S option altering meaning of . is on.
         */
        internal bool UseOptionS()
        {
            return (_options & RegexOptions.Singleline) != 0;
        }

        /*
         * True if X option enabling whitespace/comment mode is on.
         */
        internal bool UseOptionX()
        {
            return (_options & RegexOptions.IgnorePatternWhitespace) != 0;
        }

        /*
         * True if E option enabling ECMAScript behavior is on.
         */
        internal bool UseOptionE()
        {
            return (_options & RegexOptions.ECMAScript) != 0;
        }

        internal const byte Q = 5;    // quantifier
        internal const byte S = 4;    // ordinary stopper
        internal const byte Z = 3;    // ScanBlank stopper
        internal const byte X = 2;    // whitespace
        internal const byte E = 1;    // should be escaped

        /*
         * For categorizing ASCII characters.
        */
        internal static readonly byte[] _category = new byte[] {
            // 0 1 2 3 4 5 6 7 8 9 A B C D E F 0 1 2 3 4 5 6 7 8 9 A B C D E F
               0,0,0,0,0,0,0,0,0,X,X,0,X,X,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            //   ! " # $ % & ' ( ) * + , - . / 0 1 2 3 4 5 6 7 8 9 : ; < = > ?
               X,0,0,Z,S,0,0,0,S,S,Q,Q,0,0,S,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,Q,
            // @ A B C D E F G H I J K L M N O P Q R S T U V W X Y Z [ \ ] ^ _
               0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,S,S,0,S,0,
            // ' a b c d e f g h i j k l m n o p q r s t u v w x y z { | } ~
               0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,Q,S,0,0,0};

        /*
         * Returns true for those characters that terminate a string of ordinary chars.
         */
        internal static bool IsSpecial(char ch)
        {
            return (ch <= '|' && _category[ch] >= S);
        }

        /*
         * Returns true for those characters that terminate a string of ordinary chars.
         */
        internal static bool IsStopperX(char ch)
        {
            return (ch <= '|' && _category[ch] >= X);
        }

        /*
         * Returns true for those characters that begin a quantifier.
         */
        internal static bool IsQuantifier(char ch)
        {
            return (ch <= '{' && _category[ch] >= Q);
        }

        internal bool IsTrueQuantifier()
        {
            int nChars = CharsRight();
            if (nChars == 0)
                return false;
            int startpos = Textpos();
            char ch = CharAt(startpos);
            if (ch != '{')
                return ch <= '{' && _category[ch] >= Q;
            int pos = startpos;
            while (--nChars > 0 && (ch = CharAt(++pos)) >= '0' && ch <= '9') ;
            if (nChars == 0 || pos - startpos == 1)
                return false;
            if (ch == '}')
                return true;
            if (ch != ',')
                return false;
            while (--nChars > 0 && (ch = CharAt(++pos)) >= '0' && ch <= '9') ;
            return nChars > 0 && ch == '}';
        }

        /*
         * Returns true for whitespace.
         */
        internal static bool IsSpace(char ch)
        {
            return (ch <= ' ' && _category[ch] == X);
        }
#if false

        /*
         * Returns true for chars that should be escaped.
         */
        internal static bool IsMetachar(char ch)
        {
            return (ch <= '|' && _category[ch] >= E);
        }
#endif

        /*
         * Add a string to the last concatenate.
         */
        internal void AddConcatenate(int pos, int cch, bool isReplacement)
        {
            RegexNode node;

            if (cch == 0)
                return;

            if (cch > 1)
            {
                string str = GetPatternSubstring(pos, cch);

                if (UseOptionI() && !isReplacement)
                {
                    // We do the ToLower character by character for consistency.  With surrogate chars, doing
                    // a ToLower on the entire string could actually change the surrogate pair.  This is more correct
                    // linguistically, but since Regex doesn't support surrogates, it's more important to be
                    // consistent.
                    var sb = StringBuilderCache.Acquire(str.Length);
                    for (int i = 0; i < str.Length; i++)
                        sb.Append(_culture.TextInfo.ToLower(str[i]));
                    str = StringBuilderCache.GetStringAndRelease(sb);
                }

                node = new RegexNode(RegexNode.Multi, _options, str);
            }
            else
            {
                char ch = _pattern[pos];

                if (UseOptionI() && !isReplacement)
                    ch = _culture.TextInfo.ToLower(ch);

                node = new RegexNode(RegexNode.One, _options, ch);
            }

            _concatenation.AddChild(node);
        }

        /*
         * Push the parser state (in response to an open paren)
         */
        internal void PushGroup()
        {
            _group._parent = _stack;
            _alternation._parent = _group;
            _concatenation._parent = _alternation;
            _stack = _concatenation;
        }

        /*
         * Remember the pushed state (in response to a ')')
         */
        internal void PopGroup()
        {
            _concatenation = _stack;
            _alternation = _concatenation._parent;
            _group = _alternation._parent;
            _stack = _group._parent;

            // The first () inside a Testgroup group goes directly to the group
            if (_group.Type() == RegexNode.Testgroup && _group.ChildCount() == 0)
            {
                if (_unit == null)
                    throw MakeException(SR.IllegalCondition);

                _group.AddChild(_unit);
                _unit = null;
            }
        }

        /*
         * True if the group stack is empty.
         */
        internal bool EmptyStack()
        {
            return _stack == null;
        }

        /*
         * Start a new round for the parser state (in response to an open paren or string start)
         */
        internal void StartGroup(RegexNode openGroup)
        {
            _group = openGroup;
            _alternation = new RegexNode(RegexNode.Alternate, _options);
            _concatenation = new RegexNode(RegexNode.Concatenate, _options);
        }

#if false
        /*
         * Finish the current concatenation (in response to a |)
         */
        internal void AddAlternate()
        {
            // The | parts inside a Testgroup group go directly to the group

            if (_group.Type() == RegexNode.Testgroup || _group.Type() == RegexNode.Testref)
            {
                _group.AddChild(_concatenation.ReverseLeft());
            }
            else
            {
                _alternation.AddChild(_concatenation.ReverseLeft());
            }

            _concatenation = new RegexNode(RegexNode.Concatenate, _options);
        }

        /*
         * Finish the current quantifiable (when a quantifier is not found or is not possible)
         */
        internal void AddConcatenate()
        {
            // The first (| inside a Testgroup group goes directly to the group

            _concatenation.AddChild(_unit);
            _unit = null;
        }

        /*
         * Finish the current quantifiable (when a quantifier is found)
         */
        internal void AddConcatenate(bool lazy, int min, int max)
        {
            _concatenation.AddChild(_unit.MakeQuantifier(lazy, min, max));
            _unit = null;
        }

        /*
         * Returns the current unit
         */
        internal RegexNode Unit()
        {
            return _unit;
        }
#endif
        /*
         * Sets the current unit to a single char node
         */
        internal void AddUnitOne(VirtualChar ch)
        {
            //if (UseOptionI())
            //    ch = _culture.TextInfo.ToLower(ch);

            _unit = new RegexNode(RegexNode.One, _options, ch);
        }
#if false

        /*
         * Sets the current unit to a single inverse-char node
         */
        internal void AddUnitNotone(char ch)
        {
            if (UseOptionI())
                ch = _culture.TextInfo.ToLower(ch);

            _unit = new RegexNode(RegexNode.Notone, _options, ch);
        }

        /*
         * Sets the current unit to a single set node
         */
        internal void AddUnitSet(string cc)
        {
            _unit = new RegexNode(RegexNode.Set, _options, cc);
        }

        /*
         * Sets the current unit to a subtree
         */
        internal void AddUnitNode(RegexNode node)
        {
            _unit = node;
        }

        /*
         * Sets the current unit to an assertion of the specified type
         */
        internal void AddUnitType(int type)
        {
            _unit = new RegexNode(type, _options);
        }

        /*
         * Finish the current group (in response to a ')' or end)
         */
        internal void AddGroup()
        {
            if (_group.Type() == RegexNode.Testgroup || _group.Type() == RegexNode.Testref)
            {
                _group.AddChild(_concatenation.ReverseLeft());

                if (_group.Type() == RegexNode.Testref && _group.ChildCount() > 2 || _group.ChildCount() > 3)
                    throw MakeException(SR.TooManyAlternates);
            }
            else
            {
                _alternation.AddChild(_concatenation.ReverseLeft());
                _group.AddChild(_alternation);
            }

            _unit = _group;
        }
#endif

        /*
         * Saves options on a stack.
         */
        internal void PushOptions()
        {
            _optionsStack.Add(_options);
        }

        /*
         * Recalls options from the stack.
         */
        internal void PopOptions()
        {
            _options = _optionsStack[_optionsStack.Count - 1];
            _optionsStack.RemoveAt(_optionsStack.Count - 1);
        }

        /*
         * True if options stack is empty.
         */
        internal bool EmptyOptionsStack()
        {
            return (_optionsStack.Count == 0);
        }

        /*
         * Pops the option stack, but keeps the current options unchanged.
         */
        internal void PopKeepOptions()
        {
            _optionsStack.RemoveAt(_optionsStack.Count - 1);
        }

        /*
         * Fills in an ArgumentException
         */
        internal ArgumentException MakeException(string message)
        {
            return new ArgumentException(SR.Format(SR.MakeException, _pattern, message));
        }

        /*
         * Returns the current parsing position.
         */
        internal int Textpos()
        {
            return _currentPos;
        }

        /*
         * Zaps to a specific parsing position.
         */
        internal void Textto(int pos)
        {
            _currentPos = pos;
        }

        /*
         * Returns the char at the right of the current parsing position and advances to the right.
         */
        internal VirtualChar MoveRightGetChar()
        {
            return _pattern[_currentPos++];
        }

        /*
         * Moves the current position to the right.
         */
        internal void MoveRight()
        {
            MoveRight(1);
        }

        internal void MoveRight(int i)
        {
            _currentPos += i;
        }

        /*
         * Moves the current parsing position one to the left.
         */
        internal void MoveLeft()
        {
            --_currentPos;
        }

        /*
         * Returns the char left of the current parsing position.
         */
        internal VirtualChar CharAt(int i)
        {
            return _pattern[i];
        }

        /*
         * Returns the char right of the current parsing position.
         */
        internal VirtualChar RightChar()
        {
            return _pattern[_currentPos];
        }

        /*
         * Returns the char i chars right of the current parsing position.
         */
        internal VirtualChar RightChar(int i)
        {
            return _pattern[_currentPos + i];
        }

        /*
         * Number of characters to the right of the current parsing position.
         */
        internal int CharsRight()
        {
            return _pattern.Length - _currentPos;
        }
    }
}
#endif
