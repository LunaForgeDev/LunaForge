using LunaForge.Backend.Attributes;
using LunaForge.Services;

namespace LunaForge.Backend.Enums;

public enum BaseConfigEnum
{
    // ## GENERAL ## //
    [BaseConfig(ConfigSystemCategory.General, false)] SetupDone,
    [BaseConfig(ConfigSystemCategory.General, "")] ProjectsFolder,
    [BaseConfig(ConfigSystemCategory.General, 4)] CodeIndentSpaces, // Number of spaces for code indent.
    [BaseConfig(ConfigSystemCategory.General, "", true)] PinnedProjects,
    [BaseConfig(ConfigSystemCategory.General, "")] LuaSTGInstances, // List of LuaSTG installations separated by ';'
    [BaseConfig(ConfigSystemCategory.General, false)] CheckForUpdatesOnStartup,

    // ## SERVICES ## //
    [BaseConfig(ConfigSystemCategory.Services, false)] UseDiscordRPC,
    [BaseConfig(ConfigSystemCategory.Services, "https://lunaforge.rulholos.fr/marketplace")] TemplateServerUrl,

    // ## DEFAULT PROJECT ## //
    [BaseConfig(ConfigSystemCategory.DefaultProject, "", true)] OpenedFiles,
    [BaseConfig(ConfigSystemCategory.DefaultProject, "Untitled")] ProjectName,
    [BaseConfig(ConfigSystemCategory.DefaultProject, "John Dough")] ProjectAuthor,
    [BaseConfig(ConfigSystemCategory.DefaultProject, "", true)] ProjectFilesOpenedRecently,
    [BaseConfig(ConfigSystemCategory.DefaultProject, "")] LuaSTGExecutablePath,
    [BaseConfig(ConfigSystemCategory.DefaultProject, "")] CompilationTarget,
    [BaseConfig(ConfigSystemCategory.DefaultProject, false)] UsePlainFilesPackaging, // If false, compiler will create a zip instead of copying all files directly.
}
