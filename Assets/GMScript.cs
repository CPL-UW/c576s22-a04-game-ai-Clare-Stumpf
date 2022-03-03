using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

// ReSharper disable once InconsistentNaming
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class GMScript : MonoBehaviour
{
    public TileBase pieceTile;
    public TileBase emptyTile;
    public TileBase chunkTile;
    //public TileBase[] numberTiles;
    public Tilemap boardMap;
    public Tilemap enemyMap;
    public TMP_Text infoText;
    private int _score;
    private int _difficulty;
    private int _fixedUpdateFramesToWait = 10;
    private int _fixedUpdateCount;

    // ReSharper disable once InconsistentNaming
    public bool DEBUG_MODE;
    private bool Dirty { get; set; }
    private bool _initialized;
    private const int BOUNDS_MAX = 25;
    private int _minBx = BOUNDS_MAX;
    private int _minBy = BOUNDS_MAX;
    private int _maxBx = -BOUNDS_MAX;
    private int _maxBy = -BOUNDS_MAX;
    private int _minEx = BOUNDS_MAX;
    private int _minEy = BOUNDS_MAX;
    private int _maxEx = -BOUNDS_MAX;
    private int _maxEy = -BOUNDS_MAX;

    private int _inARow;

    // private int _width = 0, _height = 0;

    private Vector3Int[] _myPiece;
    private Vector3Int[] _myChunk;
    private Vector3Int[] _enemyPiece;
    private Vector3Int[] _enemyChunk;
    
    private Vector3Int[] PIECE_T;
    private Vector3Int[] PIECE_L;
    private Vector3Int[] PIECE_Z;
    private Vector3Int[] PIECE_J;
    private Vector3Int[] PIECE_S;
    private Vector3Int[] PIECE_I;
    private Vector3Int[][] PIECES;

  

    // Various tetris pieces
    private void InitializePieces()
    {
        PIECE_T = new Vector3Int[] { new(0,-1), new(1,-1), new(0,0),  new(-1,-1) };
        PIECE_L = new Vector3Int[] { new(0,-1), new(1,-1), new(1,0),  new(-1,-1) };
        PIECE_J = new Vector3Int[] { new(0,-1), new(1,-1), new(-1,0), new(-1,-1) };
        PIECE_S = new Vector3Int[] { new(0,-1), new(-1,-1),new(0,0),  new(1,0) };
        PIECE_Z = new Vector3Int[] { new(0,-1), new(1,-1), new(0,0),  new(-1,0) };
        PIECE_I = new Vector3Int[] { new(0,0),  new(-1,0), new(-2,0), new(1,0) };
        PIECES = new []{PIECE_T,PIECE_L,PIECE_Z,PIECE_J,PIECE_S,PIECE_I};
    }
    
    // initializes some variables at the start
    void Start()
    {
        _myPiece = null;
        _myChunk = null;
        Dirty = true;
        _initialized = false;
        InitializePieces();
    }
    
    // Creates a new piece and places it on the board
    private Vector3Int[] CreateAPiece(int midX, int maxY)
    {
        var targetPiece = PIECES[Random.Range(0, PIECES.Length)];
        var newPiece = new Vector3Int[targetPiece.Length];
        for (var i = 0; i < targetPiece.Length; i++)
        {
            newPiece[i].x = targetPiece[i].x + midX;
            newPiece[i].y = targetPiece[i].y + maxY;
        }
        return newPiece;
    }

    // Creates a blank board of empty tiles
    private void BlankABoard(Tilemap map,int x1, int y1, int x2, int y2)
    {
        for (var j = y1; j <= y2; j++)
        for (var i = x1; i <= x2; i++)
        {
            map.SetTile(new Vector3Int(i,j,0),emptyTile);
        }
    }

    // Makes both boards blank
    private void BlankAllBoards()
    {
        BlankABoard(boardMap,_minBx,_minBy,_maxBx,_maxBy);
        BlankABoard(enemyMap,_minEx,_minEy,_maxEx,_maxEy);
    }

    // Finds the bounds for both boards, blanks the boards, and creates a piece for each board
    private void SetupBaseBoards()
    {
        // Find the bounds for the visible board
        _initialized = true;
        for (var wy = -1 * BOUNDS_MAX; wy < BOUNDS_MAX; wy++)
        for (var wx = -1 * BOUNDS_MAX; wx < BOUNDS_MAX; wx++)
        {
            var myTile = boardMap.GetTile(new Vector3Int(wx,wy,0));
            var enemyTile = enemyMap.GetTile(new Vector3Int(wx,wy,0));
            if (myTile)
            {
                if (wx < _minBx) _minBx = wx;
                if (wy < _minBy) _minBy = wy;
                if (wx > _maxBx) _maxBx = wx;
                if (wy > _maxBy) _maxBy = wy;
            }
            if (enemyTile)
            {
                if (wx < _minEx) _minEx = wx;
                if (wy < _minEy) _minEy = wy;
                if (wx > _maxEx) _maxEx = wx;
                if (wy > _maxEy) _maxEy = wy;
            }
        }

        BlankAllBoards();
        _myPiece = CreateAPiece((_minBx + _maxBx)/2,_maxBy);
        _enemyPiece = CreateAPiece((_minEx + _maxEx) / 2, _maxEy);
        EnemyDoAction();

        if (!ValidPiece(_myPiece, true))
        {
            Debug.Log("NO VALID MOVES FROM START");
            Debug.Break();
        }

        Debug.Log($"MY BOARD SIZE = {(1 + _maxBx - _minBx)} x {(1 + _maxBy - _minBy)} ({_minBx},{_minBy}) -> ({_maxBx},{_maxBy})");
        Debug.Log($"AI BOARD SIZE = {(1 + _maxEx - _minEx)} x {(1 + _maxEy - _minEy)} ({_minEx},{_minEy}) -> ({_maxEx},{_maxEy})");
    }

    // Deletes a specified row
    private static Vector3Int[] KillRow(Vector3Int[] chunk, int row)
    {
        var newChunk = new Vector3Int[] { };
        foreach (var p in chunk)
        {
            if (p.y > row)
            {
                Vector3Int [] movedPieces = {new(p.x, p.y - 1, p.z)};
                newChunk = newChunk.Concat(movedPieces).ToArray();
            } else if (p.y < row)
            {
                Vector3Int [] movedPieces = {p};
                newChunk = newChunk.Concat(movedPieces).ToArray();
            }
        }
        return newChunk;
    }

    // Checks all the rows and loops through to see if the row is full
    private const int NO_ROW = -10 * BOUNDS_MAX; 
    private static int FindKillableRow(Vector3Int[] chunk, int max_width)
    {
        if (null == chunk) return NO_ROW;
        for (var row = -BOUNDS_MAX; row <= BOUNDS_MAX; row++) // just MIN_BOUND to MAX_BOUND?
        {
            var maxCount = max_width;//_maxBx - _minBx + 1; // width
            foreach (var p in chunk)
            {
                if (p.y == row)
                {
                    maxCount--;
                }
            }
            if (0 == maxCount)
            {
                return row;
            }
        }
        return NO_ROW;   
    }
    
    // Returns if the players is in a valid world position also dependent on whether it is the player or enemy
    private bool ValidWorldXY(int wx, int wy, bool player)
    {
        if (player)
            return (wx <= _maxBx && wx >= _minBx && wy <= _maxBy && wy >= _minBy);
        return (wx <= _maxEx && wx >= _minEx && wy <= _maxEy && wy >= _minEy);
    }

    // Makes sure that the move is valid and not bumping into the chunk
    private bool ValidMoveXY(int wx, int wy, bool player)
    {
        if (!ValidWorldXY(wx, wy, player))
            return false;
        if (player)
            return (null == _myChunk) || (_myChunk.All(p => p.x != wx || p.y != wy));
        return (null == _enemyChunk) || (_enemyChunk.All(p => p.x != wx || p.y != wy));
    }

    // Makes sure the piece is valid
    private bool ValidPiece(Vector3Int[] piece,bool player)
    {
        return (null != piece) && (piece.All(p => ValidMoveXY(p.x, p.y, player)));
    }

    // Shifts the either the enemy or player piece
    private Vector3Int[] ShiftPiece(IReadOnlyList<Vector3Int> piece, int dx, int dy, bool player)
    {
        if (null == piece) return null;
        var outPiece = new Vector3Int[piece.Count];
        foreach (var p in piece)
        {
            if (!ValidMoveXY(p.x + dx, p.y + dy, player))
            {
                // if (DEBUG_MODE) Debug.Log($"INVALID MOVE = {p.x + dx}, {p.y + dy}");
                return null;
            }
        }
        for (var i = 0; i < piece.Count; i++)
        {
            outPiece[i] = new Vector3Int(piece[i].x + dx, piece[i].y + dy);
        }
        
        return outPiece;
    }

    // Rotates the piece
    private Vector3Int[] RotatePiece(Vector3Int[] piece, bool player)
    {
        // rotated_x = (current_y + origin_x - origin_y)
        // rotated_y = (origin_x + origin_y - current_x - ?max_length_in_any_direction)
        if (null == piece) return null;
        var newPiece = new Vector3Int[piece.Length];
        Array.Copy(piece,newPiece,piece.Length);

        var origin = piece[0];
        for (var i = 1; i < piece.Length; i++ )
        {
            var rotatedX = piece[i].y + origin.x - origin.y;
            var rotatedY = origin.x + origin.y - piece[i].x;
            if (!ValidMoveXY(rotatedX, rotatedY, player))
                return piece;
            newPiece[i] = new Vector3Int(rotatedX, rotatedY);
        }

        //Array.Copy(newPiece, piece, piece.Length);
        return newPiece;
    }

    // Returns a random location in specified coordinates
    private static Vector3Int RandomEnemyPointInRange(int x1, int y1, int x2, int y2)
    {
        return new Vector3Int(Random.Range(x1,x2),Random.Range(y1,(y1 + y2)/2));
    }

    // Adds a chunk at the specified point and adds it to the landed array if the spot isn't already occupied
    private Vector3Int[] AddChunkAtPoint(Vector3Int[] chunk, Vector3Int chunkPoint)
    {
        chunk ??= new Vector3Int[] {};
        if (chunk.Any(p => p.x == chunkPoint.x && p.y == chunkPoint.y))
            return chunk;
        return chunk.Concat(new [] {chunkPoint}).ToArray();
    }

    // Drops the piece until it can't anymore
    private Vector3Int[] DropPiece(Vector3Int[] piece, bool player)
    {
        var lastPiece = piece;
        while (null != lastPiece)
        {
            piece = lastPiece;
            lastPiece = ShiftPiece(piece,0, -1, player);
        }
        return piece;
    }

    // Adds current piece to the landed 'chunk'
    private static Vector3Int[] ChunkPiece(Vector3Int[] piece, Vector3Int[] chunk)
    {
        chunk ??= new Vector3Int[] {};
        if (null == piece) return chunk;
        return chunk.Concat(piece).ToArray();
    }
    
    // Shifts player piece to left
    private void PlayerDoLeft()
    {
        Dirty = true;
        var tmpPiece = ShiftPiece(_myPiece, -1,0, true);
        if (null != tmpPiece)
            _myPiece = tmpPiece;
    }

    // Shifts player piece to right
    private void PlayerDoRight()
    {
        Dirty = true;
        var tmpPiece = ShiftPiece(_myPiece, 1,0, true);
        if (null != tmpPiece)
            _myPiece = tmpPiece;
    }

    // Rotates player piece
    private void PlayerDoUp()
    {
        Dirty = true;
        _myPiece = RotatePiece(_myPiece, true);
    }

    // Drops player piece down (called once every fixed update)
    private void PlayerDoDown()
    {
        Dirty = true;
        var tmpPiece = ShiftPiece(_myPiece, 0, -1, true);
        if (null == tmpPiece)
        {
            _myChunk = ChunkPiece(_myPiece, _myChunk);
            _myPiece = null;
        }
        else
        {
            _myPiece = tmpPiece;
        }
    }

    // private string ChunkToString(Vector3Int[] chunk)
    // {
    //     var output = "";
    //     var min_y = chunk.Min(p => p.y);
    //     var min_x = chunk.Min(p => p.x);
    //     var max_y = chunk.Max(p => p.y);
    //     var max_x = chunk.Max(p => p.x);
    //     var truth_board = new bool[max_x-min_x+2,max_y-min_y+2];
    //     foreach (var p in chunk)
    //     {
    //         truth_board[p.x - min_x, p.y - min_y] = true;
    //     }
    //     for (var x = min_x; x <= max_x; x++)
    //     {
    //         for (var y = min_y; y <= max_y; y++)
    //         {
    //             output += truth_board[x - min_x, y - min_y] ? "." : " ";
    //         }
    //         output += "\n";
    //     }
    //
    //     return output;
    // } 

    // TODO - Still not working properly (I think it's something to do with indexing)
    // See how high the stack currently is
    // Find holes in landed chunk
    // See if holes would form from move
    // Grade as follows
    // -4 if creates a hole (open or closed)
    // -2 if it stacks on top of an existing holes
    // -1 for every line it makes the stack higher
    // +1 for every line cleared
    private const int GOOD_SCORE = 10000;
    private int EvaluateEnemyPieceScore(Vector3Int[] piece, Vector3Int[] chunk, bool drop = true)
    {

        if (null == piece || null == chunk) return -GOOD_SCORE;

        // How high the current stack is
        var currentHeight = 0;
        foreach (var p in chunk) {
            if(p.y > currentHeight) {
                currentHeight = p.y;
            }
        }
        
        // Where are current holes?
        var blocksInCol = new int[14];
        foreach (var p in chunk) {
            blocksInCol[p.x]++;
        }

        var colHeight = new int[14];
        foreach(var p in chunk) {
            if(p.y > colHeight[p.x]) {
                colHeight[p.x] = p.y;
            }
        }

        var currentHoles = new int[14];
        foreach(var p in blocksInCol) {
            if(blocksInCol[p] < colHeight[p]) {
                currentHoles[p]++;            
            }
        }

        var numCurrentHoles = 0;
        foreach(var p in currentHoles) {
            numCurrentHoles += currentHoles[0];
        }



        // Combine the potential move and the landed stack
        var combined = drop ? DropPiece(piece,false).Concat(chunk).ToArray() : piece.Concat(chunk).ToArray();

        // Calculates the new height
        var newHeight = 0;
        foreach (var p in combined) {
            if(p.y > newHeight) {
                newHeight = p.y;
            }
        }

        // Where are current holes?
        var newBlocksInCol = new int[14];
        foreach (var p in combined) {
            newBlocksInCol[p.x]++;
        }

        var newColHeight = new int[14];
        foreach(var p in combined) {
            if(p.y > newColHeight[p.x]) {
                newColHeight[p.x] = p.y;
            }
        }

        var newHoles = new int[14];
        foreach(var p in newBlocksInCol) {
            if(newBlocksInCol[p] < newColHeight[p]) {
                newHoles[p]++;            
            }
        }

        var numNewHoles = 0;
        foreach(var p in newHoles) {
            numCurrentHoles += newHoles[0];
        }

        // Sees if it lands on top of other holes
        var onTopOfHole = 0;
        foreach(var p in currentHoles) {
            if(p > 0) {
                onTopOfHole += newColHeight[p] - colHeight[p];
            }
        }

        // Sees how many rows can be cleared
        var rowsCleared = 0;
        var row = 0;
        // Max 4 rows can be cleared by one piece
        for(int i = 0; i < 4; i++) {
            row = FindKillableRow(combined, _maxEx - _minEx + 1);
            if(row >= 0) {
            combined = KillRow(combined, row);
            rowsCleared++;
            }
        }
       
        if (row != NO_ROW)
        {
            Debug.Log("FOUND A LINE: ");
            return GOOD_SCORE; // LINE!
        }
        if (DEBUG_MODE) Debug.Log($"{combined.Average(p => p.y)}");//\n{ChunkToString(combined)}");



        // Calculating the score from criteria listed above
        var score = 0;
        if(numCurrentHoles < numNewHoles) {
            score -= 4 * (numNewHoles - numCurrentHoles);
        }
        if(onTopOfHole > 0) {
            score -= 2 * onTopOfHole;
        }
        if(currentHeight > newHeight) {
            score -= (newHeight - currentHeight);
        }
        score += rowsCleared;
        Debug.Log($"score = {score}");

        Debug.Log($"Height1 = {currentHeight}");
        Debug.Log($"Height2 = {newHeight}");

        return score;
        //return 100 * (int) (BOUNDS_MAX - combined.Average(p => p.y)); 
    }

    // Puts all possible actions into an array and valid ones into another array
    // Returns piece if there are no valid options
    // Evaluates the enemy piece score and does the action that is best suited to it.
    private Vector3Int[] EnemyChooseAction(Vector3Int[] piece)
    {
        if (null == piece) return null; 
        var enemyLeft1 = ShiftPiece(piece, -1, 0, false);
        var enemyLeft2 = ShiftPiece(piece, -2, 0, false);
        var enemyLeft3 = ShiftPiece(piece, -3, 0, false);
        var enemyLeft4 = ShiftPiece(piece, -4, 0, false);
        var enemyRight1 = ShiftPiece(piece, 1, 0, false);
        var enemyRight2 = ShiftPiece(piece, 2, 0, false);
        var enemyRight3 = ShiftPiece(piece, 3, 0, false);
        var enemyRight4 = ShiftPiece(piece, 4, 0, false);
        
        var enemyRotate1 = RotatePiece(piece, false);
        var enemyR1Left1 = ShiftPiece(enemyRotate1, -1, 0, false);
        var enemyR1Left2 = ShiftPiece(enemyRotate1, -2, 0, false);
        var enemyR1Left3 = ShiftPiece(enemyRotate1, -3, 0, false);
        var enemyR1Left4 = ShiftPiece(enemyRotate1, -4, 0, false);
        var enemyR1Right1 = ShiftPiece(enemyRotate1, 1, 0, false);
        var enemyR1Right2 = ShiftPiece(enemyRotate1, 2, 0, false);
        var enemyR1Right3 = ShiftPiece(enemyRotate1, 3, 0, false);
        var enemyR1Right4 = ShiftPiece(enemyRotate1, 4, 0, false);

        var enemyRotate2 = RotatePiece(enemyRotate1, false);
        var enemyR2Left1 = ShiftPiece(enemyRotate2, -1, 0, false);
        var enemyR2Left2 = ShiftPiece(enemyRotate2, -2, 0, false);
        var enemyR2Left3 = ShiftPiece(enemyRotate2, -3, 0, false);
        var enemyR2Left4 = ShiftPiece(enemyRotate2, -4, 0, false);
        var enemyR2Right1 = ShiftPiece(enemyRotate2, 1, 0, false);
        var enemyR2Right2 = ShiftPiece(enemyRotate2, 2, 0, false);
        var enemyR2Right3 = ShiftPiece(enemyRotate2, 3, 0, false);
        var enemyR2Right4 = ShiftPiece(enemyRotate2, 4, 0, false);

        var enemyRotate3 = RotatePiece(enemyRotate2, false);
        var enemyR3Left1 = ShiftPiece(enemyRotate3, -1, 0, false);
        var enemyR3Left2 = ShiftPiece(enemyRotate3, -2, 0, false);
        var enemyR3Left3 = ShiftPiece(enemyRotate3, -3, 0, false);
        var enemyR3Left4 = ShiftPiece(enemyRotate3, -4, 0, false);
        var enemyR3Right1 = ShiftPiece(enemyRotate3, 1, 0, false);
        var enemyR3Right2 = ShiftPiece(enemyRotate3, 2, 0, false);
        var enemyR3Right3 = ShiftPiece(enemyRotate3, 3, 0, false);
        var enemyR3Right4 = ShiftPiece(enemyRotate3, 4, 0, false);

        Vector3Int[][] enemyOptions = 
        {
        piece, enemyLeft1, enemyLeft2, enemyLeft3, enemyLeft4, enemyRight1, enemyRight2, enemyRight3, enemyRight4,
        enemyRotate1, enemyR1Left1, enemyR1Left2, enemyR1Left3, enemyR1Left4, enemyR1Right1, enemyR1Right2, enemyR1Right3, enemyR1Right4,
        enemyRotate2, enemyR2Left1, enemyR2Left2, enemyR2Left3, enemyR2Left4, enemyR2Right1, enemyR2Right2, enemyR2Right3, enemyR2Right4,
        enemyRotate3, enemyR3Left1, enemyR3Left2, enemyR3Left3, enemyR3Left4, enemyR3Right1, enemyR3Right2, enemyR3Right3, enemyR3Right4
        };

        var validOptions = enemyOptions.Where(p => ValidPiece(p, false)).ToArray();
        if (!validOptions.Any()) return piece;

        var pieceScore = 0;
        var maxScore = 0;
        foreach(var p in validOptions) {
            pieceScore = EvaluateEnemyPieceScore(p, _enemyChunk);
            if(pieceScore > maxScore) {
                maxScore = pieceScore;
            }
        }

        //var maxScore = validOptions.Max(p => EvaluateEnemyPieceScore(p, _enemyChunk));
        validOptions = validOptions.Where(p => EvaluateEnemyPieceScore(p, _enemyChunk) == maxScore).ToArray();
        Debug.Log("All Scores Computed -----------");
        if (DEBUG_MODE) Debug.Log($"max score = {maxScore}; options = {validOptions.Length}");


        return validOptions.ElementAt(Random.Range(0, validOptions.Count())); 
    }
    
    // Creates a piece for the enemy if none exists and chooses its action
    // It moves the piece down one until it is landed onto the chunk
    private void EnemyDoAction()
    {
        Dirty = true;
        if (null == _enemyPiece)
        {
            _enemyPiece = CreateAPiece((_minEx + _maxEx) / 2, _maxEy);
            if (!ValidPiece(_enemyPiece, false))
            {
                if (DEBUG_MODE) Debug.Log("ENEMY DEAD");
            }
            _enemyPiece = EnemyChooseAction(_enemyPiece);

        }
        else
        {
             var tmpPiece = ShiftPiece(_enemyPiece, 0, -1, false);
            if (!ValidPiece(tmpPiece, false))
            {
                _enemyChunk = ChunkPiece(_enemyPiece, _enemyChunk);
                _enemyPiece = null;
            } else {
                _enemyPiece = tmpPiece;
            }
                //_enemyPiece = EnemyChooseRecursive(tmpPiece);   
        }

    }

    // Drops the player piece
    private void PlayerDoDrop()
    {
        Dirty = true;
        _myPiece = DropPiece(_myPiece, true);
    }

    // Draws a blank board and iterates through to draw chunk pieces for landed tiles
    private void DrawAllBoards()
    {
        BlankAllBoards();

        if (null != _myChunk)
        {
            foreach (var p in _myChunk)
            {
                boardMap.SetTile(p, chunkTile);
            }
        }

        if (null != _enemyChunk)
        {
            foreach (var p in _enemyChunk)
            {
                enemyMap.SetTile(p, chunkTile);
            }
        }
        
    }

    // Similar to DrawAllBoards(), it iterates through to draw the current falling piece on the boards
    private void DrawAllPieces()
    {
        if (null != _myPiece)
        {
            foreach (var p in _myPiece)
            {
                boardMap.SetTile(p, pieceTile);
            }
        }

        if (null != _enemyPiece)
        {
            foreach (var p in _enemyPiece)
            {
                enemyMap.SetTile(p, pieceTile);
            }
        }
    }

    // Makes a random angry chunk on the board
    private void MakeRandomAngryChunk()
    {
        _myChunk = AddChunkAtPoint(_myChunk,RandomEnemyPointInRange(_minBx,_minBy,_maxBx,_maxBy));
    }
    
    // Piece drops and enemy can do action at every fixed update point
    // Speeds the game up if you get more in a row than the current difficulty level
    // Deletes a row if possible and updates inARow
    // Makes a random angry chunk
    // Updates score
    void FixedUpdate()
    {
        if (0 != _fixedUpdateCount++ % _fixedUpdateFramesToWait) return;
        PlayerDoDown();
        EnemyDoAction();




        if (_inARow > _difficulty)
        {
            _difficulty = _inARow;
            if (_fixedUpdateFramesToWait > 1)
            {
                _fixedUpdateFramesToWait--;
            }
        }

        var row_to_kill = FindKillableRow(_myChunk,_maxBx - _minBx + 1);
        if (NO_ROW != row_to_kill)
        {
            _myChunk = KillRow(_myChunk, row_to_kill);
            _inARow++;
            MakeRandomAngryChunk();
        }
        else _inARow = 0;

        var enemy_row_to_kill = FindKillableRow(_enemyChunk, _maxEx - _minEx + 1);
        if (NO_ROW != enemy_row_to_kill)
        {
            _enemyChunk = KillRow(_enemyChunk, enemy_row_to_kill);
        }

        infoText.text = $"PTS:{_score}\t\tMAX:{_difficulty}\nCURRIC 576";
        _fixedUpdateCount = 1;
    }
    
    // Creates a piece if there is none and you still have room
    // Let's you add a 'chunk' if you want
    // Has player controls
    // Draws the board and pieces
    void Update()
    {
        if (null == Camera.main) return; 
        if (!_initialized) SetupBaseBoards();
        if (null == _myPiece)
        {
            _myPiece = CreateAPiece((_minBx + _maxBx) / 2, _maxBy);
            if (!ValidPiece(_myPiece, true))
            {   
                Debug.Log("NO VALID MOVE");
                Debug.Break();
            }
        }
        
        
        if (Input.GetKeyDown(KeyCode.Q)) { Debug.Break(); }
        else if (Input.GetMouseButtonDown(0)) 
        {
            var point = Camera.main.ScreenToWorldPoint(Input.mousePosition); 
            var selectedTile = boardMap.WorldToCell(point);
            _myChunk = AddChunkAtPoint(_myChunk, selectedTile);
            // Debug.Log(selectedTile);
            // boardMap.SetTile(selectedTile, pieceTile); 
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) { PlayerDoLeft(); }
        else if (Input.GetKeyDown(KeyCode.RightArrow)) { PlayerDoRight(); }
        else if (Input.GetKeyDown(KeyCode.UpArrow)) { PlayerDoUp(); }
        else if (Input.GetKeyDown(KeyCode.DownArrow)) { PlayerDoDrop(); }

        if (!Dirty) return;
        DrawAllBoards();
        DrawAllPieces();
        Dirty = false;
    } 
    
   
}
