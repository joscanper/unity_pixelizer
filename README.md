# Ratchet &amp; Clank™ Pixelizer FX in Unity3D
This project shows how to implement Ratchet &amp; Clank™ pixelizer weapon fx in Unity3D.

Please watch the following video to have a better understanding of how it works : https://youtu.be/b16JFKrGrFE

> Ratchet &amp; Clank is a trademark of SONY INTERACTIVE ENTERTAINMENT LLC in the U.S. and/or other countries.

![Pixelizer demo](https://github.com/joscanper/unity_pixelizer/blob/master/Showcase/Demo1b.gif)

This effect is implemented using a secondary camera to render the character to a tiny texture (TextureSize parameter on the PixelizerRenderer component).

The PixelizerRenderer renders the voxels of the texture every frame using the Pixelated material & shader.

The Pixelizer prefab contains all the needed elements to pixelate a 3d object. You just have to set the parameter Target on the PixelizerRenderer and adjust the TextureSize and VoxelSize so it can contain the character/object. 

## Assets
BOSS Class - Bull by Dr.Game

Low Poly Nature Assets Sample by PolyLabs
