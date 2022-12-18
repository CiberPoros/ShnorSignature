using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using BigMath;
using System.IO;
using static System.Console;

namespace ShnorSignature
{
    public class ParametersEl
    {
        public BigInteger Val { get; set; }
        public bool IsPoint { get; set; }
        public BigInteger X { get; set; }
        public BigInteger Y { get; set; }
        public string Name { get; set; }

        public ParametersEl(BigInteger val, string name)
        {
            Val = val;
            IsPoint = false;
            Name = name;
        }

        public ParametersEl(BigInteger x, BigInteger y, string name)
        {
            X = x;
            Y = y;
            IsPoint = true;
            Name = name;
        }

        public ParametersEl(F_int x, F_int y, string name)
        {
            if (x.IsNull())
            {
                X = -1;
                Y = -1;
            }
            else
            {
                X = x._val;
                Y = y._val;
            }

            IsPoint = true;
            Name = name;
        }

        public static bool ReadInFile(string fileName, __arglist)
        {
            string[] temp = new string[0]; 

            try { temp = File.ReadAllLines(@"..\..\..\..\Протокол\" + fileName, Encoding.Default); }
            catch { throw new Exception("File \"" + fileName + "\" does not exists!"); }

            ArgIterator iterator = new ArgIterator(__arglist);

            string s_out = "";
            while (iterator.GetRemainingCount() > 0)
            {
                TypedReference r = iterator.GetNextArg();

                int index = -1;
                for (int i = 0; i < temp.Length; i++)
                {
                    if (temp[i].Split(' ')[0] == __refvalue(r, ParametersEl).Name)
                    {
                        index = i;
                        break;
                    }
                }

                if (index == -1)
                    throw new Exception("Description of parameter " + __refvalue(r, ParametersEl).Name + " does not exists in file \"" + fileName + "\"");

                if (temp[index].Split(' ')[2].Split(';').Length == 2)
                {
                    __refvalue(r, ParametersEl).IsPoint = true;
                    __refvalue(r, ParametersEl).X = BigInteger.Parse(temp[index].Split(' ')[2].Split(';')[0].Trim(new char[] { '(', ')' }));
                    __refvalue(r, ParametersEl).Y = BigInteger.Parse(temp[index].Split(' ')[2].Split(';')[1].Trim(new char[] { '(', ')' }));
                }
                else
                {
                    __refvalue(r, ParametersEl).IsPoint = false;
                    __refvalue(r, ParametersEl).Val = BigInteger.Parse(temp[index].Split(' ')[2]);
                }

                s_out = s_out + __refvalue(r, ParametersEl).Name + ", ";
            }

            s_out = s_out.Substring(0, s_out.Length - 2);
            WriteLine($"Parameters {s_out} downloaded from file {fileName}");
            WriteLine();

            return true;
        }

        public static bool OutToFile(string fileName, params ParametersEl[] values)
        {
            var temp = new string[values.Length];
            var names = new string[values.Length];

            var s_out = "";
            for (int i = 0; i < values.Length; i++)
            { 
                temp[i] = values[i].ToString();
                names[i] = values[i].Name;
                s_out = s_out + values[i].Name + ", ";
            }

            var temp_read = new string[1];
            try
            {
                temp_read = File.ReadAllLines(@"..\..\..\..\Протокол\" + fileName, Encoding.Default);
            }
            catch
            {
                temp_read = new string[0];
            }

            var tempTo = new List<string>();

            for (int i = 0; i < temp_read.Length; i++)
            {
                if (temp_read[i].Length < 2)
                    continue;

                if (names.Contains(temp_read[i].Split(' ')[0]))
                    continue;

                tempTo.Add(temp_read[i]);
            }

            foreach (string s in temp)
                tempTo.Add(s);

            File.WriteAllLines(@"..\..\..\..\Протокол\" + fileName, tempTo.ToArray(), Encoding.Unicode);

            s_out = s_out.Substring(0, s_out.Length - 2);
            WriteLine($"Parameters {s_out} saved to file {fileName}");
            WriteLine();

            return true;
        }

        public override string ToString()
        {
            return !IsPoint 
                ? Name + " = " + Val.ToString() 
                : Name + " = (" + X.ToString() + ";" + Y.ToString() + ")";
        }
    }
}
