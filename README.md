# UniMod
A Unity package that adds modding support to your project.
* **No content restrictions**. Mods can contain **assets** and **scripts**.
* **Easy mod distribution and installation**. Mods are built into a single `.umod` archive file that users can drag and drop into a folder under the Unity's persistent data path.
* **Easy setup**. All you need is adding the `UniModContextInitializer` component into any GameObject and your project will be ready to load mods. The GameObject doesn't need to be persisted once awaken.
* **Script debugging support**.
* **Seamless in-editor development** experience for modders:
  - Develop your mods **inside the original project** with all the benefits of the **Play Mode** fast iteration (supports multiple mods at the same time).
  - OR Develop your mods in a **separate Unity project**.
* **Customizable mod builder** with fast iteration features:
  - Can reuse the editor's precompiled assemblies to skip compilation when developing in a separated project.
  - Remembers your last build output location and configuration for one-click rebuilds.
  - Configurable development builds that directly updates a mod installation. You can choose to rebuild scripts or assets separately.
* **Dependency resolution system** that uses [Semantic Versioning 2.0.0](https://semver.org/):
  - Mods can be standalone or target a specific application ID and version.
  - Mods can declare dependencies on other mods specifying ID and target version.
  - Mods are loaded in the correct order based on their dependency graph.
* **Flexible and customizable**:
  - You can use the `UniModContextInitializer` component or easely initialize the UniMod context from code with higher control.
  - You can disable scripting in mods if you want modders to add content only.
  - You don't need to make your project public to enable modding. Your modding API can be provided in many ways (as a `.unitypackage`, through the Package Manager...). These assemblies will not be included in the mod builds.
  - **Editor API** that allows you to automate mod builds and some other utilities.

# Platform Support
**Requires Unity 2021.3 or above**

For mods without script assemblies UniMod should work on any platfrom supported by the Unity Addressables system.

For mods containing script assemblies UniMod can support any platform using the Mono scripting backend, although only the following platforms are currently implemented:
* **All standalone platforms** : Windows, OSX and Linux
* **Android**

If you are using a Mono compatible platform not implemented in UniMod, you can extend the `CustomAssemblyBuilder` to specify how to build the managed scripting assemblies.

# Getting Started

Follow the next steps whether you are enabling mod support in your project or you are creating a mod:

1. UniMod uses UniTask for all async operations, so you will need to [install it first](https://github.com/Cysharp/UniTask#install-via-git-url). *Note: I'm not the author of UniTask*.
2. Install UniMod by using the the following Git URL in the Package Manager (just like you did for UniTask):
```
https://github.com/katas94/UniMod.git
```
3. Checkout the [UniMod's documentation](Documentation~/UniMod.md)

# State Of Development
I consider the initial development of this package done but I will start using it on a private project that will put it to the test. It could receive meaningful changes until I consider to release it as 1.0.0, although I find this unlickely since I'm very satisfied with the current design. Most likely I will be fixing bugs.

The package may not be stable yet so use it at your own risk.