using System;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    readonly int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        (_, Move bestMove) = Search(board,4);
        return bestMove;
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

    private Tuple<int,Move> Search (Board board, int depth){
        if (depth == 0){
            return new Tuple<int, Move>(Evaluate(board), Move.NullMove);
        }

        if (board.IsInCheckmate()){
            return new Tuple<int, Move>(int.MinValue, Move.NullMove);
        }

        Move[] moves = board.GetLegalMoves();

        if (moves.Length == 0){
            return new Tuple<int, Move>(0, Move.NullMove);
        }

        int bestEval = int.MinValue;
        Random rng = new();
        Move bestMove = moves[rng.Next(moves.Length)];

        foreach (Move move in moves){
            board.MakeMove(move);
            (int eval, Move temp) = Search(board, depth - 1);
            eval = -eval;
            if (eval > bestEval){
                bestEval = eval;
                bestMove = move;
            }
            board.UndoMove(move);
        }
        return new Tuple<int, Move>(bestEval,bestMove);
    }
}