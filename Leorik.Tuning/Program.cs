﻿using Leorik.Core;
using Leorik.Tuning;
using System.Diagnostics;
using System.Globalization;

float MSE_SCALING = 100;
int ITERATIONS = 10;
int MATERIAL_ALPHA = 1000;
int PHASE_ALPHA = 100;
int MATERIAL_BATCH = 100;
int PHASE_BATCH = 5;


//https://www.desmos.com/calculator/k7qsivwcdc
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine(" Leorik Tuning v16 ");
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine();

ReplKingSafety();
//ReplCurves();

List<Data> data = LoadData("data/quiet-labeled.epd");
//RenderFeatures(data);

//MSE_SCALING = Tuner.Minimize((k) => Tuner.MeanSquareError(data, k), 1, 1000);
TestLeorikMSE();

float[] cPhase = PhaseTuner.GetLeorikPhaseCoefficients();
float[] cFeatures = FeatureTuner.GetLeorikCoefficients();

//float[] cPhase = PhaseTuner.GetUntrainedCoefficients();
//float[] cFeatures = FeatureTuner.GetUntrainedCoefficients();

Console.WriteLine($"Preparing TuningData for {data.Count} positions");
long t0 = Stopwatch.GetTimestamp();
List<TuningData> tuningData = new(data.Count);
foreach (Data entry in data)
{
    var td = Tuner.GetTuningData(entry, cPhase, cFeatures);
    tuningData.Add(td);
}
long t1 = Stopwatch.GetTimestamp();
Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
Tuner.ValidateConsistency(tuningData, cPhase, cFeatures);
Console.WriteLine();

TestMaterialMSE(cFeatures);
TestPhaseMSE(cPhase);

t0 = Stopwatch.GetTimestamp();
for (int it = 0; it < ITERATIONS; it++)
{
    Console.WriteLine($"{it}/{ITERATIONS} ");
    TunePhaseBatch(PHASE_BATCH, PHASE_ALPHA);
    TuneMaterialBatch(MATERIAL_BATCH, MATERIAL_ALPHA);
    Tuner.ValidateConsistency(tuningData, cPhase, cFeatures);
}
t1 = Stopwatch.GetTimestamp();
Console.WriteLine($"Tuning took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");

PrintCoefficients(cFeatures);

Console.ReadKey();
/*
* 
* FUNCTIONS 
* 
*/

void TuneMaterialBatch(int batchSize, float alpha)
{
    double msePre = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
    Console.Write($"  Material MSE={msePre:N12} Alpha={alpha,5} ");
    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < batchSize; i++)
    {
        FeatureTuner.MinimizeParallel(tuningData, cFeatures, MSE_SCALING, alpha);
    }
    Tuner.SyncFeaturesChanges(tuningData, cFeatures);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
    Console.WriteLine($"Delta={msePre - msePost:N10} Time={(t_1 - t_0) / (double)Stopwatch.Frequency:0.###}s");
}

void TunePhaseBatch(int batchSize, float alpha)
{
    double msePre = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($"     Phase MSE={msePre:N12} Alpha={alpha,5} ");
    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < batchSize; i++)
    {
        PhaseTuner.MinimizeParallel(tuningData, cPhase, MSE_SCALING, alpha);
    }
    Tuner.SyncPhaseChanges(tuningData, cPhase, true);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($"Delta={msePre - msePost:N10} Time={(t_1 - t_0) / (double)Stopwatch.Frequency:0.###}s ");
    PhaseTuner.Report(cPhase);
}

void TestLeorikMSE()
{
    long t0 = Stopwatch.GetTimestamp();
    double mse = Tuner.MeanSquareError(data, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"Leorik's MSE(data) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
    Console.WriteLine();
}

void TestMaterialMSE(float[] coefficients)
{
    long t0 = Stopwatch.GetTimestamp();
    double mse = FeatureTuner.MeanSquareError(tuningData, coefficients, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"MSE(cFeatures) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
    Console.WriteLine();
}

void TestPhaseMSE(float[] coefficients)
{
    long t0 = Stopwatch.GetTimestamp();
    double mse = PhaseTuner.MeanSquareError(tuningData, coefficients, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"MSE(cPhase) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
    Console.WriteLine();
}

List<Data> LoadData(string epdFile)
{
    List<Data> data = new List<Data>();
    Console.WriteLine($"Loading DATA from '{epdFile}'");
    var file = File.OpenText(epdFile);
    while (!file.EndOfStream)
        data.Add(Tuner.ParseEntry(file.ReadLine()));

    Console.WriteLine($"{data.Count} labeled positions loaded!");
    return data;
}

void PrintCoefficients(float[] coefficients)
{
    Console.WriteLine("MIDGAME");
    for (int i = 0; i < 6; i++)
        WriteTable(i * 128, 2, coefficients);
    
    Console.WriteLine("ENDGAME");
    for (int i = 0; i < 6; i++)
        WriteTable(i * 128 + 1, 2, coefficients);

    //Console.WriteLine("Mobility - MG");
    //WriteMobilityTable(6 * 128, 2, coefficients);
    //Console.WriteLine("Mobility - EG");
    //WriteMobilityTable(6 * 128 + 1, 2, coefficients);

    Console.WriteLine("KingSafety - MG");
    KingSafetyTuner.Report(6 * 128, 2, coefficients);
    Console.WriteLine("KingSafety - EG");
    KingSafetyTuner.Report(6 * 128 + 1, 2, coefficients);
    Console.WriteLine();
    Console.WriteLine("Phase");
    PhaseTuner.Report(cPhase);

    double mse = FeatureTuner.MeanSquareError(tuningData, coefficients, MSE_SCALING);
    Console.WriteLine($"MSE(cFeatures) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
}

void WriteMobilityTable(int offset, int step, float[] coefficients)
{
    MobilityTuner.Report(Piece.Pawn, offset, step, coefficients);
    MobilityTuner.Report(Piece.Knight, offset, step, coefficients);
    MobilityTuner.Report(Piece.Bishop, offset, step, coefficients);
    MobilityTuner.Report(Piece.Rook, offset, step, coefficients);
    MobilityTuner.Report(Piece.Queen, offset, step, coefficients);
    MobilityTuner.Report(Piece.King, offset, step, coefficients);
    Console.WriteLine();
}


void WriteTable(int offset, int step, float[] coefficients)
{
    for (int i = 0; i < 8; i++)
    {
        for (int j = 0; j < 8; j++)
        {
            int k = offset + 8 * i * step + j * step;
            float c = (k < coefficients.Length) ? coefficients[k] : 0;
            Console.Write($"{(int)Math.Round(c),5},");
        }
        Console.WriteLine();
    }
    Console.WriteLine();
}

void PrintBitboard(ulong bits)
{
    for (int i = 0; i < 8; i++)
    {
        for (int j = 0; j < 8; j++)
        {
            int sq = (7-i) * 8 + j;
            bool bit = (bits & (1UL << sq)) != 0;
            Console.Write(bit ? "O " : "- ");
        }
        Console.WriteLine();
    }
}

void PrintPosition(BoardState board)
{
    Console.WriteLine("   A B C D E F G H");
    Console.WriteLine(" .----------------.");
    for (int rank = 7; rank >= 0; rank--)
    {
        Console.Write($"{rank + 1}|"); //ranks aren't zero-indexed
        for (int file = 0; file < 8; file++)
        {
            int square = rank * 8 + file;
            Piece piece = board.GetPiece(square);
            SetColor(piece, rank, file);
            Console.Write(Notation.GetChar(piece));
            Console.Write(' ');
        }
        Console.ResetColor();
        Console.WriteLine($"|{rank + 1}"); //ranks aren't zero-indexed
    }
    Console.WriteLine(" '----------------'");
}


void SetColor(Piece piece, int rank, int file)
{
    if ((rank + file) % 2 == 1)
        Console.BackgroundColor = ConsoleColor.DarkGray;
    else
        Console.BackgroundColor = ConsoleColor.Black;

    if ((piece & Piece.ColorMask) == Piece.White)
        Console.ForegroundColor = ConsoleColor.White;
    else
        Console.ForegroundColor = ConsoleColor.Gray;
}

void ReplKingSafety()
{
    while (true)
    {
        string fen = Console.ReadLine();
        if (fen == "")
            break;
        //fen = "8/8/7p/1P2Pp1P/2Pp1PP1/8/8/8 w - - 0 1";
        BoardState board = Notation.GetBoardState(fen);
        RenderFeature(board);
    }
}

void RenderFeatures(List<Data> data)
{
    foreach (var entry in data)
        RenderFeature(entry.Position);
}

void RenderFeature(BoardState board)
{
    Console.WriteLine(Notation.GetFEN(board));
    PrintPosition(board);
    KingSafetyTuner.Print(board);
}

void ReplCurves()
{
    Console.WriteLine("Curve(0..'width') = 'a' + x * 'b')");
    while (true)
    {
        string input = Console.ReadLine();
        if (input == "")
            break;

        string[] tokens = input.Trim().Split();
        
        int width = int.Parse(tokens[0]);
        int a = int.Parse(tokens[1]);
        int b = int.Parse(tokens[2]);
        PrintCurve(width, a, b);
    }
}

void PrintCurve(int width, int a, int b)
{
    for (int i = 0; i <= width; i++)
    {
        Console.Write(a + b * i);
        Console.Write(" ");
    }
    Console.WriteLine();
}

int Arc(int x, int width, int height)
{
    //Looks like an inverted parabola with Arc(0) = 0 Arc(width) = 0 and Arc(width/2) = height
    return height * 4 * (width * x - x * x) / (width * width);
}

