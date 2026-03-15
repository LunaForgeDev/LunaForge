# LunaForge Editor

Blah blah blah Sharp-X replacement blah blah

## Alpha release roadmap
This is a list of features I want to have in the first alpha release of LunaForge. This list will change over time.

- [X] Main Interface
- [ ] LUA editor
- [X] LFD editor
- [ ] THlib support (including all nodes from Sharp-X)
- [ ] Cyn environment support
- [ ] Traces system (missing a lot of messages, and global filtering)
- [ ] LuaSTG Instances and Libraries manager (downloading, locating, ...)
- [ ] Editor and Project settings
- [X] Plugin system
- [ ] Selecting Compilation Target
- [ ] Launcher window
- [ ] As many crash handlers as possible
- [X] Drag and drop support inside LFD editor
- [ ] Base Project templates
- [ ] Base file templates
- [ ] Object Indexer (half-implemented)
- [X] AddNode helper window
- [ ] Presets (save and insert)
- [ ] Edit Windows

## Beta release roadmap
- [ ] Plugin store (in-editor and website)
- [ ] LFS (Shader) Editor
- [ ] UI identity
- [ ] Stream Deck integration

## Secondary Concerns
These features are low priority and may not be implemented in alpha or beta.

- [ ] Editor branches (release, pre-release, beta and alpha)
- [ ] Update checks and update system (half-implemented)
- [ ] Discord RPC

## Sidecar files
LunaForge, by default, saves all projects and configurations in the "Documents" folder. 

However, to manage LuaSTG Instances, it will save a ".lunaforge" file in the same folder as the LuaSTG executable.

This file contains infos sensitive for this instance's configuration in the editor. If it's removed, strange behaviours could occur.

## Library Compatibility
LunaForge comes out of the box with support for THlib and BerryLib and Cyn.

However, if you are a library creator and want to have compatibility with LunaForge, you must have these things that LunaForge expects:
* Your library can read both folder format and .zip format.
* Your library loads ONE mod at a time.
* Your library must target either LuaSTG Evo, Sub, Flux or X. ExPlus and below are not officially supported.
* 

The editor will use `require()` to load files internally.