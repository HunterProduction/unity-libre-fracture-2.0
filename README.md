# Libre Fracture 2.0
![librefracture2-dragon-fall](https://github.com/HunterProduction/unity-libre-fracture-2.0/wiki/librefracture2-dragon-fall.gif)

Hello dear readers! Welcome to **Libre Fracture 2.0**, a tool for **object destruction in Unity**.

As I was browsing the Internet in search of some open source Unity projects concerning this topic, I came across two repos, [Unity Fracture](https://github.com/ElasticSea/unity-fracture) and [Libre Fracture](https://github.com/U3DC/unity-libre-fracture). Both had some interesting implementations ideas, but were a bit outdated and poorly maintained. So I tried to put together what I think were the best aspects of the two.

After a lot of refactoring, code cleanup and polishing, I decided to share the tool with the community. I don't know how much time and effort I will be able to spend to this project in the future, but feel free to give feedback and let me know if you find it interesting and useful!

*- Mattia*

## Install package
Project has been developed on Unity 6, but it should properly work with older version of the editor too. 

This repository can be added to your project as a Unity package:

1. In the GitHub page, click the Code button and copy the repo url
2. In the Unity editor, open the **Package Manager** going to `Window/Package Manager` 
3. In the top left corner, click the **+** button and go to `Install package from git URL...`

> [!NOTE]
> This installation method does not support dependency resolution and package upgrading when a new version is released. So it's a good practice to manually check for updates in the Package Manager, and make sure that eventual dependencies have already been installed.

## Features

### Editor Tool
With Libre Fracture 2.0 you can create a **fractured tween** of a GameObject thanks to the dedicated Editor tool, located under the menu `Tools/Libre Fracture 2.0`. 

![librefracture2-editor-tool](https://github.com/HunterProduction/unity-libre-fracture-2.0/wiki/librefracture2-editor-tool.gif)

- **Visual preview** of the fractured GameObject, with customizable chunks distance
- Customizable **material** for the **mesh interior**
- Different fracture approaches
- Possibility to create the fractured instance in the scene, or to save it as a Prefab to be stored in the project

### Graph-based Runtime Chunk Management

The runtime simulation is modeled as a **graph** where each `ChunkNode` is connected with the nearby ones through a `FixedJoint`. 
Each `ChunkNode` controls its status in the graph independently. Possible chunk states are:
- **Connected** (Blue gizmo): Chunk is totally connected with all of its original neighbours
- **Broken** (Pink gizmo): At least one of the connections with the neighbour chunks has been broken
- **Detached** (Red gizmo): All of the connections with the neighbour chunks are broken. Chunk is totally detached from the object structure.
- **Anchored**[^1] (Yellow gizmo): Chunk is fixed, anchored to its default place. 

![librefracture2-graph](https://github.com/HunterProduction/unity-libre-fracture-2.0/wiki/librefracture2-graph.gif)

The `ChunkGraphManager` component is only responsible of setting up and initialize the whole chunks graph, avoiding frequent loops across the chunks collection.
Some of its main parameters that defines the structure's behaviour are:
- `anchored`: If true, chunks bordering other colliders nearby in the scene (e.g. the floor, a wall) are set to Anchored. This may be useful to achieve a result where the object is not totally free to move, and part of it has to remain attached to the environment nearby (e.g. a breakable wall/floor/surface)
- `freezeConnectedChunks`: If true, each chunk's rigidbody has its constraints set to frozen when still connected to the graph. This may reduce the "wobbling" of the fragmented object's structure, but may not preserve the accuracy of the collision's reaction.
- `startAsleep`: If true, the component at start disables all its chunks and enables the root mesh renderer and collider. When a first collision with the root collider happens, the chunks structure is re-enabled.

[^1]: A chunk may enter this state on startup only if `ChunkGraphManager`'s `anchored` toggle is enabled.

## Known Issues and Limitations 

There are certainly many. But I simply didn't have the time to check on all of them to provide a detailed list, or the relative roadmap.

So feel free to play with the tool knowing that it is far from perfect. If you want to notify anything, feel free to use the Issue page. I may not give assurance on the project long term maintainance, but since it is open source any kind of community feedback could be useful.

## Credits
Mattia Cacciatore - [mattiacacciatore.mc@gmail.com](mattiacacciatore.mc@gmail.com)

See [third party notices](THIRD%20PARTY%20NOTICES.md) for more.
