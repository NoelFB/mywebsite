{
	"title": "Procedural Generation - The Dungeons",
	"date": "June, 2013",
	"oldurl": "2013/06/procedural-generation-the-dungeons"
}
---
Way back in 2011 I wrote a <a href="{{rel}}posts/thecaves/">blog post</a> on procedurally generating caves based on an algorithm I half made up, and people seemed to really enjoy reading about it. So, I'm going to explain some more procedural generation adventures I've had as I try to make a little action roguelike game.

Before I start explaining, here's a few images of what you can expect from this generation:

<center>
<a href="http://noelberry.ca/wp-content/uploads/2013/06/procgendun2.png"><img alt="procgendun2" src="http://noelberry.ca/wp-content/uploads/2013/06/procgendun2.png" width="265" height="265" style="display: inline"/></a> 
<a href="http://noelberry.ca/wp-content/uploads/2013/06/procgendun3.png"><img alt="procgendun3" src="http://noelberry.ca/wp-content/uploads/2013/06/procgendun3.png" width="265" height="265" style="display: inline"/></a>
<a href="http://noelberry.ca/wp-content/uploads/2013/06/procgendun1.png"><img alt="procgendun1" src="http://noelberry.ca/wp-content/uploads/2013/06/procgendun1.png" width="265" height="265" style="display: inline"/></a>
<a href="http://noelberry.ca/wp-content/uploads/2013/06/procgendun4.png"><img alt="procgendun1" src="http://noelberry.ca/wp-content/uploads/2013/06/procgendun4.png" width="265" height="265" style="display: inline"/></a>
</center>

<h2><strong>THE RUNDOWN</strong></h2>
The goal of this generation (as seen above) is to create a bunch of interconnecting rooms that the player can traverse. All the rooms are either a pre-designed level, or simply a rectangle of a random size. The placing of the rooms is completely random, and the tunnels are then dug between each room in a circular fasion, trying to stay on already-existing paths.


<b>Step 1: The Map</b><br />
The very first thing to do is create your map, and fill it up with Wall tiles. In my case, there are 3 types of tiles: Floor, Wall, Stone. Each of these will be explained later, but basically each one should be an integer value of some cost (I had Floor = 1, Wall = 4, Stone = 20). My Map is then a 2D integer array of these.


<b>Step 2: Place Random Rooms</b><br />
Okay, next we choose some number of rooms to place. This generally depends on the size of your map, and the min / max size of the rooms you can place. Once you've decided how many rooms to place, and how big and small they can be, you can begin placing rooms. Attempting to place a room essentially means you're checking to make sure the random area you selected <i>does not</i> contain any floor tiles already. To do that, you simply check the area that you're going to be placing the room (a rectangle) in your 2D map array and make sure it doesn't contain any floor tiles (in this case, positions in your array containing a 1). I also had template rooms, that were pre-designed rooms. Whenever attempting to place a room, the generator would decide to either place a normal rectangle room or a predesigned room.

Note that each rectangular area of the room is stored in an array of rectangles, for later use (just so we know where the rooms are).

Once you've found a clean area of Walls to place your room, you dig it out with floor tiles. Now, here's where things get a bit interesting. I decided to place a border of <i>Stone</i> Tiles around the room once it's placed. If you noticed before, Stone has a higher integer value than both Floors and Walls. The reason I do this is because I want to make it so when tunnels dig themselves they <i>avoid</i> going directly through a room as much as possible, to keep the general shape of the room. However, I do leave a few gaps in the stone to act as &#8220;doors&#8221;, or easy places for the tunnels to go through. Here's what a general room looks like:

<a href="http://noelberry.ca/wp-content/uploads/2013/06/generalroom1.png"><img class="aligncenter size-full wp-image-1598" alt="generalroom" src="http://noelberry.ca/wp-content/uploads/2013/06/generalroom1.png" width="320" height="260" /></a>

Here's what it looks like in our map's 2D array:<br />

<a href="http://noelberry.ca/wp-content/uploads/2013/06/costs1.png"><img class="aligncenter size-full" alt="generalroom" src="http://noelberry.ca/wp-content/uploads/2013/06/costs1.png" width="400" height="140" /></a>


Okay, cool! Now, after all the rooms are placed, we should have something like this (note: some rooms look a bit odd because I have some template rooms that get randomly placed, too)

<a href="http://noelberry.ca/wp-content/uploads/2013/06/rooms.png"><img class="aligncenter size-full wp-image-1600" alt="rooms" src="http://noelberry.ca/wp-content/uploads/2013/06/rooms.png" width="255" height="255" /></a>

<b>Step 3: Tunnels</b><br />
So, we have a bunch of rooms sitting around and things are feeling pretty good, right? Well, let's toss some tunnels in there. There's a bunch of ways to do this. You could make little miners that move from one room to the next, and just avoid Stone tiles as much as possible, or something. But, in my case, I decided to use A* pathfinding, using the costs I described above. I'm not going to get into the details of A*, so <a href="http://www.policyalmanac.org/games/aStarTutorial.htm">here's a really great explanation</a> that I used to learn it.

Specifically, tunnels are dug from the <i>center</i> of one room, to the center of the next. Once a path has been found, the given tiles in the path are turned into floor. Note, that the tunnel is dug out from the map BEFORE we do the next pathfind. This way, other tunnels will often follow the path of already existing tunnels, instead of doing their own thing.

Also note, that in my actual code, once a path is found, I will choose a random SIZE for the tunnel. The size in this case is only 1 or 2 tiles, but basically it digs out the tunnel larger than the actual original path (adding some random variance and makes things look more organic).

The order the tunnels are dug just starts from the first room in the rooms array (referenced in the note in step 2) to the next room in the array, and so on, until every room is connected.


<b>Step 4: Placing stuff</b><br />
Okay, so now there's a bunch of rooms and tunnels connecting all of them. For placing stuff, we have a lot of options. You could just randomly place stuff all over the place (searching for tiles that aren't walls or stones). But what I tend to do is loop through every room in our rooms array, and place a set amount of enemies in each one. You'll still want to probably make sure whatever position you select in a room is actually a floor (in case you create template rooms). Things like treasure could pick a random room and then a random position in it.

I haven't decided how to pick the start / end position yet, but I think the best thing to do would be to select the first room in the rooms array and place the player there, and then find the room furthest from that one and place the end around there.

<h2><strong>THE CODE</strong></h2>
This is going to be a bit messy, and I'm not sure how useful you'll find this, but here's the C# code I used to generate the maps in my games. There's some parts that are pretty gross (my A* pathfinding code is kind of ugly and could really use a clean rewrite). But it gives you a basic idea of what's going on.

<a href="http://pastebin.com/bjiHcETZ">Generator.cs</a>

Along with that, here's an example template room.<br />
The template room was created using <a href="http://ogmoeditor.com/">Ogmo Editor</a>

<a href="http://pastebin.com/tiwrz1uG">Room1.xml</a>

<h2><strong>CONCLUSION</strong></h2>
Thaaaat's pretty much it, hopefully it was somewhat useful or informative or something. Leave a comment if you have any questions and I'll try to get back to you!