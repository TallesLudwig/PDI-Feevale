﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Image_Processing.Model;

namespace Image_Processing.Controllers.Extraction
{
    public class CircleExtractionController
    {
        public List<Circle> Circles = new List<Circle>();
        private Stack<Color> Colors = new Stack<Color>();

        #region Masks

        private static int[,] Square0 = new int[,] {
            { 0, 0, 0 },
            { 0, 255, 255 },
            { 255, 255, 255 }
        };

        private static int[,] Square1 = new int[,]
        {
            { 0, 0, 0 },
            { 255, 255, 0 },
            { 255, 255, 255 }
        };

        #endregion

        public Bitmap CircleExtraction(Image original)
        {
            this.Circles = new List<Circle>();
            this.Colors.Push(Color.Red);
            this.Colors.Push(Color.Blue);
            this.Colors.Push(Color.Green);
            this.Colors.Push(Color.Yellow);

            Bitmap image = (Bitmap)original.Clone();
            int width = image.Width - 1;
            int height = image.Height - 1;

            ToBlackWhite(ref image, width, height);

            for (int y = 1; y < height; y++)
            {
                for (int x = 1; x < width; x++)
                {
                    CircleEgdesValidation(image, y, x);
                }
            }

            Circles.RemoveAll(x => x.Point90.Ending.X == 0 || x.Point90.Ending.Y == 0);

            foreach (var c in Circles)
            {
                IdentifyingCircles(image, c, width, height);
                FloodFill(ref image, c, Color.White, Colors.Pop());
            }

                return image;
        }

        #region finding 90º

        private void CircleEgdesValidation(Bitmap image, int y, int x)
        {
            var isCirclesNotEmpyt = Circles.Any();
            if (ValidadeMask(image, y, x, Square0))
            {
                var newCircle = new Circle() { Point90 = new CirclePoint { Begin =  new Point() { X = x, Y = y } } };
                Circles.Add(newCircle);
            }
            else if (isCirclesNotEmpyt && ValidadeMask(image, y, x, Square1))
            {
                var squareMach = Circles.LastOrDefault(s => s.Point90.Begin.Y == y);
                if (squareMach != null)
                {
                    squareMach.Point90.Ending = new Point() { X = x, Y = y };
                    Color pixel = image.GetPixel(squareMach.Point90.Center.X, squareMach.Point90.Center.Y - 1);

                    if (pixel.R != 0 || squareMach.Point90.Ending.X == 0 )
                    {
                        Circles.Remove(squareMach);
                    }
                }
            }
        }

        private bool ValidadeMask(Bitmap image, int y, int x, int[,] mask)
        {
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    Color pixel = image.GetPixel(x + (j - 1), y + (i - 1));
                    var maskValue = mask[i, j];
                    if (pixel.R != maskValue)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion

        #region finding the rest

        private void IdentifyingCircles(Bitmap image, Circle circle, int width, int height)
        {
            var currentPoint = image.GetPixel(circle.Point90.Center.X, circle.Point90.Center.Y);

            for (int i = circle.Point90.Center.Y; i < height; i++)
            {
                currentPoint = image.GetPixel(circle.Point90.Center.X, i);
                if (currentPoint.R == 0)
                    break;

                circle.Point270 = new CirclePoint //// Não é preciso
                {
                    Begin = new Point { X = circle.Point90.Begin.X, Y = i },
                    Ending = new Point { X = circle.Point90.Ending.X, Y = i }
                };
            }

            int centerX = circle.Point90.Center.X;
            int centerY = circle.Point90.Center.Y + (circle.Point270.Center.Y - circle.Point90.Center.Y)/2;

            for (int i = centerX; i < width; i++)
            {
                currentPoint = image.GetPixel(i, centerY);
                if (currentPoint.R == 0)
                    break;

                circle.Point360 = new CirclePoint
                {
                    Begin = new Point { X = i, Y = centerY },
                    Ending = new Point { X = i, Y = centerY }
                };
            }

            for (int i = centerX; i > 1; i--)
            {
                currentPoint = image.GetPixel(i, centerY);
                if (currentPoint.R == 0)
                    break;

                circle.Point180 = new CirclePoint
                {
                    Begin = new Point { X = i, Y = centerY },
                    Ending = new Point { X = i, Y = centerY }
                };
            }

            circle.Center = new Point { X = circle.Point90.Center.X, Y = circle.Point360.Center.Y };
        }

        #endregion

        #region Foodfill

        private void FloodFill(ref Bitmap bmp, Circle circle, Color targetColor, Color replacementColor)
        {
            Stack<Point> pixels = new Stack<Point>();
            targetColor = bmp.GetPixel(circle.Center.X, circle.Center.Y);
            pixels.Push(circle.Center);
            circle.Color = replacementColor;

            while (pixels.Count > 0)
            {
                Point a = pixels.Pop();
                if (a.X < bmp.Width && a.X > 0 && a.Y < bmp.Height && a.Y > 0)//make sure we stay within bounds
                {

                    if (bmp.GetPixel(a.X, a.Y) == targetColor)
                    {
                        circle.AreaByPixel++;
                        bmp.SetPixel(a.X, a.Y, replacementColor);
                        pixels.Push(new Point(a.X - 1, a.Y));
                        pixels.Push(new Point(a.X + 1, a.Y));
                        pixels.Push(new Point(a.X, a.Y - 1));
                        pixels.Push(new Point(a.X, a.Y + 1));
                    }
                }
            }
            return;
        }

        #endregion

        private void ToBlackWhite(ref Bitmap result, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var p = result.GetPixel(x, y);
                    int a = p.A;
                    int r = p.R;
                    int g = p.G;
                    int b = p.B;
                    int avg = (r + g + b) / 3;
                    avg = avg < 128 ? 0 : 255;     // Converting gray pixels to either pure black or pure white
                    result.SetPixel(x, y, Color.FromArgb(a, avg, avg, avg));
                }
            }
        }
    }
}
