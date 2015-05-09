// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;

public class Range
{
    public ClassifiedSpan ClassifiedSpan { get; private set; }
    public string Text { get; private set; }

    public Range(string classification, TextSpan span, SourceText text) :
        this(classification, span, text.GetSubText(span).ToString())
    {
    }

    public Range(string classification, TextSpan span, string text) :
        this(new ClassifiedSpan(classification, span), text)
    {
    }

    public Range(ClassifiedSpan classifiedSpan, string text)
    {
        this.ClassifiedSpan = classifiedSpan;
        this.Text = text;
    }

    public string ClassificationType
    {
        get { return ClassifiedSpan.ClassificationType; }
    }

    public TextSpan TextSpan
    {
        get { return ClassifiedSpan.TextSpan; }
    }
}
