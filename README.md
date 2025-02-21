# Libre Fracture 2.0
![librefracture2-dragon-fall](https://github.com/HunterProduction/unity-libre-fracture-2.0/wiki/librefracture2-dragon-fall.gif)

Hello dear readers! Welcome to **Libre Fracture 2.0**, a tool for **object destruction in Unity**.

As I was browsing the Internet in search of some open source Unity projects concerning this topic, I came across two repos, [Unity Fracture](https://github.com/ElasticSea/unity-fracture) and [Libre Fracture](https://github.com/U3DC/unity-libre-fracture). Both had some interesting implementations ideas, but were a bit outdated and poorly maintained. So I tried to put together what I think were the best aspects of the two.

After a lot of refactoring, code cleanup and polishing, I decided to share the tool with the community. I don't know how much time and effort I will be able to spend to this project in the future, but feel free to give feedback and let me know if you find it interesting and useful!

*- Mattia*

## Install package

Please note that this method does not support dependency resolution and package upgrading when a new version is released. So it's a good practice to manually check for updates in the Package Manager.

## Features

### Editor Tool
It is possible to create a fractured tween of a GameObject thanks to the dedicated Editor tool, located under the menu `Tools/Libre Fracture 2.0`. 

![librefracture2-editor-tool](https://github.com/HunterProduction/unity-libre-fracture-2.0/wiki/librefracture2-editor-tool.gif)

- Visual preview of the fractured GameObject, with customizable chunks distance
- Customizable material for the mesh interior
- Different fracture approaches
- Possibility to create the fractured instance in the scene, or to save it as a Prefab to be stored in the project

### Graph-based Runtime Chunk Management

![librefracture2-graph](https://github.com/HunterProduction/unity-libre-fracture-2.0/wiki/librefracture2-graph.gif)


## Credits
Mattia Cacciatore - [mattiacacciatore.mc@gmail.com](mattiacacciatore.mc@gmail.com)

See [third party notices](THIRD%20PARTY%20NOTICES.md) for more.
