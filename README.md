## Alpha release roadmap

This is a list of features I want to have in the first alpha release of LunaForge. This list will change over time.

- [X] Main Interface
- [ ] LUA editor
- [X] LFD editor
- [ ] THlib support (including all nodes from Sharp-X)
- [ ] BerryLib/Cyn support
- [ ] Traces system
- [ ] LuaSTG Instances and Libraryes manager (downloading, locating, ...)
- [ ] Editor and Project settings
- [X] Plugin system
- [ ] Selecting Compilation Target
- [ ] Launcher window
- [ ] As many crash handlers as possible

## Beta release roadmap
- [ ] Plugin store
- [ ] Shader Editor
- [ ] UI identity

### Secondary Concerns
- [ ] Editor branches (release, pre-release, beta and alpha)
- [ ] Update checks and update system

## Library Compatibility

LunaForge comes out of the box with support for THlib and BerryLib/Cyn.

However, if you are a library creator and want to have compatibility with LunaForge, you must have these things that LunaForge expects:
* Your library can read both folder format and .zip format.
* Your library loads ONE mod at a time.
* Your library must target either LuaSTG Evo, Sub, Flux or X. ExPlus and below are not officially supported.
* 

The editor will use `require()` to load files internally.