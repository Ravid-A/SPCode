﻿using System.Text;

namespace SPCode.Utils.SPSyntaxTidy;

public static class SPSyntaxTidy
{
    public static string TidyUp(string source)
    {
        var LookForSingleIndentationSegment = false;
        var SingleIndentationSegmentScope = 0;
        var indentationLevel = 0;
        var outString = new StringBuilder();
        var token = SPTokenizer.Tokenize(source);
        var length = token.Length;
        for (var i = 0; i < length; ++i)
        {
            if (token[i].Kind == SPTokenKind.Newline)
            {
                outString.AppendLine();
                var subIndentLevel = indentationLevel;
                var nextToken = GetTokenSave(i + 1, token, length);
                if (nextToken.Kind == SPTokenKind.BracketClose)
                {
                    --subIndentLevel;
                    if (subIndentLevel < 0) { subIndentLevel = 0; }
                }
                else if (nextToken.Kind != SPTokenKind.BracketOpen)
                {
                    if (LookForSingleIndentationSegment)
                    {
                        if (SingleIndentationSegmentScope == 0)
                        {
                            subIndentLevel++;
                        }
                    }
                }
                else if (nextToken.Kind == SPTokenKind.PreProcessorLine) //preporcessor directives should not indented
                {
                    subIndentLevel = 0;
                }
                LookForSingleIndentationSegment = false;
                SingleIndentationSegmentScope = 0;
                for (var j = 0; j < subIndentLevel; ++j)
                {
                    outString.Append(Program.Indentation);
                }
                continue;
            }
            if (token[i].Kind == SPTokenKind.BracketOpen)
            {
                LookForSingleIndentationSegment = false;
                var lastToken = GetTokenSave(i - 1, token, length);
                if (lastToken.Kind != SPTokenKind.Newline && lastToken.Kind != SPTokenKind.Comma && lastToken.Kind != SPTokenKind.Operator)
                {
                    outString.Append(" ");
                }
                outString.Append("{");
                if (GetTokenSave(i + 1, token, length).Kind != SPTokenKind.Newline)
                {
                    outString.Append(" ");
                }
                ++indentationLevel;
                continue;
            }
            if (token[i].Kind == SPTokenKind.BracketClose)
            {
                if (GetTokenSave(i - 1, token, length).Kind != SPTokenKind.Newline)
                {
                    outString.Append(" ");
                }
                outString.Append("}");
                var nextToken = GetTokenSave(i + 1, token, length);
                if (nextToken.Kind != SPTokenKind.Newline && nextToken.Kind != SPTokenKind.Comma && nextToken.Kind != SPTokenKind.Semicolon
                    && nextToken.Kind != SPTokenKind.SingleLineComment && nextToken.Kind != SPTokenKind.BracketClose)
                {
                    outString.Append(" ");
                }
                --indentationLevel;
                if (indentationLevel < 0) { indentationLevel = 0; }
                continue;
            }
            if (token[i].Kind == SPTokenKind.PreProcessorLine)
            {
                outString.Append(token[i].Value);
                continue;
            }
            if (token[i].Kind == SPTokenKind.SingleLineComment)
            {
                if (GetTokenSave(i - 1, token, length).Kind != SPTokenKind.Newline)
                {
                    outString.Append(" ");
                }
                outString.Append(token[i].Value);
                continue;
            }
            if (token[i].Kind == SPTokenKind.Operator)
            {
                if (token[i].Value == "-")
                {
                    var lastToken = GetTokenSave(i - 1, token, length);
                    var nextToken = GetTokenSave(i + 1, token, length);
                    var lastTokenIsName = lastToken.Kind == SPTokenKind.Name;
                    var lastTokenValid = lastTokenIsName || IsTokenNumber(lastToken);
                    if (!lastTokenValid)
                    {
                        if (lastToken.Kind == SPTokenKind.Symbol)
                        {
                            lastTokenValid = (lastToken.Value == ")") || (lastToken.Value == "]");
                        }
                    }
                    if (lastTokenIsName)
                    {
                        lastTokenValid = lastToken.Value != "e" && lastToken.Value != "return";
                    }
                    var nextTokenValid = (nextToken.Kind == SPTokenKind.Name) || IsTokenNumber(nextToken);
                    if (!nextTokenValid)
                    {
                        if (nextToken.Kind == SPTokenKind.Symbol)
                        {
                            nextTokenValid = nextToken.Value == "(";
                        }
                    }
                    if (nextTokenValid && lastTokenValid)
                    {
                        outString.Append(" - ");
                    }
                    else
                    {
                        outString.Append("-");
                    }
                    continue;
                }
                outString.Append(" " + token[i].Value + " ");
                continue;
            }
            if (token[i].Kind == SPTokenKind.Name)
            {
                if (token[i].Value == "return" && GetTokenSave(i + 1, token, length).Kind != SPTokenKind.Semicolon)
                {
                    outString.Append("return ");
                    continue;
                }
                if (token[i].Value == "if" || token[i].Value == "else" || token[i].Value == "for" || token[i].Value == "while")
                {
                    LookForSingleIndentationSegment = true;
                    SingleIndentationSegmentScope = 0;
                }
                outString.Append(token[i].Value);
                if (GetTokenSave(i + 1, token, length).Kind == SPTokenKind.Name)
                {
                    outString.Append(" ");
                }
                else if (IsPreWhiteSpaceName(token[i].Value))
                {
                    outString.Append(" ");
                }
                continue;
            }
            if (token[i].Kind == SPTokenKind.Comma)
            {
                outString.Append(", ");
                continue;
            }
            if (token[i].Kind == SPTokenKind.Semicolon)
            {
                LookForSingleIndentationSegment = false;
                outString.Append(";");
                var nextToken = GetTokenSave(i + 1, token, length);
                if (nextToken.Kind != SPTokenKind.Newline && nextToken.Kind != SPTokenKind.BracketClose && nextToken.Kind != SPTokenKind.SingleLineComment)
                {
                    outString.Append(" ");
                }
                continue;
            }
            if (token[i].Kind == SPTokenKind.Symbol)
            {
                if (token[i].Value == "]")
                {
                    if (GetTokenSave(i + 1, token, length).Kind == SPTokenKind.Name)
                    {
                        outString.Append("] ");
                        continue;
                    }
                }
                if (token[i].Value == "(")
                {
                    ++SingleIndentationSegmentScope;
                    ++indentationLevel;
                }
                else if (token[i].Value == ")")
                {
                    --SingleIndentationSegmentScope;
                    --indentationLevel;
                }
                if (token[i].Value == "&") //addressof operator
                {
                    if (GetTokenSave(i - 1, token, length).Kind == SPTokenKind.Name && GetTokenSave(i + 1, token, length).Kind == SPTokenKind.Name)
                    {
                        outString.Append(" &");
                        continue;
                    }
                }
            }
            outString.Append(token[i].Value);
        }
        return outString.ToString();
    }

    public static SPToken GetTokenSave(int index, SPToken[] token, int length)
    {
        if (index < 0 || index >= length)
        {
            return new SPToken() { Kind = SPTokenKind.Invalid };
        }
        return token[index];
    }

    public static bool IsPreWhiteSpaceName(string name)
    {
        return name switch
        {
            "if" => true,
            "for" => true,
            "while" => true,
            "switch" => true,
            "case" => true,
            _ => false,
        };
    }

    public static bool IsTokenNumber(SPToken token)
    {
        if (token == null)
        {
            return false;
        }
        if (token.Kind == SPTokenKind.Invalid)
        {
            return false;
        }
        if (token.Value.Length == 1)
        {
            if (token.Value[0] >= '0' && token.Value[0] <= '9')
            {
                return true;
            }
        }
        return false;
    }
}