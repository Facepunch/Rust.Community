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
        // had to port this from a javascript version

        public static Dictionary<string, BezierEasing> cache = new Dictionary<string, BezierEasing>();
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



        public static float Ease(float X1, float Y1, float X2, float Y2, float time){
            string key = $"{X1}{Y1}{X2}{Y2}";
            if(!cache.ContainsKey(key)){
                cache[key] = new BezierEasing(X1, Y1, X2, Y2);
            }
            return cache[key].GetPosition(time);
        }



        float A (float aA1, float aA2) { return 1.0f - 3.0f * aA2 + 3.0f * aA1; }
        float B (float aA1, float aA2) { return 3.0f * aA2 - 6.0f * aA1; }
        float C (float aA1) { return 3.0f * aA1; }

        // Returns x(t) given t, x1, and x2, or y(t) given t, y1, and y2.
        public float calcBezier (float aT, float aA1, float aA2) {
            return ((A(aA1, aA2)*aT + B(aA1, aA2))*aT + C(aA1))*aT;
        }

        // Returns dx/dt given t, x1, and x2, or dy/dt given t, y1, and y2.
        public float getSlope (float aT, float aA1, float aA2) {
            return 3.0f * A(aA1, aA2)*aT*aT + 2.0f * B(aA1, aA2) * aT + C(aA1);
        }

        public float binarySubdivide (float aX, float aA, float aB) {
            float currentX, currentT, i = 0f;
            do {
                currentT = aA + (aB - aA) / 2.0f;
                currentX = calcBezier(currentT, mX1, mX2) - aX;
                if (currentX > 0.0f) {
                    aB = currentT;
                } else {
                    aA = currentT;
                }
            } while (Math.Abs(currentX) > SubDivPrec && ++i < SubDivItts);
            return currentT;
        }

        public BezierEasing (float X1, float Y1, float X2, float Y2) {
            mX1 = X1;
            mY1 = Y1;
            mX2 = X2;
            mY2 = Y2;
            mSampleValues = new float[kSplineTableSize];
        }

        void calcSampleValues () {
            for (var i = 0; i < kSplineTableSize; ++i) {
                mSampleValues[i] = calcBezier(i * kSampleStepSize, mX1, mX2);
            }
        }

        float getTForX (float aX) {
            float intervalStart = 0.0f;
            int currentSample = 1;
            int lastSample = kSplineTableSize - 1;

            for (; currentSample != lastSample && mSampleValues[currentSample] <= aX; ++currentSample) {
                intervalStart += kSampleStepSize;
            }
            --currentSample;

            float dist = (aX - mSampleValues[currentSample]) / (mSampleValues[currentSample+1] - mSampleValues[currentSample]);
            float guessForT = intervalStart + dist * kSampleStepSize;
            float initialSlope = getSlope(guessForT, mX1, mX2);
            if (initialSlope >= NewMinSlope) {
                return newtonRaphsonIterate(aX, guessForT);
            } else if (initialSlope == 0.0f) {
                return guessForT;
            } else {
                return binarySubdivide(aX, intervalStart, intervalStart + kSampleStepSize);
            }
        }

        void precompute() {
            _precomputed = true;
            if (mX1 != mY1 || mX2 != mY2)
                calcSampleValues();
        }

        public float GetPosition(float aX) {
            if (!_precomputed) precompute();
            if (mX1 == mY1 && mX2 == mY2) return aX; // linear
            if (aX == 0f) return 0f;
            if (aX == 1f) return 1f;
            return calcBezier(getTForX(aX), mY1, mY2);
        }

        float newtonRaphsonIterate (float aX, float aGuessT) {
            for (var i = 0; i < NewItts; ++i) {
                float currentSlope = getSlope(aGuessT, mX1, mX2);
                if (currentSlope == 0.0f) return aGuessT;
                var currentX = calcBezier(aGuessT, mX1, mX2) - aX;
                aGuessT -= currentX / currentSlope;
            }
            return aGuessT;
        }
    }

}

#endif
