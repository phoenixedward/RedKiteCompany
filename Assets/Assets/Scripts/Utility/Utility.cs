﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
//using CsvHelper;
using Newtonsoft.Json;
using System.IO;

namespace RedKite
{
    static class Utility
    {
        static System.Random rand = new System.Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rand.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }


        public static VectorExtrema GetVectorExtrema(Vector3[] vecArray)
        {
            Vector3 tempMax = new Vector3(Vector3.negativeInfinity.x, Vector3.negativeInfinity.y,0);
            Vector3 tempMin = new Vector3(Vector3.positiveInfinity.x,Vector3.positiveInfinity.y,0);

            foreach (Vector3 vector in vecArray)
            {
                if (tempMax.x < vector.x)
                    tempMax.x = vector.x;
                if (tempMin.x > vector.x)
                    tempMin.x = vector.x;
                if (tempMax.y < vector.y)
                    tempMax.y = vector.y;
                if (tempMin.y > vector.y)
                    tempMin.y = vector.y;
                if (tempMax.z < vector.z)
                    tempMax.z = vector.z;
                if (tempMin.z > vector.z)
                    tempMin.z = vector.z;
            }

            Vector3 outMin = tempMin;
            Vector3 outMax = tempMax;

            return new VectorExtrema(outMin, outMax);

        }

        public static float DirectedDist(Vector3 start, Vector3 end)
        {
            float outDist = 0;

            if (end.x > start.x | end.y > start.y)
                outDist = -Vector3.Distance(start, end);
            else
                outDist = Vector3.Distance(start, end);

            return outDist;
        }


        public static Vector3[] CoordRange(Vector3 first, Vector3 second)
        {
            List<Vector3> outVectors = new List<Vector3>();

            outVectors.Add(first);

            for (int i = 0; i < Vector3.Distance(first, second); i++)
            {
                if (first.x < second.x)
                    outVectors.Add(Vector3.Lerp(first + (new Vector3(1, 0, 0) * i), second, 1 / Vector3.Distance(first + (new Vector3(1, 0, 0) * i), second)));
                else if (second.x < first.x)
                    outVectors.Add(Vector3.Lerp(first + (new Vector3(-1, 0, 0) * i), second, 1 / Vector3.Distance(first + (new Vector3(-1, 0, 0) * i), second)));
                else if (first.y < second.y)
                    outVectors.Add(Vector3.Lerp(first + (new Vector3(0, 1, 0) * i), second, 1 / Vector3.Distance(first + (new Vector3(0, 1, 0) * i), second)));
                else if (second.y < first.y)
                    outVectors.Add(Vector3.Lerp(first + (new Vector3(0, -1, 0) * i), second, 1 / Vector3.Distance(first + (new Vector3(0, -1, 0) * i), second)));

            }


            return outVectors.ToArray();
        }

        public static void LevelToJSON(Dictionary<int, Area> areas)
        {
            var json = JsonConvert.SerializeObject(areas, Newtonsoft.Json.Formatting.Indented);

            File.WriteAllText(@"C:\Users\phoen\UnitySource\Red Kite Company\Assets\Data\LevelData.json", json);
        }

        /*public static void LevelToCSV(char[,] map)
        {

            FileStream fs = null;
            List<Dictionary<int, char>> mapExpo = new List<Dictionary<int, char>>();

            for (int y = 0; y < TileMapper.H; y++)
            {
                Dictionary<int, char> next = new Dictionary<int, char>();
                for (int x = 0; x < TileMapper.W; x++)
                {
                    next[x] = map[x, y];
                }
                mapExpo.Add(next);
            }



            try
            {
                fs = new FileStream(@"C:\Users\phoen\source\repos\SandBox\Data\LevelData.csv", FileMode.OpenOrCreate);
                using (var writer = new StreamWriter(fs, Encoding.UTF8))
                using (var csv = new CsvWriter(writer))
                {
                    csv.WriteField("x,y");

                    foreach (KeyValuePair<int, char> record in mapExpo[0].OrderBy(x => x.Key))
                    {
                        csv.WriteField(record.Key);
                    }

                    csv.NextRecord();


                    for (int i = TileMapper.H - 1; i > -1; i--)
                    {
                        csv.WriteField(i);
                        foreach (KeyValuePair<int, char> record in mapExpo[i].OrderBy(x => x.Key))
                        {
                            csv.WriteField(record.Value);
                        }
                        csv.NextRecord();
                    }
                }
            }
            finally
            {
                if (fs != null)
                    fs.Dispose();
            }
        }
        */
        public static bool WithinBounds(Vector3 point, int width, int height)
        {
            bool tooHigh = point.y >= height;
            bool tooLow = point.y < 0;
            bool tooEast = point.x >= width;
            bool tooWest = point.x < 0;

            bool OOB = tooHigh | tooLow | tooEast | tooWest ? false : true;

            return OOB;
        }

        public static int ExclusiveRandom(int rangeStart, int rangeEnd, HashSet<int> exclude)
        {
            var range = Enumerable.Range(rangeStart, rangeEnd + rangeStart).Where(i => !exclude.Contains(i));

            int index = rand.Next(0, rangeEnd - exclude.Count);

            Debug.Log("range: " + range.ToList().Count);
            Debug.Log("index: " + index);
            return range.ElementAt(index);
        }

        public static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            checked
            {
                return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
            }
        }

        public static int ManhattanDistance(Vector3Int a, Vector3Int b)
        {
            checked
            {
                return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
            }
        }

        public static Vector3Int RandomCoords(Vector3Int first, Vector3Int second)
        {
            Vector3Int outVector = Vector3Int.zero;

            if (first.x > second.x)
                outVector.x = rand.Next((int)first.x, (int)second.x);
            else if (second.x > first.x)
                outVector.x = rand.Next((int)second.x, (int)first.x);

            if (first.y > second.y)
                outVector.y = rand.Next((int)first.y, (int)second.y);
            else if (second.y > first.y)
                outVector.y = rand.Next((int)second.y, (int)first.y);

            if (first.z > second.z)
                outVector.z = rand.Next((int)first.z, (int)second.z);
            else if (second.z > first.z)
                outVector.z = rand.Next((int)second.z, (int)first.z);

            return outVector;

        }

        public static Vector3Int[] CoordRange(Vector3Int first, Vector3Int second)
        {
            List<Vector3Int> outVectors = new List<Vector3Int>();

            if (first.x < second.x | first.y < second.y)
                outVectors.Add(first);
            if (first.x > second.x | first.y > second.y)
                outVectors.Add(second);


            for (int i = 0; i < Vector3.Distance(first, second); i++)
            {
                if (first.x < second.x)
                    outVectors.Add(Vector3Int.RoundToInt(Vector3.Lerp(first + Vector3.right * i, second, 1 / Vector3.Distance(first + Vector3.right * i, second))));
                else if (second.x < first.x)
                    outVectors.Add(Vector3Int.RoundToInt(Vector3.Lerp(first + Vector3.left * i, second, 1 / Vector3.Distance(first + Vector3.left * i, second))));
                else if (first.y < second.y)
                    outVectors.Add(Vector3Int.RoundToInt(Vector3.Lerp(first + Vector3.up * i, second, 1 / Vector3.Distance(first + Vector3.up * i, second))));
                else if (second.y < first.y)
                    outVectors.Add(Vector3Int.RoundToInt(Vector3.Lerp(first + Vector3.down * i, second, 1 / Vector3.Distance(first + Vector3.down * i, second))));

            }

            return outVectors.ToArray();
        }

        public static Vector3 ToVector3(Vector3Int inVector)
        {
            Vector3 outVector = new Vector3((float)inVector.x, (float)inVector.y, (float)inVector.z);

            return outVector;
        }


    }

    public struct VectorExtrema
    {
        public Vector3 min;
        public Vector3 max;
        public float width;
        public float height;
        public VectorExtrema(Vector3 _min, Vector3 _max)
        {
            min = _min;
            max = _max;
            width = _max.x - _min.x + 1;
            height = _max.y - _min.y + 1;
        }

    }
}
