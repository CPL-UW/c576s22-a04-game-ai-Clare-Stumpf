# CS576-Tetris-Sample

1. Clone this repo
2. Open it in a 2021 version of Unity
3. 2xClick TetrisScene (in Assets/Scenes)
4. Hit run?

Apologies for the mess. YMMV.

I can't think of anything better or more creative right now, and I should have started earlier. It doesn't work properly. I've been super busy this week and have been having a pretty bad week. :( That's not a good excuse, though.

The windows build is in this repository if you run the executable file.

Brute Searching All Possible End Positions, and Scoring Them. The best score gets chosen as the move. Scoring is as follows:
1. Creating a hole (includes open holes where t-spins and stuff could theoretically go): -4 per hole created
2. Putting a block above a hole: -2 per block put above hole
3. Creating a higher stack: -1 per additional line of height
4. Clearing a line: +1 per line

The position in the chosen at the start and just steadily moves down until it lands.

Known Bugs:
1. The scoring isn't working properly. I'm pretty sure it has to do with something regarding indexing or maybe I'm misunderstanding what some of the given code does. I'll try to fix it, but it definitely won't be by 1:59pm. :(
