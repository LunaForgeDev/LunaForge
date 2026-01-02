using CommunityToolkit.Mvvm.ComponentModel;
using LunaForge.Models.Documents;
using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Models.ShaderEditor;

public abstract partial class ShaderNode : ObservableObject
{
    public DocumentFileLFS ParentDocument { get; set; }
}
