using System;
using System.ComponentModel.DataAnnotations;
using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class MyBot : IChessBot
{
    readonly int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    readonly int searchDepth = 4;

    public Move Think(Board board, Timer timer)
    {
        (_, Move moveToPlay) = Search(board, searchDepth);
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

    Tuple<int, Move> Search (Board board, int depth){
        if (depth == 0){
            return new Tuple<int, Move>(Evaluate(board),Move.NullMove);
        }

        Move[] moves = board.GetLegalMoves();

        if (moves.Length == 0){
            return new Tuple<int, Move>(0, Move.NullMove);
        }

        int bestScore = int.MinValue;
        // Random rng = new();
        // Move bestMove = moves[rng.Next(moves.Length)];
        Move bestMove = moves[0];

        foreach (Move move in moves){
            board.MakeMove(move);

            if (board.IsInCheckmate()){
                bestScore = int.MaxValue;
                bestMove = move;
                board.UndoMove(move);
                break;
            }
            
            (int score, _) = Search(board, depth - 1);
            score = -score;
            if (score > bestScore){
                bestScore = score;
                bestMove =  move;
            }
            board.UndoMove(move);
        }
        return new Tuple<int, Move>(bestScore, bestMove);
    }
}