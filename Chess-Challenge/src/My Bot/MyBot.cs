using System;
using System.ComponentModel.DataAnnotations;
using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    readonly int[] pieceValues = { 0, 1, 3, 3, 5, 9, 100 };
    readonly int searchDepth = 4;
    // readonly int alphaBetaLimit = 1

    private Move bestMove;

    public Move Think(Board board, Timer timer)
    {
        Search(board, searchDepth, -1000, 1000);
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

    int Search (Board board, int depth, int alpha, int beta){
        if (depth == 0){
            return Evaluate(board);
        }

        Move[] moves = board.GetLegalMoves();

        if (moves.Length == 0){
            return 0;
        }

        foreach (Move move in moves){
            board.MakeMove(move);
            int score = 0;

            if (board.IsInCheckmate()){
                score += pieceValues[6];
            }else{
                score += -Search(board, depth - 1, -beta, -alpha);
            }
            
            board.UndoMove(move);
            if (score >= beta){
                return beta;
            }

            if (score > alpha){
                alpha = score;
                if (depth == searchDepth){
                    this.bestMove = move;
                }

            }
        }
        return alpha;
    }
}