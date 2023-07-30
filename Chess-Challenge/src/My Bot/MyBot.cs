using System;
using System.ComponentModel.DataAnnotations;
using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class MyBot : IChessBot
{
    readonly int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    readonly int searchDepth = 4;

    private Move bestMove;

    public Move Think(Board board, Timer timer)
    {
        Search(board, searchDepth);
        return this.bestMove;

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

    int Search (Board board, int depth){
        if (depth == 0){
            return Evaluate(board);
        }

        Move[] moves = board.GetLegalMoves();

        if (moves.Length == 0){
            return 0;
        }

        int bestScore = int.MinValue;

        foreach (Move move in moves){
            board.MakeMove(move);

            if (board.IsInCheckmate()){
                bestScore = int.MaxValue;
                if (depth == searchDepth){
                    this.bestMove = move;
                }
                board.UndoMove(move);
                break;
            }
            
            int score = -Search(board, depth - 1);
            board.UndoMove(move);


            if (score > bestScore){
                bestScore = score;
                if (depth == searchDepth){
                    this.bestMove = move;
                }
            }
        }
        return bestScore;
    }
}