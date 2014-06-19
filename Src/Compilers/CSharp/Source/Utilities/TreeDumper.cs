using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp
{
    internal sealed class TreeDumper
    {
        private StringBuilder sb;
        private TreeDumper()
        {
            this.sb = new StringBuilder();
        }

        public static string DumpCompact(TreeDumperNode root)
        {
            var dumper = new TreeDumper();
            dumper.DoDumpCompact(root, string.Empty);
            return dumper.sb.ToString();
        }

        private void DoDumpCompact(TreeDumperNode node, string indent)
        {
            Debug.Assert(node != null);
            Debug.Assert(indent != null);

            // Precondition: indentation and prefix has already been output
            // Precondition: indent is correct for node's *children*
            sb.Append(node.Text);
            if (node.Value != null)
            {
                sb.AppendFormat(": {0}", DumperString(node.Value));
            }

            sb.AppendLine();
            var children = node.Children.ToList();
            for (int i = 0; i < children.Count; ++i)
            {
                var child = children[i];
                if (child == null)
                {
                    continue;
                }

                sb.Append(indent);
                sb.Append(i == children.Count - 1 ? '└' : '├');
                sb.Append('─');

                // First precondition met; now work out the string needed to indent 
                // the child node's children:
                DoDumpCompact(child, indent + (i == children.Count - 1 ? "  " : "│ "));
            }
        }

        public static string DumpXML(TreeDumperNode root)
        {
            var dumper = new TreeDumper();
            dumper.DoDumpXML(root);
            return dumper.sb.ToString();
        }

        private void DoDumpXML(TreeDumperNode node)
        {
            Debug.Assert(node != null);
            if (!node.Children.Any(child => child != null))
            {
                if (node.Value != null)
                {
                    sb.AppendFormat("<{0}>{1}</{0}>", node.Text, DumperString(node.Value));
                }
                else
                {
                    sb.AppendFormat("<{0} />", node.Text);
                }

                sb.AppendLine();
            }
            else
            {
                sb.AppendFormat("<{0}>", node.Text);
                sb.AppendLine();
                if (node.Value != null)
                {
                    sb.AppendFormat("{0}", DumperString(node.Value));
                    sb.AppendLine();
                }

                foreach (var child in node.Children)
                {
                    if (child == null)
                    {
                        continue;
                    }

                    DoDumpXML(child);
                }

                sb.AppendFormat("</{0}>", node.Text);
                sb.AppendLine();
            }
        }

        private string DumperString(object o)
        {
            string result;

            if (o == null)
            {
                result = "(null)";
            }
            else if (o is string)
            {
                result = (string)o;
            }
            else if (o is IEnumerable)
            {
                IEnumerable seq = (IEnumerable)o;
                result = string.Format("{{{0}}}", string.Join(", ", seq.Cast<object>().Select(x => DumperString(x)).ToArray()));
            }
            else
            {
                result = o.ToString();
            }

            return result;
        }
    }
}