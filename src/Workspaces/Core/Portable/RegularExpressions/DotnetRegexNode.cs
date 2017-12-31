#if false
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This RegexNode class is internal to the Regex package.
// It is built into a parsed tree for a regular expression.

// Implementation notes:
//
// Since the node tree is a temporary data structure only used
// during compilation of the regexp to integer codes, it's
// designed for clarity and convenience rather than
// space efficiency.
//
// RegexNodes are built into a tree, linked by the _children list.
// Each node also has a _parent and _ichild member indicating
// its parent and which child # it is in its parent's list.
//
// RegexNodes come in as many types as there are constructs in
// a regular expression, for example, "concatenate", "alternate",
// "one", "rept", "group". There are also node types for basic
// peephole optimizations, e.g., "onerep", "notsetrep", etc.
//
// Because perl 5 allows "lookback" groups that scan backwards,
// each node also gets a "direction". Normally the value of
// boolean _backward = false.
//
// During parsing, top-level nodes are also stacked onto a parse
// stack (a stack of trees). For this purpose we have a _next
// pointer. [Note that to save a few bytes, we could overload the
// _parent pointer instead.]
//
// On the parse stack, each tree has a "role" - basically, the
// nonterminal in the grammar that the parser has currently
// assigned to the tree. That code is stored in _role.
//
// Finally, some of the different kinds of nodes have data.
// Two integers (for the looping constructs) are stored in
// _operands, an object (either a string or a set)
// is stored in _data

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    internal sealed class RegexNode
    {
        // RegexNode types

        // The following are leaves, and correspond to primitive operations

        internal const int Oneloop = 1;// RegexCode.Oneloop;                 // c,n      a*
        internal const int Notoneloop = 2;// RegexCode.Notoneloop;           // c,n      .*
        internal const int Setloop = 3;// RegexCode.Setloop;                 // set,n    \d*

        internal const int Onelazy = 4;// RegexCode.Onelazy;                 // c,n      a*?
        internal const int Notonelazy = 5;// RegexCode.Notonelazy;           // c,n      .*?
        internal const int Setlazy = 6;// RegexCode.Setlazy;                 // set,n    \d*?

        internal const int One = 7;// RegexCode.One;                         // char     a
        internal const int Notone = 8;// RegexCode.Notone;                   // char     . [^a]
        internal const int Set = 9;// RegexCode.Set;                         // set      [a-z] \w \s \d

        internal const int Multi = 10;// RegexCode.Multi;                     // string   abcdef
        internal const int Ref = 11;// RegexCode.Ref;                         // index    \1

        internal const int Bol = 12;// RegexCode.Bol;                         //          ^
        internal const int Eol = 13;// RegexCode.Eol;                         //          $
        internal const int Boundary = 14;// RegexCode.Boundary;               //          \b
        internal const int Nonboundary = 15;// RegexCode.Nonboundary;         //          \B
        internal const int ECMABoundary = 16;// RegexCode.ECMABoundary;       // \b
        internal const int NonECMABoundary = 17;// RegexCode.NonECMABoundary; // \B
        internal const int Beginning = 18;// RegexCode.Beginning;             //          \A
        internal const int Start = 19;// RegexCode.Start;                     //          \G
        internal const int EndZ = 20;// RegexCode.EndZ;                       //          \Z
        internal const int End = 21;// RegexCode.End;                         //          \z

        // Interior nodes do not correspond to primitive operations, but
        // control structures compositing other operations

        // Concat and alternate take n children, and can run forward or backwards

        internal const int Nothing = 22;                                //          []
        internal const int Empty = 23;                                  //          ()

        internal const int Alternate = 24;                              //          a|b
        internal const int Concatenate = 25;                            //          ab

        internal const int Loop = 26;                                   // m,x      * + ? {,}
        internal const int Lazyloop = 27;                               // m,x      *? +? ?? {,}?

        internal const int Capture = 28;                                // n        ()
        internal const int Group = 29;                                  //          (?:)
        internal const int Require = 30;                                //          (?=) (?<=)
        internal const int Prevent = 31;                                //          (?!) (?<!)
        internal const int Greedy = 32;                                 //          (?>) (?<)
        internal const int Testref = 33;                                //          (?(n) | )
        internal const int Testgroup = 34;                              //          (?(...) | )

        // RegexNode data members

        internal int _type;

        internal List<RegexNode> _children;

        //internal string _str;
        //internal char _ch;
        internal ImmutableArray<VirtualChar> _str;
        internal VirtualChar _ch;
        //internal int _m;
        //internal int _n;
        internal readonly RegexOptions _options;

        // Note: original regex source code uses _next.  But it always means _parent, so we
        // rename for clarity.
        internal RegexNode _parent;

        internal RegexNode(int type, RegexOptions options)
        {
            _type = type;
            _options = options;
        }

        internal RegexNode(int type, RegexOptions options, VirtualChar ch)
        {
            _type = type;
            _options = options;
            _ch = ch;
        }

        internal RegexNode(int type, RegexOptions options, ImmutableArray<VirtualChar> str)
        {
            _type = type;
            _options = options;
            _str = str;
        }

        //internal RegexNode(int type, RegexOptions options, int m)
        //{
        //    _type = type;
        //    _options = options;
        //    _m = m;
        //}

        //internal RegexNode(int type, RegexOptions options, int m, int n)
        //{
        //    _type = type;
        //    _options = options;
        //    _m = m;
        //    _n = n;
        //}

#if false
        internal bool UseOptionR()
        {
            return (_options & RegexOptions.RightToLeft) != 0;
        }

        internal RegexNode ReverseLeft()
        {
            if (UseOptionR() && _type == Concatenate && _children != null)
            {
                _children.Reverse(0, _children.Count);
            }

            return this;
        }

        /// <summary>
        /// Pass type as OneLazy or OneLoop
        /// </summary>
        internal void MakeRep(int type, int min, int max)
        {
            _type += (type - One);
            _m = min;
            _n = max;
        }
#endif
        /// <summary>
        /// Removes redundant nodes from the subtree, and returns a reduced subtree.
        /// </summary>
        internal RegexNode Reduce()
        {
            // Not supported (or desirable) in this implementation.  We want to keep around
            // all nodes so we have a tree that represents all data (like a normal Roslyn tree).
            // Note: it would be interesting to have a way to 'reduce' this tree so that we
            // could suggestion regex simplifications to users.  However, that's difficult as
            // the .net regex library encodes char-classes in complex ways, and it's not
            // obvious how to go back from the encoded system to a user representaiton for 
            // their code.
            return this;

#if false
            RegexNode n;

            switch (Type())
            {
            case Alternate:
                n = ReduceAlternation();
                break;

            case Concatenate:
                n = ReduceConcatenation();
                break;

            case Loop:
            case Lazyloop:
                n = ReduceRep();
                break;

            case Group:
                n = ReduceGroup();
                break;

            case Set:
            case Setloop:
                n = ReduceSet();
                break;

            default:
                n = this;
                break;
            }

            return n;
#endif
        }
#if false
        /// <summary>
        /// Simple optimization. If a concatenation or alternation has only
        /// one child strip out the intermediate node. If it has zero children,
        /// turn it into an empty.
        /// </summary>
        internal RegexNode StripEnation(int emptyType)
        {
            switch (ChildCount())
            {
            case 0:
                return new RegexNode(emptyType, _options);
            case 1:
                return Child(0);
            default:
                return this;
            }
        }

        /// <summary>
        /// Simple optimization. Once parsed into a tree, non-capturing groups
        /// serve no function, so strip them out.
        /// </summary>
        internal RegexNode ReduceGroup()
        {
            RegexNode u;

            for (u = this; u.Type() == Group;)
                u = u.Child(0);

            return u;
        }

        /// <summary>
        /// Nested repeaters just get multiplied with each other if they're not
        /// too lumpy
        /// </summary>
        internal RegexNode ReduceRep()
        {
            RegexNode u;
            RegexNode child;
            int type;
            int min;
            int max;

            u = this;
            type = Type();
            min = _m;
            max = _n;

            for (; ; )
            {
                if (u.ChildCount() == 0)
                    break;

                child = u.Child(0);

                // multiply reps of the same type only
                if (child.Type() != type)
                {
                    int childType = child.Type();

                    if (!(childType >= Oneloop && childType <= Setloop && type == Loop ||
                          childType >= Onelazy && childType <= Setlazy && type == Lazyloop))
                        break;
                }

                // child can be too lumpy to blur, e.g., (a {100,105}) {3} or (a {2,})?
                // [but things like (a {2,})+ are not too lumpy...]
                if (u._m == 0 && child._m > 1 || child._n < child._m * 2)
                    break;

                u = child;
                if (u._m > 0)
                    u._m = min = ((int.MaxValue - 1) / u._m < min) ? int.MaxValue : u._m * min;
                if (u._n > 0)
                    u._n = max = ((int.MaxValue - 1) / u._n < max) ? int.MaxValue : u._n * max;
            }

            return min == int.MaxValue ? new RegexNode(Nothing, _options) : u;
        }

        /// <summary>
        /// Simple optimization. If a set is a singleton, an inverse singleton,
        /// or empty, it's transformed accordingly.
        /// </summary>
        internal RegexNode ReduceSet()
        {
            // Extract empty-set, one and not-one case as special

            if (RegexCharClass.IsEmpty(_str))
            {
                _type = Nothing;
                _str = null;
            }
            else if (RegexCharClass.IsSingleton(_str))
            {
                _ch = RegexCharClass.SingletonChar(_str);
                _str = null;
                _type += (One - Set);
            }
            else if (RegexCharClass.IsSingletonInverse(_str))
            {
                _ch = RegexCharClass.SingletonChar(_str);
                _str = null;
                _type += (Notone - Set);
            }

            return this;
        }

        /// <summary>
        /// Basic optimization. Single-letter alternations can be replaced
        /// by faster set specifications, and nested alternations with no
        /// intervening operators can be flattened:
        ///
        /// a|b|c|def|g|h -> [a-c]|def|[gh]
        /// apple|(?:orange|pear)|grape -> apple|orange|pear|grape
        /// </summary>
        internal RegexNode ReduceAlternation()
        {
            // Combine adjacent sets/chars

            bool wasLastSet;
            bool lastNodeCannotMerge;
            RegexOptions optionsLast;
            RegexOptions optionsAt;
            int i;
            int j;
            RegexNode at;
            RegexNode prev;

            if (_children == null)
                return new RegexNode(Nothing, _options);

            wasLastSet = false;
            lastNodeCannotMerge = false;
            optionsLast = 0;

            for (i = 0, j = 0; i < _children.Count; i++, j++)
            {
                at = _children[i];

                if (j < i)
                    _children[j] = at;

                for (; ; )
                {
                    if (at._type == Alternate)
                    {
                        for (int k = 0; k < at._children.Count; k++)
                            at._children[k]._next = this;

                        _children.InsertRange(i + 1, at._children);
                        j--;
                    }
                    else if (at._type == Set || at._type == One)
                    {
                        // Cannot merge sets if L or I options differ, or if either are negated.
                        optionsAt = at._options & (RegexOptions.RightToLeft | RegexOptions.IgnoreCase);


                        if (at._type == Set)
                        {
                            if (!wasLastSet || optionsLast != optionsAt || lastNodeCannotMerge || !RegexCharClass.IsMergeable(at._str))
                            {
                                wasLastSet = true;
                                lastNodeCannotMerge = !RegexCharClass.IsMergeable(at._str);
                                optionsLast = optionsAt;
                                break;
                            }
                        }
                        else if (!wasLastSet || optionsLast != optionsAt || lastNodeCannotMerge)
                        {
                            wasLastSet = true;
                            lastNodeCannotMerge = false;
                            optionsLast = optionsAt;
                            break;
                        }


                        // The last node was a Set or a One, we're a Set or One and our options are the same.
                        // Merge the two nodes.
                        j--;
                        prev = _children[j];

                        RegexCharClass prevCharClass;
                        if (prev._type == One)
                        {
                            prevCharClass = new RegexCharClass();
                            prevCharClass.AddChar(prev._ch);
                        }
                        else
                        {
                            prevCharClass = RegexCharClass.Parse(prev._str);
                        }

                        if (at._type == One)
                        {
                            prevCharClass.AddChar(at._ch);
                        }
                        else
                        {
                            RegexCharClass atCharClass = RegexCharClass.Parse(at._str);
                            prevCharClass.AddCharClass(atCharClass);
                        }

                        prev._type = Set;
                        prev._str = prevCharClass.ToStringClass();
                    }
                    else if (at._type == Nothing)
                    {
                        j--;
                    }
                    else
                    {
                        wasLastSet = false;
                        lastNodeCannotMerge = false;
                    }
                    break;
                }
            }

            if (j < i)
                _children.RemoveRange(j, i - j);

            return StripEnation(Nothing);
        }

        /// <summary>
        /// Basic optimization. Adjacent strings can be concatenated.
        ///
        /// (?:abc)(?:def) -> abcdef
        /// </summary>
        internal RegexNode ReduceConcatenation()
        {
            // Eliminate empties and concat adjacent strings/chars

            bool wasLastString;
            RegexOptions optionsLast;
            RegexOptions optionsAt;
            int i;
            int j;

            if (_children == null)
                return new RegexNode(Empty, _options);

            wasLastString = false;
            optionsLast = 0;

            for (i = 0, j = 0; i < _children.Count; i++, j++)
            {
                RegexNode at;
                RegexNode prev;

                at = _children[i];

                if (j < i)
                    _children[j] = at;

                if (at._type == Concatenate &&
                    ((at._options & RegexOptions.RightToLeft) == (_options & RegexOptions.RightToLeft)))
                {
                    for (int k = 0; k < at._children.Count; k++)
                        at._children[k]._next = this;

                    _children.InsertRange(i + 1, at._children);
                    j--;
                }
                else if (at._type == Multi ||
                         at._type == One)
                {
                    // Cannot merge strings if L or I options differ
                    optionsAt = at._options & (RegexOptions.RightToLeft | RegexOptions.IgnoreCase);

                    if (!wasLastString || optionsLast != optionsAt)
                    {
                        wasLastString = true;
                        optionsLast = optionsAt;
                        continue;
                    }

                    prev = _children[--j];

                    if (prev._type == One)
                    {
                        prev._type = Multi;
                        prev._str = Convert.ToString(prev._ch, CultureInfo.InvariantCulture);
                    }

                    if ((optionsAt & RegexOptions.RightToLeft) == 0)
                    {
                        if (at._type == One)
                            prev._str += at._ch.ToString();
                        else
                            prev._str += at._str;
                    }
                    else
                    {
                        if (at._type == One)
                            prev._str = at._ch.ToString() + prev._str;
                        else
                            prev._str = at._str + prev._str;
                    }
                }
                else if (at._type == Empty)
                {
                    j--;
                }
                else
                {
                    wasLastString = false;
                }
            }

            if (j < i)
                _children.RemoveRange(j, i - j);

            return StripEnation(Empty);
        }

        internal RegexNode MakeQuantifier(bool lazy, int min, int max)
        {
            RegexNode result;

            if (min == 0 && max == 0)
                return new RegexNode(Empty, _options);

            if (min == 1 && max == 1)
                return this;

            switch (_type)
            {
            case One:
            case Notone:
            case Set:

                MakeRep(lazy ? Onelazy : Oneloop, min, max);
                return this;

            default:
                result = new RegexNode(lazy ? Lazyloop : Loop, _options, min, max);
                result.AddChild(this);
                return result;
            }
        }

#endif

        internal void AddChild(RegexNode newChild)
        {
            RegexNode reducedChild;

            if (_children == null)
                _children = new List<RegexNode>(4);

            reducedChild = newChild.Reduce();

            _children.Add(reducedChild);
            reducedChild._parent = this;
        }

#if false

        internal RegexNode Child(int i)
        {
            return _children[i];
        }

        internal int ChildCount()
        {
            return _children == null ? 0 : _children.Count;
        }

        internal int Type()
        {
            return _type;
        }

#if DEBUG
        internal static readonly string[] TypeStr = new string[] {
            "Onerep", "Notonerep", "Setrep",
            "Oneloop", "Notoneloop", "Setloop",
            "Onelazy", "Notonelazy", "Setlazy",
            "One", "Notone", "Set",
            "Multi", "Ref",
            "Bol", "Eol", "Boundary", "Nonboundary",
            "ECMABoundary", "NonECMABoundary",
            "Beginning", "Start", "EndZ", "End",
            "Nothing", "Empty",
            "Alternate", "Concatenate",
            "Loop", "Lazyloop",
            "Capture", "Group", "Require", "Prevent", "Greedy",
            "Testref", "Testgroup"};

        internal string Description()
        {
            StringBuilder ArgSb = new StringBuilder();

            ArgSb.Append(TypeStr[_type]);

            if ((_options & RegexOptions.ExplicitCapture) != 0)
                ArgSb.Append("-C");
            if ((_options & RegexOptions.IgnoreCase) != 0)
                ArgSb.Append("-I");
            if ((_options & RegexOptions.RightToLeft) != 0)
                ArgSb.Append("-L");
            if ((_options & RegexOptions.Multiline) != 0)
                ArgSb.Append("-M");
            if ((_options & RegexOptions.Singleline) != 0)
                ArgSb.Append("-S");
            if ((_options & RegexOptions.IgnorePatternWhitespace) != 0)
                ArgSb.Append("-X");
            if ((_options & RegexOptions.ECMAScript) != 0)
                ArgSb.Append("-E");

            switch (_type)
            {
            case Oneloop:
            case Notoneloop:
            case Onelazy:
            case Notonelazy:
            case One:
            case Notone:
                ArgSb.Append("(Ch = " + RegexCharClass.CharDescription(_ch) + ")");
                break;
            case Capture:
                ArgSb.Append("(index = " + _m.ToString(CultureInfo.InvariantCulture) + ", unindex = " + _n.ToString(CultureInfo.InvariantCulture) + ")");
                break;
            case Ref:
            case Testref:
                ArgSb.Append("(index = " + _m.ToString(CultureInfo.InvariantCulture) + ")");
                break;
            case Multi:
                ArgSb.Append("(String = " + _str + ")");
                break;
            case Set:
            case Setloop:
            case Setlazy:
                ArgSb.Append("(Set = " + RegexCharClass.SetDescription(_str) + ")");
                break;
            }

            switch (_type)
            {
            case Oneloop:
            case Notoneloop:
            case Onelazy:
            case Notonelazy:
            case Setloop:
            case Setlazy:
            case Loop:
            case Lazyloop:
                ArgSb.Append("(Min = " + _m.ToString(CultureInfo.InvariantCulture) + ", Max = " + (_n == int.MaxValue ? "inf" : Convert.ToString(_n, CultureInfo.InvariantCulture)) + ")");
                break;
            }

            return ArgSb.ToString();
        }

        internal const string Space = "                                ";

        internal void Dump()
        {
            List<int> Stack = new List<int>();
            RegexNode CurNode;
            int CurChild;

            CurNode = this;
            CurChild = 0;

            Debug.WriteLine(CurNode.Description());

            for (; ; )
            {
                if (CurNode._children != null && CurChild < CurNode._children.Count)
                {
                    Stack.Add(CurChild + 1);
                    CurNode = CurNode._children[CurChild];
                    CurChild = 0;

                    int Depth = Stack.Count;
                    if (Depth > 32)
                        Depth = 32;

                    Debug.WriteLine(Space.Substring(0, Depth) + CurNode.Description());
                }
                else
                {
                    if (Stack.Count == 0)
                        break;

                    CurChild = Stack[Stack.Count - 1];
                    Stack.RemoveAt(Stack.Count - 1);
                    CurNode = CurNode._next;
                }
            }
        }
#endif
#endif
    }
}
#endif
