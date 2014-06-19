// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp
{
    internal class ILVisualizer
    {
        private ILBuilder stream;
        private LocalSlotManager slotManager;

        internal ILVisualizer(ILBuilder stream, LocalSlotManager slotManager)
        {
            this.stream = stream;
            this.slotManager = slotManager;
        }

        public string Visualize()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("{");
            VisualizeHeader(sb);
            VisualizeIL(sb);
            sb.AppendLine("}");

            return sb.ToString();
        }

        private void VisualizeHeader(StringBuilder sb)
        {
            sb.AppendLine(String.Format("  // Code size {0,8} (0x{0:x})", stream.CodeSize));

            if (slotManager.NumSlots > 0)
            {
                for (int i = 0; i < slotManager.NumSlots; ++i)
                {
                    sb.Append(i == 0 ? "  .locals init (" : "           ");
                    LocalSymbol l = slotManager.LocalAtSlot(i);
                    sb.Append(String.Format("[{0}] {1} {2}", i, l.Type, l.Name));
                    sb.AppendLine(i == slotManager.NumSlots - 1 ? ")" : ",");
                }
            }
        }

        private void VisualizeIL(StringBuilder sb)
        {
            var first = true;
            foreach (object item in stream.GetStream())
            {
                if (item is ILOpCode)
                {
                    if (!first)
                    {
                        sb.AppendLine();
                    }

                    sb.Append(String.Format("  {0,-10}", ((ILOpCode)item).Name()));
                }
                else if (item is string)
                {
                    sb.Append(String.Format(" \"{0}\"", item));
                }
                else if (item is sbyte)
                {
                    sb.Append(String.Format(" 0x{0:x2}", item));
                }
                else if (item is int)
                {
                    sb.Append(String.Format(" 0x{0:x4}", item));
                }
                else if (item is long)
                {
                    sb.Append(String.Format(" 0x{0:x8}", item));
                }
                else
                {
                    sb.Append(String.Format(" {0}", item));
                }

                first = false;
            }

            sb.AppendLine();
        }
    }
}
