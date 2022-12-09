using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Console;
using System.Numerics;
using System.IO;
using BigMath;
using static ShnorSignature.ParametersEl;
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
        private const string _signaturePath = "подпись.txt";

        private static Random _random = new();
        private static readonly RandomBigInteger _randomBigInteger = new();

        public static void Main()
        {
            OutputEncoding = Encoding.UTF8;
            for (; ; )
            {
                WriteLine("Введите число из интервала [1, 4]:");

                int n = -1;
                try
                {
                    n = Convert.ToInt32(ReadLine());
                }
                catch
                {
                    WriteLine("Число должно быть из интервала [1, 4]!");
                    continue;
                }

                if (n < 1 || n > 4)
                {
                    WriteLine("Число должно быть из интервала [1, 4]!");
                    continue;
                }

                switch (n)
                {
                    case 1:
                        GenerateCommonParameters();
                        WriteLine();
                        break;
                    case 2:
                        GeneratePrivateParameters();
                        WriteLine();
                        break;
                    case 3:
                        CreateSignature();
                        WriteLine();
                        break;
                    case 4:
                        CheckSignature();
                        WriteLine();
                        break;
                }
            }
        }

        static void GenerateCommonParameters()
        {
            var l = ReadL();
            File.WriteAllText(_lFileName, l.ToString());

            DeleteFilesByNames(_publicParametersPath, _openKeyPath, _closeKeyPath, _signaturePath);

            var start = new ProcessStartInfo
            {
                FileName = @"C:\Users\Елизавета\AppData\Local\Programs\Python\Python310\python.exe",
                Arguments = @"..\..\..\..\Generator\Generator.py",
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
            DeleteFilesByNames(_openKeyPath, _closeKeyPath, _signaturePath);

            ParametersEl _p = new ParametersEl(0, "p"), _A = new ParametersEl(0, "A"), _r = new ParametersEl(0, "r"), Q = new ParametersEl(0, 0, "Q");

            try
            {
                var input = File.ReadAllLines(_publicParametersPath);
                _p = new ParametersEl(BigInteger.Parse(input[0].Split('=', StringSplitOptions.RemoveEmptyEntries).Last().Trim()), "p");
                _A = new ParametersEl(BigInteger.Parse(input[1].Split('=').Last().Trim()), "A");
                Q = new ParametersEl(
                    BigInteger.Parse(input[2].Split('=').Last().Split(',').First().Trim(' ', '(', ')')),
                    BigInteger.Parse(input[2].Split('=').Last().Split(',').Last().Trim(' ', '(', ')')),
                    "Q");
                _r = new ParametersEl(BigInteger.Parse(input[3].Split('=').Last().Trim()), "r");
            }
            catch (Exception e)
            {
                WriteLine("Error when read file. Details: " + e.Message);
                return;
            }

            BigInteger l = _randomBigInteger.NextBigInteger(ref _random, 1, _r.Val);
            OutToFile(_closeKeyPath, new ParametersEl(l, "l"));

            WriteLine("Private key generation completed successfuly!");
            WriteLine();
        }

        private static void GeneratePublicKey()
        {
            DeleteFilesByNames(_openKeyPath, _closeKeyPath, _signaturePath);

            var _p = new ParametersEl(0, "p");
            var _A = new ParametersEl(0, "A");
            var _r = new ParametersEl(0, "r");
            var Q = new ParametersEl(0, 0, "Q");
            var _l = new ParametersEl(0, "l");

            try
            {
                var input = File.ReadAllLines(_publicParametersPath);
                _p = new ParametersEl(BigInteger.Parse(input[0].Split('=', StringSplitOptions.RemoveEmptyEntries).Last().Trim()), "p");
                _A = new ParametersEl(BigInteger.Parse(input[1].Split('=').Last().Trim()), "A");
                Q = new ParametersEl(
                    BigInteger.Parse(input[2].Split('=').Last().Split(',').First().Trim(' ', '(', ')')),
                    BigInteger.Parse(input[2].Split('=').Last().Split(',').Last().Trim(' ', '(', ')')),
                    "Q");
                _r = new ParametersEl(BigInteger.Parse(input[3].Split('=').Last().Trim()), "r");

                ReadInFile(_closeKeyPath, __arglist(_l));
            }
            catch (Exception e)
            {
                WriteLine("Error when read file. Details: " + e.Message);
                return;
            }

            F_int q1 = new F_int(Q.X, _p.Val), q2 = new F_int(Q.Y, _p.Val);
            GeneratorEl.MultiPointOnConst(q1, q2, _l.Val, new F_int(_A.Val, _p.Val), out F_int p1, out F_int p2);

            OutToFile(_openKeyPath, new ParametersEl(p1, p2, "P"));

            WriteLine("Public key calculation completed successfuly!");
            WriteLine();
        }

        private static void CreateSignature()
        {
            DeleteFilesByNames(_signaturePath);

            ParametersEl _p = new ParametersEl(0, "p"), _A = new ParametersEl(0, "A"), _r = new ParametersEl(0, "r"), Q = new ParametersEl(0, 0, "Q"), _l = new ParametersEl(0, "l");

            try
            {
                var input = File.ReadAllLines(_publicParametersPath);
                _p = new ParametersEl(BigInteger.Parse(input[0].Split('=', StringSplitOptions.RemoveEmptyEntries).Last().Trim()), "p");
                _A = new ParametersEl(BigInteger.Parse(input[1].Split('=').Last().Trim()), "A");
                Q = new ParametersEl(
                    BigInteger.Parse(input[2].Split('=').Last().Split(',').First().Trim(' ', '(', ')')),
                    BigInteger.Parse(input[2].Split('=').Last().Split(',').Last().Trim(' ', '(', ')')),
                    "Q");
                _r = new ParametersEl(BigInteger.Parse(input[3].Split('=').Last().Trim()), "r");
                ReadInFile(_closeKeyPath, __arglist(_l));
            }
            catch (Exception e)
            {
                WriteLine("Error when read file. Details: " + e.Message);
                return;
            }

            BigInteger k = -1;
            for (; ; )
            {
                k = _randomBigInteger.NextBigInteger(ref _random, 1, _r.Val);

                F_int q1 = new F_int(Q.X, _p.Val), q2 = new F_int(Q.Y, _p.Val), A = new F_int(_A.Val, _p.Val);

                GeneratorEl.MultiPointOnConst(q1, q2, k, A, out F_int r1, out F_int r2);

                ParametersEl _R = new ParametersEl(r1, r2, "R");

                BigInteger e = GetHash(File.ReadAllText(_messagePath, Encoding.Default) + _R.ToString(), _r.Val);

                if (e == 0)
                    continue;

                BigInteger s = ((_l.Val * e) % _r.Val + k) % _r.Val;

                OutToFile(_signaturePath, new ParametersEl(e, "e"), new ParametersEl(s, "s"));

                WriteLine("Signature was created successfuly!");
                WriteLine();

                return;
            }
        }

        private static void CheckSignature()
        {
            ParametersEl _p = new ParametersEl(0, "p"), _A = new ParametersEl(0, "A"), _r = new ParametersEl(0, "r"), _Q = new ParametersEl(0, 0, "Q"), 
                _P = new ParametersEl(0, 0, "P"), _s = new ParametersEl(0, "s"), _e = new ParametersEl(0, "e");

            try
            {
                var input = File.ReadAllLines(_publicParametersPath);
                _p = new ParametersEl(BigInteger.Parse(input[0].Split('=', StringSplitOptions.RemoveEmptyEntries).Last().Trim()), "p");
                _A = new ParametersEl(BigInteger.Parse(input[1].Split('=').Last().Trim()), "A");
                _Q = new ParametersEl(
                    BigInteger.Parse(input[2].Split('=').Last().Split(',').First().Trim(' ', '(', ')')),
                    BigInteger.Parse(input[2].Split('=').Last().Split(',').Last().Trim(' ', '(', ')')),
                    "Q");
                _r = new ParametersEl(BigInteger.Parse(input[3].Split('=').Last().Trim()), "r");

                ReadInFile(_openKeyPath, __arglist(_P));
                ReadInFile(_signaturePath, __arglist(_s, _e));
            }
            catch (Exception e)
            {
                WriteLine("Error when read file. Details: " + e.Message);
                return;
            }

            F_int q1 = new F_int(_Q.X, _p.Val), q2 = new F_int(_Q.Y, _p.Val), A = new F_int(_A.Val, _p.Val), p1 = new F_int(_P.X, _p.Val), p2 = new F_int(_P.Y, _p.Val);     

            GeneratorEl.MultiPointOnConst(q1, q2, _s.Val, A, out F_int point1_x, out F_int point1_y);
            GeneratorEl.MultiPointOnConst(p1, p2, _e.Val, A, out F_int point2_x, out F_int point2_y);

            point2_y._val = (((-point2_y._val) % _p.Val) + _p.Val) % _p.Val;
            GeneratorEl.SummPointOnPoint(point1_x, point1_y, point2_x, point2_y, out F_int r1, out F_int r2, A);

            ParametersEl _R = new ParametersEl(r1, r2, "R");

            BigInteger ___e = GetHash(File.ReadAllText(_messagePath, Encoding.Default) + _R.ToString(), _r.Val);

            if (___e != _e.Val)
                WriteLine("Signature confirmed!");
            else
                WriteLine("Signature not confirmed!");
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
            Console.WriteLine("Chose step:");
            Console.WriteLine("1. Create common parameters;");
            Console.WriteLine("2. Create private key;");
            Console.WriteLine("3. Create public key;");
            Console.WriteLine("4. Create signature;");
            Console.WriteLine("5. Verify signature...");

            for (; ; )
            {

            }
        }
    }
}
