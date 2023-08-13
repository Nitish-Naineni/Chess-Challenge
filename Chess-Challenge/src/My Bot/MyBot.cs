using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    readonly int[] pieceValues = { 0, 10, 30, 30, 50, 90, 1000 };
    readonly int searchDepth = 5;
    Move moveToPlay;
    Dictionary<ulong , TTEntry> transTable = new();

    struct TTEntry{
		public Move move;
		public int depth, score, bound;
		public TTEntry(Move _move, int _depth, int _score, int _bound){
			move = _move; depth = _depth; score = _score; bound = _bound;
		}
	}

    public Move Think(Board board, Timer timer){
        moveToPlay = Move.NullMove;
        Search(board, -int.MaxValue, int.MaxValue, searchDepth);
        return moveToPlay;
    }

    private int Evaluate(Board board)
    {
        int materialScore = board.GetAllPieceLists().Sum(pieces => 
            pieces.Count * pieceValues[(int)pieces.TypeOfPieceInList] * (board.IsWhiteToMove == pieces.IsWhitePieceList ? 1 : -1));
        
        int mobilityScore = board.GetLegalMoves().Length;
        board.ForceSkipTurn();
        mobilityScore -= board.GetLegalMoves().Length;
        board.UndoSkipTurn();

        return materialScore + mobilityScore;
    }

    int Search(Board board, int alpha, int beta, int depth)
    {
        int origAlpha = alpha;
        bool firstMove = true;

        if (searchDepth != depth && board.IsRepeatedPosition()) return 0;

        if (transTable.TryGetValue(board.ZobristKey, out TTEntry entry) && entry.depth >= depth && (
            entry.bound == 3 ||
            (entry.bound == 2 && entry.score >= beta) ||
            (entry.bound == 1 && entry.score <= alpha)))
        {
            moveToPlay = entry.move;
            return entry.score;
        }

        if (depth <= 0){
            int score = Evaluate(board);
            if (score >= beta) return score;
            alpha = Math.Max(alpha, score);
            return alpha;
        }

        Move[] moves = board.GetLegalMoves();
        if (moves.Length == 0) return board.IsInCheck() ? -pieceValues[6] + (searchDepth - depth) : 0;

        int[] moveScores = GetMoveScores(moves, entry.move);

        int bestScore = -int.MaxValue;
        Move bestMove = Move.NullMove;

        for (int i = 0; i < moves.Length; i++)
        {
            for (int j = i + 1; j < moves.Length; j++)
                if (moveScores[j] > moveScores[i])
                    (moveScores[i], moveScores[j], moves[i], moves[j]) = (moveScores[j], moveScores[i], moves[j], moves[i]);

            board.MakeMove(moves[i]);
            int score = -Search(board, firstMove ? -beta : -alpha - 1, -alpha, depth - 1);
            if (!firstMove && score > alpha)
            {
                alpha = Math.Max(alpha, score);
                if (score < beta) score = -Search(board, -beta, -alpha, depth - 1);
            }
            board.UndoMove(moves[i]);

            if (score > bestScore)
            {
                (bestScore, bestMove, alpha) = (score, moves[i], Math.Max(alpha, score));
                if (alpha >= beta) break;
            }
            firstMove = false;
        }

        if (depth == searchDepth) moveToPlay = bestMove;
        if (entry.depth < depth) transTable[board.ZobristKey] = new TTEntry(bestMove, depth, bestScore, bestScore >= beta ? 2 : bestScore > origAlpha ? 3 : 1);
        
        return bestScore;
    }

    int[] GetMoveScores(Move[] moves, Move lastBestMove)
    {
        return moves.Select(move => 
            move == lastBestMove ? int.MaxValue : 
            move.IsCapture ? 100 * pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType] : 
            0).ToArray();
    }
}