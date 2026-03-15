using CommunityToolkit.Mvvm.ComponentModel;
using LunaForge.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.ViewModels;

public partial class LuaSTGInstancesVM : ObservableObject
{
    /*
     * Idea: have a list of luaSTG instances,
     * 
     * For the libraries:
     * - A "stackable" field/flag. Is if false, it can't be installed if another library already been installed.
     *  If true, it can be installed on top of previous libraries (example: RyannLib for THlib)
     *  
     * Have a .lunaforge hidden file in each instance folder to store the libraries (and versions), and the engine branch/version.
     */

    public LuaSTGInstancesVM()
    {
        
    }
}
