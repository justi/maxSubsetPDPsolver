using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Media;

namespace WindowsFormsApplication2
{
    public partial class GeneticAlgorithm : Form
    {
        public GeneticAlgorithm()
        {
            InitializeComponent();
        }

        public List<int> segments;
        public int maxSegment;
        //rozmiar populacji
        private int _populationSize;
        //wspolczynnik mutacji
        private double _mutationRatio;
        //wspolczynnik krzyzowania
        private double _crossingRatio;
        //maksymalna ilość generacji
        public int maxGenerationCount = 100000;
        //najlepsza wartosc punktowa dla kazdej z generacji
        private int _takeBestCount;
        public List<int> maxValues;
        // lista osobnikow w danej generacji
        private List<Gene> genes = new List<Gene>();
        public int maxK;
        private int _optimalK;
        public int median;
        public int maxSegmentsCount;
        public int maxPointsFromData;
        Random _random = new Random();
        PlotGraph plotG;
        Gene bestValidGene = null;
        private List<Gene> bestValidGenes = new List<Gene>();
        List<Gene> startPop = new List<Gene>();
        List<Gene> otherResults = new List<Gene>();
        DateTime startTime;
        public int try_n_times = 5; // ile razy ma probowac dokonac mutacji w przyapdku niepowodzenia

        public int PopulationSize
        {
            set { _populationSize = value; }
        }

        public double MutationRatio
        {
            set { _mutationRatio = value; }
        }

        public double CrossingRatio
        {
            set { _crossingRatio = value; }
        }

        public int TakeBestCount
        {
            set { _takeBestCount = value; }
        }

        public int  OptimalK
        {
            set { _optimalK = value; }
        }

        public void TerminateCalculations()
        {
            backgroundWorker1.CancelAsync();
        }

        private void calculateMaxCutsFromData() // ile maksymalnie mogloby byc ciec dla podanej ilosci segmentow
        {
            var seg = segments.Count;
            maxK = (int)(1 + Math.Sqrt(1 + 8*seg))/2-2;
        }

        private void calculateMaxSegmentsFromData() // z ilu segementow bedzie skladala się najlepsza sekwencja z max. iloscia ciec
        {
            maxSegmentsCount = ((maxK + 1) * (maxK + 2)) /2;
        }

        private void calculateMaxPointsFromData() // ile punktow maksymalnie mozna uzyskac przy dostepnych danych wejsciowych (segmentach)
        {
            maxPointsFromData = maxSegmentsCount;
        }

        private void calculateMedian()
        {
            var seg = segments.Count;
            segments.Sort();
            int index = (seg / 2);
            median = segments[index];
        }

        private void GeneticAlgorithm_Load(object sender, EventArgs e)
        {
            startTime = DateTime.Now;
            calculateMaxCutsFromData();
            calculateMaxSegmentsFromData();
            calculateMaxPointsFromData();
            calculateMedian();
            
            toolStripProgressBar1.Maximum = maxGenerationCount;
            toolStripProgressBar1.Minimum = 0;
            toolStripProgressBar1.Value = 0;
            toolStripStatusLabel1.Text = "obliczenia w toku ...";

            trackBar1.Minimum = 0;
            trackBar1.Maximum = maxPointsFromData;
            trackBar1.Value = trackBar1.Maximum;
            label3.Text = trackBar1.Value.ToString();
            var tmpVal = Decimal.Round((decimal)(trackBar1.Value - trackBar1.Minimum) / (trackBar1.Maximum - trackBar1.Minimum) * 100, 2);
            //label7.Text = tmpVal.ToString() + " %";

            backgroundWorker1.RunWorkerAsync();

            maxSegment = segments.Max();
            maxValues = new List<int>();

            plotG = new PlotGraph(this.maxGenerationCount, this.maxPointsFromData, 400, 200, Color.Transparent);
            this.Width = 700;
            this.Height = 450;
        }

        public void refreshPlotGraph(List<int> values)
        {

            pictureBox1.Image = plotG.Draw(values);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bgw = sender as BackgroundWorker;

            // inicjalizacja populacji v1.0
            /*for (int i = 0; i < _populationSize; i++)
            {
                int val = (int)(_random.NextDouble() * segments.Count);
                Gene gene = new Gene(segments[val] + 1);
                gene.points = genePointsValue(gene);
                genes.Add(gene);
                //Invoke(new Action(() => listView1.Items.Add(gene.PrintBody() + " " + gene.points.ToString())));
                //listView2.Items.Add(genes[i].CountCuts().ToString());
            }*/

            // inicjalizacja populacji v2.0
            genes = generatePopulation2();

            for (int generation = 0; generation < maxGenerationCount; generation++)
            {
                List<Gene> GenerationGeneList = new List<Gene>();
                int maxGenePoint = -10000;

                for (int j = 0; j < _populationSize - _takeBestCount; j++)
                {
                    List<Gene> result = new List<Gene>();
                    double val = _random.NextDouble();

                    //losujem 2 dowolne geny
                    int one = (int)(_random.NextDouble() * _populationSize);
                    int two = (int)(_random.NextDouble() * _populationSize);

                    Gene first = (Gene)genes[one].Clone();
                    Gene second = (Gene)genes[two].Clone();

                    int firstBodyLen = first.body.Length;
                    int secondBodyLen = second.body.Length;

                    if (val < _crossingRatio) // krzyzowanie
                    {
                        var crossingType = _random.NextDouble();
                        // jednopunktowe losowe
                        if (crossingType < 0.5 && firstBodyLen > 2 && secondBodyLen > 2) 
                            OneCrossPoint(result, first, second, firstBodyLen, secondBodyLen);
                        // jednopunktowe polowkowe
                        else if (crossingType >= 0.6 && crossingType < 0.95 && firstBodyLen > 2 && secondBodyLen > 2) 
                            OneHalfCrossPoint(result, first, second, firstBodyLen, secondBodyLen);
                        // polacz oba geny
                        else JoinCrossing(result, first, second, firstBodyLen, secondBodyLen);
                    }
                    // nie krzyzuj
                    else { }

                    int points1 = first.points;
                    int points2 = second.points;
                    
                    if (val < _mutationRatio) //mutacja
                    {
                        Gene gene;
                        if (points1 > points2) gene = first;
                        else gene = second;

                        int gene_cuts = gene.CountCuts();
                        // za duzo ciec, zmniejszamy k przez mutacje
                        if (gene_cuts > maxK) DecrementCutsMutation(result, gene);  
                        else if (gene_cuts == maxK) 
                        {
                            double mut_type = _random.NextDouble();
                            // utrzymujemy ta sama liczbe ciec
                            if (mut_type < 0.33) NoCutsChangeMutation(result, gene);
                            // zmniejszamy liczbe ciec
                            else DecrementCutsMutation(result, gene);                
                        }
                        else // za malo ciec, zwiekszamy k przez mutacje
                        {
                            double mut_type = _random.NextDouble();
                            if (mut_type < 0.5) IncrementCutsMutation(result, gene, gene_cuts);
                            else if (mut_type >= 0.5 && mut_type < 0.9) NoCutsChangeMutation(result, gene);//SimpleMutation(result, gene);
                            // mutuj poprzez zamiane
                            else CrossMutation(result, gene);
                        }             
                    }

                    if (result.Count == 0) // nie bylo ani mutacji ani krzyzowania
                    {
                        if (points1 > points2)
                        {
                            Array.Reverse(first.body);
                            result.Add(first);
                        }
                        else
                        {
                            Array.Reverse(second.body);
                            result.Add(second);
                        }
                    }

                    // wybieramy najlepszy gen z rezultatow
                    int resCount = result.Count;

                    if (resCount > 1)
                    {
                        for (int n = 0; n < resCount; n++)
                        {
                            int points = result[n].points;
                            if (maxGenePoint < points && result[n].isValid == true) maxGenePoint = points;
                        }
                        result = result.OrderByDescending(item => item.points).ToList();
                        if (result[1].points >0) otherResults.Add(result[1]);
                    }
                    else
                    {
                        int points = result[0].points;
                        if (maxGenePoint < points) maxGenePoint = points;
                    }
                    
                    
                    // najlepszy wynik dodajemy do glownej listy
                    GenerationGeneList.Add(result[0]);                  
                }
                genes = genes.OrderByDescending(item => item.points).ToList();
                for (int j = 0; j < _takeBestCount; j++)
                {
                    GenerationGeneList.Add(genes[j]); // dodanie 3 najlepszych z poprzedniego pokolenia
                }
                // utworzenie listy populacji dla nastepnego kroku
                genes = GenerationGeneList;
                genes = genes.OrderByDescending(item => item.points).ToList();
                var bestGene = genes[0];
                
                var bestVal = bestGene.points;
                maxValues.Add(bestVal);

                var restartVal = (int)(20000 / maxPointsFromData);
                if (restartVal < 5) restartVal = 5;
                int tmpTrackBarVal = 0;
               

                if (bestGene.isValid == true)
                {
                    bestValidGene = bestGene;
                    if (bestValidGenes.Count == 0)
                    {
                        bestValidGenes.Add(bestValidGene);

                    }
                    else if (bestValidGenes.Last().points < bestValidGene.points && !bestValidGenes.Any(item => item.body == bestValidGene.body))
                    {
                        bestValidGenes.Add(bestValidGene);
                        Invoke(new Action(() => 
                            resultsListView.Items.Add("P: "+ bestValidGene.points.ToString() + ", c: " 
                            + bestValidGene.CountCuts().ToString() 
                            + ", t: " + totalSecondLabelCount.Text + "[sek]")));
                    }
                }

                Invoke(new Action(() =>
                {
                    tmpTrackBarVal = trackBar1.Value;
                    label2.Text = "Punkty: " + bestVal.ToString();
                    if (bestValidGene != null)
                    {
                        label1.Text = bestValidGenes.Last().PrintBody();
                        label6.Text = "Długość: " + (bestValidGenes.Last().body.Count() - 1).ToString();
                        label8.Text = "Zapis zbioru: " + bestValidGenes.Last().PrintBodyAsSet();
                        label10.Text = "Liczba ciec: " + bestValidGenes.Last().CountCuts().ToString();   
                    }
                    
                    label9.Text = "maxK: " + maxK.ToString();                  
                    label13.Text = "Restart po: " + restartVal.ToString();
                    toolStripProgressBar1.Increment(1);
                    refreshPlotGraph(maxValues);
                }));

                updateTime();
                
                if ((generation % restartVal) == (restartVal - 1))
                {
                    bool needChanges = checkIfNoChanges(restartVal - 1);
                    
                    if (needChanges)
                    {
                        addToResultsNewGenes3(genes); ////////////////////////////////////////////////////////////////////////
                    }
                }

                if (maxGenePoint >= tmpTrackBarVal || bgw.CancellationPending)
                {
                    if (bgw.CancellationPending)
                        e.Cancel = true;
                    Invoke(new Action(() => button1.Enabled = false));
                    break;
                }
            }
            
            Invoke(new Action(() =>
            {
                if (bestValidGenes.Count > 0)
                {
                    int bestVG = bestValidGenes.Last().CountCuts();

                    label17.Text = _optimalK.ToString();
                    label15.Text = bestVG.ToString();
                    label14.Text = Decimal.Round((decimal)bestVG / _optimalK * 100, 2).ToString() + " %";
                }
                else
                {
                    label17.Text = _optimalK.ToString();
                    label15.Text = "Brak rozwiązania";
                    label14.Text = Decimal.Round((decimal)0 / _optimalK * 100, 2).ToString() + " %";
                }   
                
                toolStripStatusLabel1.Text = "Obliczenia zakończono.";
                toolStripProgressBar1.Value = 0;
                beepPing();
            }));
        }

        private void CrossMutation(List<Gene> result, Gene gene)
        {
            var gene_len = gene.body.Length;
            Gene crossedGene = new Gene(gene_len + 1); // wiekszy o jeden
            int pos = (int)(_random.NextDouble() * (gene_len - 2)) + 1;
            Array.Copy(gene.body, pos, crossedGene.body, 1, (gene_len - pos));
            Array.Copy(gene.body, 1, crossedGene.body, gene_len - pos + 1, pos - 1);
            crossedGene.body[0] = 1;
            crossedGene.body[gene_len] = 1;
            crossedGene.points = genePointsValue(crossedGene);
            result.Add(crossedGene);
        }

        private void SimpleMutation(List<Gene> result, Gene gene)
        {
            var count = (int)(_random.NextDouble() * 10) + 1;
            for (int n = 0; n < count; n++)
            {
                int pos = (int)(_random.NextDouble() * (gene.body.Length - 2)) + 1;
                gene.body[pos] = 1 - gene.body[pos]; // zmiana 0 na 1 i odwrotnie
            }
            gene.points = genePointsValue(gene);
            result.Add(gene);
        }

        private void IncrementCutsMutation(List<Gene> result, Gene gene, int gene_cuts)
        {
            if (gene_cuts < gene.body.Length - 2) // jesli istnieje przynajmniej jedno "0"
            {
                for (var ni = 0; ni < try_n_times; ni++) // probuj kilka razy
                {
                    int pos = (int)(_random.NextDouble() * (gene.body.Length - 2)) + 1;
                    if (gene.body[pos] == 0)
                    {
                        gene.body[pos] = 1;
                        gene.points = genePointsValue(gene);
                        break;
                    }
                }
            }
            result.Add(gene);
        }

        private void DecrementCutsMutation(List<Gene> result, Gene gene)
        {
            for (var ni = 0; ni < try_n_times; ni++) // probuje kilka razy
            {
                int pos = (int)(_random.NextDouble() * (gene.body.Length - 2)) + 1;
                if (gene.body[pos] == 1)
                {
                    gene.body[pos] = 0;
                    gene.points = genePointsValue(gene);
                    break;
                }
            }
            result.Add(gene);
        }

        private List<Gene> generatePopulation2()
        {
            int pos1, pos2;
            for (int i = 0; i < _populationSize; i++)
            {
                int ok = 0;
                if (segments.Count > 1)
                {
                    do
                    {
                        pos1 = (int)(_random.NextDouble() * segments.Count);
                        pos2 = (int)(_random.NextDouble() * segments.Count);

                        if (pos1 != pos2)
                        {
                            foreach (var el in segments)
                            {
                                if (el == segments[pos1] + segments[pos2])
                                {
                                    ok = 1;
                                    Gene gene = new Gene(el + 1);
                                    gene.body[segments[pos1]] = 1;
                                    gene.points = genePointsValue(gene);
                                    startPop.Add(gene);
                                    break;
                                }
                            }
                        }
                        else // wylosowano 2 razy ten sam element, losuj dalej
                            ok = 0;

                    } while (ok == 0);
                }
                else // nie losuj, jest tylko 1 element
                {
                    Gene gene = new Gene(segments[0] + 1);
                    gene.points = genePointsValue(gene);
                    startPop.Add(gene);
                }
            }
            return startPop;
        }

        private void NoCutsChangeMutation(List<Gene> result, Gene gene)
        {
            int first_pos = (int)(_random.NextDouble() * (gene.body.Length - 2)) + 1;
            int second_pos = (int)(_random.NextDouble() * (gene.body.Length - 2)) + 1;

            gene.body[first_pos] = 1 - gene.body[first_pos]; // zmiana 0 na 1 i odwrotnie
            gene.body[second_pos] = 1 - gene.body[second_pos]; // zmiana 0 na 1 i odwrotnie

            gene.points = genePointsValue(gene);
            result.Add(gene);
        }

        private void JoinCrossing(List<Gene> result, Gene first, Gene second, int firstBodyLen, int secondBodyLen)
        {
            var lenghtVal = firstBodyLen + secondBodyLen - 1;
            Gene crossedGene = new Gene(lenghtVal);
            first.body.CopyTo(crossedGene.body, 0);
            second.body.CopyTo(crossedGene.body, first.body.Length - 1); // nadpisujemy ostatnia jedynke - ciecie
            crossedGene.points = genePointsValue(crossedGene);
            result.Add(crossedGene);
        }

        private void OneHalfCrossPoint(List<Gene> result, Gene first, Gene second, int firstBodyLen, int secondBodyLen)
        {
            int crossPoint = (int)(_random.NextDouble() * (secondBodyLen - 1) + 1);
            Gene firstCrossedGene = new Gene(firstBodyLen + (secondBodyLen - crossPoint));
            Gene secondCrossedGene = new Gene(crossPoint + 1);

            Array.Copy(first.body, firstCrossedGene.body, 0);
            Array.Copy(second.body, crossPoint, firstCrossedGene.body, firstBodyLen, (secondBodyLen - crossPoint));
            Array.Copy(second.body, secondCrossedGene.body, crossPoint + 1);
            secondCrossedGene.body[crossPoint] = 1;


            firstCrossedGene.points = genePointsValue(firstCrossedGene);
            result.Add(firstCrossedGene);

            secondCrossedGene.points = genePointsValue(secondCrossedGene);
            result.Add(secondCrossedGene);
        }

        private void OneCrossPoint(List<Gene> result, Gene first, Gene second, int firstBodyLen, int secondBodyLen)
        {
            int firstCrossPoint = (int)(_random.NextDouble() * (firstBodyLen - 1) + 1);
            int secondCrossPoint = (int)(_random.NextDouble() * (secondBodyLen - 1) + 1);

            Gene firstCrossedGene = new Gene(firstCrossPoint + (secondBodyLen - secondCrossPoint));
            Gene secondCrossedGene = new Gene(secondCrossPoint + (firstBodyLen - firstCrossPoint));

            Array.Copy(first.body, firstCrossedGene.body, firstCrossPoint);
            Array.Copy(second.body, secondCrossPoint, firstCrossedGene.body, firstCrossPoint, (secondBodyLen - secondCrossPoint));
            Array.Copy(second.body, secondCrossedGene.body, secondCrossPoint);
            Array.Copy(first.body, firstCrossPoint, secondCrossedGene.body, secondCrossPoint, (firstBodyLen - firstCrossPoint));


            firstCrossedGene.points = genePointsValue(firstCrossedGene);
            result.Add(firstCrossedGene);

            secondCrossedGene.points = genePointsValue(secondCrossedGene);
            result.Add(secondCrossedGene);
        }

        private void addToResultsNewGenes(List<Gene> genes)
        {
            int count = bestValidGenes.Count;
            int n = (int)(_random.NextDouble() * count) + 1;

            for (int i = 1; i < n && i < _populationSize - 1 ; i++)
            {
                genes[_populationSize - i] = bestValidGenes[i - 1];
            }
        }

        private void addToResultsNewGenes2(List<Gene> genes)
        {
            int n = (int)(_random.NextDouble() * _populationSize ) + 1;

            for (int i = 1; i < n && i < _populationSize - 1; i++)
            {
                int p = (int)(_random.NextDouble() * _populationSize);
                genes[_populationSize - i] = (Gene)startPop[p].Clone();
            }
        }

        private void addToResultsNewGenes3(List<Gene> genes)
        {
            int n = (int)(_random.NextDouble() * _populationSize / 2) + 1;
            var pStart = bestValidGenes.Count();
            var oRes = bestValidGenes.Count();

            for (int i = 1; i < n; i++)
            {
                int p = (int)(_random.NextDouble() * pStart);
                genes[i] = (Gene)startPop[p].Clone();
                Array.Reverse(genes[_populationSize - i].body);
            }

            for (int i = n; i < _populationSize; i++)
            {
                int p = (int)(_random.NextDouble() * oRes);
                genes[i] = (Gene)otherResults[p].Clone();
            }
            var bvd = bestValidGenes.OrderByDescending(item => item.points).ToList();
            if (bvd.Count < 3) genes[0] = (Gene)bvd[(int)(_random.NextDouble() * bvd.Count / 2)].Clone();
            else genes[0] = (Gene)bvd[(int)(_random.NextDouble() * 3)].Clone();
        }

        private void addToResultsNewGenes4(List<Gene> genes)
        {
            var g = bestValidGenes.Count - 1;
            int count = 0;
            if (g > -1)
            {
                Gene gene = (Gene)bestValidGenes[g].Clone();
                int cuts = gene.CountCuts();
                int size = gene.body.Length;
                Array.Reverse(gene.body);

                for (int i = 1; (i < size - 1) && (count < _populationSize); i++)
                {
                    if (gene.body[i] == 1)
                    {
                        Gene tmpGene = new Gene(i+1);
                        Array.Copy(gene.body, 0, tmpGene.body, 0, i + 1);
                        tmpGene.points = genePointsValue(tmpGene);
                        genes[count++] = tmpGene;
                        
                        tmpGene = new Gene(size - i);
                        Array.Copy(gene.body, i, tmpGene.body, 0, size - i);
                        tmpGene.points = genePointsValue(tmpGene);
                        genes[count++] = tmpGene;

                    }
                }
                
            }
        }

        private void updateTime()
        {
            DateTime stopTime = DateTime.Now;
            TimeSpan duration = stopTime - startTime;
            Invoke(new Action(() =>
            {
                hoursLabel.Text = "Godziny:";
                minutesLabel.Text = "Minuty:";
                secondLabel.Text = "Sekundy:";
                milisecondLabel.Text = "Milisek:";
                totalSecondLabel.Text = "Sekund:";

                hoursLabelCount.Text = duration.Hours.ToString();
                minutesLabelCount.Text = duration.Minutes.ToString();
                secondLabelCount.Text = duration.Seconds.ToString();
                milisecondLabelCount.Text = duration.Milliseconds.ToString();
                totalSecondLabelCount.Text = duration.TotalSeconds.ToString();

            }));
        }

        private int genePointsValue(Gene gene_to_evaluate)
        {
            List<int> cuts = new List<int>();
            List<int> gene_segments = new List<int>();
            List<int> temp_gene_segments = new List<int>();
            List<int> temp_segments = segments.Where(e => true).ToList();

            for (int i = 0; i < gene_to_evaluate.body.Length; i++)
            {
                if (gene_to_evaluate.body[i] == 1)
                {
                    cuts.Add(i);
                }
            }
            int cutsCount = cuts.Count;
            for (int i = 0; i < cutsCount - 1; i++)
            {
                for (int j = i + 1; j < cutsCount; j++)
                {
                    gene_segments.Add(cuts[j] - cuts[i]);
                    temp_gene_segments.Add(cuts[j] - cuts[i]);
                }
            }
            int points = 0;
            int gs_count = gene_segments.Count;
            // jesli prawidlowa sekwencja to temp_gene_segments powinno byc puste a temp_segments jak najmneij elementów (zostana tylko FP)
            for (int i = 0; i < gs_count; i++)
            {
                if (temp_segments.Contains(gene_segments[i]))
                {
                    temp_segments.Remove(gene_segments[i]);
                    points++;
                    temp_gene_segments.Remove(gene_segments[i]);
                }
            }
            //maxCurrentK = Gene.calculateMaxCutsFromCurrentData(gs_count);
            var gte_len = gene_to_evaluate.body.Length;
            if (gte_len - 1 > maxSegment) // kara za przekroczenie dlugosci
                points -= (int)((gte_len - maxSegment)* _random.NextDouble());
            var tgs_count = temp_gene_segments.Count;
            if (tgs_count != 0)
            {
                points -= (int)((tgs_count) * maxK);
            }
            else
                gene_to_evaluate.isValid = true;
            var gte_count = gene_to_evaluate.CountCuts();
            if (gte_count > maxK)
                points -= (gte_count - maxK);// +(int)(maxK * _random.NextDouble());

             return points;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.TerminateCalculations();
            button1.Enabled = false;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label3.Text = trackBar1.Value.ToString();
            var tmpVal = Decimal.Round((decimal)(trackBar1.Value - trackBar1.Minimum) / (trackBar1.Maximum - trackBar1.Minimum)* 100, 2);
            //label7.Text = tmpVal.ToString() + " %";
        }

        private bool checkIfNoChanges(int n)
        {
            bool noChanges;
            noChanges = true;

            int lastIndex = maxValues.Count - 1;
            int val = maxValues[lastIndex];
            for (int i = 1; i < n; i++)
            {
                if (maxValues[lastIndex - i] != val)
                {
                    noChanges = false;
                    break;
                }
            }
            return noChanges;
        }

        public void beepPing()
        {
            SystemSounds.Beep.Play();
            System.Threading.Thread.Sleep(1000);
            SystemSounds.Exclamation.Play();
        }
    }
}
