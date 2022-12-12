using System;
using System.Linq;
using System.Text;
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
                    case StepType.CREATE_SIGNATURE:
                        CreateSignature();
                        break;
                    case StepType.VERIFY_SIGNATURE:
                        VerifySignature();
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
                FileName = _pythonExePath,
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
                var input = File.ReadAllLines(@"..\..\..\..\Протокол\" + _publicParametersPath);
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
            DeleteFilesByNames(_openKeyPath, _signaturePath);

            var _p = new ParametersEl(0, "p");
            var _A = new ParametersEl(0, "A");
            var _r = new ParametersEl(0, "r");
            var Q = new ParametersEl(0, 0, "Q");
            var _l = new ParametersEl(0, "l");

            try
            {
                var input = File.ReadAllLines(@"..\..\..\..\Протокол\" + _publicParametersPath);
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
                var input = File.ReadAllLines(@"..\..\..\..\Протокол\" +  _publicParametersPath);
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
                    
                BigInteger e = GetHash(File.ReadAllText(@"..\..\..\..\Протокол\" + _messagePath, Encoding.Default) + _R.ToString(), _r.Val);

                if (e == 0)
                    continue;

                BigInteger s = ((_l.Val * e) % _r.Val + k) % _r.Val;

                OutToFile(_signaturePath, new ParametersEl(e, "e"), new ParametersEl(s, "s"));

                WriteLine("Signature was created successfuly!");
                WriteLine();

                return;
            }
        }

        private static void VerifySignature()
        {
            ParametersEl _p = new ParametersEl(0, "p"), _A = new ParametersEl(0, "A"), _r = new ParametersEl(0, "r"), _Q = new ParametersEl(0, 0, "Q"), 
                _P = new ParametersEl(0, 0, "P"), _s = new ParametersEl(0, "s"), _e = new ParametersEl(0, "e");

            try
            {
                var input = File.ReadAllLines(@"..\..\..\..\Протокол\" + _publicParametersPath);
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

            BigInteger ___e = GetHash(File.ReadAllText(@"..\..\..\..\Протокол\" + _messagePath, Encoding.Default) + _R.ToString(), _r.Val);

            if (___e != _e.Val)
                WriteLine("Signature confirmed!");
            else
                WriteLine("Signature not confirmed!");
            WriteLine();
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
            WriteLine("4. Create signature;");
            WriteLine("5. Verify signature;");
            WriteLine("0. Exit program...");
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
                        return StepType.CREATE_SIGNATURE;
                    case ConsoleKey.D5:
                    case ConsoleKey.NumPad5:
                        return StepType.VERIFY_SIGNATURE;
                    case ConsoleKey.D0:
                    case ConsoleKey.NumPad0:
                        return StepType.NONE;
                    default:
                        continue;
                }
            }
        }
    }
}
