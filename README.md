# Voxel Textures

This repository is very much inspired by [this](https://github.com/bbtarzan12/Unity-Procedural-Voxel-Terrain) repository where they managed to have a greedy mesh with properly textured meshes. The only problem I found was that the texture atlas was pre-made. So I set out to fix that by generating the atlas during runtime and avoiding duplicate textures on the atlas.

**This entire repository is just a proof of concept!**

### Job system
I also put the cube building inside a job mostly just to make sure it works inside the job system.

### How the atlas generation works (roughly)
1. Get all the unique textures
2. Pack it into an atlas
3. Figure out the X and Y index of each texture using the rect array returned when packing the atlas
4. Calculate the atlas X and Y size (not in pixels but "texture amount")
5. Calculate the atlas rect.

I commented the code to the best of my abilities so you can hopefully see how it works. There's only one script that generates a test cube with the textured applied.

## Credits
[bbtarzan12](https://github.com/bbtarzan12) and their repository for the shader and base code.
