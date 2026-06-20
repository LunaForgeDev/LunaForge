using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using LunaForge.Helpers;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace LunaForge.Controls;

public partial class LuaTextEditor : TextEditor
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

    private bool _isUpdatingText;

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
            if (editor._isUpdatingText)
                return;

            string newValue = e.NewValue as string ?? string.Empty;
            if (editor.Document.Text != newValue)
            {
                editor._isUpdatingText = true;
                editor.Document.Text = newValue;
                editor._isUpdatingText = false;
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
        Options.EnableHyperlinks = true;
        Options.EnableImeSupport = true;
        Options.ShowColumnRuler = true;
        Options.ShowSpaces = true;
        Options.ShowTabs = true;

        TextArea.IndentationStrategy = null;

        TextArea.TextEntering += OnTextEntering;
        TextArea.TextEntered += OnTextEntered;

        TextChanged += (sender, args) =>
        {
            if (_isUpdatingText)
                return;

            _isUpdatingText = true;
            Text = Document.Text;
            _isUpdatingText = false;
        };
    }

    private void OnTextEntering(object sender, TextCompositionEventArgs e)
    {
        if (e.Text.Length > 0 && char.IsControl(e.Text[0]))
            return;

        if (e.Text.Length == 1)
        {
            char typedChar = e.Text[0];

            if (!TextArea.Selection.IsEmpty && Array.IndexOf(OpeningBrackets, typedChar) >= 0)
            {
                WrapSelection(typedChar);
                e.Handled = true;
                return;
            }

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
        else
        {
            HandleKeywordUnindent();
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Key == Key.Back && !e.Handled)
        {
            HandleBackspace(e);
        }
    }

    private void WrapSelection(char openingChar)
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

        if (closingChar == '\0') return;

        var selection = TextArea.Selection;
        int start = selection.SurroundingSegment.Offset;
        int length = selection.SurroundingSegment.Length;
        string selectedText = Document.GetText(start, length);

        Document.Replace(start, length, openingChar + selectedText + closingChar);
        TextArea.Selection = Selection.Create(TextArea, start + 1, start + 1 + length);
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

    private void HandleBackspace(KeyEventArgs e)
    {
        int caretOffset = TextArea.Caret.Offset;
        if (caretOffset <= 0) return;

        if (caretOffset < Document.TextLength)
        {
            char prevChar = Document.GetCharAt(caretOffset - 1);
            char nextChar = Document.GetCharAt(caretOffset);

            int openIndex = Array.IndexOf(OpeningBrackets, prevChar);
            if (openIndex >= 0 && ClosingBrackets[openIndex] == nextChar)
            {
                Document.Remove(caretOffset - 1, 2);
                e.Handled = true;
                return;
            }
        }

        DocumentLine line = Document.GetLineByOffset(caretOffset);
        int lineOffset = caretOffset - line.Offset;
        if (lineOffset >= Options.IndentationSize)
        {
            string textBeforeCaret = Document.GetText(line.Offset, lineOffset);
            string targetIndentSpace = new(' ', Options.IndentationSize);

            if (textBeforeCaret.EndsWith(targetIndentSpace) && string.IsNullOrWhiteSpace(textBeforeCaret))
            {
                Document.Remove(caretOffset - Options.IndentationSize, Options.IndentationSize);
                e.Handled = true;
            }
        }
    }

    private void HandleNewLine()
    {
        int caretOffset = TextArea.Caret.Offset;
        if (caretOffset == 0)
            return;

        DocumentLine previousLine = Document.GetLineByOffset(caretOffset - 1);
        string lineText = Document.GetText(previousLine.Offset, previousLine.Length);
        string trimmedText = lineText.Trim();
        string indentation = GetIndentation(previousLine);

        foreach (string keyword in LuaKeywordsRequiringEnd)
        {
            if (ShouldInsertEnd(trimmedText, keyword))
            {
                if (IsEndAlreadyPresentForward(previousLine.LineNumber + 1))
                    break;

                string newIndentation = indentation + GetIndentString();
                string textToInsert = $"{newIndentation}\n{indentation}end";

                Document.Insert(caretOffset, textToInsert);
                TextArea.Caret.Offset = caretOffset + newIndentation.Length;
                return;
            }
        }

        if (trimmedText.EndsWith("then") ||
            trimmedText.EndsWith("do") ||
            trimmedText.EndsWith('{'))
        {
            indentation += GetIndentString();
        }

        if (!string.IsNullOrEmpty(indentation))
        {
            Document.Insert(caretOffset, indentation);
            TextArea.Caret.Offset = caretOffset + indentation.Length;
        }
    }

    private void HandleKeywordUnindent()
    {
        int caretOffset = TextArea.Caret.Offset;
        DocumentLine currentLine = Document.GetLineByOffset(caretOffset);
        string lineText = Document.GetText(currentLine.Offset, caretOffset - currentLine.Offset);
        string trimmed = lineText.Trim();

        if (trimmed == "end" || trimmed == "else" || trimmed == "elseif")
        {
            string indent = GetIndentation(currentLine);
            int indentSize = Options.IndentationSize;

            if (indent.Length >= indentSize)
            {
                Document.Remove(currentLine.Offset, indentSize);
            }
        }
    }

    private static bool ShouldInsertEnd(string lineText, string keyword)
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
            string afterKeyword = lineText[endIndex..].Trim();
            if (afterKeyword.Length > 0 && !afterKeyword.StartsWith("--"))
                return false;
        }

        if (keyword == "if" && lineText.Contains("then"))
        {
            int thenIndex = lineText.IndexOf("then", StringComparison.Ordinal);
            string afterThen = lineText[(thenIndex + 4)..].Trim();
            if (!string.IsNullOrEmpty(afterThen) && !afterThen.StartsWith("--"))
                return false;
        }

        return true;
    }

    private bool IsEndAlreadyPresentForward(int startLineNumber)
    {
        int openBlocksExpected = 1;

        for (int i = startLineNumber; i <= Document.LineCount; i++)
        {
            var line = Document.GetLineByNumber(i);
            string text = Document.GetText(line.Offset, line.Length).Trim();

            if (text.StartsWith("--")) continue;

            int commentIdx = text.IndexOf("--", StringComparison.Ordinal);
            if (commentIdx >= 0) text = text[..commentIdx];

            string[] tokens = ContextTokens().Split(text);
            bool containsWhileOrFor = tokens.Contains("while") || tokens.Contains("for");

            foreach (string token in tokens)
            {
                if (token is "function" or "if" or "while" or "for")
                {
                    openBlocksExpected++;
                }
                else if (token == "do" && !containsWhileOrFor)
                {
                    openBlocksExpected++;
                }
                else if (token == "end")
                {
                    openBlocksExpected--;
                }
            }

            if (openBlocksExpected <= 0)
            {
                return true;
            }
        }

        return false;
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

        return lineText[..indentLength];
    }

    private string GetIndentString()
    {
        return new string(' ', Options.IndentationSize);
    }

    [GeneratedRegex(@"\W+")]
    private static partial Regex ContextTokens();
}