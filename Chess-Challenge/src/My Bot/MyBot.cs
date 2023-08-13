using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    // readonly int[] pieceValues = { 0, 82, 337, 365, 477, 1025, 10000 };
    readonly int searchDepth = 5;
    Move moveToPlay;
    Dictionary<ulong , TTEntry> transTable = new();

    private readonly short[] pieceValues = { 82, 337, 365, 477, 1025, 20000, // Middlegame
                                     94, 281, 297, 512, 936, 20000}; // Endgame
    private readonly decimal[] PackedPestoTables = { 63746705523041458768562654720m, 71818693703096985528394040064m, 75532537544690978830456252672m, 75536154932036771593352371712m, 76774085526445040292133284352m, 3110608541636285947269332480m, 936945638387574698250991104m, 75531285965747665584902616832m, 77047302762000299964198997571m, 3730792265775293618620982364m, 3121489077029470166123295018m, 3747712412930601838683035969m, 3763381335243474116535455791m, 8067176012614548496052660822m, 4977175895537975520060507415m, 2475894077091727551177487608m, 2458978764687427073924784380m, 3718684080556872886692423941m, 4959037324412353051075877138m, 3135972447545098299460234261m, 4371494653131335197311645996m, 9624249097030609585804826662m, 9301461106541282841985626641m, 2793818196182115168911564530m, 77683174186957799541255830262m, 4660418590176711545920359433m, 4971145620211324499469864196m, 5608211711321183125202150414m, 5617883191736004891949734160m, 7150801075091790966455611144m, 5619082524459738931006868492m, 649197923531967450704711664m, 75809334407291469990832437230m, 78322691297526401047122740223m, 4348529951871323093202439165m, 4990460191572192980035045640m, 5597312470813537077508379404m, 4980755617409140165251173636m, 1890741055734852330174483975m, 76772801025035254361275759599m, 75502243563200070682362835182m, 78896921543467230670583692029m, 2489164206166677455700101373m, 4338830174078735659125311481m, 4960199192571758553533648130m, 3420013420025511569771334658m, 1557077491473974933188251927m, 77376040767919248347203368440m, 73949978050619586491881614568m, 77043619187199676893167803647m, 1212557245150259869494540530m, 3081561358716686153294085872m, 3392217589357453836837847030m, 1219782446916489227407330320m, 78580145051212187267589731866m, 75798434925965430405537592305m, 68369566912511282590874449920m, 72396532057599326246617936384m, 75186737388538008131054524416m, 77027917484951889231108827392m, 73655004947793353634062267392m, 76417372019396591550492896512m, 74568981255592060493492515584m, 70529879645288096380279255040m};
    private readonly int[][] UnpackedPestoTables;
    readonly int[] phase_weight = { 0, 1, 1, 2, 4, 0 };

    // Sidhant-Roymoulik's PeSTO unpacking
    public MyBot(){
        UnpackedPestoTables = new int[64][];
        for (int i = 0; i < 64; i++){
            int pieceType = 0;
            UnpackedPestoTables[i] = decimal.GetBits(PackedPestoTables[i]).Take(3)
                .SelectMany(c => BitConverter.GetBytes(c)
                    .Select((byte square) => (int)((sbyte)square * 1.461) + pieceValues[pieceType++]))
                .ToArray();
        }
    }

    int Evaluate(Board board){
        int midGameEval = 0, endGameEval = 0, phase = 0;
        foreach (bool sideToMove in new[] { true, false }){
            for (int piece = 0; piece < 6; piece++){
                ulong bitBoard = board.GetPieceBitboard((PieceType)(piece + 1), sideToMove);
                while (bitBoard != 0){
                    int squareIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref bitBoard) ^ (sideToMove ? 56 : 0);
                    midGameEval += UnpackedPestoTables[squareIndex][piece];
                    endGameEval += UnpackedPestoTables[squareIndex][piece + 6];
                    phase += phase_weight[piece];
                }
            }
            (midGameEval,endGameEval) = (-midGameEval, -endGameEval);
        }
        phase = Math.Min(phase, 24);
        return (midGameEval * phase + endGameEval * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }

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
        if (moves.Length == 0) return board.IsInCheck() ? -pieceValues[5] + (searchDepth - depth) : 0;

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
            move.IsCapture ? 100 * (int)move.CapturePieceType - (int)move.MovePieceType : 0).ToArray();
    }
}