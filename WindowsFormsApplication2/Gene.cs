using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication2
{
    public class Gene : Object, ICloneable
    {
        private List<int> keyValList = new List<int>();
        public int[] body;
        public int points;
        public bool isValid;
        public int cuts;

        public Gene(int size)
        {
            this.body = Enumerable.Repeat(0, size).ToArray();
            // dodanie ograniczenia poczatowego i koncowego dla elementu, np:
            // odcinek o dlugosci 1 to [1,1]
            // odcinek o dlugosci 4 to [1,0,0,0,1]
            this.body[0] = 1;
            this.body[size - 1] = 1;
            this.isValid = false;
        }

        public string PrintBody()
        {
            var str = "";
            for(int i = 0; i< this.body.Length; i++)
            {
                str += this.body[i].ToString() + " ";
            }
            return str;
        }

        public string PrintBodyAsSet()
        {
            var str = "{";
            for (int i = 0; i < this.body.Length - 1; i++)
            {
                if (this.body[i] == 1)
                    str += i.ToString() + ", ";
            }
            str += this.body.Length - 1 + "}";
            return str;
        }

        public int CountCuts()
        {
            int cuts = 0;
            for (int i = 1; i < this.body.Length - 1; i++)
            {
                cuts += this.body[i];
            }
            this.cuts = cuts;
            return cuts;
        }

        public object Clone()
        {
            var newObj = new Gene(this.body.Length);
            newObj.points = points;
            newObj.keyValList = keyValList.ToList();
            newObj.body = (int[]) body.Clone();

            return newObj;
        }

        static public int calculateMaxCutsFromCurrentData(int segments_count) // ile maksymalnie mogloby byc ciec dla podanej ilosci segmentow
        {
            int seg = segments_count;
            int maxK = (int)(1 + Math.Sqrt(1 + 8 * seg)) / 2 - 2;
            return maxK;
        }
    }
}