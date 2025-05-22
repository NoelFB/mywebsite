{
	"title": "Making Video Games in 2025 (without an engine)",
	"date": "May 18, 2025",
	"description": "Thoughts, tools, and libraries I use to make games",
	"postcard_visible": false
}
---

It's 2025 and I am still making video games, which [according to archive.org](https://web.archive.org/web/20110902045531/http://noelberry.ca/) is 20 years since I started making games! That's a pretty long time to be doing one thing...

![Screenshot of my website circa 2011]({{path}}/2005.jpeg)
*<center>Screenshot of my website circa 2011</center>*

When I share stuff I'm working on, people frequently ask how I make games and are often surprised (and sometimes concerned?) when I tell them I don't use commercial game engines. There's an assumption around making games without a big tool like Unity or Unreal that you're out there hand writing your own assembly instruction by instruction.

I genuinely believe making games without a big "do everything" engine can be easier, more fun, and often less overhead. I am not making a "do everything" game and I do not need 90% of the features these engines provide. I am very particular about how my games feel and look, and how I interact with my tools. I often find the default feature implementations in large engines like Unity so lacking I end up writing my own anyway. Eventually, my projects end up being mostly my own tools and systems, and the engine becomes just a vehicle for a nice UI and some rendering...

At which point, why am I using this engine? What is it providing me? Why am I letting a tool potentially destroy my ability to work when they suddenly make [unethical and terrible business decisions](https://www.theverge.com/2023/9/12/23870547/unit-price-change-game-development)? Or push out an update that they require to run my game on consoles, that also happens to break an entire system in my game, forcing me to rewrite it? Why am I fighting this thing daily for what essentially becomes a glorified asset loader and editor UI framework, by the time I'm done working around their default systems?

The obvious answer for me is to just not use big game engines, and write my own small tools for my specific use cases. It's more fun, and I like controlling my development stack. I know when something goes wrong I can find the problem and address it, instead of submitting a bug report and 3 months later hearing back it "won't be fixed". I like knowing that in another two decades from now I will still be able to compile my game without needing to pirate an ancient version of a broken game engine.

Obviously this is my personal preference - and it's one of someone who has been making indie games for a long time. I used engines like Game Maker for years before transitioning to more lightweight and custom workflows. I also work in very small teams, where it's easy to make one-off tools for team members. But I want to push back that making games "from scratch" is some big impossible task - especially in 2025 with the state of open source frameworks and libraries. A [lot of](https://github.com/TerryCavanagh/VVVVVV) [popular](https://gamefromscratch.com/balatro-made-with-love-love2d-that-is/) [indie](https://store.steampowered.com/app/813230/ANIMAL_WELL/) [games](https://www.stardewvalley.net/) [are made](https://store.steampowered.com/app/504230/Celeste/) [in small](https://github.com/flibitijibibo/RogueLegacy1) frameworks like FNA, Love2D, or SDL. Making games "without an engine" doesn't literally mean opening a plain text editor and writing system calls (unless you want to). Often, the overhead of learning how to implement these systems yourself is just as time consuming as learning the proprietary workflows of the engine itself.

With that all said, I think it'd be fun to talk about my workflow, and what I actually use to make games.

## Programming Languages

Most of my career I've worked in C#, and aside from a [short stint in C++](https://github.com/noelfb/blah) a few years ago, I've settled back into a modern C# workflow.

I think sometimes when I mention C# to non-indie game devs their minds jump to what it looked like circa 2003 - a closed source, interpreted, verbose, garbage collected language, and... the language has *greatly* improved since then. The C# of 2025 is vastly different from the C# of even 2015, and many of those changes are geared towards the performance and syntax of the language. You can allocate dynamically sized arrays on the stack! `C++` can't do that (*although `C99` can ;) ...*).

The dotnet developers have also implemented hot reload in C# (which works... *most of the time*), and it's pretty fantastic for game development. You can launch your project with `dotnet watch` and it will live-update code changes, which is amazing when you want to change how something draws or the way an enemy updates.

C# also ends up being a great middle-ground between running things fast (which you need for video games) and easy to work with on a day-to-day basis. For example, I have been working on [City of None](https://cityofnone.com) with my brother [Liam](https://liamberry.ca), who had done very little coding when we started the project. But over the last year he's slowly picked up the language to the point where he's programming entire boss fights by himself, because C# is just that accessible - and fairly foot-gun free. For small teams where everyone wears many hats, it's a really nice language.

<img src="{{path}}/bossfight2.gif" alt="A boss fight that Liam coded" width="90%" style="image-rendering: pixelated;"/>
<p><center><i>A boss fight that Liam coded</i></center></p>

And finally, it has built in reflection... And while I wouldn't use it for release code, being able to quickly reflect on game objects for editor tooling is very nice. I can easily make live-inspection tools that show me the state of game objects without needing any custom meta programming or in-game reflection data. After spending a few years making games in C++ I really like having this back.

![Inspecting an object with reflection in Dear ImGui]({{path}}/reflection.jpeg)
*<center>Inspecting an object with reflection in Dear ImGui</center>*


## Windows... Input... Rendering... Audio?

This is kind of the big question when writing "a game from scratch", but there are a lot of great libraries to help you get stuff onto the screen - from [SDL](https://www.libsdl.org/), to [GLFW](https://www.glfw.org/), to [Love2D](https://www.love2d.org/), to [Raylib](https://www.raylib.com/), etc.

I have been using [SDL3](https://wiki.libsdl.org/SDL3/FrontPage) as it does everything I need as a cross-platform abstraction over the system - from windowing, to game controllers, to rendering. It works on Linux, Windows, Mac, Switch, PS4/5, Xbox, etc, and as of SDL3 there is a [GPU abstraction](https://wiki.libsdl.org/SDL3/CategoryGPU) that handles rendering across DirectX, Vulkan, and Metal. It just _works_, is open source, and is used by a lot of the industry (ex. Valve). I started using it because [FNA](https://fna-xna.github.io/), which Celeste uses to run on non-Windows platforms, uses it as its platform abstraction.

That said, I have written [my own C# layer](https://github.com/FosterFramework/Foster) on top of SDL for general rendering and input utilities I share across projects. I make highly opinionated choices about how I structure my games so I like having this little layer to interface with. It works really well for my needs, but there are full-featured alternatives like [MoonWorks](https://github.com/MoonsideGames/MoonWorks) that fill a similar space.

Before SDL3's release with the GPU abstraction, I was writing my own OpenGL and DirectX implementations - which isn't trivial! But it was a [great learning experience](https://learnopengl.com/), and not as bad as I expected it to be. I am however, very grateful for SDL GPU as it is a very solid foundation that will be tested across millions of devices.

Finally, for Audio we're using [FMOD](https://www.fmod.com/). This is the last proprietary tool in our workflow, which I don't love (especially [when something stops working](https://www.reddit.com/r/linux_gaming/comments/1ijcfnt/celeste_not_finding_libfmodstudioso10/) and you have to hand-patch their library), but it's the best tool for the job. There are more lightweight open source libraries if you just want to play sounds, but I work with audio teams that want finite control over dynamic audio, and a tool like FMOD is a requirement.

## Assets

I don't have much to say about assets, because when you're rolling your own engine you just load up what files you want, when you need them, and move on. For all my pixel art games, I load the whole game up front and it's "fine" because the entire game is like 20mb. When I was working on [Earthblade](https://exok.com/games/earthblade/), which had larger assets, we would register them at startup and then only load them on request, disposing them after scene transitions. We just went with the most dead-simple implementation that accomplished the job.

![Assets loading in 0.4 seconds]({{path}}/assets.jpeg)
*<center>All the assets for City of None loading in 0.4 seconds</center>*

Sometimes you'll have assets that need to be converted before the game uses them, in which case I usually write a small script that runs when the game compiles that does any processing required. That's it.

## Level Editors, UI...

Some day I'll write a fully procedural game, but until then I need tools to design the in-game spaces. There are a lot of really great existing tools out there, like [LDtk](https://ldtk.io/), [Tiled](https://www.mapeditor.org/), [Trenchbroom](https://trenchbroom.github.io/), and so on. I have used many of these to varying degrees and they're easy to set up and get running in your project - you just need to write a script to take the data they output and instantiate your game objects at runtime.

However, I usually like to write my own custom level editors for my projects. I like to have my game data tie directly into the editor, and I never go that deep on features because the things we need are specific but limited.

![A small custom level editor for City of None using Dear ImGui]({{path}}/postcard.png)
*<center>A small custom level editor for City of None using Dear ImGui</center>*

But I don't want to write the actual UI - coding textboxes and dropdowns isn't something I'm super keen on. I want a simple way to create fields and buttons, kind of like when [you write your own small editor utilities](https://docs.unity3d.com/6000.0/Documentation/Manual/editor-CustomEditors.html) in the Unity game engine.

This is where [Dear ImGui](https://github.com/ocornut/imgui/) comes in. It's a lightweight, cross-platform, immediate-mode GUI engine that you can easily drop in to any project. The editor screenshot above uses it for everything with the exception of the actual "scene" view, which is custom as it's just drawing my level. There are more full-featured (and heavy-duty) alternatives, but if it's good enough for [all these games](https://github.com/ocornut/imgui/wiki/Software-using-dear-imgui) including [Tears of the Kingdom](https://github.com/ocornut/imgui/issues/7503#issuecomment-2308380962) it's good enough for me.

Using ImGui makes writing editor tools extremely simple. I like having my tools pull data directly from my game, and using ImGui along with C# reflection makes that very convenient. I can loop over all the Actor classes in C# and have them accessible in my editor with a few lines of code! For more complicated tools it's sometimes overkill to write my own implementation, which is where I fall back to using existing tools built for specific jobs (like [Trenchbroom](https://trenchbroom.github.io/), for designing 3D environments).

## Porting Games ... ?

The main reason I learned C++ a few years ago was because of my concerns with portability. At the time, it was not trivial to run C# code on consoles because C# was "just in time" compiled, which isn't something many platforms allow. Our game, Celeste, used a tool called [BRUTE](http://brute.rocks/) to transpile the C# [IL](https://en.wikipedia.org/wiki/Common_Intermediate_Language) (intermediate language binaries) to C++, and then recompiled that for the target platform. Unity [has a very similar tool](https://docs.unity3d.com/6000.0/Documentation/Manual/scripting-backends-il2cpp.html) that does the same thing. This worked, but was not ideal for me. I wanted to be able to just compile our code for the target platform, and so learning C++ felt like the only real option.

Since then, however, C# has made incredible progress with their [Native-AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) toolchain (which basically just means all the code is compiled "ahead of time" - what languages like C++ and Rust do by default). It is now possible to compile C# code for all the major console architectures, which is amazing. The [FNA project](https://fna-xna.github.io/docs/appendix/Appendix-B%3A-FNA-on-Consoles/) has been extremely proactive with this, leading to the release of games across all major platforms, while using C#.

![Platforms supported by SDL3]({{path}}/platforms.jpeg)
*<center>Platforms supported by SDL3</center>*

And finally, SDL3 has console ports for all the major platforms. Using it as your platform abstraction layer (as long as you're careful about how you handle system calls) means a lot of it will "just work".

## Goodbye, Windows

Finally, to wrap all this up ... I no longer use Windows to develop my games (aside from testing). I feel like this is in line with my general philosophy around using open source, cross-platform tools and libraries. I have found Windows [increasingly frustrating to work](https://www.howtogeek.com/739837/fyi-windows-11-home-will-require-a-microsoft-account-for-initial-setup/) with, their [business practices gross](https://bdsmovement.net/microsoft), and their OS generally lacking. I grew up using Windows, but I switched to Linux full time around 3 years ago. And frankly, for programming video games, I have not missed it at all. It just doesn't offer me anything I can't do faster and more elegantly than on Linux.

There are of course certain workflows and tools that do not work on Linux, and that is just the current reality. I'm not entirely free of Microsoft either - I use vscode, I write my games in C#, and I host my projects on github... But the more people use Linux daily, the more pressure there is to support it, and the more support there is for open source alternatives.

(as a fun aside, I play most of my games on my steam deck these days, which means between my PC, game console, web server, and phone, I am always on a Linux platform)

## Miscellaneous thoughts

- **What about Godot?**<br/>If you're in the position to want the things a larger game engine provides, I definitely think [Godot](https://godotengine.org/) is the best option. That it is open-source and community-maintained eliminates a lot of the issues I have with other proprietary game engines, but it still isn't usually the way I want to make games. I do intend to play around with it in the future for some specific ideas I have.
- **What about 3D?**<br/>I think that using big engines definitely has more of a place for 3D games - but even so for any kind of 3D project I want to do, I would roll my own little framework. I want to make highly stylized games that do not require very modern tech, and I have found that to be fairly straight forward (for example, we made [Celeste 64](https://github.com/exok/celeste64) without very much prior 3D knowledge in under 2 weeks).

	![Celeste 64 screenshot]({{path}}/celeste64.jpeg)
	*<center>Celeste 64 Screenshot</center>*

- **I need only the best fancy tech to pull off my game idea**<br/>Then use Unreal! There's nothing wrong with that, but my projects don't require those kinds of features (and I would argue most of the things I do need can usually be learned fairly quickly).
- **My whole team knows [Game Engine XYZ]**<br/>The cost of migrating a whole team to a custom thing can be expensive and time consuming. I'm definitely talking about this from the perspective of smaller / solo teams. But that said, speaking from experience, I know several middle-sized studios moving to custom engines because they have determined the potential risk of using proprietary engines to be too high, and the migration and learning costs to be worth it. I think using custom stuff for larger teams is easier now than it has been in a long time.
- **Game-specific workflows**

	![Screeshot of Aseprite]({{path}}/aseprite.jpeg)
	*<center>Aseprite assets are loaded in automatically</center>*
	
	I load in [Aseprite](https://www.aseprite.org/) files and have my City of None engine automatically turn them into game animations, using their built in tags and frame timings. The format is [surprisingly straight forward](https://github.com/aseprite/aseprite/blob/main/docs/ase-file-specs.md). When you write your own tools it's really easy to add things like this!

## Alright!

That's it from me! That's how I make games in 2025!

Do I think you should make games without a big engine? My answer is: If it sounds fun.

-Noel
