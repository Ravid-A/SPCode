﻿using System.Windows;

namespace SPCode.UI.Windows;

public partial class OptionsWindow
{
    private void LoadSH()
    {
        SH_Comments.SetContent("Comments", Program.OptionsObject.SH_Comments);
        SH_CommentMarkers.SetContent("Comment Markers", Program.OptionsObject.SH_CommentsMarker);
        SH_PreProcessor.SetContent("Pre-Processor Directives", Program.OptionsObject.SH_PreProcessor);
        SH_Strings.SetContent("Strings / Includes", Program.OptionsObject.SH_Strings);
        SH_Types.SetContent("Types", Program.OptionsObject.SH_Types);
        SH_TypesValues.SetContent("Type-Values", Program.OptionsObject.SH_TypesValues);
        SH_Keywords.SetContent("Keywords", Program.OptionsObject.SH_Keywords);
        SH_ContextKeywords.SetContent("Context-Keywords", Program.OptionsObject.SH_ContextKeywords);
        SH_Chars.SetContent("Chars", Program.OptionsObject.SH_Chars);
        SH_Numbers.SetContent("Numbers", Program.OptionsObject.SH_Numbers);
        SH_SpecialCharacters.SetContent("Special Characters", Program.OptionsObject.SH_SpecialCharacters);
        SH_UnknownFunctions.SetContent("Unknown Functions", Program.OptionsObject.SH_UnkownFunctions);
        SH_Deprecated.SetContent("Deprecated Content", Program.OptionsObject.SH_Deprecated);
        SH_Constants.SetContent("Parsed Contants", Program.OptionsObject.SH_Constants);
        SH_Functions.SetContent("Parsed Functions", Program.OptionsObject.SH_Functions);
        SH_Methods.SetContent("Parsed Methods", Program.OptionsObject.SH_Methods);
    }

    private void Comments_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging) return;
        Program.OptionsObject.SH_Comments = SH_Comments.GetColor();
    }

    private void CommentMarker_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging) return;
        Program.OptionsObject.SH_CommentsMarker = SH_CommentMarkers.GetColor();
    }

    private void PreProcessor_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging) return;
        Program.OptionsObject.SH_PreProcessor = SH_PreProcessor.GetColor();
    }

    private void String_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging) return;
        Program.OptionsObject.SH_Strings = SH_Strings.GetColor();
    }

    private void Types_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging) return;
        Program.OptionsObject.SH_Types = SH_Types.GetColor();
    }

    private void TypeValues_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging) return;
        Program.OptionsObject.SH_TypesValues = SH_TypesValues.GetColor();
    }

    private void Keywords_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging) return;
        Program.OptionsObject.SH_Keywords = SH_Keywords.GetColor();
    }

    private void ContextKeywords_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging) return;
        Program.OptionsObject.SH_ContextKeywords = SH_ContextKeywords.GetColor();
    }

    private void Chars_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging) return;
        Program.OptionsObject.SH_Chars = SH_Chars.GetColor();
    }
    private void UFunctions_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging) return;
        Program.OptionsObject.SH_UnkownFunctions = SH_UnknownFunctions.GetColor();
    }
    private void Numbers_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging) return;
        Program.OptionsObject.SH_Numbers = SH_Numbers.GetColor();
    }
    private void SpecialCharacters_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging) return;
        Program.OptionsObject.SH_SpecialCharacters = SH_SpecialCharacters.GetColor();
    }
    private void Deprecated_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging) return;
        Program.OptionsObject.SH_Deprecated = SH_Deprecated.GetColor();
    }
    private void Constants_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging) return;
        Program.OptionsObject.SH_Constants = SH_Constants.GetColor();
    }
    private void Functions_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging) return;
        Program.OptionsObject.SH_Functions = SH_Functions.GetColor();
    }
    private void Methods_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging) return;
        Program.OptionsObject.SH_Methods = SH_Methods.GetColor();
    }
}