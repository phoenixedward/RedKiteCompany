﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RedKite
{
    public class Segment
    {
        public bool IsPath;
        public bool IsRemoved;
        public bool IsCorner;
        public Orient Orientation;
        public Vector3 Min;
        public Vector3 Max;
        public Vector3 Center;
        public Vector3 Scale;
        public float Height;

        static int[,] space = new int[TileMapper.Instance.W,TileMapper.Instance.H];

        public Segment(Orient _orientation, Vector3 _min, Vector3 _max, float _height, bool _isPath = false, bool _isRemoved = false, bool _isCorner = false)
        {
            IsCorner = _isCorner;
            IsPath = _isPath;
            IsRemoved = _isRemoved;
            Orientation = _orientation;

            Min = _min;
            Max = _max;


            if (_orientation == Orient.North | _orientation == Orient.South)
            {
                Scale = new Vector3(Max.x - Min.x + 1, 1, _height);
                Center = Min + ((Max - Min) / 2);
            }
            else
            {
                Scale = new Vector3(1, Max.y - Min.y + 1, _height);
                Center = Min + ((Max - Min) / 2);
            }
            Height = _height;

        }

        public static bool operator ==(Segment a, Segment b)
        {
            bool matchMin = a.Min == b.Min;

            bool matchMax = a.Max == b.Max;

            return matchMin & matchMax;
        }

        public override bool Equals(object other)
        {
            return base.Equals(other);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator !=(Segment a, Segment b)
        {
            bool matchMin = a.Min != b.Min;

            bool matchMax = a.Max != b.Max;

            return matchMin & matchMax;
        }
    }
}
