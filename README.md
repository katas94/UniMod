# UniMod
A Unity package that adds modding support to your project.
* Mods can contain **imported assets** and **scripts**.
* **Easy mod distribution and installation**: mods are built into a single `.umod` archive file that the users can drag and drop into a local installation folder. You can also extend UniMod to fetch mods from a server so users don't need to leave the application.
* Seamless **in-editor development** experience for modders:
  - Develop your mods **inside the original project** with all the benefits of the **Play Mode** fast iteration.
  - OR Develop your mods in a **separate Unity project** with the required assets provided by the moddable application developers (precompiled, as a Unity package or through the Unity Package Manager system).
* **Debugging support**: you can perform debug mod builds and debug them in the editor or a player build.
* **Easy integration** with your project: just add the `UniModContextInitializer` component into any GameObject and setup your configuration (i.e.: allowing mods containing script assemblies).
* **Customizable mod builder** with fast iteration features:
  - Can reuse the editor's precompiled assemblies to skip compilation when doing development iteration.
  - Remembers your last build output location and configuration for one-click rebuilds.
  - Configurable development builds that directly updates a mod installation. You can choose to rebuild scripts or assets separately.
* **Dependency resolution system** that uses [Semantic Versioning 2.0.0](https://semver.org/) to check mod compatibility and allow mods to declare dependencies on other mods by specifying their IDs and target version. It also ensures that mods are loaded in the correct order.
* **Highly customizable**: the core of the package uses public interfaces that you can implement to adapt anything to your needs.

# Platform Support
**Requires Unity 2021.3 or above**

For mods without script assemblies, UniMod should work on any platfrom supported by the Unity Addressables system.

For mods containing script assemblies, UniMod can support any platform using the Mono scripting backend, although only **standalone** and **Android** platforms are currently implemented. 

If you are using a Mono compatible platform not implemented in UniMod, you can extend the `CustomAssemblyBuilder` to specify how to build the managed scripting assemblies.

# Getting Started

Do the following steps whether you are enabling mod support in your project or you are creating a mod:

1. UniMod uses UniTask (I've not the author of UniTask) for all async operations, so you will need to [install it first](https://github.com/Cysharp/UniTask#install-via-git-url).
2. Install UniMod by using the the following Git URL in the Package Manager (just like you did for UniTask):
```
https://github.com/katas94/UniMod.git
```
3. Checkout the [UniMod's documentation](Documentation~/UniMod.md)

# State Of Development
I consider the initial development of this package done but I will start using it on a private project that will put it to the test. It could receive meaningful changes until I consider to release it as 1.0.0. The package may not be stable yet so use it at your own risk.