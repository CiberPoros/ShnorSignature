using System;
using System.Collections.Generic;
using System.Numerics;
using BigMath;
using static ShnorSignature.ObjectExtensions;

namespace ShnorSignature
{
    class GeneratorEl
    {
        /// <summary>
        /// Разбивает простое число p на p = a^2 + d * b^2 алгоритмом из книги
        /// </summary>
        /// <param name="p">Простое число p</param>
        /// <returns></returns>
        public static bool Decompose(BigInteger p, BigInteger d, out BigInteger a, out BigInteger b)
        {
            a = -1;
            b = -1;
            // шаг  1
            if (Generator.FastPowOnMod((-d + p), (p - 1) / 2, p) == p - 1)
                return false;

            // шаг 2 
            F_int u = F_int.Sqrt(new F_int(-d + p, p));

            List<BigInteger> uMas = new List<BigInteger>();
            List<BigInteger> mMas = new List<BigInteger>();
            uMas.Add(u._val);
            mMas.Add(p);
            int i = 0;
            for (; ; i++)
            {
                // m[i + 1] = (u[i]^2 + d) / m[i]
                mMas.Add(((((uMas[i] * uMas[i] + d) / mMas[i]) % p) + p) % p);

                // u[i + 1] = min(u[i] % m[i + 1], (m[i + 1] - u[i]) % m[i+1])
                if (((uMas[i] % mMas[i + 1]) + mMas[i + 1]) % mMas[i + 1] < (((mMas[i + 1] - uMas[i]) % mMas[i + 1]) + mMas[i + 1]) % mMas[i + 1])
                    uMas.Add(((uMas[i] % mMas[i + 1]) + mMas[i + 1]) % mMas[i + 1]);
                else
                    uMas.Add((((mMas[i + 1] - uMas[i]) % mMas[i + 1]) + mMas[i + 1]) % mMas[i + 1]);

                if (mMas[i + 1] == 1)
                    break;
            }

            List<BigInteger> aMas = new List<BigInteger>();
            List<BigInteger> bMas = new List<BigInteger>();
            for (int j = 0; j <= i; j++)
            {
                aMas.Add(0);
                bMas.Add(0);
            }

            aMas[i] = uMas[i];
            bMas[i] = 1;

            for (; ; i--)
            {
                if (i == 0)
                {
                    a = BigInteger.Abs(aMas[i]);
                    b = BigInteger.Abs(bMas[i]);
                    return true;
                }

                // a[i - 1] = (+-u[i - 1] * a[i] + d * b[i]) / (a[i]^2 + d * b[i]^2)
                if ((uMas[i - 1] * aMas[i] + bMas[i] * d) % (aMas[i] * aMas[i] + bMas[i] * bMas[i] * d) == 0)
                    aMas[i - 1] = (uMas[i - 1] * aMas[i] + bMas[i] * d) / (aMas[i] * aMas[i] + bMas[i] * bMas[i] * d);
                else
                    aMas[i - 1] = -((uMas[i - 1] * aMas[i]) + bMas[i] * d) / (aMas[i] * aMas[i] + bMas[i] * bMas[i] * d);
                // b[i - 1] = (-a[i] +- u[i - 1] * b[i]) / (a[i]^2 + d * b[i]^2)
                if ((-aMas[i] + uMas[i - 1] * bMas[i]) % (aMas[i] * aMas[i] + bMas[i] * bMas[i] * d) == 0)
                    bMas[i - 1] = (-aMas[i] + uMas[i - 1] * bMas[i]) / (aMas[i] * aMas[i] + bMas[i] * bMas[i] * d);
                else
                    bMas[i - 1] = (-aMas[i] - uMas[i - 1] * bMas[i]) / (aMas[i] * aMas[i] + bMas[i] * bMas[i] * d);
            }
        }

        /// <summary>
        /// Реализация шага 3 алгоритма
        /// </summary>
        /// <param name="p">Простое число p</param>
        /// <returns></returns>
        private static bool CheckStep3(BigInteger p, BigInteger a, BigInteger b, out BigInteger N, out BigInteger r, ref Random rnd)
        {
            BigInteger[] T_mass = new BigInteger[] { -2 * a, 2 * a, -2 * b, 2 * b };
            N = -1;
            r = -1;

            foreach (BigInteger T in T_mass)
            {
                N = p + 1 + T;

                r = N / 2;
                if (Generator.IsPrimeByMillerRabin(r, 15, ref rnd))
                    return true;

                if (r % 2 == 0 && Generator.IsPrimeByMillerRabin(r / 2, 15, ref rnd))
                {
                    r = r / 2;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Реализация шага 4 алгоритма
        /// </summary>
        /// <param name="p">Простое число p</param>
        /// <returns></returns>
        private static bool CheckStep4(BigInteger p, BigInteger r, int m)
        {
            if (p == r)
                return false;

            for (int i = 1; i < m; i++)
            {
                BigInteger val = 1;

                for (int j = 0; j < i; j++)
                    val = (val * p) % r;

                if (val == 1)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Создает сет из всех квадратичных вычетов для m
        /// </summary>
        /// <returns></returns>
        private static HashSet<F_int> CreateSetQuadraticResidues(BigInteger mod)
        {
            HashSet<F_int> Quadratic_residues = new HashSet<F_int>();

            for (BigInteger i = 0; i < mod; i++)
                Quadratic_residues.Add(new F_int(i * i % mod, mod));

            return Quadratic_residues;
        }

        /// <summary>
        /// Складывает точку с точкой, вернет false, если угловой коефф был 0
        /// </summary>
        /// <returns></returns>
        public static bool SummPointOnPoint(F_int x1, F_int y1, F_int x2, F_int y2, out F_int x3, out F_int y3, F_int A)
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
                    SummPointOnPoint(_x, _y, _x, _y, out _x, out _y, A);
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
                    SummPointOnPoint(res_x, res_y, _x, _y, out res_x, out res_y, A);
                }
            }

            return (res_x.IsNull() && res_y.IsNull());
        }

        public static void Gen(int l, int m, out BigInteger p, out F_int A, out F_int q1, out F_int q2, out BigInteger r)
        {
            // Для всех генераторов один экземпляр этого класса
            Random rnd = new Random();

            p = -1;
            r = -1;
            BigInteger a = -1, b = -1, N = -1;

            // Цикл для возврата с шага 3 или 4 к шагу 1
            int rounds_cnt = 0, kv = l * l;
            while (true)
            {
                // Шаг 1
                p = Generator.GeneratePrimeNumber(l, 1, 2, ref rnd); // быстро

                // p = 65129; // Это пример из книги
                // Шаг 2
                if (!Decompose(p, 1, out a, out b))
                    continue; // быстро

                // Шаг 3
                if (!CheckStep3(p, a, b, out N, out r, ref rnd)) // быстро
                    continue;

                // Шаг 4
                if (CheckStep4(p, r, m)) // быстро
                    break;

                rounds_cnt++;
                if (rounds_cnt > kv)
                {
                    m = Math.Min(1, m - m / 3);
                    rounds_cnt = 0;
                }
            }

            // наборы всех квардатичных вычетов для 2r и для 4r соответственно
            F_int x, y;

            // Цикл для возврата с шага 5 или 6 к шагу 5
            while (true)
            {
                // Шаг 5 быстро
                x = new F_int(Generator.GenBigRandom(ref rnd, 1, p), p);
                y = new F_int(Generator.GenBigRandom(ref rnd, 1, p), p);

                // x = new F_int(1, p); // Это пример из книги
                // y = new F_int(2, p); // Это пример из книги

                A = (y * y - x * x * x) / x;

                // быстро
                // Если А - квадратичный вычет для 2r OR А - квадратичный невычет для 4r, то вернуться к шагу 5
                if ((N == 2 * r && F_int.IsQuadraticResidue(-A)) || ((N == 4 * r && !F_int.IsQuadraticResidue(-A))))
                    continue;

                // Шаг 6 быстро
                MultiPointOnConst(x, y, N, A, out F_int x_out, out F_int y_out);
                if (x_out.IsNull() && y_out.IsNull())
                {
                    break;
                }
            }

            // Шаг 7 и 8 (Q = (x_out; y_out)) быстро
            MultiPointOnConst(x, y, N / r, A, out q1, out q2);
        }
    }
}
