using System;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace LunaForge.Controls.LuaEditor;

public class LuaCompletionData : ICompletionData
{
    private readonly LuaApiItem apiItem;

    public LuaCompletionData(LuaApiItem apiItem)
    {
        this.apiItem = apiItem;
        Text = apiItem.Name;
        Content = apiItem.Name;
    }

    public ImageSource Image => null;
    public string Text { get; private set; }
    public object Content { get; private set; }
    public double Priority => 0;

    public object Description
    {
        get
        {
            var textBlock = new TextBlock
            {
                TextWrapping = System.Windows.TextWrapping.Wrap,
                MaxWidth = 400
            };

            textBlock.Inlines.Add(new Run(apiItem.Usage ?? apiItem.Name)
            {
                FontStyle = System.Windows.FontStyles.Italic,
                FontWeight = System.Windows.FontWeights.Bold
            });
            textBlock.Inlines.Add(new LineBreak());

            textBlock.Inlines.Add(new Run(apiItem.Description));

            if (apiItem.Parameters != null && apiItem.Parameters.Count > 0)
            {
                textBlock.Inlines.Add(new LineBreak());
                textBlock.Inlines.Add(new LineBreak());
                textBlock.Inlines.Add(new Run("Arguments:") { FontWeight = System.Windows.FontWeights.Bold });

                foreach (var param in apiItem.Parameters)
                {
                    textBlock.Inlines.Add(new LineBreak());
                    textBlock.Inlines.Add(new Run($" - {param.Name} ({param.Type}): ") { FontWeight = System.Windows.FontWeights.SemiBold });
                    textBlock.Inlines.Add(new Run(param.Description));
                }
            }

            return textBlock;
        }
    }

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, this.Text);
    }
}
