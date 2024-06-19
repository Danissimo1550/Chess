using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PieceLogic : MonoBehaviour
{
    public Sprite WhitePawn;
    public Sprite BlackPawn;
    public Sprite BlackKnight;
    public Sprite WhiteKnight;
    public Sprite WhiteBishop;
    public Sprite BlackBishop;
    public Sprite BlackRook;
    public Sprite WhiteRook;
    public Sprite WhiteQueen;
    public Sprite BlackQueen;
    public Sprite BlackKing;
    public Sprite WhiteKing;
    public Sprite KillMarkerspr;
    public Tile MoveMarker;
    public Tile KillMarker;
    Tilemap markerTilemap;
    public bool alive = true; // alive or not
    public Vector3Int[] KillMoves = new Vector3Int[64];
    Vector2 mousePos;
    Logic logic;
    Vector3Int[] tiles = new Vector3Int[64];
    public int type;
    public bool color; // false is black
    public int ID;
    static int amountPieceDrag; //amount of pieces dragged
    bool enemyDetected = false;
    int enPassantIndex = 0;
    Vector3Int previousPosition;
    int dir1; //topleft Mathf.Min(target.x, target.y) - 1;
    int dir2; //topright Mathf.Min(target.x, 9 - target.y) - 1;
    int dir3; //bottomleft 8 - Mathf.Max(target.x, 9 - target.y);
    int dir4; //bottomright 8 - Mathf.Max(target.x, target.y);

    [SerializeField] Vector3Int[] AllPossibleMoves;
    void Start()
    {
        logic = GameObject.Find("Logic").GetComponent<Logic>();
        markerTilemap = GameObject.Find("MarkerTileMap").GetComponent<Tilemap>();
    }
    void Update()
    {
        color = type >= 0;
        mousePos = GameObject.Find("Main Camera").GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);
        gameObject.name = GetComponent<SpriteRenderer>().sprite.name;

        UpdateType();
        Collider2D clickcollision = Physics2D.OverlapCircle(mousePos, 0.000000001f);

        if (Input.GetKeyDown(KeyCode.Mouse0) && clickcollision.gameObject == logic.IDs[ID] && amountPieceDrag == 0 && clickcollision != null)
        {
            StartCoroutine(WaitUntilRelease(ID));
        }
    }
    IEnumerator WaitUntilRelease(int id)
    {
        int index = 0;
        foreach (Vector3Int vector in KillMoves)
        {
            KillMoves[index] = Vector3Int.down * 10;
            index++;
        }
        ClearTiles();
        if (logic.debug == false)
        {
            if (logic.IDs[id].GetComponent<PieceLogic>().type < 0)
            {
                if (logic.turn == true) { yield break; }
            }
            if (logic.IDs[id].GetComponent<PieceLogic>().type > 0)
            {
                if (logic.turn == false) { yield break; }
            }
        }
        previousPosition = Vector3Int.RoundToInt(transform.position);
        amountPieceDrag++;
        PossibleMovesFor(Vector3Int.FloorToInt(previousPosition));
        while (Input.GetKey(KeyCode.Mouse0))
        {
            Collider2D clickcollision = Physics2D.OverlapCircle(mousePos, 0.01f);

            if (clickcollision)
            {
                logic.IDs[id].transform.position = mousePos;
            }
            yield return new WaitForEndOfFrame();
        }
        amountPieceDrag--;
        Move(id, new Vector3Int((int)Mathf.Round(transform.position.x), (int)Mathf.Round(transform.position.y)));
        ClearTiles();
    }
    void Move(int id, Vector3Int destination)
    {
        ClearTiles();
        if (logic.debug == false)
        {
            if (!ArrayContains(PossibleMovesFor(Vector3Int.FloorToInt(previousPosition)), destination) && !ArrayContains(KillMoves, destination))
            {
                transform.position = previousPosition;
                return;
            }
        }

        if (ArrayContains(KillMoves,destination))
        {
            if (GetPieceAtPosition(destination + Vector3Int.down * type) != null && logic.enPassant == GetPieceAtPosition(destination + Vector3Int.down * type).GetComponent<PieceLogic>().ID)
            {
                logic.IDs[logic.enPassant].GetComponent<PieceLogic>().DestroyPiece();
            }
            else
            {
                GetPieceAtPosition(destination).GetComponent<PieceLogic>().DestroyPiece();
            }
        }
        logic.enPassant = -1;
        if (Mathf.Abs(type) == 1 && previousPosition == destination + type * 2 * Vector3Int.down) //if its a double move by a pawn
        {
            logic.enPassant = ID;
        }
        logic.IDs[id].transform.position = destination;
        logic.turn = !logic.turn;
    }
    Vector3Int[] PossibleMovesFor(Vector3Int pos)
    {
        enemyDetected = false;
        var i = 0;
        AllPossibleMoves = new Vector3Int[63];
        if (Mathf.Abs(type) == 1)
        {
            if (CollisionCheck(new Vector3Int(pos.x, pos.y + 1 * type), type, true) == false)
            {
                AllPossibleMoves[i] = new Vector3Int(pos.x, pos.y + 1 * type);
            }
            i++;
            if (pos.y == 2 || pos.y == 7)
            {
                if (CollisionCheck(new Vector3Int(pos.x, pos.y + 2 * type), type, false) == false &&
                    ArrayContains(AllPossibleMoves, new Vector3Int(pos.x, pos.y + 1 * type)))
                {
                    AllPossibleMoves[i] = new Vector3Int(pos.x, pos.y + 2 * type);
                    enPassantIndex = i;
                }
            }
            for (int n = -1; n < 2; n += 2)
            {
                if (CollisionCheck(previousPosition + new Vector3Int(n, type, 0), type, false) == true)
                {
                    i++;
                    KillMoves[i] = new Vector3Int(n, type, 0) + previousPosition;
                }
                if (CollisionCheck(previousPosition + new Vector3Int(n, 0, 0), type, false) == true)
                {
                    if (GetPieceAtPosition(previousPosition + new Vector3Int(n, 0, 0)).GetComponent<PieceLogic>().ID == logic.enPassant)
                    { // if the piece on the left or right has made a double move
                        i++;
                        KillMoves[i] = new Vector3Int(n, type, 0) + previousPosition;
                    }
                }
            }
            if (AllPossibleMoves[i].y > 8 || AllPossibleMoves[i].y < 1) { AllPossibleMoves[i] = Vector3Int.down * 10; }
        }
        if (Mathf.Abs(type) == 4)
        {
            AllPossibleMoves = CheckLines(pos, 1);
        }
        if (Mathf.Abs(type) == 2)
        {
            AllPossibleMoves = CheckLines(pos, 3);
        }
        if (Mathf.Abs(type) == 3)
        {
            AllPossibleMoves = CheckLines(pos, -1);
        }
        if (Mathf.Abs(type) == 5)
        {
            AllPossibleMoves = CheckLines(pos, 2);
        }
        if (Mathf.Abs(type) == 6)
        {
            AllPossibleMoves = CheckLines(pos, 0);
        }
        i = 0;
        foreach (Vector3Int killMove in KillMoves)
        {
            if (killMove != Vector3Int.down * 10)
            {
                markerTilemap.SetTile(markerTilemap.WorldToCell(killMove), KillMarker);
                tiles[i] = markerTilemap.WorldToCell(killMove);
                i++;
            }
        }
        foreach (Vector3Int possibleMove in AllPossibleMoves)
        {
            if (possibleMove != Vector3Int.down * 10)
            {
                markerTilemap.SetTile(markerTilemap.WorldToCell(possibleMove), MoveMarker);
                tiles[i] = markerTilemap.WorldToCell(possibleMove);
                if (Mathf.Abs(type) == 1)
                {
                    if (GetPieceAtPosition(possibleMove) != null)
                    {
                        if (GetPieceAtPosition(possibleMove).GetComponent<PieceLogic>().color == color || GetPieceAtPosition(possibleMove).transform.position.x == transform.position.x)
                        {
                            AllPossibleMoves[i] = Vector3Int.down * 10;
                            markerTilemap.SetTile(markerTilemap.WorldToCell(possibleMove), null);
                            tiles[i] = markerTilemap.WorldToCell(Vector3Int.down * 10);
                        }
                    }
                }
                i++;
                if (GetPieceAtPosition(possibleMove) != null && GetPieceAtPosition(possibleMove).GetComponent<PieceLogic>().color != color)
                {
                    if (GetPieceAtPosition(possibleMove).GetComponent<PieceLogic>().color != color || logic.enPassant == GetPieceAtPosition(possibleMove).GetComponent<PieceLogic>().ID)
                    {
                        KillMoves[i] = possibleMove;
                        markerTilemap.SetTile(markerTilemap.WorldToCell(possibleMove), KillMarker);
                        tiles[i] = markerTilemap.WorldToCell(possibleMove);
                    }
                }
            }

        }
        return AllPossibleMoves;
    }
    bool ArrayContains(Vector3Int[] targetArray, Vector3Int targetValue)
    {
        int i = 0;
        foreach (Vector3Int vector in targetArray)
        {
            if (targetArray[i].Equals(targetValue)) return true;
            i++;
        }
        return false;
    }
    void ClearTiles()
    {
        foreach (Vector3Int vector in tiles)
        {
            markerTilemap.SetTile(vector, null);
        }
        for (int y = 0; y <= 8; y++)
        {
            for (int x = 0; x <= 8; x++)
            {
                markerTilemap.SetTile(markerTilemap.WorldToCell(new Vector3Int(x, y)), null);
            }
        }
    }
    /* int HowManyElementsAreInMyArray(Vector3Int[] a)
    {
        int notNulls = 0;
        for (int x = 0; x < a.Length; x++)
        {
            if (a[x] != Vector3Int.zero) notNulls++;
        }
        return notNulls;
    } */
    Vector3Int[] CheckLines(Vector3Int target, int lineType) // 1 = horizontal lines, -1 = diagonal lines, 2 = both
    {
        int x;
        Vector3Int[] possibleMoves = new Vector3Int[64];
        Vector3Int targetposition;
        dir1 = Mathf.Min(target.x, 9 - target.y) - 1;
        dir2 = 8 - Mathf.Max(target.x, target.y);
        dir3 = 8 - Mathf.Max(target.x, 9 - target.y);
        dir4 = Mathf.Min(target.x, target.y) - 1; // there is 9 - target.y because the y cord needs to be mirrored to return correct values

        Vector3Int[] horizontaloffsets = new Vector3Int[4] { new Vector3Int(-1, 0), new Vector3Int(0, 1), new Vector3Int(1, 0), new Vector3Int(0, -1) };

        int[] directions = new int[4] { dir1, dir2, dir3, dir4 };
        Vector3Int[] diroffsets = new Vector3Int[4] { new Vector3Int(-1, 1), new Vector3Int(1, 1), new Vector3Int(1, -1), new Vector3Int(-1, -1) };

        if (lineType == 1 || lineType == 2)
        {
            int i = 0;
            for (int n = target.x; n > 0; n--)
            {
                if (CollisionCheck(new Vector3Int(n - 1, target.y), type, true) == true) break;
                possibleMoves[i] = new Vector3Int(n - 1, target.y);
                i++;
            }
            int y = target.x;
            enemyDetected = false;
            while (y <= 8)
            {
                y++;
                if (CollisionCheck(new Vector3Int(y, target.y), type, true) == true) break;
                possibleMoves[i] = new Vector3Int(y, target.y);
                i++;
            }
            enemyDetected = false;
            for (int n = target.y; n > 0; n--)
            {
                if (CollisionCheck(new Vector3Int(target.x, n - 1), type, true) == true) break;
                possibleMoves[i] = new Vector3Int(target.x, n - 1);
                i++;
            }
            y = target.y;
            enemyDetected = false;
            while (y <= 8)
            {
                y++;
                if (CollisionCheck(new Vector3Int(target.x, y), type, true) == true) break;
                possibleMoves[i] = new Vector3Int(target.x, y);
                i++;
            }
        }
        if (lineType == -1 || lineType == 2)
        {
            int n = -1;
            x = 0; // amount of possible moves already registered in the array
            if (lineType == 2) x = 16;
            foreach (Vector3Int offset in diroffsets)
            {
                enemyDetected = false;
                n++;
                targetposition = target;
                for (int i = 0; i < directions[n]; i++)
                {
                    targetposition += diroffsets[n];
                    if (CollisionCheck(targetposition, type, true) == true) break;
                    possibleMoves[x] = targetposition;
                    x++;
                }
            }
        }
        if (lineType == 3) // if its a knight
        {
            x = 0;
            Vector3Int[] knightoffsets = new Vector3Int[8] { new Vector3Int(-1, 2), new Vector3Int(1, 2), new Vector3Int(2, 1), new Vector3Int(2, -1),
            new Vector3Int(1, -2), new Vector3Int(-2,1), new Vector3Int(-2,-1), new Vector3Int(-1,-2)};
            foreach (Vector3Int knightmove in knightoffsets)
            {
                enemyDetected = false;
                if (CollisionCheck(target + knightmove, type, true) == false)
                {
                    possibleMoves[x] = target + knightmove;
                }
                x++;
            }
        }
        if (Mathf.Abs(type) == 6)
        {
            for (int i = 0; i < 32; i++)
            {
                possibleMoves[i] = Vector3Int.down * 10;
            }
            enemyDetected = false;
            x = 0;
            foreach (Vector3Int move in diroffsets)
            {
                if (GetPieceAtPosition(target + move) == null || GetPieceAtPosition(target + move) != null && GetPieceAtPosition(target + move).GetComponent<PieceLogic>().color != color) { possibleMoves[x] = target + move; }
                x++;
            }
            enemyDetected = false;
            int n = 0;
            foreach (Vector3Int offset in horizontaloffsets)
            {
                if (GetPieceAtPosition(target + offset) == null || GetPieceAtPosition(target + offset) != null && GetPieceAtPosition(target + offset).GetComponent<PieceLogic>().color != color) { possibleMoves[x + n] = target + offset; }
                n++;
            }
        }

        int index = 0;
        foreach (Vector3Int v in possibleMoves)
        {
            if (possibleMoves[index].x < 1 || possibleMoves[index].y < 1 || possibleMoves[index].y > 8 || possibleMoves[index].x > 8)
            {
                if (possibleMoves[index] != Vector3Int.down * 10) { possibleMoves[index] = Vector3Int.down * 10; }
            }
            index++;
        }
        return possibleMoves;
    }
    GameObject GetPieceAtPosition(Vector3Int position)
    {
        foreach (GameObject piece in logic.IDs)
        {
            if (piece.transform.position == position)
            {
                return piece;
            }
        }
        return null;
    }
    bool CollisionCheck(Vector3Int target, int piecetype, bool detectEnemies) // returns true if a piece occupies the target square.
    {
        if (detectEnemies) { if (enemyDetected == true) { enemyDetected = false; return true; } }
        foreach (GameObject piece in logic.IDs)
        {
            if (detectEnemies == false)
            {
                if (piece.transform.position == target)
                {
                    return true;
                }
            }
            if (piecetype < 0)
            {
                if (piece.transform.position == target && detectEnemies == true && piece.GetComponent<PieceLogic>().color != color && enemyDetected == false)
                {
                    enemyDetected = true;
                }
                if (piece.transform.position == target)
                {
                    return true;
                } // if its a piece of the same color
            }
            else if (piecetype > 0)
            {
                if (piece.transform.position == target && detectEnemies == true && piece.GetComponent<PieceLogic>().type < 0 && enemyDetected == false)
                {
                    enemyDetected = true;
                }
                if (piece.transform.position == target)
                {
                    return true;
                } // same for black
            }
        }
        return false;
    }
    void DestroyPiece()
    {
        GetComponent<SpriteRenderer>().color = Color.clear;
        transform.position = Vector3Int.down * 10;
    }
    void UpdateType()
    {
        var SpriteRen = GetComponent<SpriteRenderer>();
        if (type == 0) return;
        if (type == 1) SpriteRen.sprite = WhitePawn;
        if (type == -1) SpriteRen.sprite = BlackPawn;
        if (type == 2) SpriteRen.sprite = WhiteKnight;
        if (type == -2) SpriteRen.sprite = BlackKnight;
        if (type == 3) SpriteRen.sprite = WhiteBishop;
        if (type == -3) SpriteRen.sprite = BlackBishop;
        if (type == 4) SpriteRen.sprite = WhiteRook;
        if (type == -4) SpriteRen.sprite = BlackRook;
        if (type == 5) SpriteRen.sprite = WhiteQueen;
        if (type == -5) SpriteRen.sprite = BlackQueen;
        if (type == 6) SpriteRen.sprite = WhiteKing;
        if (type == -6) SpriteRen.sprite = BlackKing;
    }
}