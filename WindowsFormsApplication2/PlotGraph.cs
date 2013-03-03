using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace WindowsFormsApplication2
{
    public class PlotGraph
    {
        private int _width;
        private int _height;

        private Bitmap _bitmap;
        private Color _bgColor;

        private decimal _hSizeRatio;
        private decimal _vSizeRatio;

        private int _maxVal;

        public PlotGraph(int popSize, int maxVal, int width, int height, Color bgColor)
        {
            _width = width;
            _height = height;
            _bgColor = bgColor;
            _maxVal = maxVal;

            _vSizeRatio = Decimal.Zero;
            _hSizeRatio = Decimal.Zero;
            _hSizeRatio = (decimal)_width / popSize;
            _vSizeRatio = (decimal)_height / _maxVal;
        }
        public Bitmap Draw(List<int> vals)
        {
            // Create a new image and erase the background
            _bitmap = new Bitmap(_width + 50, _height + 85, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            
            Pen blackPen = new Pen(Color.Black, 1);
            Pen grayPen = new Pen(Color.LightGray, 1);
            Pen greenPen = new Pen(Color.GreenYellow, 1);
            Pen redPen = new Pen(Color.Salmon, 1);
            
            Graphics graphics = Graphics.FromImage(_bitmap);
            SolidBrush brush = new SolidBrush(_bgColor);
            
            graphics.FillRectangle(brush, 0, 10, _width, _height+10);
            //graphics.DrawRectangle(grayPen, 0, 10, _width + 49, _height + 84);
            
            graphics.DrawLine(redPen, 0, _height + 11, _width + 1, _height + 11);
            graphics.DrawLine(blackPen, 0, 10, 0, _height + 85);
            graphics.DrawLine(blackPen, _width + 1, 10, _width + 1, _height + 65);
            
            brush.Dispose();

            for (int j = 0; j <= _maxVal; j++)
            {
                if (j % (decimal)(20/_vSizeRatio) == 0)
                {
                    var yVal = (int)(_height - j * _vSizeRatio);
                    graphics.DrawLine(grayPen, new Point(1, yVal+10), new Point(_width, yVal+10));
                    graphics.DrawLine(blackPen, new Point(_width + 1, yVal+10), new Point(_width + 5, yVal+10));
                    graphics.DrawString((j).ToString(), new Font("Calibri", 10), new SolidBrush(Color.Black), _width + 10, yVal + 1);
                }
            }

            // Draw the plot

            Point lastPoint = new Point(0,0);
            Point currentPoint = new Point(0,0);
             
            if (vals.Count >= 1)
            {
                lastPoint.X = 0;
                if (vals.Count < _width) lastPoint.Y = (int)(_height - vals[0] * _vSizeRatio) + 10;
                else lastPoint.Y = (int)(_height - vals[vals.Count - _width] * _vSizeRatio) + 10;             
            }
            if (vals.Count <= _width)
            {
                graphics.DrawString("0", new Font("Calibri", 10), new SolidBrush(Color.Black), 0, _height + 70);
                graphics.DrawLine(blackPen, new Point(0, _height + 40), new Point(0, _height + 85));
                for (int i = 1; i < vals.Count; i++)
                {

                    currentPoint = new Point(i, (int)(_height - vals[i]  * _vSizeRatio)+10);
                    // print
                    graphics.DrawLine(blackPen, lastPoint, currentPoint);
                    if (i % 50 == 0)
                    {
                        graphics.DrawString(i.ToString(), new Font("Calibri", 10), new SolidBrush(Color.Black), i, _height + 70);
                        graphics.DrawLine(blackPen, new Point(i, _height + 30), new Point(i, _height + 85));
                    }
                    lastPoint = currentPoint;
                }  
            }
            else
            {
                for (int i = 1; i <= _width; i++)
                {
                    var tempI = vals.Count - _width + i;
                    currentPoint = new Point(i, (int)(_height - vals[tempI - 1]  * _vSizeRatio)+10);
                    // print
                    graphics.DrawLine(blackPen, lastPoint, currentPoint);
                    if ((tempI) % 50 == 0)
                    {
                        graphics.DrawString((tempI).ToString(), new Font("Calibri", 10), new SolidBrush(Color.Black), i, _height + 70);
                        graphics.DrawLine(blackPen, new Point(i, _height + 30), new Point(i, _height + 85));
                    }
                    lastPoint = currentPoint;
                }  
            }

            var xMaxVal = (int)(_height - _maxVal  * _vSizeRatio+10);
            graphics.DrawLine(greenPen, 1, xMaxVal, _width + 1, xMaxVal);
            //graphics.DrawString((_maxVal).ToString(), new Font("Calibri", 14), new SolidBrush(greenPen.Color), _width + 7, 0);

            blackPen.Dispose();
            greenPen.Dispose();
            redPen.Dispose();
            grayPen.Dispose();

            return _bitmap;
        }
    }
}
