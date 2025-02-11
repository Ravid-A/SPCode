﻿using System;
using System.Collections.Generic;
using System.Text;
using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser;

public partial class Condenser
{
    private int ConsumeSMMethodmap()
    {
        var startIndex = _tokens[_position].Index;
        var iteratePosition = _position + 1;
        if (_position + 4 < _length)
        {
            var methodMapName = string.Empty;
            var methods = new List<SMObjectMethod>();
            var fields = new List<SMObjectField>();
            if (_tokens[iteratePosition].Kind == TokenKind.Identifier)
            {
                methodMapName = _tokens[iteratePosition].Value;

                // Handle declaration like "methodmap MyMap __nullable { ... }"
                if (_tokens[iteratePosition + 1].Kind == TokenKind.Identifier)
                {
                    ++iteratePosition;
                }
                ++iteratePosition;
            }
            var inheriteType = string.Empty;
            var enteredBlock = false;
            var braceIndex = 0;
            var lastIndex = -1;
            for (; iteratePosition < _length; ++iteratePosition)
            {
                if (_tokens[iteratePosition].Kind == TokenKind.BraceOpen)
                {
                    ++braceIndex;
                    enteredBlock = true;
                    continue;
                }

                if (_tokens[iteratePosition].Kind == TokenKind.BraceClose)
                {
                    --braceIndex;
                    if (braceIndex <= 0)
                    {
                        lastIndex = iteratePosition;
                        break;
                    }
                }
                else if (braceIndex == 0 && _tokens[iteratePosition].Kind == TokenKind.Character)
                {
                    if (_tokens[iteratePosition].Value == "<")
                    {
                        if (iteratePosition + 1 < _length)
                        {
                            if (_tokens[iteratePosition + 1].Kind == TokenKind.Identifier)
                            {
                                inheriteType = _tokens[iteratePosition + 1].Value;
                                ++iteratePosition;
                                continue;
                            }
                        }
                    }
                }
                else if (enteredBlock)
                {
                    if (_tokens[iteratePosition].Kind == TokenKind.FunctionIndicator)
                    {
                        var mStartIndex = _tokens[iteratePosition].Index;
                        var functionCommentString = string.Empty;
                        var commentTokenIndex = BacktraceTestForToken(iteratePosition - 1, TokenKind.MultiLineComment, true, false);
                        if (commentTokenIndex == -1)
                        {
                            commentTokenIndex = BacktraceTestForToken(iteratePosition - 1, TokenKind.SingleLineComment, true, false);
                            if (commentTokenIndex != -1)
                            {
                                var strBuilder = new StringBuilder(_tokens[commentTokenIndex].Value);
                                while ((commentTokenIndex = BacktraceTestForToken(commentTokenIndex - 1, TokenKind.SingleLineComment, true, false)) != -1)
                                {
                                    strBuilder.Insert(0, Environment.NewLine);
                                    strBuilder.Insert(0, _tokens[commentTokenIndex].Value);
                                }
                                functionCommentString = strBuilder.ToString();
                            }
                        }
                        else
                        {
                            functionCommentString = _tokens[commentTokenIndex].Value;
                        }
                        var mEndIndex = mStartIndex;
                        var functionIndicators = new List<string>();
                        var parameters = new List<string>();
                        var methodName = string.Empty;
                        var methodReturnType = string.Empty;
                        var ParsingIndicators = true;
                        var InCodeSection = false;
                        var ParenthesisIndex = 0;
                        var mBraceIndex = 0;
                        var AwaitingName = true;
                        var lastFoundParam = string.Empty;
                        var foundCurentParameter = false;
                        var InSearchForComma = false;
                        for (var i = iteratePosition; i < _length; ++i)
                        {
                            if (InCodeSection)
                            {
                                if (_tokens[i].Kind == TokenKind.BraceOpen)
                                {
                                    ++mBraceIndex;
                                }
                                else if (_tokens[i].Kind == TokenKind.BraceClose)
                                {
                                    --mBraceIndex;
                                    if (mBraceIndex <= 0)
                                    {
                                        iteratePosition = i;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (ParsingIndicators)
                                {
                                    if (_tokens[i].Kind == TokenKind.FunctionIndicator)
                                    {
                                        functionIndicators.Add(_tokens[i].Value);
                                        continue;
                                    }
                                    else
                                    {
                                        ParsingIndicators = false;
                                    }
                                }
                                if (_tokens[i].Kind == TokenKind.Identifier && AwaitingName)
                                {
                                    if ((i + 1) < _length)
                                    {
                                        if (_tokens[i + 1].Kind == TokenKind.Identifier)
                                        {
                                            methodReturnType = _tokens[i].Value;
                                            methodName = _tokens[i + 1].Value;
                                            ++i;
                                        }
                                        else
                                        {
                                            methodName = _tokens[i].Value;
                                        }
                                        AwaitingName = false;
                                    }
                                    continue;
                                }
                                if (_tokens[i].Kind == TokenKind.ParenthesisOpen)
                                {
                                    ++ParenthesisIndex;
                                    continue;
                                }
                                if (_tokens[i].Kind == TokenKind.ParenthesisClose)
                                {
                                    --ParenthesisIndex;
                                    if (ParenthesisIndex == 0)
                                    {
                                        if (foundCurentParameter)
                                        {
                                            parameters.Add(lastFoundParam);
                                            lastFoundParam = string.Empty;
                                        }
                                        InCodeSection = true;
                                        if ((i + 1) < _length)
                                        {
                                            if (_tokens[i + 1].Kind == TokenKind.Semicolon)
                                            {
                                                iteratePosition = i + 1;
                                                mEndIndex = _tokens[i + 1].Index;
                                                break;
                                            }
                                            iteratePosition = i;
                                            mEndIndex = _tokens[i].Index;
                                        }
                                    }
                                    continue;
                                }
                                if ((_tokens[i].Kind == TokenKind.Identifier) && (!InSearchForComma))
                                {
                                    lastFoundParam = _tokens[i].Value;
                                    foundCurentParameter = true;
                                    continue;
                                }
                                if (_tokens[i].Kind == TokenKind.Comma)
                                {
                                    parameters.Add(lastFoundParam);
                                    lastFoundParam = string.Empty;
                                    InSearchForComma = false;
                                }
                                else if (_tokens[i].Kind == TokenKind.Assignment)
                                {
                                    InSearchForComma = true;
                                }
                            }
                        }
                        if (mStartIndex < mEndIndex)
                        {
                            methods.Add(new SMObjectMethod()
                            {
                                Index = mStartIndex,
                                Name = methodName,
                                ReturnType = methodReturnType,
                                /*MethodKind = functionIndicators.ToArray(),
                                Parameters = parameters.ToArray(),*/
                                FullName = TrimFullname(_source.Substring(mStartIndex, mEndIndex - mStartIndex + 1)),
                                Length = mEndIndex - mStartIndex + 1,
                                CommentString = TrimComments(functionCommentString),
                                ClassName = methodMapName,
                                File = _fileName
                            });
                        }
                    }
                    else if (_tokens[iteratePosition].Kind == TokenKind.Property)
                    {
                        var fStartIndex = _tokens[iteratePosition].Index;
                        var fEndIndex = fStartIndex;
                        if ((iteratePosition - 1) >= 0)
                        {
                            if (_tokens[iteratePosition - 1].Kind == TokenKind.FunctionIndicator)
                            {
                                fStartIndex = _tokens[iteratePosition - 1].Index;
                            }
                        }
                        var fieldName = string.Empty;
                        var InPureSemicolonSearch = false;
                        var fBracketIndex = 0;
                        for (var j = iteratePosition; j < _length; ++j)
                        {
                            if (_tokens[j].Kind == TokenKind.Identifier && !InPureSemicolonSearch)
                            {
                                fieldName = _tokens[j].Value;
                                continue;
                            }
                            if (_tokens[j].Kind == TokenKind.Assignment)
                            {
                                InPureSemicolonSearch = true;
                                continue;
                            }
                            if (_tokens[j].Kind == TokenKind.Semicolon)
                            {
                                if (fStartIndex == fEndIndex && fBracketIndex == 0)
                                {
                                    iteratePosition = j;
                                    fEndIndex = _tokens[j].Index;
                                    break;
                                }
                            }
                            if (_tokens[j].Kind == TokenKind.BraceOpen)
                            {
                                if (!InPureSemicolonSearch)
                                {
                                    InPureSemicolonSearch = true;
                                    fEndIndex = _tokens[j].Index - 1;
                                }
                                ++fBracketIndex;
                            }
                            else if (_tokens[j].Kind == TokenKind.BraceClose)
                            {
                                --fBracketIndex;
                                if (fBracketIndex == 0)
                                {
                                    if ((j + 1) < _length)
                                    {
                                        if (_tokens[j + 1].Kind == TokenKind.Semicolon)
                                        {
                                            iteratePosition = j + 1;
                                        }
                                        else
                                        {
                                            iteratePosition = j;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        if (fStartIndex < fEndIndex)
                        {
                            fields.Add(new SMObjectField
                            {
                                Index = fStartIndex,
                                Length = fEndIndex - fStartIndex + 1,
                                Name = fieldName,
                                File = _fileName,
                                ClassName = methodMapName,
                                FullName = _source.Substring(fStartIndex, fEndIndex - fStartIndex + 1)
                            });
                        }
                    }
                }
            }
            if (enteredBlock && braceIndex == 0)
            {
                var mm = new SMMethodmap()
                {
                    Index = startIndex,
                    Length = _tokens[lastIndex].Index - startIndex + 1,
                    Name = methodMapName,
                    File = _fileName,
                    InheritedType = inheriteType
                };
                mm.Methods.AddRange(methods);
                mm.Fields.AddRange(fields);
                _def.Methodmaps.Add(mm);
                _position = lastIndex;
            }
        }
        return -1;
    }
}