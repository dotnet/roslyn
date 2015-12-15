// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// Object pretty printer.
    /// </summary>
    public abstract partial class CommonObjectFormatter
    {
        private sealed class Builder
        {
            private readonly StringBuilder _sb;

            private readonly bool _suppressEllipsis;

            private readonly BuilderOptions _options;

            private int _currentLimit;

            public Builder(BuilderOptions options, bool suppressEllipsis)
            {
                _sb = new StringBuilder();
                _suppressEllipsis = suppressEllipsis;
                _options = options;
                _currentLimit = Math.Min(_options.MaximumLineLength, _options.MaximumOutputLength);
            }

            public bool LimitReached
            {
                get { return _sb.Length == _options.MaximumOutputLength; }
            }

            public int Remaining
            {
                get { return _options.MaximumOutputLength - _sb.Length; }
            }

            // can be negative (the min value is -Ellipsis.Length - 1)
            private int CurrentRemaining
            {
                get { return _currentLimit - _sb.Length; }
            }

            public void AppendLine()
            {
                // remove line length limit so that we can insert a new line even 
                // if the previous one hit maxed out the line limit:
                _currentLimit = _options.MaximumOutputLength;

                Append(_options.NewLine);

                // recalc limit for the next line:
                _currentLimit = (int)Math.Min((long)_sb.Length + _options.MaximumLineLength, _options.MaximumOutputLength);
            }

            private void AppendEllipsis()
            {
                if (_suppressEllipsis)
                {
                    return;
                }

                var ellipsis = _options.Ellipsis;
                if (string.IsNullOrEmpty(ellipsis))
                {
                    return;
                }

                _sb.Append(ellipsis);
            }

            public void Append(char c, int count = 1)
            {
                if (CurrentRemaining < 0)
                {
                    return;
                }

                int length = Math.Min(count, CurrentRemaining);

                _sb.Append(c, length);

                if (!_suppressEllipsis && length < count)
                {
                    AppendEllipsis();
                }
            }

            public void Append(string str, int start = 0, int count = Int32.MaxValue)
            {
                if (str == null || CurrentRemaining < 0)
                {
                    return;
                }

                count = Math.Min(count, str.Length - start);
                int length = Math.Min(count, CurrentRemaining);
                _sb.Append(str, start, length);

                if (!_suppressEllipsis && length < count)
                {
                    AppendEllipsis();
                }
            }

            public void AppendFormat(string format, params object[] args)
            {
                Append(string.Format(format, args));
            }

            public void AppendGroupOpening()
            {
                Append('{');
            }

            public void AppendGroupClosing(bool inline)
            {
                if (inline)
                {
                    Append(" }");
                }
                else
                {
                    AppendLine();
                    Append('}');
                    AppendLine();
                }
            }

            public void AppendCollectionItemSeparator(bool isFirst, bool inline)
            {
                if (isFirst)
                {
                    if (inline)
                    {
                        Append(' ');
                    }
                    else
                    {
                        AppendLine();
                    }
                }
                else
                {
                    if (inline)
                    {
                        Append(", ");
                    }
                    else
                    {
                        Append(',');
                        AppendLine();
                    }
                }

                if (!inline)
                {
                    Append(_options.Indentation);
                }
            }

            /// <remarks>
            /// This is for conveying cyclic dependencies to the user, not for detecting them.
            /// </remarks>
            internal void AppendInfiniteRecursionMarker()
            {
                AppendGroupOpening();
                AppendCollectionItemSeparator(isFirst: true, inline: true);
                Append("...");
                AppendGroupClosing(inline: true);
            }

            public override string ToString()
            {
                return _sb.ToString();
            }
        }
    }
}