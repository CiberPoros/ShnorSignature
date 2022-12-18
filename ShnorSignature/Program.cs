using System;
using System.Text;
using static System.Console;
using System.Numerics;
using System.IO;
using BigMath;
using static ShnorSignature.Param;
using BigMath.RandomNumbers;
using System.Security.Cryptography;
using System.Diagnostics;

namespace ShnorSignature
{
    internal class Program
    {
        private const string _lFileName = "l.txt";
        private const string _publicParametersPath = "общие_параметры.txt";
        private const string _openKeyPath = "открытый_ключ.txt";
        private const string _closeKeyPath = "закрытый_ключ.txt";
        private const string _messagePath = "сообщение.txt";

        private const string _create_kValue = "подписание_k_число.txt";
        private const string _create_RPoint = "подписание_R_точка.txt";
        private const string _create_eValue = "подписание_e_число.txt";
        private const string _create_sValue = "подписание_s_число.txt";

        private const string _verify_RPoint = "проверка_R_точка.txt";
        private const string _verify_eValue = "проверка_e_число.txt";

        private const string _pythonExePath = @"C:\Program Files (x86)\Microsoft Visual Studio\Shared\Python39_64\python.exe";

        private static Random _random = new();
        private static readonly RandomBigInteger _randomBigInteger = new();

        public static void Main()
        {
            OutputEncoding = Encoding.UTF8;

            for (; ; )
            {
                var key = ReadStepType();

                switch (key)
                {
                    case StepType.NONE:
                        return;
                    case StepType.GENERATE_COMMON_PARAMETERS:
                        GenerateCommonParameters();
                        break;
                    case StepType.GENERATE_PRIVATE_KEY:
                        GeneratePrivateKey();
                        break;
                    case StepType.GENERATE_PUBLIC_KEY:
                        GeneratePublicKey();
                        break;
                    case StepType.CREATE_CALC_k:
                        CreateGenerateK();
                        break;
                    case StepType.CREATE_CALC_R:
                        CreateCalculateR();
                        break;
                    case StepType.CREATE_CALC_e:
                        CreateCalculateE();
                        break;
                    case StepType.CREATE_CALC_s:
                        CreateCalculateS();
                        break;
                    case StepType.VERIFY_CALC_R:
                        VerifyCalculateR();
                        break;
                    case StepType.VERIFY_CALC_e:
                        VerifyCalculateE();
                        break;
                    case StepType.VERIFY_SIGN:
                        VerifySign();
                        break;
                }
            }
        }

        static void GenerateCommonParameters()
        {
            var l = ReadL();
            File.WriteAllText(_lFileName, l.ToString());

            DeleteFilesByNames(_publicParametersPath, _openKeyPath, _closeKeyPath);
            DeleteFilesByNames(_create_kValue, _create_RPoint, _create_eValue, _create_sValue, _verify_RPoint, _verify_eValue);

            var start = new ProcessStartInfo
            {
                FileName = Extensios.ArgumentsPath,
                Arguments = @"..\..\..\..\Generator\Gеnerator.py",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Write(result);
                }

                process.WaitForExit();
            }

            WriteLine("Common parameters generation completed successfuly!");
            WriteLine();
        }

        private static void GeneratePrivateKey()
        {
            DeleteFilesByNames(_openKeyPath, _closeKeyPath);
            DeleteFilesByNames(_create_kValue, _create_RPoint, _create_eValue, _create_sValue, _verify_RPoint, _verify_eValue);

            Param _p = new Param(0, "p"), _A = new Param(0, "A"), _r = new Param(0, "r");

            try
            {
                ReadInFile(_publicParametersPath, __arglist(_p, _A, _r));
            }
            catch (Exception e)
            {
                WriteLine("Error when read file. Details: " + e.Message);
                return;
            }

            BigInteger l = _randomBigInteger.NextBigInteger(ref _random, 1, _r.Val);
            OutToFile(_closeKeyPath, new Param(l, "l"));

            WriteLine("Private key generation completed successfuly!");
            WriteLine();
        }

        private static void GeneratePublicKey()
        {
            DeleteFilesByNames(_openKeyPath);
            DeleteFilesByNames(_create_kValue, _create_RPoint, _create_eValue, _create_sValue, _verify_RPoint, _verify_eValue);

            var _p = new Param(0, "p");
            var _A = new Param(0, "A");
            var _r = new Param(0, "r");
            var Q = new Param(0, 0, "Q");
            var _l = new Param(0, "l");

            try
            {
                ReadInFile(_publicParametersPath, __arglist(_p, _A, _r, Q));
                ReadInFile(_closeKeyPath, __arglist(_l));

                if (BigInteger.ModPow(Q.Y, 2, _p.Val) != (BigInteger.ModPow(Q.X, 3, _p.Val) + _A.Val * Q.X) % _p.Val)
                {
                    WriteLine($"Тогда {nameof(Q)} не принадлежит кривой!");
                    return;
                }
            }
            catch (Exception e)
            {
                WriteLine("Error when read file. Details: " + e.Message);
                return;
            }

            F_int q1 = new F_int(Q.X, _p.Val), q2 = new F_int(Q.Y, _p.Val);
            Operations.MultiPointOnConst(q1, q2, _l.Val, new F_int(_A.Val, _p.Val), out F_int p1, out F_int p2);

            OutToFile(_openKeyPath, new Param(p1, p2, "P"));

            WriteLine("Public key calculation completed successfuly!");
            WriteLine();
        }

        private static void CreateGenerateK()
        {
            DeleteFilesByNames(_create_kValue, _create_RPoint, _create_eValue, _create_sValue, _verify_RPoint, _verify_eValue);

            var _r = new Param(0, "r");

            try
            {
                var input = File.ReadAllLines(@"..\..\..\..\Протокол\" + _publicParametersPath);
                ReadInFile(_publicParametersPath, __arglist(_r));
            }
            catch (Exception e)
            {
                WriteLine("Error when read file. Details: " + e.Message);
                return;
            }

            var k = _randomBigInteger.NextBigInteger(ref _random, 1, _r.Val);

            OutToFile(_create_kValue, new Param(k, "k"));

            WriteLine("k generated!");
            WriteLine();
        }

        private static void CreateCalculateR()
        {
            DeleteFilesByNames(_create_RPoint, _create_eValue, _create_sValue, _verify_RPoint, _verify_eValue);

            Param _p = new Param(0, "p"), _A = new Param(0, "A"), Q = new Param(0, 0, "Q"), k = new Param(0, "k");

            try
            {
                ReadInFile(_publicParametersPath, __arglist(_p, _A, Q));
                ReadInFile(_create_kValue, __arglist(k));

                if (BigInteger.ModPow(Q.Y, 2, _p.Val) != (BigInteger.ModPow(Q.X, 3, _p.Val) + _A.Val * Q.X) % _p.Val)
                {
                    WriteLine($"Тогда {nameof(Q)} не принадлежит кривой!");
                    return;
                }
            }
            catch (Exception e)
            {
                WriteLine("Error when read file. Details: " + e.Message);
                return;
            }

            F_int q1 = new F_int(Q.X, _p.Val), q2 = new F_int(Q.Y, _p.Val), A = new F_int(_A.Val, _p.Val);
            Operations.MultiPointOnConst(q1, q2, k.Val, A, out F_int r1, out F_int r2);
            Param _R = new Param(r1, r2, "R");

            OutToFile(_create_RPoint, _R);

            WriteLine("R calculated!");
            WriteLine();
        }

        private static void CreateCalculateE()
        {
            DeleteFilesByNames(_create_eValue, _create_sValue, _verify_RPoint, _verify_eValue);

            Param _p = new Param(0, "p"), _A = new Param(0, "A"), _r = new Param(0, "r"), R = new Param(0, 0, "R");

            try
            {
                ReadInFile(_publicParametersPath, __arglist(_p, _A, _r));
                ReadInFile(_create_RPoint, __arglist(R));

                if (BigInteger.ModPow(R.Y, 2, _p.Val) != (BigInteger.ModPow(R.X, 3, _p.Val) + _A.Val * R.X) % _p.Val)
                {
                    WriteLine($"Тогда {nameof(R)} не принадлежит кривой!");
                    return;
                }
            }
            catch (Exception ex)
            {
                WriteLine("Error when read file. Details: " + ex.Message);
                return;
            }

            BigInteger e = GetHash(File.ReadAllText(@"..\..\..\..\Протокол\" + _messagePath, Encoding.Default) + R.ToString(), _r.Val);

            if (e == 0)
            {
                WriteLine("Вычисленное значение e != 0! Перейдите к шагу 4");
                WriteLine();
                return;
            }    

            OutToFile(_create_eValue, new Param(e, "e"));

            WriteLine("e calculated!");
            WriteLine();
        }

        private static void CreateCalculateS()
        {
            DeleteFilesByNames(_create_sValue, _verify_RPoint, _verify_eValue);

            Param _r = new Param(0, "r"), _l = new Param(0, "l"), e = new Param(0, "e"), k = new Param(0, "k");

            try
            {
                ReadInFile(_publicParametersPath, __arglist(_r));
                ReadInFile(_closeKeyPath, __arglist(_l));
                ReadInFile(_create_kValue, __arglist(k));
                ReadInFile(_create_eValue, __arglist(e));
            }
            catch (Exception ex)
            {
                WriteLine("Error when read file. Details: " + ex.Message);
                return;
            }

            var s = ((_l.Val * e.Val) % _r.Val + k.Val) % _r.Val;

            OutToFile(_create_sValue, new Param(s, "s"));

            WriteLine("s calculated!");
            WriteLine();
        }

        private static void VerifyCalculateR()
        {
            DeleteFilesByNames(_verify_RPoint, _verify_eValue);

            Param _p = new Param(0, "p"), _A = new Param(0, "A"), _Q = new Param(0, 0, "Q"),
                _P = new Param(0, 0, "P"), _s = new Param(0, "s"), _e = new Param(0, "e");

            try
            {
                ReadInFile(_publicParametersPath, __arglist(_p, _A, _Q));
                ReadInFile(_openKeyPath, __arglist(_P));
                ReadInFile(_create_sValue, __arglist(_s));
                ReadInFile(_create_eValue, __arglist(_e));

                if (BigInteger.ModPow(_Q.Y, 2, _p.Val) != (BigInteger.ModPow(_Q.X, 3, _p.Val) + _A.Val * _Q.X) % _p.Val)
                {
                    WriteLine($"Тогда {nameof(_Q)} не принадлежит кривой!");
                    return;
                }

                if (BigInteger.ModPow(_P.Y, 2, _p.Val) != (BigInteger.ModPow(_P.X, 3, _p.Val) + _A.Val * _P.X) % _p.Val)
                {
                    WriteLine($"Тогда {nameof(_P)} не принадлежит кривой!");
                    return;
                }
            }
            catch (Exception ex)
            {
                WriteLine("Error when read file. Details: " + ex.Message);
                return;
            }

            F_int q1 = new F_int(_Q.X, _p.Val), q2 = new F_int(_Q.Y, _p.Val), 
                A = new F_int(_A.Val, _p.Val), p1 = new F_int(_P.X, _p.Val), p2 = new F_int(_P.Y, _p.Val);

            Operations.MultiPointOnConst(q1, q2, _s.Val, A, out F_int point1_x, out F_int point1_y);
            Operations.MultiPointOnConst(p1, p2, _e.Val, A, out F_int point2_x, out F_int point2_y);

            point2_y._val = (((-point2_y._val) % _p.Val) + _p.Val) % _p.Val;
            Operations.SumPoints(point1_x, point1_y, point2_x, point2_y, out F_int r1, out F_int r2, A);

            OutToFile(_verify_RPoint, new Param(r1, r2, "R"));

            WriteLine("R' calculated!");
            WriteLine();
        }

        private static void VerifyCalculateE()
        {
            DeleteFilesByNames(_verify_eValue);

            Param _p = new Param(0, "p"), _A = new Param(0, "A"), _r = new Param(0, "r"), R = new Param(0, 0, "R");

            try
            {
                ReadInFile(_publicParametersPath, __arglist(_p, _A, _r));
                ReadInFile(_verify_RPoint, __arglist(R));

                if (BigInteger.ModPow(R.Y, 2, _p.Val) != (BigInteger.ModPow(R.X, 3, _p.Val) + _A.Val * R.X) % _p.Val)
                {
                    WriteLine($"Тогда {nameof(R)} не принадлежит кривой!");
                    return;
                }
            }
            catch (Exception ex)
            {
                WriteLine("Error when read file. Details: " + ex.Message);
                return;
            }

            var ___e = GetHash(File.ReadAllText(@"..\..\..\..\Протокол\" + _messagePath, Encoding.Default) + R.ToString(), _r.Val);
            OutToFile(_verify_eValue, new Param(___e, "e"));

            WriteLine("e' calculated!");
            WriteLine();
        }

        private static void VerifySign()
        {
            Param e = new Param(0, "e"), _e = new Param(0, "e");

            try
            {
                ReadInFile(_create_eValue, __arglist(e));
                ReadInFile(_verify_eValue, __arglist(_e));
            }
            catch (Exception ex)
            {
                WriteLine("Error when read file. Details: " + ex.Message);
                return;
            }

            if (e.Val == _e.Val)
            {
                WriteLine("Подпись верна!");
            }
            else
            {
                WriteLine("Подпись не верна!");
            }
        }

        private static BigInteger GetHash(string s, BigInteger mod) => (((new BigInteger(MD5.Create().ComputeHash(Encoding.Default.GetBytes(s)))) % mod) + mod) % mod;

        private static bool DeleteFilesByNames(params string[] fileNames)
        {
            foreach (string path in fileNames)
                File.Delete(path);

            return true;
        }

        private static int ReadL()
        {
            WriteLine("Enter integer positive number l:");

            for (; ; )
            {
                var input = ReadLine();

                if (!int.TryParse(input, out var result) || result <= 0)
                {
                    WriteLine("Input error. Try again...");
                }

                WriteLine();
                return result;
            } 
        }

        private static StepType ReadStepType()
        {
            WriteLine("Chose step:");
            WriteLine("1. Create common parameters;");
            WriteLine("2. Create private key;");
            WriteLine("3. Create public key;");
            WriteLine("4. Generate k;");
            WriteLine("5. Calculate R;");
            WriteLine("6. Calculate e;");
            WriteLine("7. Calculate s;");
            WriteLine("8. Calculate R';");
            WriteLine("9. Calculate e';");
            WriteLine("0. Check sign...");
            WriteLine();

            for (; ; )
            {
                var key = ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.D1:
                    case ConsoleKey.NumPad1:
                        return StepType.GENERATE_COMMON_PARAMETERS;
                    case ConsoleKey.D2:
                    case ConsoleKey.NumPad2:
                        return StepType.GENERATE_PRIVATE_KEY;
                    case ConsoleKey.D3:
                    case ConsoleKey.NumPad3:
                        return StepType.GENERATE_PUBLIC_KEY;
                    case ConsoleKey.D4:
                    case ConsoleKey.NumPad4:
                        return StepType.CREATE_CALC_k;
                    case ConsoleKey.D5:
                    case ConsoleKey.NumPad5:
                        return StepType.CREATE_CALC_R;
                    case ConsoleKey.D6:
                    case ConsoleKey.NumPad6:
                        return StepType.CREATE_CALC_e;
                    case ConsoleKey.D7:
                    case ConsoleKey.NumPad7:
                        return StepType.CREATE_CALC_s;
                    case ConsoleKey.D8:
                    case ConsoleKey.NumPad8:
                        return StepType.VERIFY_CALC_R;
                    case ConsoleKey.D9:
                    case ConsoleKey.NumPad9:
                        return StepType.VERIFY_CALC_e;
                    case ConsoleKey.D0:
                    case ConsoleKey.NumPad0:
                        return StepType.VERIFY_SIGN;
                    default:
                        continue;
                }
            }
        }
    }
}
