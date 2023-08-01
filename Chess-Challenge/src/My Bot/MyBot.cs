using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    readonly int[] pieceValues = { 0, 1, 3, 3, 5, 9, 100 };
    readonly int alphaBetaLimit = 139;
    readonly TimeSpan timeout = TimeSpan.FromMilliseconds(1000);

    Dictionary<ulong, Tuple<int, int>> trans = new();
    private Move bestMove;
    int searchDepth;
    DateTime startTime;

    public Move Think(Board board, Timer timer)
    {
        startTime = DateTime.UtcNow;
        try{
            for (int i = 1; i < 10; i++){
                searchDepth = i;
                Search(board, searchDepth, -alphaBetaLimit, alphaBetaLimit);
            }
        }catch(Exception){
            Console.WriteLine("Level {0} Move: " + bestMove.ToString(),searchDepth-1);
        }
        
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

    int Search (Board board, int depth, int alpha, int beta){
        if (DateTime.UtcNow - startTime > timeout){throw new Exception("Search Halted");}
        if (depth == 0){
            return Quiesce(board, alpha, beta);
        }

        Move[] moves = board.GetLegalMoves();
        Random rng = new();
        int rngIndex = rng.Next(moves.Length);
        (moves[0], moves[rngIndex]) = (moves[rngIndex], moves[0]);

        if (moves.Length == 0){
            return 0;
        }

        Move currentBestMove = moves[0];

        foreach (Move move in moves){
            board.MakeMove(move);
            int score = 0;

            if (board.IsInCheckmate()){
                score += pieceValues[6];
            }else if (board.IsDraw()){
                score -= pieceValues[6]/2;
            }else{
                ulong zKey = board.ZobristKey;

                if (trans.TryGetValue(zKey, out var transValue) && transValue.Item2 >= depth){
                    score += transValue.Item1;
                }else{
                    int partScore = -Search(board, depth - 1, -beta, -alpha);
                    score += partScore;
                    trans[zKey] = Tuple.Create(partScore, depth);
                }

            }
            
            board.UndoMove(move);
            if (score >= beta){
                return beta;
            }

            if (score > alpha){
                alpha = score;
                currentBestMove = move;
            }
        }
        if (depth == searchDepth){
            this.bestMove = currentBestMove;
        }
        return alpha;
    }


    int Quiesce(Board board, int alpha, int beta) {
        if (DateTime.UtcNow - startTime > timeout){throw new Exception("Search Halted");}
        int stand_pat = Evaluate(board);
        if (stand_pat >= beta)
            return beta;
        if (alpha < stand_pat)
            alpha = stand_pat;

        Move[] captures = board.GetLegalMoves(true);
        foreach (Move capture in captures) {
            board.MakeMove(capture);
            int score = -Quiesce(board, -beta, -alpha);
            board.UndoMove(capture);

            if (score >= beta)
                return beta;
            if (score > alpha)
            alpha = score;
        }
        return alpha;
    }
}