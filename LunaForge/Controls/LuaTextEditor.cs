using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using LunaForge.Helpers;
using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace LunaForge.Controls;

public class LuaTextEditor : TextEditor
{
    private static readonly char[] OpeningBrackets = ['(', '[', '{', '"', '\''];
    private static readonly char[] ClosingBrackets = [')', ']', '}', '"', '\''];
    
    private static readonly string[] LuaKeywordsRequiringEnd = 
    [
        "function",
        "if",
        "for",
        "while",
        "repeat",
        "do"
    ];

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(LuaTextEditor),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnTextPropertyChanged,
                null,
                false,
                UpdateSourceTrigger.PropertyChanged));

    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LuaTextEditor editor)
        {
            string newValue = e.NewValue as string ?? string.Empty;
            
            if (editor.Document.Text != newValue)
            {
                editor.Document.Text = newValue;
            }
        }
    }

    public new string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public LuaTextEditor()
    {
        SyntaxHighlighting = SyntaxHighlightingHelper.GetLuaHighlighting();
        
        Options.ConvertTabsToSpaces = true;
        Options.IndentationSize = 4;
        Options.EnableRectangularSelection = true;
        Options.EnableTextDragDrop = true;
        Options.ShowBoxForControlCharacters = true;
        Options.HighlightCurrentLine = true;
        
        TextArea.TextEntering += OnTextEntering;
        TextArea.TextEntered += OnTextEntered;
        
        TextChanged += (sender, args) =>
        {
            if (GetValue(TextProperty) as string != Document.Text)
            {
                SetValue(TextProperty, Document.Text);
            }
        };
    }

    private void OnTextEntering(object sender, TextCompositionEventArgs e)
    {
        if (e.Text.Length > 0 && char.IsControl(e.Text[0]))
            return;

        if (e.Text.Length == 1)
        {
            char typedChar = e.Text[0];
            
            if (Array.IndexOf(ClosingBrackets, typedChar) >= 0)
            {
                int caretOffset = TextArea.Caret.Offset;
                if (caretOffset < Document.TextLength && Document.GetCharAt(caretOffset) == typedChar)
                {
                    TextArea.Caret.Offset++;
                    e.Handled = true;
                }
            }
        }
    }

    private void OnTextEntered(object sender, TextCompositionEventArgs e)
    {
        if (e.Text.Length == 0)
            return;

        char typedChar = e.Text[0];
        
        if (Array.IndexOf(OpeningBrackets, typedChar) >= 0)
        {
            InsertClosingBracket(typedChar);
        }
        else if (typedChar == '\n' || typedChar == '\r')
        {
            HandleNewLine();
        }
    }

    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Key == Key.Back && !e.Handled)
        {
            HandleBackspace();
        }
    }

    private void InsertClosingBracket(char openingChar)
    {
        char closingChar = openingChar switch
        {
            '(' => ')',
            '[' => ']',
            '{' => '}',
            '"' => '"',
            '\'' => '\'',
            _ => '\0'
        };

        if (closingChar == '\0')
            return;

        if (openingChar == '"' || openingChar == '\'')
        {
            int caretOffset = TextArea.Caret.Offset;
            if (caretOffset < Document.TextLength)
            {
                char nextChar = Document.GetCharAt(caretOffset);
                if (nextChar == openingChar)
                    return;
            }
        }

        int offset = TextArea.Caret.Offset;
        Document.Insert(offset, closingChar.ToString());
        TextArea.Caret.Offset = offset;
    }

    private void HandleBackspace()
    {
        int caretOffset = TextArea.Caret.Offset;
        
        if (caretOffset > 0 && caretOffset < Document.TextLength)
        {
            char prevChar = Document.GetCharAt(caretOffset - 1);
            char nextChar = Document.GetCharAt(caretOffset);
            
            int openIndex = Array.IndexOf(OpeningBrackets, prevChar);
            if (openIndex >= 0 && ClosingBrackets[openIndex] == nextChar)
            {
                Document.Remove(caretOffset, 1);
            }
        }
    }

    private void HandleNewLine()
    {
        int caretOffset = TextArea.Caret.Offset;
        if (caretOffset == 0)
            return;

        DocumentLine currentLine = Document.GetLineByOffset(caretOffset - 1);
        string lineText = Document.GetText(currentLine.Offset, currentLine.Length).Trim();

        foreach (string keyword in LuaKeywordsRequiringEnd)
        {
            if (ShouldInsertEnd(lineText, keyword))
            {
                string indentation = GetIndentation(currentLine);
                string newIndentation = indentation + GetIndentString();
                
                string textToInsert = $"{newIndentation}\n{indentation}end";
                Document.Insert(caretOffset, textToInsert);
                
                TextArea.Caret.Offset = caretOffset + newIndentation.Length;
                return;
            }
        }

        DocumentLine previousLine = Document.GetLineByOffset(caretOffset - 1);
        string prevLineText = Document.GetText(previousLine.Offset, previousLine.Length);
        string prevIndentation = GetIndentation(previousLine);
        
        if (prevLineText.TrimEnd().EndsWith("then") || 
            prevLineText.TrimEnd().EndsWith("do") ||
            prevLineText.TrimEnd().EndsWith("{"))
        {
            prevIndentation += GetIndentString();
        }

        if (!string.IsNullOrEmpty(prevIndentation))
        {
            Document.Insert(caretOffset, prevIndentation);
            TextArea.Caret.Offset = caretOffset + prevIndentation.Length;
        }
    }

    private bool ShouldInsertEnd(string lineText, string keyword)
    {
        int keywordIndex = lineText.IndexOf(keyword, StringComparison.Ordinal);
        if (keywordIndex < 0)
            return false;

        if (keywordIndex > 0 && char.IsLetterOrDigit(lineText[keywordIndex - 1]))
            return false;
        
        int endIndex = keywordIndex + keyword.Length;
        if (endIndex < lineText.Length && char.IsLetterOrDigit(lineText[endIndex]))
            return false;

        if (keyword == "do")
        {
            string afterKeyword = lineText.Substring(endIndex).Trim();
            if (afterKeyword.Length > 0 && !afterKeyword.StartsWith("--"))
                return false;
        }

        if (keyword == "if" && lineText.Contains("then"))
        {
            int thenIndex = lineText.IndexOf("then", StringComparison.Ordinal);
            string afterThen = lineText.Substring(thenIndex + 4).Trim();
            if (!string.IsNullOrEmpty(afterThen) && !afterThen.StartsWith("--"))
                return false;
        }

        return true;
    }

    private string GetIndentation(DocumentLine line)
    {
        string lineText = Document.GetText(line.Offset, line.Length);
        int indentLength = 0;
        
        foreach (char c in lineText)
        {
            if (c == ' ' || c == '\t')
                indentLength++;
            else
                break;
        }
        
        return lineText.Substring(0, indentLength);
    }

    private string GetIndentString()
    {
        return new string(' ', Options.IndentationSize);
    }
}
