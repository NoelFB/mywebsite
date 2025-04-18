{
	"title": "Remaking Celeste's Lighting",
	"date": "Sep 25, 2017",
	"description": "A technical breakdown on the lighting in Celeste",
	"mastodon_account": "https://mastodon.social/@noelfb",
	"mastodon_post": "114361360192032882"
}
---
A large part of [Celeste](http://www.celestegame.com/) is its mood and atmosphere. We’re trying very hard to make it feel like you’re exploring (and overcoming) a Mountain, and so lighting plays an important role.

We decided early on the lighting had to interact with the environment. As the game is about maneuvering and climbing through difficult terrain, having simple “Spotlights” wouldn’t cut it — the lights needed to actually hit walls.

![Lighting Comparison]({{path}}/01.png)
<center><i>Left: Lights that collide with the terrain, Right: “Spotlights” that have no ineraction</i></center>

## MESH IMPLEMENTATION
My first implementation of the lighting was fairly complicated. I wanted something that would be fast and allow us to have lots of lights, so I decided the best way to do this would be to construct a Mesh for each light, and draw them all at once.

![Mesh Visualization]({{path}}/02.gif)

Which worked like so:
<ul>
	<li>For each light, create a list of all the wall surfaces within the bounds of the light, and add 4 surfaces at the radius of the light (making that red box around it you see above).</li>
	<li>Join the surfaces at their corner or at intersections.</li>
	<li>Raycast out from the edges of each surface, and join them at their closest intersection.</li>
	<li>Start at the nearest surface to the center of the light, and move along each joined surface or edge raycast clockwise, creating triangles until we’re back at the first surface we started with. If we never get back to the first surface, something went wrong.</li>
</ul>

And this worked pretty well! We used this method for over a year. But it had a lot of edge cases, bugs, and floating point precision problems (for example, if two raycasts landed at the same spot on a surface, or if two solids were overlapping or touching, you only want the outline of their shape, not the parts of their walls that intersect).

## CUTOUT IMPLEMENTATION
Due to all the subtle problems, I recently decided to re-implement the lights entirely using a different method. Instead, what I would do is draw the “spotlight” of the light first to a texture, and then all the surfaces would draw their shadow projection (each just a single quad) “on top”, cutting out the spotlight.

![Cutout Illustration]({{path}}/03.png)

The result, after the light has been drawn and walls have erased their projected shadows, looks like this:

![Cutout Mask]({{path}}/04.png)
<center><i>Red is just the mask representation of the light, not necessarily the final light color</i></center><br/>

This works super well, and is a much simpler solution. However, using the previous Mesh Implementation, I could draw all the lights in a single draw-call since they were just a bunch of vertices. With this Cutout Implementation every light needed to be on their own texture (or a single texture where you swap between the screen and texture for each light).

There’s some places in the game that have a lot of lights. It didn’t make sense to have to swap between textures all the time. It’s expensive, and I’d like the lights to have little to no impact on the performance of the game.

## SINGLE-CHANNEL MASKS
The first optimization is realizing that the mask of the light doesn’t need to use all the channels on a texture (Red, Green, Blue, Alpha), but rather only one. If each light only uses a single channel, we can actually draw 4 overlapping lights per texture:

![Overlapping Lights]({{path}}/05.png)
<center><i>2 lights on the same texture</i></center><br/>

Above are 2 separate lights, drawn overlapping on the same texture, one using the Red Channel for its mask, and the other using the Green Channel. When the game draws the light to the screen, it simply uses the channel as a mask (I have a shader that draws the light’s color, but multiplied by the mask of the channel).

We can now have 32 lights, but only use 8 textures, which is a pretty nice improvement! But I wanted to find a way to cut it down even more.

## CELESTE IS (VISUALLY) TINY
I got to this point when I realized … we’re making a pixel art game. Our screen resolution is tiny (320x180), and textures can be pretty dang big these days (4096x4096+). So I decided to just… throw every light in the game into a grid, on a single texture. With a 2048x2048 texture, and giving each light a maximum size of 256x256, we can have 64 grid spaces. Since each light only takes up 1 channel, we can have a total of 256 lights on a single, big texture.

![Light Grid]({{path}}/06.png)

This allows me to still draw all the light masks in one big pass, and then again in another single pass to the game screen, and I don’t have to be swapping textures a million times.

This method ends up being a lot faster than the Mesh Implementation, as there’s a lot less work on the CPU (constructing all those meshes), and only a minor hit to the GPU. The lights are now some of the fastest code in the game, and we can have a ton of them on-screen at once with very little impact. Yay!

![Celeste Result]({{path}}/07.gif)