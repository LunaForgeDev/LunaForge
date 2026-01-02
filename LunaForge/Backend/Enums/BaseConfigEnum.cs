using LunaForge.Backend.Attributes;
using LunaForge.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunaForge.Backend.Enums;

public enum BaseConfigEnum
{
    // ## GENERAL ## //
    [BaseConfig(ConfigSystemCategory.General, false)] SetupDone,
    [BaseConfig(ConfigSystemCategory.General, "")] ProjectsFolder,
    [BaseConfig(ConfigSystemCategory.General, 4)] CodeIndentSpaces, // Number of spaces for code indent.
    [BaseConfig(ConfigSystemCategory.General, "", true)] PinnedProjects,

    // ## SERVICES ## //
    [BaseConfig(ConfigSystemCategory.Services, false)] UseDiscordRPC,
    [BaseConfig(ConfigSystemCategory.Services, "https://lunaforge.rulholos.fr/")] TemplateServerUrl,

    // ## DEFAULT PROJECT ## //
    [BaseConfig(ConfigSystemCategory.DefaultProject, "", true)] OpenedFiles,
    [BaseConfig(ConfigSystemCategory.DefaultProject, "Untitled")] ProjectName,
    [BaseConfig(ConfigSystemCategory.DefaultProject, "John Dough")] ProjectAuthor,
}
