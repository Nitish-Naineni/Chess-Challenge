using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    readonly int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        (int score, Move moveToPlay) = Search(board, 7, -int.MaxValue, int.MaxValue);
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
        if (depth == 0){return (Evaluate(board), Move.NullMove);}
        Move[] moves = board.GetLegalMoves();
        if (moves.Length == 0){return (0, Move.NullMove);}
        Random rng = new();
        Move bestMove = moves[rng.Next(moves.Length)];

        for (int i = 0; i < moves.Length; i++){
            board.MakeMove(moves[i]);
            int score = board.IsInCheckmate() ? pieceValues[6] : -Search(board, depth - 1, -beta, -alpha).Item1;
            board.UndoMove(moves[i]);

            if (score >= beta){return (beta,Move.NullMove);}
            if (score > alpha){(alpha, bestMove) = (score, moves[i]);}
        }
        return (alpha, bestMove);
    }
}