using System;

namespace Cadenza.Golden.Modules
{
    public static class MathModule
    {
        /// <summary>
        /// </summary>
        /// <param name="a">Parameter of type int</param>
        /// <param name="b">Parameter of type int</param>
        /// <returns>Returns int</returns>
        public static int add(int a, int b)
        {
            return a + b;
        }

        /// <summary>
        /// </summary>
        /// <param name="a">Parameter of type int</param>
        /// <param name="b">Parameter of type int</param>
        /// <returns>Returns int</returns>
        public static int multiply(int a, int b)
        {
            return a * b;
        }

        /// <summary>
        /// Pure function - no side effects
        /// </summary>
        /// <param name="x">Parameter of type int</param>
        /// <returns>Returns int</returns>
        public static int square(int x)
        {
            return x * x;
        }
    }
    
    public static class CadenzaProgram
    {
        /// <summary>
        /// </summary>
        /// <param name="x">Parameter of type int</param>
        /// <param name="y">Parameter of type int</param>
        /// <returns>Returns int</returns>
        public static int calculate(int x, int y)
        {
            var sum = MathModule.add(x, y);
            return MathModule.square(sum);
        }
    }
}