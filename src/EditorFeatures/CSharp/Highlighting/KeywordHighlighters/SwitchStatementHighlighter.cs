﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters
{
    [ExportHighlighter(LanguageNames.CSharp)]
    internal class SwitchStatementHighlighter : AbstractKeywordHighlighter<SwitchStatementSyntax>
    {
        protected override IEnumerable<TextSpan> GetHighlights(
            SwitchStatementSyntax switchStatement, CancellationToken cancellationToken)
        {
            var spans = new List<TextSpan>();

            spans.Add(switchStatement.SwitchKeyword.Span);

            foreach (var switchSection in switchStatement.Sections)
            {
                foreach (var label in switchSection.Labels)
                {
                    spans.Add(label.Keyword.Span);
                    spans.Add(EmptySpan(label.ColonToken.Span.End));
                }

                HighlightRelatedKeywords(switchSection, spans, true, true);
            }

            return spans;
        }

        /// <summary>
        /// Finds all breaks and continues that are a child of this node, and adds the appropriate spans to the spans
        /// list.
        /// </summary>
        private void HighlightRelatedKeywords(SyntaxNode node, List<TextSpan> spans, bool highlightBreaks, bool highlightGotos)
        {
            Debug.Assert(highlightBreaks || highlightGotos);

            if (highlightBreaks && node is BreakStatementSyntax breakStatement)
            {
                spans.Add(breakStatement.BreakKeyword.Span);
                spans.Add(EmptySpan(breakStatement.SemicolonToken.Span.End));
            }
            else if (highlightGotos && node is GotoStatementSyntax gotoStatement
                && (!gotoStatement.IsKind(SyntaxKind.GotoStatement) || gotoStatement.Expression.IsMissing))
            {
                var start = gotoStatement.GotoKeyword.SpanStart;
                var end = !gotoStatement.CaseOrDefaultKeyword.IsKind(SyntaxKind.None)
                    ? gotoStatement.CaseOrDefaultKeyword.Span.End
                    : gotoStatement.GotoKeyword.Span.End;

                spans.Add(TextSpan.FromBounds(start, end));
                spans.Add(EmptySpan(gotoStatement.SemicolonToken.Span.End));
            }
            else
            {
                foreach (var child in node.ChildNodes())
                {
                    var highlightBreaksForChild = highlightBreaks && !child.IsBreakableConstruct();
                    var highlightGotosForChild = highlightGotos && !child.IsKind(SyntaxKind.SwitchStatement);

                    // Only recurse if we have anything to do
                    if (highlightBreaksForChild || highlightGotosForChild)
                    {
                        HighlightRelatedKeywords(child, spans, highlightBreaksForChild, highlightGotosForChild);
                    }
                }
            }
        }
    }
}
