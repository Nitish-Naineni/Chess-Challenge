using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    readonly int[] pieceValues = { 0, 10, 30, 30, 50, 90, 1000 };
    readonly int searchDepth = 4;
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
        int score = Search(board, -int.MaxValue, int.MaxValue, searchDepth);
        return moveToPlay;
    }

    private int Evaluate(Board board){
        int materialScore  = 0;
        PieceList[] allPieces = board.GetAllPieceLists();
        foreach (PieceList Pieces in allPieces){
            int value = Pieces.Count * pieceValues[(int)Pieces.TypeOfPieceInList];
            materialScore  += board.IsWhiteToMove == Pieces.IsWhitePieceList ? value : -value;
        }
        int mobilityScore = board.GetLegalMoves().Length;
        board.ForceSkipTurn();
        mobilityScore -= board.GetLegalMoves().Length;
        board.UndoSkipTurn();


        return materialScore + mobilityScore * 1;
    }

    int Search (Board board, int alpha, int beta, int depth){
        int bestScore = -int.MaxValue;
        Move bestMove = Move.NullMove;
        int origAlpha = alpha;
        bool firstMove = true;
        if (searchDepth != depth && board.IsRepeatedPosition()){return 0;}
        TTEntry entry = new();
        if(transTable.ContainsKey(board.ZobristKey)){
            entry = transTable[board.ZobristKey];
            if (entry.depth >= depth && (
                entry.bound == 3 ||
                (entry.bound == 2 && entry.score >= beta) ||
                (entry.bound == 1 && entry.score <= alpha)
            )){
                moveToPlay = entry.move;
                return entry.score;
            }
        }

        if (depth <= 0){
            bestScore = Evaluate(board);
            if (bestScore >= beta) return bestScore;
            alpha = Math.Max(alpha, bestScore);
            return alpha;
        }

        Move[] moves = board.GetLegalMoves();
        int[] moveScores = GetMoveScores(moves, entry.move);
        
        for(int i=0; i<moves.Length; i++){
            int score;
            for (int j = i + 1; j < moves.Length; j++){
				if (moveScores[j] > moveScores[i]){
                    (moveScores[i], moveScores[j], moves[i], moves[j]) = (moveScores[j], moveScores[i], moves[j], moves[i]);
                }
			}
            board.MakeMove(moves[i]);
            if (firstMove){
                firstMove = false;
                score = -Search(board, -beta, -alpha, depth - 1);
            }else{
                score = -Search(board, -alpha-1, -alpha, depth - 1);
                if( score > alpha && score < beta ) {
                    score = -Search(board, -beta, -alpha, depth-1);
                    if( score > alpha ){alpha = score;}
                }
            }
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
        int bound = bestScore >= beta ? 2 : bestScore > origAlpha ? 3 : 1;
        if (entry.depth < depth) transTable[board.ZobristKey] = new TTEntry(bestMove, depth, bestScore, bound);
        return bestScore;
    }

    int[] GetMoveScores(Move[] moves, Move lastBestMove){
        int[] moveScores = new int[moves.Length];
        for (int i = 0; i < moves.Length ; i++){
            if (moves[i] == lastBestMove){
                moveScores[i] = int.MaxValue;
            }else if (moves[i].IsCapture){
                moveScores[i] = 100 * (int)moves[i].CapturePieceType - (int)moves[i].MovePieceType;
            } 
        }
        return moveScores;
    }
}