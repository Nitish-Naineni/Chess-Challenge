using System;
using System.Collections.Generic;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    readonly int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    readonly TimeSpan timeout = TimeSpan.FromMilliseconds(500);
    Dictionary<ulong, (int, int, Move)> trans = new();
    Dictionary<ulong, Move[]> moveHistroy;
    DateTime startTime;


    public Move Think(Board board, Timer timer)
    {
        int score;
        int bestSearchDepth = 0;
        Move moveToPlay = new();
        moveHistroy = new();
        startTime = DateTime.UtcNow;

        try{
            for (int i = 1; i < 10; i++){
                bestSearchDepth = i;
                (score, moveToPlay) = Search(board, i, -13900, 13900);
            }
        }catch(Exception){
            Console.WriteLine("Level {0} Move: " + moveToPlay.ToString(),bestSearchDepth-1);
        }
        
        return moveToPlay;
    }

    private int Evaluate(Board board)
    {
        int totalValue = 0;
        PieceList[] allPieces = board.GetAllPieceLists();
        foreach (PieceList Pieces in allPieces){
            int value = Pieces.Count * pieceValues[(int)Pieces.TypeOfPieceInList];
            totalValue += board.IsWhiteToMove == Pieces.IsWhitePieceList ? value : -value;
        }

        return totalValue;
    }

    (int,Move) Search (Board board, int depth, int alpha, int beta){
        if (DateTime.UtcNow - startTime > timeout){throw new Exception("Search Halted");}

        ulong zKey = board.ZobristKey;
        if (trans.ContainsKey(zKey) && trans[zKey].Item2 >= depth){return (trans[zKey].Item1, trans[zKey].Item3);}
        if (depth == 0){return (Quiesce(board, alpha, beta), Move.NullMove);}

        Move[] moves = moveHistroy.ContainsKey(zKey) ? moveHistroy[zKey] : OrderMoves(board.GetLegalMoves());
        if (moves.Length == 0 || board.IsDraw()){return (0, Move.NullMove);}

        int[] moveScores = new int[moves.Length];
        for (int i = 0; i < moveScores.Length; i++){moveScores[i] = int.MinValue;}

        Random rng = new();
        Move bestMove = moves[rng.Next(moves.Length)];

        for (int i = 0; i < moves.Length; i++){
            board.MakeMove(moves[i]);
            int score = board.IsInCheckmate() ? pieceValues[6] : -Search(board, depth - 1, -beta, -alpha).Item1;
            moveScores[i] = score;
            board.UndoMove(moves[i]);

            if (score >= beta){return (beta,Move.NullMove);}
            if (score > alpha){(alpha, bestMove) = (score, moves[i]);}
        }
        Array.Sort(moveScores, moves);
        Array.Reverse(moves);
        moveHistroy[zKey] = moves;

        trans[zKey] = (alpha, depth, bestMove);
        return (alpha, bestMove);
    }


    int Quiesce(Board board, int alpha, int beta) {
        if (DateTime.UtcNow - startTime > timeout){throw new Exception("Search Halted");}

        int stand_pat = Evaluate(board);
        if (stand_pat >= beta){return beta;}
        if (alpha < stand_pat){alpha = stand_pat;}

        Move[] captures = board.GetLegalMoves(true);
        foreach (Move capture in captures) {
            board.MakeMove(capture);
            int score = -Quiesce(board, -beta, -alpha);
            board.UndoMove(capture);

            if (score >= beta){return beta;}
            if (score > alpha){alpha = score;}
        }
        return alpha;
    }

    Move[] OrderMoves(Move[] moves){
        int[] moveScores = new int[moves.Length];
        for (int i = 0; i < moves.Length; i++){
            if (moves[i].IsCapture){moveScores[i] = pieceValues[(int)moves[i].CapturePieceType] - pieceValues[(int)moves[i].MovePieceType];}
        }
        Array.Sort(moveScores, moves);
        Array.Reverse(moves);
        return moves;
    }
}