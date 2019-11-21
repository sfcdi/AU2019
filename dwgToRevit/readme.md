# Overview
This will convert lines in a Revit model from a dwg file into walls and doors

## Setup
This script makes a few assumptions:
1. The walls are located on the "A-WALLS" layer.
2. The doors are on a layer that includes the name "door". (case-insensitive)
3. Any openings in the walls in autocad are done through wipeouts in blocks
4. The doors in AutoCAD are a block. The origin of the block should match the origin of your Revit Door Family.
5. This tool currently assumes all walls are only 6" thick.
6. The tool is hardcoded to use the first door type that it finds.

You will have to create a new add-in that uses this code.  Let us know if you need help doing this.

## Directions
1. Import (don't link) the AutoCAD file.
2. Explode the AutoCAD file.
3. Run the Add-in.

## Notes	
1. This was put together a bit hastily for AU.  We plan to develop this further
2. New development might be slow, because the team no longer has access to Revit
3. Any additions / revisions / input is welcome. 