[GitHub - NitishNaineni/Chess-Challenge](https://github.com/NitishNaineni/Chess-Challenge)

## Description
[![Alt text](https://img.youtube.com/vi/iScy18pVR58/maxresdefault.jpg)](https://www.youtube.com/watch?v=iScy18pVR58)

This project was done as a submission to Sebastian Lague’s Chess Coding Challenge (C#). The goal of the competition is for the participants to make a small chess bot with the rules of the challenge and bid them against one another to see which is better. Given the constraints of the challenge, the only feasible solutions was an algorithmic implementation. The resources used for this challenge have been cited at the end.

### **Rules**

- You may participate alone, or in a group of any size.
- You may submit a maximum of two entries.
    - Please only submit a second entry if it is significantly different from your first bot (not just a minor tweak).
    - Note: you will need to log in with a second Google account if you want submit a second entry.
- Only the following namespaces are allowed:
    - `ChessChallenge.API`
    - `System`
    - `System.Numerics`
    - `System.Collections.Generic`
    - `System.Linq`
        - You may not use the `AsParallel()` function
- As implied by the allowed namespaces, you may not read data from a
file or access the internet, nor may you create any new threads or tasks to run code in parallel/in the background.
- You may not use the unsafe keyword.
- You may not store data inside the name of a variable/function/class etc (to be extracted with `nameof()`, `GetType().ToString()`, `Environment.StackTrace` and so on). Thank you to [#12](https://github.com/SebLague/Chess-Challenge/issues/12) and [#24](https://github.com/SebLague/Chess-Challenge/issues/24).
- If your bot makes an illegal move or runs out of time, it will lose the game.
    - Games are played with 1 minute per side by default (this can be
    changed in the settings class). The final tournament time control is
    TBD, so your bot should not assume a particular time control, and
    instead respect the amount of time left on the timer (given in the Think function).
- Your bot may not use more than 256mb of memory for creating look-up tables (such as a transposition table).
- If you have added a constructor to MyBot (for generating look up
tables, etc.) it may not take longer than 5 seconds to complete.
- All of your code/data must be contained within the *MyBot.cs* file.
    - Note: you may create additional scripts for testing/training your bot, but only the *MyBot.cs* file will be submitted, so it must be able to run without them.
    - You may not rename the *MyBot* struct or *Think* function contained in the *MyBot.cs* file.
    - The code in MyBot.cs may not exceed the *bot brain capacity* of 1024 (see below).

Generally, the brains behind and chess algorithm can be divided to two halves, Search and Evaluation.

## Search

Chess a zero-sum board game, and we need a way to look through the search space efficiently to decide on the best move. Searching involves looking ahead at different move sequences and evaluating the positions after making the moves. While there are several ways to implement such an algorithm, given the rules, the most promising implementation is using a min max algorithm using alpha beta pruning and several enhancements discussed below.

- [Iterative Deepening](https://www.chessprogramming.org/Iterative_Deepening)
- [Aspiration Windows](https://www.chessprogramming.org/Aspiration_Windows)
- [Principal Variation Search](https://www.chessprogramming.org/Principal_Variation_Search)
- [Transposition Table](https://www.chessprogramming.org/Transposition_Table)
- [Move Ordering](https://www.chessprogramming.org/Move_Ordering)
    - [History Heuristic](https://www.chessprogramming.org/History_Heuristic)
    - [MVV/LVA (**M**ost **V**aluable **V**ictim - **L**east **V**aluable **A**ggressor)](https://www.chessprogramming.org/MVV-LVA)
- [Selectivity](https://www.chessprogramming.org/Selectivity)
    - [Extensions](https://www.chessprogramming.org/Extensions)
        - [Check Extensions](https://www.chessprogramming.org/Check_Extensions)
    - [Pruning](https://www.chessprogramming.org/Pruning)
        - [Futility Pruning](https://www.chessprogramming.org/Futility_Pruning)
        - [Null Move Pruning](https://www.chessprogramming.org/Null_Move_Pruning)
        - [Delta Pruning](https://www.chessprogramming.org/Delta_Pruning)
    - [Reductions](https://www.chessprogramming.org/Reductions)
        - [Late Move Reductions](https://www.chessprogramming.org/Late_Move_Reductions)
    - [Quiescence Search](https://www.chessprogramming.org/Quiescence_Search)

## Evaluation

In search, we traverse through different move sequences and if we could reach the end of the game in all these sequences the evaluation would only have values of -1 (loss), 0 (draw), and 1 (win). But it isn't feasible to search the complete tree, and we need a way to approximate an evaluation to compare positions and choose the best moves. In recent times this operation has been taken over by Neural networks but given the constraints of the challenge, we are going to go with a hand-crafted evaluation function which consists of the following.

- [Tapered Eval](https://www.chessprogramming.org/Tapered_Eval)
- [Material](https://www.chessprogramming.org/Material)
    - [Point Values](https://www.chessprogramming.org/Point_Value)
        - [Midgame](https://www.chessprogramming.org/Middlegame): 82, 337, 365, 477, 1025
        - [Endgame](https://www.chessprogramming.org/Endgame): 94, 281, 297, 512, 936
- [Piece-Square Tables](https://www.chessprogramming.org/Piece-Square_Tables)
- [Mobility](https://www.chessprogramming.org/Mobility)s

## MyBot.cs

```csharp
using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

public class MyBot : IChessBot
{
    Board board; Timer timer; Move moveToPlay;
    record struct TTEntry(ulong zKey, Move move, int depth, int score, int bound);
    TTEntry[] transTable = new TTEntry[0x400000];
    int[,,] historyTable;
    // Piece values: pawn, knight, bishop, rook, queen, king
    private readonly short[] pieceValues = {82, 337, 365, 477, 1025, 20000,  // Middlegame
                                            94, 281, 297, 512, 936,  20000}; // Endgame
    private readonly decimal[] PackedPestoTables = { 63746705523041458768562654720m, 71818693703096985528394040064m, 75532537544690978830456252672m, 75536154932036771593352371712m, 76774085526445040292133284352m, 3110608541636285947269332480m, 936945638387574698250991104m, 75531285965747665584902616832m, 77047302762000299964198997571m, 3730792265775293618620982364m, 3121489077029470166123295018m, 3747712412930601838683035969m, 3763381335243474116535455791m, 8067176012614548496052660822m, 4977175895537975520060507415m, 2475894077091727551177487608m, 2458978764687427073924784380m, 3718684080556872886692423941m, 4959037324412353051075877138m, 3135972447545098299460234261m, 4371494653131335197311645996m, 9624249097030609585804826662m, 9301461106541282841985626641m, 2793818196182115168911564530m, 77683174186957799541255830262m, 4660418590176711545920359433m, 4971145620211324499469864196m, 5608211711321183125202150414m, 5617883191736004891949734160m, 7150801075091790966455611144m, 5619082524459738931006868492m, 649197923531967450704711664m, 75809334407291469990832437230m, 78322691297526401047122740223m, 4348529951871323093202439165m, 4990460191572192980035045640m, 5597312470813537077508379404m, 4980755617409140165251173636m, 1890741055734852330174483975m, 76772801025035254361275759599m, 75502243563200070682362835182m, 78896921543467230670583692029m, 2489164206166677455700101373m, 4338830174078735659125311481m, 4960199192571758553533648130m, 3420013420025511569771334658m, 1557077491473974933188251927m, 77376040767919248347203368440m, 73949978050619586491881614568m, 77043619187199676893167803647m, 1212557245150259869494540530m, 3081561358716686153294085872m, 3392217589357453836837847030m, 1219782446916489227407330320m, 78580145051212187267589731866m, 75798434925965430405537592305m, 68369566912511282590874449920m, 72396532057599326246617936384m, 75186737388538008131054524416m, 77027917484951889231108827392m, 73655004947793353634062267392m, 76417372019396591550492896512m, 74568981255592060493492515584m, 70529879645288096380279255040m};
    private readonly int[][] UnpackedPestoTables;
    readonly int[] phase_weight = { 0, 1, 1, 2, 4, 0 };
    int maxValue = 100000, nodes;

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

    public Move Think(Board _Board, Timer _Timer){
        board = _Board; timer = _Timer;
        historyTable = new int[2, 7, 64];
        // Iterative Deepening
        for (int depth = 1; depth <= 6; depth++){
            nodes = 0;
            Search(-maxValue, maxValue, depth, 0);
            Console.WriteLine("depth\t{0} nodes\t{1}",depth,nodes);
        }
        return moveToPlay;
    }

    int Evaluate(){
        int midGameEval = 0, endGameEval = 0, phase = 0, mobility;
        foreach (bool sideToMove in new[] { true, false }){
            for (int piece = 0; piece < 6; piece++){
                ulong bitBoard = board.GetPieceBitboard((PieceType)(piece + 1), sideToMove);
                while (bitBoard != 0){
                    int squareIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref bitBoard) ^ (sideToMove ? 56 : 0);
                    mobility = CountBits(BitboardHelper.GetPieceAttacks((PieceType)(piece + 1),new Square(squareIndex), board, sideToMove));
                    midGameEval += UnpackedPestoTables[squareIndex][piece] + mobility;
                    endGameEval += UnpackedPestoTables[squareIndex][piece + 6] + mobility;
                    phase += phase_weight[piece];
                }
            }
            midGameEval = -midGameEval;
            endGameEval = -endGameEval;
        }
        phase = Math.Min(phase, 24);
        return (midGameEval * phase + endGameEval * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }

    int CountBits(ulong n) {
        int count = 0;
        while (n > 0){
            n &= n - 1;
            count++;
        }
        return count;
    }

    int Search(int alpha, int beta, int depth, int ply)
    {
        nodes++;
        
        bool firstMove = true, root = ply == 0 , quiesce = depth < 1, inCheck = board.IsInCheck(), isWhite = board.IsWhiteToMove;
        int origAlpha = alpha, bestScore = -maxValue, score, turn = isWhite? 1 : 0;
        ulong zKey = board.ZobristKey;

        if (!root && board.IsRepeatedPosition()) return 0;

        TTEntry entry = transTable[zKey & 0x3FFFFF];
        if (entry.zKey == zKey  && !root && entry.depth >= depth &&
        ( entry.bound == 0 || (entry.bound == 1 && entry.score >= beta) || (entry.bound == 2 && entry.score <= alpha))
        ) return entry.score;

        // Extensions
        if(!quiesce && inCheck) depth++;
        
        // Quiescence Search
        if (quiesce){
            bestScore = Evaluate();
            if (bestScore >= beta) return beta;
            alpha = Math.Max(alpha,bestScore);
        }

        // Null move pruning
        if (!quiesce && !root && !inCheck && depth > 2){
            board.TrySkipTurn();
            int nullScore = -Search(-beta, -beta+1, Math.Max(2,depth - 3), ply+1);
            board.UndoSkipTurn();
            if (nullScore >= beta) return beta;
        }

        // Move Ordering
        Move[] moves = board.GetLegalMoves(quiesce && !inCheck).OrderByDescending(
            move => 
                move.Equals(entry.move)? maxValue :
                move.IsCapture ? 100 * (int)move.CapturePieceType - (int)move.MovePieceType :
                historyTable[turn, (int)move.MovePieceType, move.TargetSquare.Index]
        ).ToArray();

        if (!quiesce && moves.Length == 0) return board.IsInCheck() ? -pieceValues[5] + ply : 0;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            // Principal Variation Search
            int PVS(int newAlpha) => -Search(newAlpha, -alpha, depth - 1, ply + 1);
            score = PVS(firstMove ? -beta : -alpha - 1);
            if (!firstMove && score > alpha)
            {
                alpha = Math.Max(alpha, score);
                if (score < beta) score = PVS(-beta);
            }

            board.UndoMove(move);

            if (score > bestScore){
                (bestScore, alpha) = (score, Math.Max(alpha, score));
                if(root) moveToPlay = move;
                if (alpha >= beta){
                    if (!move.IsCapture) historyTable[turn, (int)move.MovePieceType, move.TargetSquare.Index] += depth * depth;
                    break;
                }
            }
            firstMove = false;
        }

        if (!quiesce && entry.depth < depth) transTable[zKey & 0x3FFFFF] = new TTEntry(zKey, moveToPlay, depth, bestScore, bestScore >= beta ? 1 : bestScore > origAlpha ? 0 : 2);
        
        return bestScore;
    }
}
```

## References

[Coding Adventure: Chess](https://youtu.be/U4ogK0MIzqk)

[Coding Adventure: Making a Better Chess Bot](https://youtu.be/_vqlIPDR2TU)

[Chessprogramming wiki](https://www.chessprogramming.org/Main_Page)

[](https://seblague.github.io/chess-coding-challenge/documentation/)

[Join the Chess Engine Coding Discord Server!](https://discord.gg/zZsdqKx2aR)