using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using System.IO;

#if CLIENT

public partial class CommunityEntity
{

    public class BezierEasing
    {
        // the same process browsers use for the cubic-bezier css timing function
        // from the spec:
        //     "A cubic Bézier easing function is a type of easing function defined by four real numbers that specify the two control points, P1 and P2,
        //      of a cubic Bézier curve whose end points P0 and P3 are fixed at (0, 0) and (1, 1) respectively.
        //      The x coordinates of P1 and P2 are restricted to the range [0, 1]."
        // NOTE: Loosely based on https://github.com/gre/bezier-easing

        static Dictionary<BezierPoints, BezierEasing> cache = new Dictionary<BezierPoints, BezierEasing>();

        // these values modify the accuracy & performance of the curve's evaluation
        // while these are the values used in the javascript library you may find better values that produce good enough results with better performance
        static int NewtonItterations = 4;

        public float firstX = 0f;
        public float firstY = 0f;
        public float secondX = 1f;
        public float secondY = 1f;
        float[] _precomputedSections;

        static int _precomputeSize = 11;
        static float _precomputeStepSize = 1.0f / (_precomputeSize - 1.0f);
        bool _precomputed = false;


        // its beneficial to call this when we want to reset our session, like when leaving/switching servers
        public static void Clear()
        {
            cache.Clear();
        }

        // Helper function that caches BezierEasing Instances. supplying 4 points & time between 0 & 1 generates the curve & caches it, returning the value for time
        public static float Ease(float X1, float Y1, float X2, float Y2, float time)
        {
            var key = new BezierPoints(X1, Y1, X2, Y2);
            return Ease(key, time);
        }


        public static float Ease(BezierPoints points, float time)
        {
            if (!cache.TryGetValue(points, out BezierEasing easing))
                cache[points] = easing = new BezierEasing(points);

            return easing.GetPosition(time);
        }

        public BezierEasing(BezierPoints points) : this(points.P1, points.P2, points.P3, points.P4)
        {

        }

        public BezierEasing(float X1, float Y1, float X2, float Y2)
        {
            firstX = X1;
            firstY = Y1;
            secondX = X2;
            secondY = Y2;
            _precomputedSections = new float[_precomputeSize];
        }



        // returns the value on the curve for the unscaled time
        public float GetPosition(float unscaledTime)
        {
            if (!_precomputed) PreCompute();
            if (firstX == firstY && secondX == secondY) return unscaledTime; // linear
            if (unscaledTime == 0f) return 0f;
            if (unscaledTime == 1f) return 1f;
            return CalcBezier(ScaleTime(unscaledTime), firstY, secondY);
        }

        // the following 2 functions are part of a single math formula split up into 2 steps for clarity
        float StepA(float aA1, float aA2) { return 1.0f - 3.0f * aA2 + 3.0f * aA1; }
        float StepB(float aA1, float aA2) { return 3.0f * aA2 - 6.0f * aA1; }

        float CalcBezier(float time, float point1, float point2)
        {
            return ((StepA(point1, point2) * time + StepB(point1, point2)) * time + (3f * point1)) * time;
        }

        // Returns the estimated slope of the curve at the supplied time
        float GetSlope(float time)
        {
            return 3.0f * StepA(firstX, secondX) * time * time + 2.0f * StepB(firstX, secondX) * time + (3f * firstX);
        }

        // pre-calculates a set amount of points to spped up calculation
        void CalcSampleValues()
        {
            for (var i = 0; i < _precomputeSize; ++i)
            {
                _precomputedSections[i] = CalcBezier(i * _precomputeStepSize, firstX, secondX);
            }
        }

        // finds the surrounding pre-calculated sections around our unscaled time and refines it using NewtonRaphson's method
        float ScaleTime(float unscaledTime)
        {
            float intervalStart = 0.0f;
            int currentSample = 1;
            int lastSample = _precomputeSize - 1;

            // find the last precomputed section before unscaledTime
            for (; currentSample != lastSample && _precomputedSections[currentSample] <= unscaledTime; ++currentSample)
            {
                intervalStart += _precomputeStepSize;
            }
            --currentSample;

            // divide distance from last section by distance between last and next section
            float distance = (unscaledTime - _precomputedSections[currentSample]) / (_precomputedSections[currentSample + 1] - _precomputedSections[currentSample]);
            float timeGuess = intervalStart + distance * _precomputeStepSize;
            float initialSlope = GetSlope(timeGuess);
            if (initialSlope == 0.0f)
            {
                return timeGuess;
            }
            else
            {
                return NewtonRaphsonIterate(unscaledTime, timeGuess);
            }
        }

        void PreCompute()
        {
            _precomputed = true;
            if (firstX != firstY || secondX != secondY)
                CalcSampleValues();
        }

        // approximates the value for X using the Newton-Raphson Method
        float NewtonRaphsonIterate(float aX, float timeGuess)
        {
            for (var i = 0; i < NewtonItterations; ++i)
            {
                float currentSlope = GetSlope(timeGuess);
                if (currentSlope == 0.0f) return timeGuess;
                var currentX = CalcBezier(timeGuess, firstX, secondX) - aX;
                timeGuess -= currentX / currentSlope;
            }
            return timeGuess;
        }

        // used as a key for the cache
        public struct BezierPoints : IEquatable<BezierPoints>
        {
            public static readonly BezierPoints LINEAR = new BezierPoints(0f, 0f, 0f, 0f);
            public static readonly BezierPoints EASE_IN = new BezierPoints(0.42f, 0f, 1f, 1f);
            public static readonly BezierPoints EASE_OUT = new BezierPoints(0f, 0f, 0.58f, 1f);
            public static readonly BezierPoints EASE_IN_OUT = new BezierPoints(0.42f, 0f, 0.58f, 1f);

            public float P1;
            public float P2;
            public float P3;
            public float P4;

            public BezierPoints(float X1, float Y1, float X2, float Y2)
            {
                P1 = X1;
                P2 = Y1;
                P3 = X2;
                P4 = Y2;
            }

            public bool Equals(BezierPoints other)
            {
                return P1 == other.P1 && P2 == other.P2 && P2 == other.P2 && P3 == other.P3 && P4 == other.P4;
            }

            public override bool Equals(object obj)
            {
                return obj is BezierPoints points && Equals(points);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(P1, P2, P3, P4);
            }

            public static bool operator == (BezierPoints p1, BezierPoints p2)
            {
                return p1.Equals(p2);
            }

            public static bool operator != (BezierPoints p1, BezierPoints p2)
            {
                return !p1.Equals(p2);
            }
        }
    }

}

#endif
