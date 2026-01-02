using CommunityToolkit.Mvvm.ComponentModel;
using LunaForge.Models.Documents;
using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Models.ShaderEditor;


public abstract partial class ShaderNode : ObservableObject
{
    protected ShaderNode()
    {

    }

    public ShaderNode(DocumentFileLFS workspace)
        : this()
    {
        ParentDocument = workspace;
    }

    #region API

    public virtual IEnumerable<string> ToShader(int spacing)
    {
        return ToShader(spacing, null);
    }

    protected IEnumerable<string> ToShader(int spacing, object? temp)
    {
        yield return string.Empty;
    }

    #endregion
}
