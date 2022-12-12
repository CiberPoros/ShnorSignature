using System.Collections.Generic;
using System.Numerics;
using BigMath;

namespace ShnorSignature
{
    public class Operations
    {
        /// <summary>
        /// Складывает точку с точкой, вернет false, если угловой коефф был 0
        /// </summary>
        /// <returns></returns>
        public static bool SumPoints(F_int x1, F_int y1, F_int x2, F_int y2, out F_int x3, out F_int y3, F_int A)
        {
            if (x1.IsNull() && y1.IsNull())
            {
                if (x2.IsNotNull() && y2.IsNotNull())
                {
                    x3 = new F_int(x2._val, x2._mod);
                    y3 = new F_int(y2._val, x2._mod);
                }
                else
                {
                    x3 = null;
                    y3 = null;
                }

                return true;
            }
            else if (x2.IsNull() && y2.IsNull())
            {
                if (x1.IsNotNull() && y1.IsNotNull())
                {
                    x3 = new F_int(x1._val, x1._mod);
                    y3 = new F_int(y1._val, x1._mod);
                }
                else
                {
                    x3 = null;
                    y3 = null;
                }

                return true;
            }

            F_int lambda = null;
            if (x1 != x2)
            {
                lambda = (y2 - y1) / (x2 - x1);
            }
            else if (x1 == x2 && ((y1 != y2) || (y1 == y2 && y1 == 0)))
            {
                x3 = null;
                y3 = null;
                return true;
            }
            else
            {
                lambda = (3 * x1 * x1 + A) / (2 * y1);
            }

            x3 = lambda * lambda - x1 - x2;
            y3 = lambda * (x1 - x3) - y1;

            return false;
        }

        /// <summary>
        /// Умножает точку на заданное натуральное число
        /// </summary>
        /// <returns></returns>
        public static bool MultiPointOnConst(F_int x, F_int y, BigInteger N, F_int A, out F_int res_x, out F_int res_y)
        {
            List<BigInteger> degrees = new List<BigInteger>();

            BigInteger n = N;

            // разложение N на сумму степеней двоек
            while (n > 0)
            {
                BigInteger val = 1;

                for (BigInteger i = 0; i < n; i++)
                {
                    if (val * 2 > n)
                    {
                        degrees.Add(i);
                        n -= val;
                        break;
                    }
                    val = val * 2;
                }
            }

            res_x = null; res_y = null;

            for (BigInteger i = 0; i < degrees.Count; i++)
            {
                F_int _x = null, _y = null;

                if (x.IsNotNull() && y.IsNotNull())
                {
                    _x = new F_int(x._val, x._mod);
                    _y = new F_int(y._val, y._mod);
                }

                for (BigInteger j = 0; j < degrees[(int)i]; j++)
                {
                    SumPoints(_x, _y, _x, _y, out _x, out _y, A);
                }

                if (i == 0)
                {
                    if (_x.IsNotNull() && _y.IsNotNull())
                    { 
                        res_x = new F_int(_x._val, _x._mod);
                        res_y = new F_int(_y._val, _y._mod);
                    }
                    else
                    {
                        res_x = null;
                        res_y = null;
                    }
                }
                else
                {
                    SumPoints(res_x, res_y, _x, _y, out res_x, out res_y, A);
                }
            }

            return (res_x.IsNull() && res_y.IsNull());
        } 
    }
}
