using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    readonly int[] pieceValues = { 0, 10, 30, 30, 50, 90, 1000 };
    readonly int searchDepth = 6;
    Move moveToPlay;
    public Move Think(Board board, Timer timer)
    {
        moveToPlay = Move.NullMove;
        int score = Search(board, -int.MaxValue, int.MaxValue, searchDepth);
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

    int Search (Board board, int alpha, int beta, int depth){
        int bestScore = -int.MaxValue;
        Move bestMove = Move.NullMove;

        if (depth == 0){
            bestScore = Evaluate(board);
            if (bestScore >= beta) return bestScore;
            alpha = Math.Max(alpha, bestScore);
            return alpha;
        }

        Move[] moves = board.GetLegalMoves();
        for(int i=0; i<moves.Length; i++){
            board.MakeMove(moves[i]);
            int score = -Search(board, -beta, -alpha, depth - 1);
            board.UndoMove(moves[i]);
            if (score > bestScore){
                bestScore = score;
                bestMove = moves[i];
                alpha = Math.Max(alpha, score);
                if (alpha >= beta) break;
            }
        }
        if (moves.Length == 0) return board.IsInCheck() ? -pieceValues[6] + (searchDepth - depth) : 0;
        if (depth == searchDepth) moveToPlay = bestMove;
        return bestScore;
    }
}