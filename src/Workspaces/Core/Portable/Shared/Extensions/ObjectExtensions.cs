// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ObjectExtensions
    {
        public static string GetTypeDisplayName(this object obj)
        {
            return obj == null ? "null" : obj.GetType().Name;
        }
    }
}

// Code to generate typeswitches.
#if false
using System;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        var sb = new StringBuilder();
        for (int i = 1; i <= 32; i++)
        {
            GenerateActionTypeSwitch(i, sb);
            sb.AppendLine();

            GenerateFuncTypeSwitch(i, sb);
            sb.AppendLine();
        }

        var str = sb.ToString();
        Console.WriteLine(str);
    }

    private static void GenerateActionTypeSwitch(int count, StringBuilder sb)
    {
        sb.Append("public static void TypeSwitch<TBaseType");
        for (int i = 1; i <= count; i++)
        {
            sb.Append(", TDerivedType" + i);
        }
        sb.Append(">(this TBaseType obj");
        for (int i = 1; i <= count; i++)
        {
            sb.Append(string.Format(", Action<TDerivedType{0}> matchAction{0}", i));
        }
        sb.Append(", Action<TBaseType> defaultAction = null)");

        for (int i = 1; i <= count; i++)
        {
            sb.Append(string.Format(" where TDerivedType{0} : TBaseType", i));
        }

        sb.AppendLine();
        sb.AppendLine("{");
        sb.AppendLine("    if (obj is TDerivedType1)");
        sb.AppendLine("    {");
        sb.AppendLine("        matchAction1((TDerivedType1)obj);");
        sb.AppendLine("    }");

        if (count == 1)
        {
            sb.AppendLine("    else if (defaultAction != null)");
            sb.AppendLine("    {");
            sb.AppendLine("        defaultAction(obj);");
            sb.AppendLine("    }");
        }
        else
        {
            sb.AppendLine("    else");
            sb.AppendLine("    {");
            sb.Append("        TypeSwitch(obj");

            for (int i = 2; i <= count; i++)
            {
                sb.Append(string.Format(", matchAction{0}", i));
            }

            sb.AppendLine(", defaultAction);");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");
    }

    private static void GenerateFuncTypeSwitch(int count, StringBuilder sb)
    {
        sb.Append("public static TResult TypeSwitch<TBaseType");
        for (int i = 1; i <= count; i++)
        {
            sb.Append(", TDerivedType" + i);
        }
        sb.Append(", TResult>(this TBaseType obj");
        for (int i = 1; i <= count; i++)
        {
            sb.Append(string.Format(", Func<TDerivedType{0},TResult> matchFunc{0}", i));
        }
        sb.Append(", Func<TBaseType,TResult> defaultFunc = null)");

        for (int i = 1; i <= count; i++)
        {
            sb.Append(string.Format(" where TDerivedType{0} : TBaseType", i));
        }

        sb.AppendLine();
        sb.AppendLine("{");
        sb.AppendLine("    if (obj is TDerivedType1)");
        sb.AppendLine("    {");
        sb.AppendLine("        return matchFunc1((TDerivedType1)obj);");
        sb.AppendLine("    }");

        if (count == 1)
        {
            sb.AppendLine("    else if (defaultFunc != null)");
            sb.AppendLine("    {");
            sb.AppendLine("        return defaultFunc(obj);");
            sb.AppendLine("    }");
            sb.AppendLine("    else");
            sb.AppendLine("    {");
            sb.AppendLine("        return default(TResult);");
            sb.AppendLine("    }");
        }
        else
        {
            sb.AppendLine("    else");
            sb.AppendLine("    {");
            sb.Append("        return TypeSwitch(obj");

            for (int i = 2; i <= count; i++)
            {
                sb.Append(string.Format(", matchFunc{0}", i));
            }

            sb.AppendLine(", defaultFunc);");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");
    }
}
#endif
