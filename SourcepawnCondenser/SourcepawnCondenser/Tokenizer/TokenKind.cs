﻿namespace SourcepawnCondenser.Tokenizer;

public enum TokenKind
{
    Identifier,             //done
    Number,                 //d
    Character,              //d
    BraceOpen,              //d
    BraceClose,             //d
    ParenthesisOpen,        //d
    ParenthesisClose,       //d
    Quote,                  //d
    SingleLineComment,      //d
    MultiLineComment,       //d
    Semicolon,              //d
    Comma,                  //d
    Assignment,             //d

    FunctionIndicator,      //d
    Constant,               //d
    EnumStruct,
    Enum,                   //d
    Struct,                 //d
    MethodMap,              //d
    Property,               //d
    PreprocessorDirective,   //d
    TypeDef,                //d
    TypeSet,                //d
    New,

    EOL,                    //d
    EOF,                    //d
}