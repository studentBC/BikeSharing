﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeSharingSystem
{
    public class PermutationGA : GA<int>
    {

        int[] p1;
        int[] p2;
        private int[] selected;
        int capacity = 14;
        double time = 120;
        double alpha = 0.5;
        double[,] FromTo;   //已經過加權計算的距離矩陣
        double[,] PureFromTo;
        double[] StartToVertex;
        double[,] pheromone;
        Station[] stations;
        private PermutationMutation mutationType;
        private PermutationCrossover crossoverType;
        private _2ndCrossOver secondcrossverType;
        int cut;
        HybridACOGA hag;
        int count = 0;
        [Category("GA Setting")Description("突變的演算法")]
        public PermutationMutation MutationType
        {
            get { return mutationType; }
            set { mutationType = value; }
        }

        [Category("GA Setting")Description("基因交配的演算法")]
        public PermutationCrossover CrossOverType
        {
            get { return crossoverType; }
            set { crossoverType = value; }
        }
        [Category("GA Setting")Description("調配共用財段基因交配的演算法")]
        public _2ndCrossOver SecondCrossOverType
        {
            get { return secondcrossverType; }
            set { secondcrossverType = value; }
        }
        [Browsable(false)Category("GA Setting")Description("EmergentCrossover方法中決定距離和capacity per rate的權重")]
        public double Alpha
        {
            get { return alpha; }
            set
            {
                if (alpha <= 1)
                { alpha = value; }
                else { alpha = 0.5; }
            }
        }
       
        [Browsable(false)Category("Problem Setting")Description("卡車負載量")]
        public int TruckCapacity
        {
            get { return capacity; }
            set { capacity = value; }
        }
        [Browsable(false)Category("Problem Setting")Description("時窗限制")]
        public double TimeHorizon
        {
            get { return time; }
            set { time = value; }
        }
        //[Category("GA Setting")Description("N point cut 最多為總站數-1")]
        //public int Cut
        //{
        //    get { return cut; }
        //    set
        //    {
        //        cut = value;
        //        if (cut > stations.Length)
        //        {
        //            cut = stations.Length;
        //        }
        //        else
        //        {
        //            while (stations.Length % cut != 0)
        //            {
        //                cut++;
        //            }
        //        }
        //    }
        //}
        public PermutationGA(int numberOfVariables, OptimizationType opType, GA<int>.ObjectiveFunctionDelegate objectFunction, Station[] ss, double[,] distance,double []start) : base(numberOfVariables, opType, objectFunction)
        {
            stations = ss;
            p1 = new int[numberOfGenes];
            p2 = new int[numberOfGenes];
            selected = new int[numberOfGenes];
            cut = stations.Length / 2;
            PureFromTo = new double[stations.Length, stations.Length];
            //採用改良式 heuristic emergent service ㄉ作法
            FromTo = new double[stations.Length, stations.Length];
            StartToVertex = new double[stations.Length];
            //for (int i = 0; i < stations.Length; i++)
            Parallel.For(0,stations.Length,i=>
            {
                StartToVertex[i] = start[i];
                for (int j = 0; j < stations.Length; j++)
                {
                    PureFromTo[i, j] = distance[i, j];
                    if (stations[j].RatePerCapacity != 0)
                    {
                        //可以改進ㄉ地方
                        FromTo[i, j] = distance[i, j] + 0.01*(1/stations[j].RatePerCapacity);
                        //Console.Write(distance[i, j] + "  " + 0.001*1/stations[j].RatePerCapacity + "  ");
                    }
                    else
                    {
                        FromTo[i, j] = distance[i, j] * 18000;
                    }
                }
            });
            //FromTo = distance;
            
            hag = new HybridACOGA(stations.Length, OptimizationType.Min, objectFunction, FromTo,stations,TruckCapacity);
        }

        //remeber to add GA 's reset method into it
        public void Greedyinitialize(double[,] d, double[] startTOvertex)
        {

            // Allocate memory for gene related data
            chromosomes = new int[populationSize * 3][];
            int[,] temp = new int[populationSize, numberOfGenes];
            // Initialize the initial population
            for (int i = 0; i < chromosomes.Length; i++)
            {
                chromosomes[i] = new int[numberOfGenes];
            }
            // Reset computation realted variables
            soFarTheBestSolution = new int[numberOfGenes];
            objectiveValues = new double[chromosomes.Length];
            fitnessValues = new double[chromosomes.Length];
            indices = new int[chromosomes.Length];


            double[,] distance = new double[stations.Length, stations.Length];
            double min = double.MaxValue; int last = -1; int next = -1;
            List<double> fitness = new List<double>(); double judge = -1;
            List<int> routingsequence = new List<int>();
            double accumulate = 0;
            double wheelarea = 0;
            for (int i = 0; i < stations.Length; i++)
            {
                if (min > startTOvertex[i]) { min = startTOvertex[i]; last = i; }
            }
            int start = last;
            //開始加入chromosomes
            for (int i = 0; i < this.chromosomes.Length; i++)
            {
                //        start = last;
                routingsequence.Clear();
                routingsequence.Add(start);
                fitness.Clear();
                for (int r = 0; r < stations.Length; r++)
                {
                    for (int c = 0; c < stations.Length; c++)
                    {
                        distance[r, c] = d[r, c];
                    }
                }
                this.chromosomes[i][2 * stations.Length] = rnd.Next(0, capacity + 1);
                this.chromosomes[i][2 * stations.Length + 1] = rnd.Next(0, (int)TimeHorizon);
                last = start;
                do
                {

                    accumulate = 0; fitness.Clear(); wheelarea = 0;
                    for (int b = 0; b < stations.Length; b++)
                    {
                        if (distance[last, b] != 0)
                        {
                            wheelarea += 1 / distance[last, b];
                        }
                    }
                    for (int k = 0; k < stations.Length; k++)
                    {
                        if (distance[last, k] != 0)
                        {
                            fitness.Add((1 / distance[last, k]) / wheelarea);
                        }else { fitness.Add(0); }
                    }
                    judge = rnd.NextDouble(); //並沒有用什麼 upperbound - 
                    for (int a = 0; a < stations.Length; a++)
                    {
                        if (fitness.ElementAt(a) != 0)
                        {
                            accumulate += fitness.ElementAt(a);
                            if (judge <= accumulate)
                            {
                                next = a; routingsequence.Add(next);
                                break;
                            }
                        }
                    }
                    for (int j = 0; j < stations.Length; j++)
                    {
                        distance[j, last] = 0;
                    }
                    last = next;
                } while (check(distance, next));
                
                for (int j = 0; j < stations.Length; j++)
                {
                   // this.chromosomes[i][j] = routingsequence.ElementAt(j);
                    this.chromosomes[i][routingsequence.ElementAt(j)] = j ;
                    if (stations[j].Rate > 0)//pickup
                    {
                        this.chromosomes[i][j + stations.Length] = -1 * rnd.Next(0, capacity + 1);
                    }
                    else if (stations[j].Rate < 0) //delivery
                    {
                        this.chromosomes[i][j + stations.Length] = rnd.Next(0, capacity + 1);
                    }
                    else
                    {
                        this.chromosomes[i][j + stations.Length] = rnd.Next(0, capacity + 1) * rnd.Next(-1, 2);
                    }
                }
            }

            for (int j = 0; j < populationSize; j++)
            {
                objectiveValues[j] = GetObjectiveValueFunction(chromosomes[j]);
            }
            iterationCount = 0;
            soFarTheBestObjective = ((optimizationType == OptimizationType.Min) ? double.MaxValue : double.MinValue);
        }
        private bool check(double[,] m, int n)
        {
            bool flag = false;
            for (int i = 0; i < stations.Length; i++)
            {
                if (m[n, i] != 0) { flag = true; break; }
            }
            return flag;
        }
        //give ga a initial solution
        public override void initializePopulation()
        {
            int totalstation = stations.Length;
            for (int i = 0; i < populationSize; i++)
            {
                for (int j = 0; j < stations.Length; j++)
                {
                    //this.chromosomes[i][j] = stations[totalstation - j - 1].StationID;
                    this.chromosomes[i][j] = j;
                    if (stations[j].Rate > 0)//pickup
                    {
                        this.chromosomes[i][j + totalstation] = -1 * rnd.Next(0, capacity + 1);
                    }
                    else if (stations[j].Rate < 0) //delivery
                    {
                        this.chromosomes[i][j + totalstation] = rnd.Next(0, capacity + 1);
                    }
                    else
                    {
                        this.chromosomes[i][j + totalstation] = rnd.Next(0, capacity + 1) * rnd.Next(-1, 2);
                    }
                }
                KnuthShuffle(this.chromosomes[i]);
                this.chromosomes[i][2 * totalstation] = rnd.Next(0, capacity + 1);
                //this.chromosomes[i][2 * totalstation + 1] = rnd.Next(0, (int)TimeHorizon);
            }
            //initialize pheromone
            pheromone = new double[stations.Length, TruckCapacity + 1];
            hag.TruckCapacity = TruckCapacity;
            for (int i = 0; i < totalstation; i++)
            {              
                    for (int j = 0; j < TruckCapacity; j++)
                    {
                        pheromone[i, j] = 0.01;
                    }               
            }
        }

        public void KnuthShuffle(int[] array)
        {
            int temp;
            for (int i = 0; i < (array.Length - 2) / 2; i++)
            {
                int j = rnd.Next(i, (array.Length - 2) / 2); // Don't select from the entire array on subsequent loops
                temp = array[i]; array[i] = array[j]; array[j] = temp;
            }
        }

        private void swap(int target, int pos, int[] chromo) //not too sure about whehter it will amend the value of p1 or p2
        {
            for (int i = 0; i < stations.Length; i++)
            {
                if (chromo[i] == target)
                {
                    chromo[i] = chromo[pos];
                    chromo[pos] = target;
                    break;
                }
            }
        }
        //use N-point cut for crossover operation
        public override void generateAPairOfCrossoveredOffspring(int fartherIdx, int motherIdx, int child1Idx, int child2Idx)
        {
            switch (crossoverType)
            {
                //case PermutationCrossover.NpointCut:
                //    {
                //        //對前面的(this.numberOfGenes-2)/2 做 N Point cut
                //        int subinterval = stations.Length / cut;
                //        this.chromosomes[fartherIdx].CopyTo(p1, 0);
                //        this.chromosomes[motherIdx].CopyTo(p2, 0);
                //        int c = 0;
                //        for (int i = 0; i < stations.Length; i += subinterval)
                //        {
                //            c++;
                //            for (int j = i; j < i + subinterval; j++) //會有站點重複的問題
                //            {
                //                if (c % 2 == 0)
                //                {
                //                    swap(p1[j], j, p2);
                //                }
                //                else
                //                {
                //                    swap(p1[j], j, p1);
                //                }
                //            }
                //        }
                //        p1.CopyTo(this.chromosomes[child1Idx], 0);
                //        p2.CopyTo(this.chromosomes[child2Idx], 0);
                //        //對後面的(this.numberOfGenes-2)/2 做 N Point cut
                //        //先算出總共可以切成幾段
                //        int interval = (numberOfGenes - stations.Length) / cut;
                //        int count = 0;
                //        for (int a = stations.Length; a < this.numberOfGenes - 1; a += interval) //might be some wrong...
                //        {
                //            if (count % 2 == 0)
                //            {
                //                for (int b = a; b < a + interval; b++)
                //                {
                //                    this.chromosomes[child1Idx][b] = this.chromosomes[fartherIdx][b];
                //                    this.chromosomes[child2Idx][b] = this.chromosomes[motherIdx][b];
                //                }
                //            }
                //            else
                //            {
                //                for (int b = a; b < a + interval; b++)
                //                {
                //                    this.chromosomes[child1Idx][b] = this.chromosomes[motherIdx][b];
                //                    this.chromosomes[child2Idx][b] = this.chromosomes[fartherIdx][b];
                //                }
                //            }
                //            count++;
                //        }

                //        break;
                //    }
                case PermutationCrossover.OrderCross:
                    {
                        int start = rnd.Next((numberOfGenes - 2) / 2);
                        int end, temp;
                        do
                        {
                            end = rnd.Next((numberOfGenes - 2) / 2);
                        }
                        while (start == end);
                        if (start > end)
                        {
                            temp = end;
                            end = start;
                            start = temp;
                        }
                        for (int a = 0; a < numberOfGenes; a++)
                        {
                            if (a >= start && a <= end)
                            {
                                this.chromosomes[child1Idx][a] = this.chromosomes[fartherIdx][a];
                                this.chromosomes[child2Idx][a] = this.chromosomes[motherIdx][a];
                            }
                            else
                            {
                                this.chromosomes[child1Idx][a] = -1;
                                this.chromosomes[child2Idx][a] = -1;
                            }
                        }
                        int c, count;
                        for (int a = 0; a < ((numberOfGenes - 2) / 2); a++)
                        {
                            c = 0; count = 0;
                            if (a < start || a > end)
                            {
                                while (this.chromosomes[child1Idx].Contains(this.chromosomes[motherIdx][c]) && c < ((numberOfGenes - 2) / 2 - 1))
                                {
                                    c++;
                                }
                                this.chromosomes[child1Idx][a] = this.chromosomes[motherIdx][c];
                                while (this.chromosomes[child2Idx].Contains(this.chromosomes[fartherIdx][count]) && count < ((numberOfGenes - 2) / 2 - 1))
                                {
                                    count++;
                                }
                                this.chromosomes[child2Idx][a] = this.chromosomes[fartherIdx][count];
                            }
                        }

                        break;
                    }
                case PermutationCrossover.PositionBasedCross:
                    {
                        for (int j = 0; j < (numberOfGenes - 1) / 2; j++)
                        {
                            if (rnd.Next(2) == 0)
                            {
                                this.selected[j] = 1;
                            }
                            else
                            {
                                this.selected[j] = 0;
                            }
                            this.p1[this.chromosomes[fartherIdx][j]] = this.selected[j];
                            this.p2[this.chromosomes[motherIdx][j]] = this.selected[j];
                        }
                        int mom = 0;
                        int dad = 0;
                        for (int j = 0; j < (numberOfGenes - 1) / 2; j++)
                        {
                            if (this.selected[j] == 1)
                            {
                                this.chromosomes[child1Idx][j] = this.chromosomes[fartherIdx][j];
                                this.chromosomes[child2Idx][j] = this.chromosomes[motherIdx][j];
                            }
                            else
                            {
                                while (this.p1[this.chromosomes[motherIdx][dad]] == 1)
                                {
                                    dad++;
                                }
                                this.chromosomes[child1Idx][j] = this.chromosomes[motherIdx][dad];
                                dad++;
                                while (this.p2[this.chromosomes[fartherIdx][mom]] == 1)
                                {
                                    mom++;
                                }
                                this.chromosomes[child2Idx][j] = this.chromosomes[fartherIdx][mom];
                                mom++;
                            }
                        }


                        break;
                    }
                case PermutationCrossover.Heuristic://sometimes it is data wrong ... like two stations distance = 0?!!!
                    {//use 草稿紙
                        List<int> PossibleStation = new List<int>();
                        int[] routingSequence = new int[stations.Length];
                        int[] r2 = new int[stations.Length];
                        double[,] d1 = new double[stations.Length, stations.Length];
                        double[,] d2 = new double[stations.Length, stations.Length];
                        int p1startStation = rnd.Next(0, stations.Length);
                        int p2startStation;
                        do
                        {
                            p2startStation = rnd.Next(0, stations.Length);
                        } while (p1startStation == p2startStation);
                        //find the first sation and let its sequence be first
                        for (int i = 0; i < stations.Length; i++)
                        {
                            routingSequence[i] = -1;
                            r2[i] = -1;
                            for (int j = 0; j < stations.Length; j++)
                            {
                                d1[i, j] = PureFromTo[i, j];
                                d2[i, j] = PureFromTo[i, j];
                                if (j == p1startStation) { d1[i, j] = double.MaxValue; }
                                if (j == p2startStation) { d2[i, j] = double.MaxValue; }
                            }
                        }
                        routingSequence[p1startStation] = 0;
                        r2[p2startStation] = 0;
                        int sequence = 1;
                        //start to construct 
                        while (routingSequence.Contains(-1))
                        {
                            PossibleStation.Clear();
                            if (chromosomes[fartherIdx][p1startStation] == 0)
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[fartherIdx][i] == 1) { PossibleStation.Add(i); break; }
                                }
                            }
                            else if (chromosomes[fartherIdx][p1startStation] == stations.Length - 1)
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[fartherIdx][i] == stations.Length - 2) { PossibleStation.Add(i); break; }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[fartherIdx][i] == chromosomes[fartherIdx][p1startStation] - 1) { PossibleStation.Add(i); }
                                    if (chromosomes[fartherIdx][i] == chromosomes[fartherIdx][p1startStation] + 1) { PossibleStation.Add(i); }
                                }
                            }

                            if (chromosomes[motherIdx][p1startStation] == 0)
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[motherIdx][i] == 1) { PossibleStation.Add(i); break; }
                                }
                            }
                            else if (chromosomes[motherIdx][p1startStation] == stations.Length - 1)
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[motherIdx][i] == stations.Length - 2) { PossibleStation.Add(i); break; }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[motherIdx][i] == chromosomes[motherIdx][p1startStation] - 1) { PossibleStation.Add(i); }
                                    if (chromosomes[motherIdx][i] == chromosomes[motherIdx][p1startStation] + 1) { PossibleStation.Add(i); }
                                }
                            }
                            //將較近的站設為下一個起始點
                            p1startStation = NearStation(PossibleStation, p1startStation, d1);
                            if (routingSequence[p1startStation] == -1)
                            {
                                routingSequence[p1startStation] = sequence;
                                sequence++;
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    d1[i, p1startStation] = double.MaxValue;
                                }
                            }
                            else { throw new Exception("bug"); }
                        }
                        routingSequence.CopyTo(this.chromosomes[child1Idx], 0);
                        sequence = 1;
                        while (r2.Contains(-1))
                        {
                            PossibleStation.Clear();
                            if (chromosomes[fartherIdx][p2startStation] == 0)
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[fartherIdx][i] == 1) { PossibleStation.Add(i); break; }
                                }
                            }
                            else if (chromosomes[fartherIdx][p2startStation] == stations.Length - 1)
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[fartherIdx][i] == stations.Length - 2) { PossibleStation.Add(i); break; }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[fartherIdx][i] == chromosomes[fartherIdx][p2startStation] - 1) { PossibleStation.Add(i); }
                                    if (chromosomes[fartherIdx][i] == chromosomes[fartherIdx][p2startStation] + 1) { PossibleStation.Add(i); }
                                }
                            }

                            if (chromosomes[motherIdx][p2startStation] == 0)
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[motherIdx][i] == 1) { PossibleStation.Add(i); break; }
                                }
                            }
                            else if (chromosomes[motherIdx][p2startStation] == stations.Length - 1)
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[motherIdx][i] == stations.Length - 2) { PossibleStation.Add(i); break; }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[motherIdx][i] == chromosomes[motherIdx][p2startStation] - 1) { PossibleStation.Add(i); }
                                    if (chromosomes[motherIdx][i] == chromosomes[motherIdx][p2startStation] + 1) { PossibleStation.Add(i); }
                                }
                            }
                            //將較近的站設為下一個起始點
                            p2startStation = NearStation(PossibleStation, p2startStation, d2);
                            if (r2[p2startStation] == -1)
                            {
                                r2[p2startStation] = sequence;
                                sequence++;
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    d2[i, p2startStation] = double.MaxValue;
                                }
                            }
                            else { throw new Exception("bug"); }
                        }
                        r2.CopyTo(this.chromosomes[child2Idx], 0);
                  
                        break;
                    }
                case PermutationCrossover.Hybrid:
                    {
                        if (count == 0) { hag.reset(populationSize); count++; }
                        //if (count <= 1000)
                        //{
                            hag.antsConstructSolutions();
                            hag.ComputeObjectiveValues();
                            hag.updatePheromone();
                            //count++;
                        //}

                        for (int i = 0; i < stations.Length*2+2; i++)
                        {
                            this.chromosomes[child1Idx][i] = hag.chromosome[0][i];
                            this.chromosomes[child2Idx][i] = hag.chromosome[1][i];
                        }
                        break;
                    }
                case PermutationCrossover.Imminence://rate per capacity * distance to choose
                    {
                        List<int> PossibleStation = new List<int>();
                        int[] routingSequence = new int[stations.Length];
                        int[] r2 = new int[stations.Length];
                        double[,] d1 = new double[stations.Length, stations.Length];
                        double[,] d2 = new double[stations.Length, stations.Length];
                        int p1startStation = rnd.Next(0, stations.Length);
                        int p2startStation;
                        do
                        {
                            p2startStation = rnd.Next(0, stations.Length);
                        } while (p1startStation == p2startStation);
                        //find the first sation and let its sequence be first
                        //for(int i = 0; i < stations.Length;i++)
                        Parallel.For(0, stations.Length, i =>
                         {
                             routingSequence[i] = -1;
                             r2[i] = -1;
                             for (int j = 0; j < stations.Length; j++)
                             {
                                //可以改進ㄉ地方
                                d1[i, j] = FromTo[i, j];
                                 d2[i, j] = FromTo[i, j];


                                 if (j == p1startStation) { d1[i, j] = double.MaxValue; }
                                 if (j == p2startStation) { d2[i, j] = double.MaxValue; }
                             }
                         });
                        //for (int i = 0; i < stations.Length; i++)
                        //{
                        //    routingSequence[i] = -1;
                        //    r2[i] = -1;
                        //    for (int j = 0; j < stations.Length; j++)
                        //    {
                        //        //可以改進ㄉ地方
                        //        d1[i, j] = FromTo[i, j];
                        //        d2[i, j] = FromTo[i, j];

                               
                        //        if (j == p1startStation) { d1[i, j] = double.MaxValue; }
                        //        if (j == p2startStation) { d2[i, j] = double.MaxValue; }
                        //    }
                        //}
                        routingSequence[p1startStation] = 0;
                        r2[p2startStation] = 0;
                        int sequence = 1;
                        //start to construct 
                        while (routingSequence.Contains(-1))
                        {
                            PossibleStation.Clear();
                            if (chromosomes[fartherIdx][p1startStation] == 0)
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[fartherIdx][i] == 1) { PossibleStation.Add(i); break; }
                                }
                            }
                            else if (chromosomes[fartherIdx][p1startStation] == stations.Length - 1)
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[fartherIdx][i] == stations.Length - 2) { PossibleStation.Add(i); break; }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[fartherIdx][i] == chromosomes[fartherIdx][p1startStation] - 1) { PossibleStation.Add(i); }
                                    if (chromosomes[fartherIdx][i] == chromosomes[fartherIdx][p1startStation] + 1) { PossibleStation.Add(i); }
                                }
                            }

                            if (chromosomes[motherIdx][p1startStation] == 0)
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[motherIdx][i] == 1) { PossibleStation.Add(i); break; }
                                }
                            }
                            else if (chromosomes[motherIdx][p1startStation] == stations.Length - 1)
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[motherIdx][i] == stations.Length - 2) { PossibleStation.Add(i); break; }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[motherIdx][i] == chromosomes[motherIdx][p1startStation] - 1) { PossibleStation.Add(i); }
                                    if (chromosomes[motherIdx][i] == chromosomes[motherIdx][p1startStation] + 1) { PossibleStation.Add(i); }
                                }
                            }
                            //將較近的站設為下一個起始點
                            p1startStation = NearStation(PossibleStation, p1startStation, d1);
                            //p1startStation = NearStation(PossibleStation, p1startStation, d1);
                            if (routingSequence[p1startStation] == -1)
                            {
                                routingSequence[p1startStation] = sequence;
                                sequence++;
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    d1[i, p1startStation] = double.MaxValue;
                                }
                            }
                            else { throw new Exception("bug"); }
                        }
                        routingSequence.CopyTo(this.chromosomes[child1Idx], 0);
                        sequence = 1;
                        while (r2.Contains(-1))
                        {
                            PossibleStation.Clear();
                            if (chromosomes[fartherIdx][p2startStation] == 0)
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[fartherIdx][i] == 1) { PossibleStation.Add(i); break; }
                                }
                            }
                            else if (chromosomes[fartherIdx][p2startStation] == stations.Length - 1)
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[fartherIdx][i] == stations.Length - 2) { PossibleStation.Add(i); break; }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[fartherIdx][i] == chromosomes[fartherIdx][p2startStation] - 1) { PossibleStation.Add(i); }
                                    if (chromosomes[fartherIdx][i] == chromosomes[fartherIdx][p2startStation] + 1) { PossibleStation.Add(i); }
                                }
                            }

                            if (chromosomes[motherIdx][p2startStation] == 0)
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[motherIdx][i] == 1) { PossibleStation.Add(i); break; }
                                }
                            }
                            else if (chromosomes[motherIdx][p2startStation] == stations.Length - 1)
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[motherIdx][i] == stations.Length - 2) { PossibleStation.Add(i); break; }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    if (chromosomes[motherIdx][i] == chromosomes[motherIdx][p2startStation] - 1) { PossibleStation.Add(i); }
                                    if (chromosomes[motherIdx][i] == chromosomes[motherIdx][p2startStation] + 1) { PossibleStation.Add(i); }
                                }
                            }
                            //將較近的站設為下一個起始點
                            p2startStation = NearStation(PossibleStation, p2startStation, d2);
                            //p2startStation = NearStation(PossibleStation, p2startStation, d2);
                            if (r2[p2startStation] == -1)
                            {
                                r2[p2startStation] = sequence;
                                sequence++;
                                for (int i = 0; i < stations.Length; i++)
                                {
                                    d2[i, p2startStation] = double.MaxValue;
                                }
                            }
                            else { throw new Exception("bug"); }
                        }
                        r2.CopyTo(this.chromosomes[child2Idx], 0);
                        break;
                    }
            }
            switch (secondcrossverType)
            {
                case _2ndCrossOver.FBMS:
                    {
                        //採用 FMBS
                        for (int a = stations.Length; a < this.numberOfGenes - 1; a++)
                        {
                            alpha = rnd.NextDouble();
                            if (stations[a - stations.Length].Rate > 0)
                            {
                                if (this.chromosomes[motherIdx][a] < this.chromosomes[fartherIdx][a])
                                {
                                    this.chromosomes[child2Idx][a]
                                    = -(int)(-(this.chromosomes[fartherIdx][a]) * alpha + (1 - alpha) * (-this.chromosomes[motherIdx][a] + 1));
                                    this.chromosomes[child1Idx][a]
                                    = -(int)((-this.chromosomes[fartherIdx][a]) * (1 - alpha) + alpha * (-this.chromosomes[motherIdx][a] + 1));
                                }
                                else if (this.chromosomes[motherIdx][a] > this.chromosomes[fartherIdx][a])
                                {
                                    this.chromosomes[child1Idx][a]
                                    = -(int)((-this.chromosomes[fartherIdx][a] + 1) * (1 - alpha) + alpha * -this.chromosomes[motherIdx][a]);
                                    this.chromosomes[child2Idx][a]
                                    = -(int)((-this.chromosomes[fartherIdx][a] + 1) * alpha + (1 - alpha) * -this.chromosomes[motherIdx][a]);
                                }
                                else
                                {
                                    this.chromosomes[child2Idx][a] = this.chromosomes[child1Idx][a] = this.chromosomes[fartherIdx][a];
                                }
                            }
                            else if (stations[a - stations.Length].Rate < 0)
                            {
                                if (this.chromosomes[motherIdx][a] > this.chromosomes[fartherIdx][a])
                                {
                                    this.chromosomes[child2Idx][a]
                                    = (int)((this.chromosomes[fartherIdx][a]) * alpha + (1 - alpha) * (this.chromosomes[motherIdx][a] + 1));
                                    this.chromosomes[child1Idx][a]
                                    = (int)((this.chromosomes[fartherIdx][a]) * (1 - alpha) + alpha * (this.chromosomes[motherIdx][a] + 1));
                                }
                                else if (this.chromosomes[motherIdx][a] < this.chromosomes[fartherIdx][a])
                                {
                                    this.chromosomes[child1Idx][a]
                                    = (int)((this.chromosomes[fartherIdx][a] + 1) * (1 - alpha) + alpha * this.chromosomes[motherIdx][a]);
                                    this.chromosomes[child2Idx][a]
                                    = (int)((this.chromosomes[fartherIdx][a] + 1) * alpha + (1 - alpha) * this.chromosomes[motherIdx][a]);
                                }
                                else
                                {
                                    this.chromosomes[child2Idx][a] = this.chromosomes[child1Idx][a] = this.chromosomes[fartherIdx][a];
                                }
                            }
                            else
                            {
                                this.chromosomes[child1Idx][a] = this.chromosomes[fartherIdx][a];
                                this.chromosomes[child2Idx][a] = this.chromosomes[motherIdx][a];
                            }
                            //test 實數解的出線機率是否相同
                            //Console.Write(this.chromosomes[child2Idx][a] + " ");
                        }
                        alpha = rnd.NextDouble();
                        if (this.chromosomes[motherIdx][numberOfGenes - 1] > this.chromosomes[fartherIdx][numberOfGenes - 1])
                        {
                            this.chromosomes[child2Idx][numberOfGenes - 1]
                            = (int)((this.chromosomes[fartherIdx][numberOfGenes - 1]) * alpha + (1 - alpha) * (this.chromosomes[motherIdx][numberOfGenes - 1] + 1));
                            this.chromosomes[child1Idx][numberOfGenes - 1]
                            = (int)((this.chromosomes[fartherIdx][numberOfGenes - 1]) * (1 - alpha) + alpha * (this.chromosomes[motherIdx][numberOfGenes - 1] + 1));
                        }
                        else if (this.chromosomes[motherIdx][numberOfGenes - 1] < this.chromosomes[fartherIdx][numberOfGenes - 1])
                        {
                            this.chromosomes[child1Idx][numberOfGenes - 1]
                            = (int)((this.chromosomes[fartherIdx][numberOfGenes - 1] + 1) * (1 - alpha) + alpha * this.chromosomes[motherIdx][numberOfGenes - 1]);
                            this.chromosomes[child2Idx][numberOfGenes - 1]
                            = (int)((this.chromosomes[fartherIdx][numberOfGenes - 1] + 1) * alpha + (1 - alpha) * this.chromosomes[motherIdx][numberOfGenes - 1]);
                        }
                        else
                        {
                            this.chromosomes[child1Idx][numberOfGenes - 1] = this.chromosomes[child2Idx][numberOfGenes - 1] = this.chromosomes[motherIdx][numberOfGenes - 1];
                        }
                       
                        break;
                    }
                case _2ndCrossOver.TES:
                    {
                        //採用 TES
                        for (int a = stations.Length; a < this.numberOfGenes - 1; a++)
                        {
                            alpha = rnd.NextDouble();
                            if (stations[a - stations.Length].Rate > 0)
                            {

                                if (this.chromosomes[motherIdx][a] < this.chromosomes[fartherIdx][a])
                                {
                                    this.chromosomes[child2Idx][a]
                                    = -(int)((-this.chromosomes[motherIdx][a]) * alpha + (1 - alpha) * (TruckCapacity + 1));
                                    this.chromosomes[child1Idx][a]
                                    = -(int)((-this.chromosomes[fartherIdx][a] + 1) * (1 - alpha) + alpha);
                                }
                                else if (this.chromosomes[motherIdx][a] > this.chromosomes[fartherIdx][a])
                                {
                                    this.chromosomes[child1Idx][a]
                                    = -(int)(-(this.chromosomes[fartherIdx][a]) * alpha + (1 - alpha) * (TruckCapacity + 1));
                                    this.chromosomes[child2Idx][a]
                                    = -(int)((1-alpha) * (-this.chromosomes[motherIdx][a] + 1) + alpha);
                                }
                                else
                                {
                                    this.chromosomes[child2Idx][a] = this.chromosomes[child1Idx][a] = this.chromosomes[fartherIdx][a];
                                }

                            }
                            else if (stations[a - stations.Length].Rate < 0)
                            {
                                if (this.chromosomes[motherIdx][a] > this.chromosomes[fartherIdx][a])
                                {
                                    this.chromosomes[child2Idx][a]
                                    = (int)((this.chromosomes[motherIdx][a]) * alpha + (1 - alpha) * (TruckCapacity+1));
                                    this.chromosomes[child1Idx][a]
                                    = (int)((this.chromosomes[fartherIdx][a]+1)* (1 - alpha)+alpha);
                                }
                                else if (this.chromosomes[motherIdx][a] < this.chromosomes[fartherIdx][a])
                                {
                                    this.chromosomes[child1Idx][a]
                                    = (int)((this.chromosomes[fartherIdx][a]) * alpha + (1 - alpha) * (TruckCapacity+1));
                                    this.chromosomes[child2Idx][a]
                                    = (int)((1 - alpha) * (this.chromosomes[motherIdx][a]+1)+alpha);
                                }
                                else
                                {
                                    this.chromosomes[child2Idx][a] = this.chromosomes[child1Idx][a] = this.chromosomes[fartherIdx][a];
                                }
                            }
                            else
                            {
                                this.chromosomes[child1Idx][a] = this.chromosomes[fartherIdx][a];
                                this.chromosomes[child2Idx][a] = this.chromosomes[motherIdx][a];
                            }
                            //Console.Write(this.chromosomes[child1Idx][a] + " ");
                        }

                        if (this.chromosomes[motherIdx][numberOfGenes-1] > this.chromosomes[fartherIdx][numberOfGenes-1])
                        {
                            this.chromosomes[child2Idx][numberOfGenes-1]
                            = (int)((this.chromosomes[motherIdx][numberOfGenes-1]) * alpha + (1 - alpha) * (TruckCapacity + 1));
                            this.chromosomes[child1Idx][numberOfGenes - 1]
                            = (int)((this.chromosomes[fartherIdx][numberOfGenes - 1] + 1) * (1 - alpha) + alpha);
                        }
                        else if (this.chromosomes[motherIdx][numberOfGenes - 1] < this.chromosomes[fartherIdx][numberOfGenes - 1])
                        {
                            this.chromosomes[child1Idx][numberOfGenes - 1]
                            = (int)((this.chromosomes[fartherIdx][numberOfGenes - 1]) * alpha + (1 - alpha) * (TruckCapacity + 1));
                            this.chromosomes[child2Idx][numberOfGenes - 1]
                            = (int)((1 - alpha) * (this.chromosomes[motherIdx][numberOfGenes - 1] + 1) + alpha);
                        }
                        else
                        {
                            this.chromosomes[child2Idx][numberOfGenes - 1] = this.chromosomes[child1Idx][numberOfGenes - 1] = this.chromosomes[fartherIdx][numberOfGenes - 1];
                        }

                        break;
                    }
                case _2ndCrossOver.LVD:
                    {
                        //採用 LVD
                        for (int a = stations.Length; a < this.numberOfGenes - 1; a++)
                        {
                                rnd.NextDouble();
                                if (stations[a - stations.Length].Rate > 0)
                                {
                                    if (this.chromosomes[motherIdx][a] < this.chromosomes[fartherIdx][a])
                                    {
                                        this.chromosomes[child2Idx][a]
                                        = -(int)Math.Ceiling(-(1 + this.chromosomes[motherIdx][a]) * alpha + (1 - alpha) * (TruckCapacity + 1));
                                        this.chromosomes[child1Idx][a] = -(int)((1 - alpha) * (-this.chromosomes[motherIdx][a] + 1) + alpha);
                                    }
                                    else if (this.chromosomes[motherIdx][a] > this.chromosomes[fartherIdx][a])
                                    {
                                        this.chromosomes[child1Idx][a]
                                        = -(int)Math.Ceiling((1 + this.chromosomes[fartherIdx][a]) * -alpha + (1 - alpha) * (TruckCapacity + 1));
                                        this.chromosomes[child2Idx][a] = -(int)((1 - alpha) * (-this.chromosomes[fartherIdx][a] + 1) + alpha);
                                    }
                                    else
                                    {
                                        this.chromosomes[child1Idx][a] = this.chromosomes[child2Idx][a] = this.chromosomes[motherIdx][a];
                                    }

                                }
                                else if (stations[a - stations.Length].Rate < 0)
                                {
                                    if (this.chromosomes[motherIdx][a] > this.chromosomes[fartherIdx][a])
                                    {
                                        this.chromosomes[child2Idx][a]
                                        = (int)Math.Ceiling((this.chromosomes[motherIdx][a] - 1) * alpha + (1 - alpha) * (TruckCapacity + 1));
                                        this.chromosomes[child1Idx][a] = (int)((1 - alpha) * (this.chromosomes[motherIdx][a] + 1) + alpha);
                                    }
                                    else if (this.chromosomes[motherIdx][a] < this.chromosomes[fartherIdx][a])
                                    {
                                        this.chromosomes[child1Idx][a]
                                        = (int)Math.Ceiling((this.chromosomes[fartherIdx][a] - 1) * alpha + (1 - alpha) * (TruckCapacity + 1));
                                        this.chromosomes[child2Idx][a] = (int)((1 - alpha) * (this.chromosomes[fartherIdx][a] + 1) + alpha);
                                    }
                                    else
                                    {
                                        this.chromosomes[child1Idx][a] = this.chromosomes[child2Idx][a] = this.chromosomes[motherIdx][a];
                                    }
                                }
                                else
                                {
                                    this.chromosomes[child1Idx][a] = this.chromosomes[fartherIdx][a];
                                    this.chromosomes[child2Idx][a] = this.chromosomes[motherIdx][a];
                                }
                            //Console.Write(this.chromosomes[child1Idx][a] + " ");
                        }
                        //for initial goods
                        if (this.chromosomes[motherIdx][numberOfGenes - 1] > this.chromosomes[fartherIdx][numberOfGenes - 1])
                        {
                            this.chromosomes[child2Idx][numberOfGenes - 1]
                            = (int)((this.chromosomes[fartherIdx][numberOfGenes - 1] + 1) * alpha + (1 - alpha) * (TruckCapacity + 1));
                            this.chromosomes[child1Idx][numberOfGenes -1]
                            = (int)((this.chromosomes[fartherIdx][numberOfGenes - 1] + 1) * (1 - alpha) + alpha);
                        }
                        else if (this.chromosomes[motherIdx][numberOfGenes - 1] < this.chromosomes[fartherIdx][numberOfGenes - 1])
                        {
                            this.chromosomes[child1Idx][numberOfGenes - 1]
                            = (int)((this.chromosomes[motherIdx][numberOfGenes - 1] + 1) * alpha + (1 - alpha) * (TruckCapacity + 1));
                            this.chromosomes[child2Idx][numberOfGenes - 1]
                            = (int)((1 - alpha) * (this.chromosomes[motherIdx][numberOfGenes - 1] + 1) + alpha);
                        }
                        else
                        {
                            this.chromosomes[child1Idx][numberOfGenes - 1] = this.chromosomes[child2Idx][numberOfGenes - 1] = this.chromosomes[motherIdx][numberOfGenes - 1];
                        }
                      
                        break;
                    }
                case _2ndCrossOver.SVD:
                    {
                        //採用 SVD
                        for (int a = stations.Length; a < this.numberOfGenes - 1; a++)
                        {
                            alpha = rnd.NextDouble();
                            if (stations[a - stations.Length].Rate > 0)
                            {

                                if (this.chromosomes[motherIdx][a] < this.chromosomes[fartherIdx][a])
                                {
                                    this.chromosomes[child2Idx][a]
                                    = -(int)((this.chromosomes[fartherIdx][a]-1) * -alpha + (1 - alpha) * (TruckCapacity + 1));
                                    this.chromosomes[child1Idx][a]
                                    = -(int)((-this.chromosomes[fartherIdx][a] + 1) * (1 - alpha) + alpha);
                                }
                                else if (this.chromosomes[motherIdx][a] > this.chromosomes[fartherIdx][a])
                                {
                                    this.chromosomes[child1Idx][a]
                                    = -(int)((this.chromosomes[motherIdx][a]-1) * -alpha + (1 - alpha) * (TruckCapacity + 1));
                                    this.chromosomes[child2Idx][a]
                                    = -(int)((1 - alpha) * (-this.chromosomes[motherIdx][a] + 1) + alpha);
                                }
                                else
                                {
                                    this.chromosomes[child1Idx][a] = this.chromosomes[child2Idx][a] = this.chromosomes[motherIdx][a];
                                }

                            }
                            else if (stations[a - stations.Length].Rate < 0)
                            {
                                if (this.chromosomes[motherIdx][a] > this.chromosomes[fartherIdx][a])
                                {
                                    this.chromosomes[child2Idx][a]
                                    = (int)((this.chromosomes[fartherIdx][a]+1) * alpha + (1 - alpha) * (TruckCapacity+1));
                                    this.chromosomes[child1Idx][a]
                                    = (int)((this.chromosomes[fartherIdx][a]+1) * (1 - alpha)+alpha);
                                }
                                else if (this.chromosomes[motherIdx][a] < this.chromosomes[fartherIdx][a])
                                {
                                    this.chromosomes[child1Idx][a]
                                    = (int)((this.chromosomes[motherIdx][a]+1) * alpha + (1 - alpha) * (TruckCapacity + 1));
                                    this.chromosomes[child2Idx][a]
                                    = (int)((1 - alpha) * (this.chromosomes[motherIdx][a]+1)+alpha);
                                }
                                else
                                {
                                    this.chromosomes[child1Idx][a] = this.chromosomes[child2Idx][a] = this.chromosomes[motherIdx][a];
                                }
                            }
                            else
                            {
                                this.chromosomes[child1Idx][a] = this.chromosomes[fartherIdx][a];
                                this.chromosomes[child2Idx][a] = this.chromosomes[motherIdx][a];
                            }
                            // Console.Write(this.chromosomes[child1Idx][a] + " ");
                        }

                        //for initial goods
                        if (this.chromosomes[motherIdx][numberOfGenes - 1] > this.chromosomes[fartherIdx][numberOfGenes - 1])
                        {
                            this.chromosomes[child2Idx][numberOfGenes - 1]
                            = (int)((this.chromosomes[fartherIdx][numberOfGenes - 1] + 1) * alpha + (1 - alpha) * (TruckCapacity + 1));
                            this.chromosomes[child1Idx][numberOfGenes - 1]
                            = (int)((this.chromosomes[fartherIdx][numberOfGenes - 1] + 1) * (1 - alpha) + alpha);
                        }
                        else if (this.chromosomes[motherIdx][numberOfGenes - 1] < this.chromosomes[fartherIdx][numberOfGenes - 1])
                        {
                            this.chromosomes[child1Idx][numberOfGenes - 1]
                            = (int)((this.chromosomes[motherIdx][numberOfGenes - 1] + 1) * alpha + (1 - alpha) * (TruckCapacity + 1));
                            this.chromosomes[child2Idx][numberOfGenes - 1]
                            = (int)((1 - alpha) * (this.chromosomes[motherIdx][numberOfGenes - 1] + 1) + alpha);
                        }
                        else
                        {
                            this.chromosomes[child1Idx][numberOfGenes - 1] = this.chromosomes[child2Idx][numberOfGenes - 1] = this.chromosomes[motherIdx][numberOfGenes - 1];
                        }
                      
                        break;
                    }
                case _2ndCrossOver.pheromone:
                    {   //先用full range
                        //for (int a = stations.Length; a < this.numberOfGenes - 2; a++)
                        //{
                        //    if (stations[a - stations.Length].Rate > 0)
                        //    {
                        //        this.chromosomes[child2Idx][a]
                        //        = rnd.Next(-TruckCapacity, 0);
                        //        this.chromosomes[child1Idx][a]
                        //        = rnd.Next(-TruckCapacity, 0);
                        //    }
                        //    else if (stations[a - stations.Length].Rate < 0)
                        //    {

                        //        this.chromosomes[child2Idx][a]
                        //         = rnd.Next(1, TruckCapacity + 1);
                        //        this.chromosomes[child1Idx][a]
                        //            = rnd.Next(1, TruckCapacity + 1);
                        //    }
                        //    else
                        //    {
                        //        this.chromosomes[child1Idx][a]
                        //        = rnd.Next(-TruckCapacity, TruckCapacity + 1);
                        //        this.chromosomes[child2Idx][a]
                        //        = rnd.Next(-TruckCapacity, TruckCapacity + 1);
                        //    }
                        //}
                        //// Console.Write(this.chromosomes[child1Idx][a] + " ");
                        //this.chromosomes[child2Idx][numberOfGenes - 2] = rnd.Next(0, TruckCapacity + 1);
                        //this.chromosomes[child1Idx][numberOfGenes - 2] = rnd.Next(0, TruckCapacity + 1);
                        //this.chromosomes[child1Idx][numberOfGenes - 1] = rnd.Next(0, (int)TimeHorizon);
                        //this.chromosomes[child2Idx][numberOfGenes - 1] = rnd.Next(0, (int)TimeHorizon);
                        //stochastic select p and d amount
                        int[] chosenpd = new int[stations.Length];
                        double wheelarea = 0.0; double hit = 0;double[] fv = new double[TruckCapacity+1];
                        for (int i = 0;i < stations.Length; i++)
                        {
                            wheelarea = 0.0;
                            for (int j = 0; j <= TruckCapacity; j++)
                            {
                                wheelarea+=pheromone[i, j];
                                fv[j] = wheelarea;
                            }

                            hit = rnd.NextDouble() * wheelarea;
                            for (int j = 0; j <= TruckCapacity; j++)
                            {
                                if (hit <= fv[j])
                                {
                                    if (stations[i].Rate<=0)
                                    {
                                        chosenpd[i] = j;
                                    }
                                    else if(stations[i].Rate > 0)
                                    {
                                        chosenpd[i] = -j;
                                    }
                                    else
                                    {
                                        chosenpd[i] = j;
                                    }
                                    break;
                                }
                            }
                        }

                        for (int i = stations.Length ; i < stations.Length*2 ; i++)
                        {
                            chromosomes[child1Idx][i] = chosenpd[i -stations.Length];
                            //Console.Write(chromosomes[child1Idx][i] + " ");
                        }
                        //Console.WriteLine();
                        //get child2 gene
                        for (int i = 0; i < stations.Length; i++)
                        {
                            wheelarea = 0.0;
                            for (int j = 0; j <= TruckCapacity; j++)
                            {
                                wheelarea += pheromone[i, j];
                                fv[j] = wheelarea;
                            }
                            hit = rnd.NextDouble() * wheelarea;
                            for (int j = 0; j <= TruckCapacity; j++)
                            {
                                if (hit <= fv[j])
                                {
                                    if (stations[i].Rate <= 0)
                                    {
                                        chosenpd[i] = j;
                                    }
                                    else if (stations[i].Rate > 0)
                                    {
                                        chosenpd[i] = -j;
                                    }
                                    else
                                    {
                                        chosenpd[i] = j;
                                    }
                                    break;
                                }
                            }
                        }

                        for (int i = stations.Length; i < stations.Length*2; i++)
                        {
                            chromosomes[child2Idx][i] = chosenpd[i - stations.Length ];
                            //Console.Write(chromosomes[child2Idx][i] + " ");
                        }
                        //Console.WriteLine();
                       
                        break;
                    }
            }
            //print it and see repeate or not
            //Console.WriteLine();
            //Console.Write("child1 solutions: ");
            //for (int i = 0; i < numberOfGenes; i++)
            //{
            //    Console.Write(chromosomes[child1Idx][i] + " ");
            //}
            //Console.WriteLine();
            //Console.Write("child2 solutions: ");
            //for (int i = 0; i < numberOfGenes; i++)
            //{
            //    Console.Write(chromosomes[child2Idx][i] + " ");
            //}
        }
        //為了使卡車可以pickup delivery相間的作法
        //private int PDNear(List<int> possible, int start, double[,] d)
        //{
        //    int change ,next ;double compare;
        //    if (stations[start].Rate > 0)
        //    {
        //        change = 1;
        //    }
        //    else if(stations[start].Rate < 0)
        //    {
        //        change = -1;
        //    }else
        //    {
        //        change = 10;
        //    }
        //    int target = -1; double min = double.MaxValue;
        //    for (int i = 0; i < possible.Count; i++)
        //    {
        //        if(stations[possible[i]].Rate > 0)
        //        {
        //            next = 1;
        //            compare = next*d[start, possible[i]]*change;
        //        }
        //        else if (stations[possible[i]].Rate < 0)
        //        {
        //            next = -1;
        //            compare = next * d[start, possible[i]] * change;
        //        }
        //        else
        //        {
        //            compare = 10 * d[start, possible[i]] * change;
        //        }
        //        if(d[start, possible[i]] == double.MaxValue)
        //        {
        //            compare = double.MaxValue;
        //        }
        //        if (min > compare)
        //        {
        //            min = compare;
        //            target = possible[i];
        //        }
        //    }
        //    if (target == -1)
        //    {
        //        for (int i = 0; i < stations.Length; i++)
        //        {
        //            if (d[start, i] != 0 && min > d[start, i])
        //            {
        //                min = d[start, i];
        //                target = i;
        //            }
        //        }
        //    }

        //    return target;
        //}
        public  void updatePheromonetable()
        {
            //update pheromone table only add the best solution pheromone
            for (int j = 0; j < stations.Length; j++)
            {
                pheromone[soFarTheBestSolution[j], Math.Abs(soFarTheBestSolution[j + stations.Length])] += 100 * 1 / soFarTheBestObjective;
            }
            // Evaporate the pheromone for all pheromone values
            for (int r = 0; r < pheromone.GetLength(0); r++)
                for (int c = 0; c < pheromone.GetLength(1); c++)
                    pheromone[r, c] *= 0.9;
        }
        private int NearStation(List<int> possible, int start, double[,] d)
        {
            int target = -1; double min = double.MaxValue;
            for (int i = 0; i < possible.Count; i++)
            {
                if (min > d[start, possible[i]])
                {
                    min = d[start, possible[i]];
                    target = possible[i];
                }
            }
            if (target == -1)
            {
                for (int i = 0; i < stations.Length; i++)
                {
                    if (d[start, i] != 0 && min > d[start, i])
                    {
                        min = d[start, i];
                        target = i;
                    }
                }
            }

            return target;
        }
        private int NearStation(int start, double[,] d)
        {
            int target = -1; double min = double.MaxValue;
            for (int i = 0; i < stations.Length; i++)
            {
                if (d[start, i] != 0 && min > d[start, i])
                {
                    min = d[start, i];
                    target = i;
                }
            }
            return target;
        }
        //use Random assignment for mutation 
        public override void performMutateOperation()
        {
            numberOfMutatedChildren = Convert.ToInt32(mutationRate * populationSize);
            int total = populationSize + numberOfCrossoveredChildren;
            int mutateID; int MutateSource;
            for (int i = 0; i < numberOfMutatedChildren; i++)
            {
                mutateID = total + i;
                MutateSource = rnd.Next(populationSize + 1);
                for (int j = 0; j < numberOfGenes; j++)
                {
                    chromosomes[mutateID][j] = chromosomes[MutateSource][j];
                }
                switch (mutationType)
                {  //amend it!
                    case PermutationMutation.Inversion:
                        {
                            int mutateN = (int)(.01 *stations.Length);
                            int from = rnd.Next(stations.Length-mutateN);
                            int temp;

                            for (int a = 0; a < mutateN/2; a++)
                            {
                                temp = chromosomes[mutateID][from+a];
                                chromosomes[mutateID][from+a] = chromosomes[mutateID][from+mutateN-1-a];
                                chromosomes[mutateID][from + mutateN - 1 - a] = temp;
                            }
                            //第二段ㄉmutate
                            int mutateNumber = (int)(.1 * stations.Length);
                            int position = -1;
                            for (int a = 0; a < mutateNumber; a++)
                            {
                                position = rnd.Next(stations.Length);
                                if (stations[position].Rate < 0)
                                {
                                    chromosomes[mutateID][position + stations.Length] = rnd.Next(0, TruckCapacity);
                                }
                                else if (stations[position].Rate > 0)
                                {
                                    chromosomes[mutateID][position + stations.Length] = -rnd.Next(0, TruckCapacity);
                                }
                            }
                            chromosomes[mutateID][numberOfGenes - 1] = rnd.Next(0, capacity + 1);//truck start routing time
                            break;
                        }

                    case PermutationMutation.ReciprocalExchange:
                        {
                            int small;
                            int big; int temp;
                            //small = rnd.Next((numberOfGenes - 2) / 2);
                            //do
                            //{
                            //    big = rnd.Next((numberOfGenes - 2) / 2);
                            //}
                            //while (small == big);
                            //if (small > big)
                            //{
                            //    temp = small;
                            //    small = big;
                            //    big = temp;
                            //}
                            //int half = (big - small + 1) / 2;
                            //for (int a = 0; a < half; a++)
                            //{
                            //    temp = chromosomes[mutateID][small + a];
                            //    chromosomes[mutateID][small + a] = chromosomes[mutateID][big - a];
                            //    chromosomes[mutateID][big - a] = temp;
                            //}
                            int selectednumber = (int)(.01*stations.Length);
                            for(int j = 0; j < selectednumber; j++)
                            {
                                big = rnd.Next(stations.Length);small = rnd.Next(stations.Length);
                                temp = chromosomes[mutateID][small];
                                chromosomes[mutateID][small] = chromosomes[mutateID][big];
                                chromosomes[mutateID][big] = temp;
                            }
                            int mutateNumber = (int)(.1 * stations.Length);
                            int position = -1;
                            for (int a = 0; a < mutateNumber; a++)
                            {
                                position = rnd.Next(stations.Length);
                                if (stations[position].Rate < 0)
                                {
                                    chromosomes[mutateID][position + stations.Length] = rnd.Next(0, TruckCapacity);
                                }
                                else if (stations[position].Rate > 0)
                                {
                                    chromosomes[mutateID][position + stations.Length] = -rnd.Next(0, TruckCapacity);
                                }
                            }
                            chromosomes[mutateID][numberOfGenes - 1] = rnd.Next(0, capacity + 1);//truck start routing time

                            break;
                        }
                    case PermutationMutation.TSPHeuristic:
                        {
                            int mutateAmount = -1;
                            //if (stations.Length <= 8)
                            //{
                            //    mutateAmount = rnd.Next(0, stations.Length);
                            //}
                            //else
                            //{
                            //    mutateAmount = 8;
                            //}
                            mutateAmount = 6;
                            List<int> mutatePosition = new List<int>();
                            int chosen;
                            while (mutatePosition.Count <= mutateAmount)
                            {
                                chosen = rnd.Next(0, stations.Length);
                                if (!mutatePosition.Contains(chosen))
                                { mutatePosition.Add(chosen); }
                            }
                            int[] inputline = new int[mutatePosition.Count];
                            mutatePosition.CopyTo(inputline, 0);
                            List<List<int>> change = Permutations(mutatePosition);
                            if (change.Count == 0)
                            {
                                throw new Exception("your stations is too large !!!");
                            }
                            int[][] temp = new int[change.Count][];
                            int c = 0; double min = double.MaxValue; double ans = 0; int smallest = -1;
                            for (int b = 0; b < change.Count; b++)
                            {
                                temp[b] = new int[numberOfGenes]; c = 0;
                                for (int a = 0; a < stations.Length; a++)
                                {
                                    temp[b][a] = this.chromosomes[mutateID][a];
                                    if (inputline.Contains(this.chromosomes[mutateID][a]))
                                    {
                                        temp[b][a] = change.ElementAt(b).IndexOf(c);
                                        c++;
                                    }
                                }
                                ans = GetObjectiveValueFunction(temp[b]);
                                if (min > ans)
                                {
                                    min = ans;
                                    smallest = b;
                                }
                            }
                            temp[smallest].CopyTo(chromosomes[mutateID], 0);
                            int mutateNumber = (int)(.1 * stations.Length);
                            int position = -1;
                            for (int a = 0; a < mutateNumber; a++)
                            {
                                position = rnd.Next(stations.Length);
                                if (stations[position].Rate < 0)
                                {
                                    chromosomes[mutateID][position + stations.Length] = rnd.Next(0, TruckCapacity);
                                }
                                else if (stations[position].Rate > 0)
                                {
                                    chromosomes[mutateID][position + stations.Length] = -rnd.Next(0, TruckCapacity);
                                }
                            }
                            chromosomes[mutateID][numberOfGenes - 1] = rnd.Next(0, capacity + 1);//truck start routing time
                            break;
                        }
                    //case PermutationMutation.Heuristic:
                    //    {
                    //        int count = 0;int turn = 0;int tmp;
                    //        //decode first
                    //        int[] routingSequence = new int[stations.Length];
                    //        int[] temp = new int[stations.Length];
                    //        for (int a = 0; a < stations.Length; a++)
                    //        {
                    //            temp[a] = chromosomes[mutateID][a]; routingSequence[a] = a; //when you add depot location just amend it
                    //        }
                    //        Array.Sort(temp, routingSequence);
                    //        for (int j = 0; j < stations.Length; j++)
                    //        {
                    //            if(stations[routingSequence[j]].Rate >0)
                    //            {
                    //                turn++;
                    //            }else if(stations[routingSequence[j]].Rate < 0)
                    //            {
                    //                turn--;
                    //            }
                    //            count++;
                    //            if(count %2 == 0)
                    //            {
                    //                if(turn >= 2)
                    //                {
                    //                    for(int b = j;b < stations.Length; b++)
                    //                    {
                    //                        if(stations[routingSequence[b]].Rate < 0)
                    //                        {
                    //                            tmp = routingSequence[b];
                    //                            routingSequence[b] = routingSequence[j];
                    //                            routingSequence[j] = tmp;
                    //                            turn = 0;
                    //                            break;
                    //                        }
                    //                    }
                    //                }
                    //                else if(turn <= -2)
                    //                {
                    //                    for (int b = j; b < stations.Length; b++)
                    //                    {
                    //                        if (stations[routingSequence[b]].Rate > 0)
                    //                        {
                    //                            tmp = routingSequence[b];
                    //                            routingSequence[b] = routingSequence[j];
                    //                            routingSequence[j] = tmp;
                    //                            turn = 0;
                    //                            break;
                    //                        }
                    //                    }
                    //                }
                    //            }                              
                    //        }
                    //        //encode
                    //        for (int c = 0;c < stations.Length; c++)
                    //        {
                    //            chromosomes[mutateID][routingSequence[c]] = c;
                    //        }
                    //        for (int a = stations.Length; a < numberOfGenes - 2; a++)
                    //        {
                    //            if (stations[a - stations.Length].Rate < 0)
                    //            {
                    //                chromosomes[mutateID][a] = rnd.Next(0,TruckCapacity);
                    //            }
                    //            else if (stations[a - stations.Length].Rate > 0)
                    //            {
                    //                chromosomes[mutateID][a] = -rnd.Next(0, TruckCapacity);
                    //            }
                    //        }
                    //        chromosomes[mutateID][numberOfGenes - 1] = rnd.Next(0, capacity + 1);//truck start routing time
                    //        //chromosomes[mutateID][numberOfGenes - 1] = rnd.Next(0, (int)time);//truck initial bike amount
                    //        break;
                    //    }
                    case PermutationMutation.Imminence:
                        {
                            //利用距離和急需服務站點的權重比例
                            int totalchange = (int)(.01 * stations.Length);int selectedstation = -1;
                            int selectedposition = -1;int change = -1;double min = double.MaxValue;
                            int nextstation = -1;bool flag = true;
                            for(int a = 0; a < totalchange; a++)
                            {
                                //抽一站來交換
                                selectedposition = rnd.Next(stations.Length/8); 
                                for(int c = 0; c < stations.Length; c++)
                                {
                                    if (chromosomes[mutateID][c] == selectedposition)
                                    {
                                        selectedstation = c;break;
                                    }
                                }
                                //search for that selected station
                                min = double.MaxValue; flag = true;
                                for (int b = 0; b < stations.Length; b++)
                                {
                                    if(FromTo[selectedstation, b]!=0 && min > FromTo[selectedstation, b])
                                    {
                                        //if (chromosomes[mutateID][b] > selectedposition + 1)//其實拿掉會比較好....
                                        //{
                                            min = FromTo[selectedstation, b];
                                            change = b;
                                        //}
                                    }
                                    if(chromosomes[mutateID][b] == selectedposition+ 1 && flag)
                                    {
                                        nextstation = b;flag = false;
                                    }
                                }
                                //if (change >= stations.Length || nextstation >= stations.Length) throw new Exception();
                                if(change != nextstation )
                                {
                                    //swap 二者的順序
                                   // chromosomes[mutateID][change] = chromosomes[mutateID][nextstation];
                                   //chromosomes[mutateID][nextstation] = selectedposition + 1;
                                    chromosomes[mutateID][nextstation] = chromosomes[mutateID][change];
                                    chromosomes[mutateID][change] = selectedposition + 1;
                                }
                               // if (chromosomes[mutateID][nextstation] >= stations.Length || chromosomes[mutateID][change] >= stations.Length) throw new Exception();
                            }
                            int mutateNumber = (int)(.1 * stations.Length);
                            int position = -1;
                            for (int a = 0; a < mutateNumber; a++)
                            {
                                position = rnd.Next(stations.Length);
                                if (stations[position].Rate < 0)
                                {
                                    chromosomes[mutateID][position+stations.Length] = rnd.Next(0, TruckCapacity);
                                }
                                else if (stations[position].Rate > 0)
                                {
                                    chromosomes[mutateID][position + stations.Length] = -rnd.Next(0, TruckCapacity);
                                }
                            }
                            chromosomes[mutateID][numberOfGenes - 1] = rnd.Next(0, capacity + 1);//truck start routing time
                            break;
                        }
                }
            }
        }

        public static List<List<T>> Permutations<T>(List<T> list)
        {
            List<List<T>> perms = new List<List<T>>();
            if (list.Count == 0)
                return perms; // Empty list.
            float factorial = 1;
            for (int i = 2; i <= list.Count; i++)
                factorial *= i;
            if (!float.IsInfinity(factorial))
            {
                for (int v = 0; v < factorial; v++)
                {
                    List<T> s = new List<T>(list);
                    int k = v;
                    for (int j = 2; j <= list.Count; j++)
                    {
                        int other = (k % j);
                        T temp = s[j - 1];
                        s[j - 1] = s[other];
                        s[other] = temp;
                        k = k / j;
                    }
                    perms.Add(s);
                }
            }
            return perms;

        }

    }
}
