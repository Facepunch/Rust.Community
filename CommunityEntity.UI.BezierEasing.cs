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

    public class BezierEasing {
        // this basically is the same process that Browsers use for their CSS easing, allowing you to define any possible easing value with 4 bezier points
        // NOTE: this is a C# version of the javascript library at https://github.com/gre/bezier-easing, i simply rewrote it to be in C# Syntax and added a Static helper method

        public static Dictionary<BezierPoints, BezierEasing> cache = new Dictionary<BezierPoints, BezierEasing>();

        // these values modify the accuracy & performance of the curve's evaluation
        // while these are the values used in the javascript library you may find better values that produce good enough results with better performance
        public static int NewItts = 4;
        public static float NewMinSlope = 0.001f;
        public static float SubDivPrec = 0.0000001f;
        public static float SubDivItts = 10;

        public float mX1 = 0f;
        public float mY1 = 0f;
        public float mX2 = 1f;
        public float mY2 = 1f;
        public float[] mSampleValues;

        public static int kSplineTableSize = 11;
        public static float kSampleStepSize = 1.0f / (kSplineTableSize - 1.0f);
        bool _precomputed = false;


        // Helper function that caches BezierEasing Instances. supplying 4 points & time between 0 & 1 generates the curve & caches it, returning the value for time
        public static float Ease(float X1, float Y1, float X2, float Y2, float time){
            var key = new BezierPoints(X1, Y1, X2, Y2);
            if(!cache.ContainsKey(key)){
                cache[key] = new BezierEasing(X1, Y1, X2, Y2);
            }
            return cache[key].GetPosition(time);
        }

        public BezierEasing (float X1, float Y1, float X2, float Y2) {
            mX1 = X1;
            mY1 = Y1;
            mX2 = X2;
            mY2 = Y2;
            mSampleValues = new float[kSplineTableSize];
        }


        // the following 3 functions are a single math formula split up into 3 steps for clarity
        float A (float aA1, float aA2) { return 1.0f - 3.0f * aA2 + 3.0f * aA1; }
        float B (float aA1, float aA2) { return 3.0f * aA2 - 6.0f * aA1; }
        float C (float aA1) { return 3.0f * aA1; }

        // Returns x(t) given t, x1, and x2, or y(t) given t, y1, and y2.
        public float CalcBezier (float aT, float aA1, float aA2) {
            return ((A(aA1, aA2)*aT + B(aA1, aA2))*aT + C(aA1))*aT;
        }

        // Returns dx/dt given t, x1, and x2, or dy/dt given t, y1, and y2.
        public float GetSlope (float aT, float aA1, float aA2) {
            return 3.0f * A(aA1, aA2)*aT*aT + 2.0f * B(aA1, aA2) * aT + C(aA1);
        }

        // performs a dichometric search to find the most accurate value between 2 precalculated points within SubDivItts loops
        public float BinarySubdivide (float aX, float aA, float aB) {
            float currentX, currentT, i = 0f;
            do {
                currentT = aA + (aB - aA) / 2.0f;
                currentX = CalcBezier(currentT, mX1, mX2) - aX;
                if (currentX > 0.0f) {
                    aB = currentT;
                } else {
                    aA = currentT;
                }
            } while (Math.Abs(currentX) > SubDivPrec && ++i < SubDivItts);
            return currentT;
        }

        // pre-calculates a set amount of points to be used within the BinarySubdivide function
        void CalcSampleValues () {
            for (var i = 0; i < kSplineTableSize; ++i) {
                mSampleValues[i] = CalcBezier(i * kSampleStepSize, mX1, mX2);
            }
        }

        // finds the surrounding pre-calculated points for x and returns the closest value, uses NewtonRaphson's method for faster computation if the slope is above a set threshold, otherwhise falls back to a binary search
        float GetTForX (float aX) {
            float intervalStart = 0.0f;
            int currentSample = 1;
            int lastSample = kSplineTableSize - 1;

            for (; currentSample != lastSample && mSampleValues[currentSample] <= aX; ++currentSample) {
                intervalStart += kSampleStepSize;
            }
            --currentSample;

            float dist = (aX - mSampleValues[currentSample]) / (mSampleValues[currentSample+1] - mSampleValues[currentSample]);
            float guessForT = intervalStart + dist * kSampleStepSize;
            float initialSlope = GetSlope(guessForT, mX1, mX2);
            if (initialSlope >= NewMinSlope) {
                return NewtonRaphsonIterate(aX, guessForT);
            } else if (initialSlope == 0.0f) {
                return guessForT;
            } else {
                return BinarySubdivide(aX, intervalStart, intervalStart + kSampleStepSize);
            }
        }

        void PreCompute() {
            _precomputed = true;
            if (mX1 != mY1 || mX2 != mY2)
                CalcSampleValues();
        }

        // returns the value on the curve for X
        public float GetPosition(float aX) {
            if (!_precomputed) PreCompute();
            if (mX1 == mY1 && mX2 == mY2) return aX; // linear
            if (aX == 0f) return 0f;
            if (aX == 1f) return 1f;
            return CalcBezier(GetTForX(aX), mY1, mY2);
        }

        // approximates the value for X using the Newton-Raphson Method
        float NewtonRaphsonIterate (float aX, float aGuessT) {
            for (var i = 0; i < NewItts; ++i) {
                float currentSlope = GetSlope(aGuessT, mX1, mX2);
                if (currentSlope == 0.0f) return aGuessT;
                var currentX = CalcBezier(aGuessT, mX1, mX2) - aX;
                aGuessT -= currentX / currentSlope;
            }
            return aGuessT;
        }

        // used as a key for the cache
        public struct BezierPoints{
            public float P1;
            public float P2;
            public float P3;
            public float P4;

            public BezierPoints(float X1, float Y1, float X2, float Y2){
                P1 = X1;
                P2 = Y1;
                P3 = X2;
                P4 = Y2;
            }
        }
    }

}

#endif
