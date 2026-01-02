using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Helpers;

public static class EditorSerializer
{
    public static readonly JsonSerializerSettings TreeNodeSettings =
        new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
        };

    public static string SerializeTreeNode(object o)
    {
        return JsonConvert.SerializeObject(o, typeof(TreeNode), TreeNodeSettings);
    }

    public static object DeserializeTreeNode(string s)
    {
        return JsonConvert.DeserializeObject(s, typeof(TreeNode), TreeNodeSettings);
    }
}