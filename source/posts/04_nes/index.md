{
	"title": "NES Limitations",
	"date": "Jan 1, 2015",
	"description": "Breakdown of NES limitations",
	"visible": false,
}
---
I've been experimenting with making a small game using NES graphical limitations, having been inspired by replaying
a bunch of Zelda 1 and Zelda 2. I needed a place to write them all down, and so here they are:

**The NES**
<ul>
	<li>256 x 224 px screen size. Technically 256 x 240, but the top and bottom 8px are cropped</li>
	<li>Composed of 2 layers: *Background Tiles* and *Sprites*</li>
	<li>
		Access to 2 sets of 256, 8 x 8 tiles (so 2 textures of 128 x 128px). Usually one is used for *Background Tiles*, and one is used for *Sprites*.
	</li>
	<li>Refresh rate of 60 Hz</li>
</ul>

**Palettes / Colors**
<ul>
	<li>The NES has access to 64 different colors that can be used throughout the game</li>
	<li>A total of 8 palettes can be used at once</li>
	<li>4 palettes are used for the *Background Tiles*. Each palette is composed of 3 colors, plus a common single background color shared between all 4 palettes</li>
	<li>4 palettes are used for the *Sprites*. Each palette is composed of 3 colors, plus transparency</li>
	<li>Total Colors onscreen: 25 (4 palettes x 3 colors = 12 for sprites, 4 palettes x 3 colors = 12 for background tiles, 1 for background)</li>
	<li>Palettes can be swapped on the fly (many games use this for cool fade in/out effects, etc)</li>
</ul>

**Background Tiles**
<ul>
	<li>8 x 8 pixels in size</li>
	<li>Snapped to an 8 x 8 grid (can't move freely, basically)</li>
	<li>Every tile can only use 1 palette</li>
	<li>Every block of 2 x 2 tiles must use the same palette. For example, tiles (0,0), (1, 0), (0, 1), (1, 1) must share the same palette</li>
	<li>The NES has access to 256 individual tiles at a time<br  />(think of a texture that is 128 x 128, split into 8 x 8 tiles)</li>
	<li>Background tiles can not be flipped, rotated, or scaled</li>
</ul>

**Sprites**
<ul>
	<li>Sprites can move around and are not snapped to the grid</li>
	<li>There are 2 Sprite Modes: 8 x 8 or 8 x 16. Your game may only use 1 mode.</li>
	<li>The game has access to 256 sprites (think of a texture that is 128 x 128, split into 8 x 8 tiles)</li>
	<li>
		**Note on 8 x 16 Mode**<br />
		In 8 x 16 Sprite mode, the game only has access to 128 sprites (the size of the sprite texture does not change). Every second tile in the texture is used along with the first one, 8px below it.<br />
		For example, if you draw Sprite[0], it will draw Sprite[0] *and also* draw Sprite[1] 8 pixels below it. You MUST only draw even sprites (so Sprite[0]+Sprite[1], never Sprite[1]+Sprite[2])<br />
		![Example Image]({{path}}/spritetiles.png)
	</li>
	<li>Each sprite can only use 1 palette (but you can overlap multiple sprites with transparency, ex. Megaman)</li>
	<li>You can only have 64 sprites on the screen at once</li>
	<li>You can only have 8 sprites per scaleline at once. What this means is that if more than 8 sprites are drawn on a horizontal line
	of pixels across the screen (a scanline), only 8 can be visible. Transparent pixels count.</li>
	<li>Sprites can be flipped horizontally and/or vertically, but can not be rotated or scaled</li>
</ul>

**Things this doesn't cover**
<ul>
	<li>Memory. The NES has relatively limited memory and how things moved, saved, etc were heavily influenced by that.</li>
	<li>"Hacks" that programmers could do, like swapping pixels mid-render</li>
	<li>Probably a bunch of other small stuff</li>
</ul>

**Cool links**
<ul>
	<li><a href="http://en.wikipedia.org/wiki/Picture_Processing_Unit">PPU</a>: The Picture Processing Unit used in the NES</li>
	<li><a href="http://wayofthepixel.net/index.php?topic=10784.msg115062#msg115062">Way Of the Pixel</a>: A good forum post containing basically all the same stuff, just not in point-form</li>
	<li><a href="http://wayofthepixel.net/index.php?topic=15781.msg144531#msg144531">Way Of the Pixel</a>: Another good post by the same author regarding background tiles</li>
	<li><a href="http://i.imgur.com/XZ0FmRb.png">NES Palette</a></li>
	<li><a href="http://bisqwit.iki.fi/utils/nespalette.php">NES palette generator</a></li>
</ul>

If I missed anything (or something is wrong) let me know!