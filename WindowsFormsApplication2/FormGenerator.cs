using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication2
{
    public partial class FormGenerator : Form
    {
        
        Random _random = new Random();

        private int _length;
        private int _segmentsCount;
        private int _cutsCount;
        private int _errorsCount;
        private double _errorsPercent;
        public List<int> errors;
        public int[] perfectBodyFromGenerator; // ciecia w formacie binarnym, 1 - ciecie na danej pozycji, 0 - brak ciecia
        public List<int> segments;
        public List<int> allSegments;
        private String _name;
        private String _comment;
        public int Length
        {
            get { return _length; }
            set { _length = value; }
        }
        
        public int SegmentsCount
        {
            get { return _segmentsCount; }
            set { _segmentsCount = value; }
        }
        public int CutsCount
        {
            get { return _cutsCount; }
            set { _cutsCount = value; }
        }
        public int ErrorsCount
        {
            get { return _errorsCount; }
            set { _errorsCount = value; }
        }
        public double ErrorsPercent
        {
            get { return _errorsPercent; }
            set { _errorsPercent = value; }
        }
        public String InstName
        {
            get { return _name; }
            set { _name = value; }
        }
        public String Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }
        public FormGenerator()
        {
            InitializeComponent();
            errors = new List<int>();
        }
        public string printIntList(List<int> list)
        {
            string result = "";
            foreach(var el in list)
            {
                result += el.ToString() + ", ";
            }
            return result;
        }
        public string printIntArray(int[] arr)
        {
            string result = "";
            foreach (var el in arr)
            {
                result += el.ToString() + ", ";
            }
            return result;
        }

        public List<int> createArrayFromString(String str)
        {
            List<int> result = new List<int>();
            String[] lines;
            lines = str.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var el in lines)
            {
                int res;
                int.TryParse(el, out res);
                result.Add(res);
            }
            return result;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "XML Files (.xml)|*.xml|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {  
                    using (var fs = new FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.Read))
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(fs);
                      
                        XmlNodeList nodeList = doc.SelectNodes("Data/Items/Item");
                        allSegments = new List<int>();
                        foreach (XmlNode node in nodeList)
                        {
                            XmlAttributeCollection attCol = node.Attributes;
                            allSegments.Add(int.Parse(attCol[0].Value)); 
                        }

                        allSegments.Sort();

                        nodeList = doc.SelectNodes("Data/Solution/Position");
                        perfectBodyFromGenerator = Enumerable.Repeat(0, nodeList.Count).ToArray();
                        for (int i = 0; i < nodeList.Count; i++)
                        {
                            perfectBodyFromGenerator[i] = int.Parse(nodeList[i].InnerText);
                        }

                        XmlNode nodeName = doc.SelectSingleNode("Data/Name");
                        InstName = nodeName.InnerText;

                        XmlNode nodeComment = doc.SelectSingleNode("Data/Comment");
                        Comment = nodeComment.InnerText;

                        XmlNode nodeLength = doc.SelectSingleNode("Data/Length");
                        Length = int.Parse(nodeLength.InnerText);

                        XmlNode nodeErrPerc = doc.SelectSingleNode("Data/ErrorsPercent");
                        ErrorsPercent = double.Parse(nodeErrPerc.InnerText);

                        XmlNode nodeErrCount = doc.SelectSingleNode("Data/ErrorsCount");
                        ErrorsCount = int.Parse(nodeErrCount.InnerText);

                        XmlNode nodeCutsCount = doc.SelectSingleNode("Data/CutsCount");
                        CutsCount = int.Parse(nodeCutsCount.InnerText);

                        XmlNode nodeSegCount = doc.SelectSingleNode("Data/SegmentsCount");
                        SegmentsCount = int.Parse(nodeSegCount.InnerText);
                       
                        fillLabelsAtSecondTab();

                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Błąd podczas otwierania pliku", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void fillLabelsAtSecondTab()
        {
            label9.Text = InstName.ToString();
            label13.Text = Length.ToString();
            label17.Text = SegmentsCount.ToString();
            label14.Text = ErrorsPercent.ToString();
            label8.Text = ErrorsCount.ToString();
            label22.Text = (SegmentsCount + ErrorsCount).ToString();
            label19.Text = CutsCount.ToString();
            textBox7.Enabled = false;
            textBox7.Text = printIntArray(perfectBodyFromGenerator);
            textBox8.Enabled = false;
            textBox8.Text = Comment.ToString();

            listView1.Clear();
            allSegments.Sort();

            for (int i = 0; i < allSegments.Count; i++)
            {
                listView1.Items.Add((allSegments[i]).ToString());
            }

            button4.Enabled = true;  
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DateTime myDate = DateTime.Now;
            var strDTime = myDate.ToString();
            
            var name = "";
            if (textBox1.Text == String.Empty) name = string.Format("undefined-{0:yyyy-MM-dd_hh-mm-ss}", myDate);
            else name = textBox1.Text;
            InstName = name;

            var comment = "";
            if (textBox5.Text == String.Empty) comment = "Brak";
            else comment = textBox5.Text;
            comment += ", data utworzenia: " + strDTime;
            Comment = comment;

            if (textBox4.Text != String.Empty) errors = createArrayFromString(textBox4.Text);
            else
            {
                ErrorsPercent = 0;
                ErrorsCount = 0;
                errors.Clear();
            }
            if (Length > 0)
            {
                generate();
                tabControl1.SelectTab("TabPage2");
                fillLabelsAtSecondTab();
                saveToXml();
            }
            else
            {
                MessageBox.Show("Wpisz długość sekwencji", "Uwaga", MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation);
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            try
            {
                var cuts = int.Parse(textBox3.Text);
                if (cuts > -1 )
                {
                    if (cuts >= Length)
                    {
                        textBox3.Text = (Length - 1).ToString();
                    }
                    else
                    
                    CutsCount = cuts;
                    SegmentsCount = ((CutsCount + 1) * (CutsCount + 2) / 2);
                    label4.Text = SegmentsCount.ToString();
                }     
            }
            catch (Exception)
            {
                label4.Text = "";   
            }    
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            errors.Clear();
            try
            {
                ErrorsPercent = double.Parse(textBox6.Text);
                if (ErrorsPercent > 0)
                {
                    textBox4.Enabled = true;
                    if (Length > 0 && SegmentsCount > 0)
                    {
                        ErrorsCount = (int)(SegmentsCount * ErrorsPercent / 100);
                        for (int i = 0; i < ErrorsCount; i++)
                        {
                            errors.Add((int)(_random.NextDouble() * Length + 1));
                        }
                        textBox4.Text = printIntList(errors);

                    }
                }

            }
            catch (Exception)
            {
                textBox4.Text = String.Empty;
                textBox4.Enabled = false;
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                var length = int.Parse(textBox2.Text);
                if (length > 0)
                {
                    Length = length;
                }
            }
            catch (Exception)
            {
                textBox2.Text = String.Empty;
            }
        }

        private void generate()
        {
            generateBody();
            computeSegments();
            addErrors();
        }

        private void generateBody()
        {
            this.perfectBodyFromGenerator = Enumerable.Repeat(0, Length + 1).ToArray();

            this.perfectBodyFromGenerator[0] = 1;
            this.perfectBodyFromGenerator[Length] = 1;

            int[] cuts;
            int cutsLen = perfectBodyFromGenerator.Length - 2; // bez poczatku i konca     
            cuts = Enumerable.Repeat(0, cutsLen).ToArray();
            int cutPosition;

            for (int i = 0; i < CutsCount; i++)
            {
                do
                {
                    cutPosition = (int)(_random.NextDouble() * cutsLen);
                }
                while (cuts[cutPosition] == 1);
                cuts[cutPosition] = 1;
            }

            // skopiowanie ciec do ciala genu
            for (int i = 0; i < cutsLen; i++)
            {
                this.perfectBodyFromGenerator[i + 1] = cuts[i]; // wypelniamy srodek perfectBodyFromGenerator
            }
        }
        private void computeSegments()
        {
            List<int> cuts = new List<int>();
            segments = new List<int>();
            for (int i = 0; i < perfectBodyFromGenerator.Length; i++)
            {
                if (perfectBodyFromGenerator[i] == 1)
                {
                    cuts.Add(i);
                }
            }
            int cutsCount = cuts.Count;
            for (int i = 0; i < cutsCount - 1; i++)
            {
                for (int j = i + 1; j < cutsCount; j++)
                {
                    segments.Add(cuts[j] - cuts[i]);
                }
            }
        }

        private void addErrors()
        {
            allSegments = new List<int>();
            allSegments = segments;
            for (int i = 0; i < errors.Count; i++)
            {
                allSegments.Add(errors[i]);
            }
        }

        private void saveToXml()
        {
            XmlTextWriter writer = new XmlTextWriter(InstName.ToString() + ".xml", null);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartDocument();

            writer.WriteStartElement("Data");

            writer.WriteElementString("Name", InstName.ToString());
            writer.WriteElementString("Comment", Comment.ToString());
            writer.WriteElementString("Length", Length.ToString());
            writer.WriteElementString("ErrorsPercent", ErrorsPercent.ToString());
            writer.WriteElementString("ErrorsCount", ErrorsCount.ToString());
            writer.WriteElementString("CutsCount", CutsCount.ToString());
            writer.WriteElementString("SegmentsCount", SegmentsCount.ToString());
            writer.WriteStartElement("Items");

            for (int i = 0; i < allSegments.Count; i++)
            {
                writer.WriteStartElement("Item");
                writer.WriteAttributeString("size", allSegments[i].ToString());
                writer.WriteEndElement(); // close Item
            }

            writer.WriteEndElement(); // close Items

            writer.WriteStartElement("Solution");

            for (int i = 0; i < perfectBodyFromGenerator.Length; i++)
            {
                writer.WriteElementString("Position", perfectBodyFromGenerator[i].ToString());
            }
            
            writer.WriteEndElement(); // close Solution

            writer.WriteEndElement(); // close Data
         
            writer.WriteEndDocument();
            writer.Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            GeneticAlgorithm GEForm = new GeneticAlgorithm();
            GEForm.segments = allSegments;
            heuristicParams(GEForm);
            GEForm.Show();
        }

        private void heuristicParams(GeneticAlgorithm GEForm)
        {
            int popSize;
            if (textBox10.Text == String.Empty) popSize = 100;
            else popSize = int.Parse(textBox10.Text);

            double mutRatio;
            if (textBox11.Text == String.Empty) mutRatio = 0.02;
            else mutRatio = double.Parse(textBox11.Text) / 100;

            double crossRatio;
            if (textBox9.Text == String.Empty) crossRatio = 0.99;
            else crossRatio = double.Parse(textBox9.Text) / 100;

            int takeBestC;
            if (textBox12.Text == String.Empty) takeBestC = 2;
            else takeBestC = int.Parse(textBox12.Text);

            GEForm.MutationRatio = mutRatio;
            GEForm.CrossingRatio = crossRatio;
            GEForm.TakeBestCount = takeBestC;
            GEForm.PopulationSize = popSize;
            GEForm.OptimalK = CutsCount;
        }
    }
}
