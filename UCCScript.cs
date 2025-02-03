using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace UCCScriptRunner
{
    public struct UCCScriptInfo()
    {
        public string Name { get; set; } = "";
        public string Pattern { get; set; } = "";
        public string Result { get; set; } = "";
    }

    public static class Replace
    {
        public static string EscapeEscape(this string Base) => Regex.Replace(Base, @"\\(.)", m => m.Value switch { "\\n" => "\n", "\\t" => "\t", _ => m.Groups[1].Value });
    }
    partial class UCCScript
    {
        private readonly string Code = "";
        public UCCScriptInfo[] Group = [];
        public string First = "";
        private static readonly string[] item = [""];
        private static readonly List<UCCScriptRef> Memory = [];
        private static string pStr = "";
        public bool Success => Memory.Count != 0 && Memory[^1].Success && pStr == "";

        private UCCScript(string _Code) => Code = _Code;
        public static implicit operator UCCScript(string _Code) => new(_Code);
        private char? trim = null;

        public UCCScript TrimChar(char c)
        {
            trim = c;
            return this;
        }

        public void Compile()
        {
            bool invalid = false;
            string _P = Code;

            while (true)
            {
                switch (_P[0])
                {
                    case char c when _P.StartsWith("First"):
                        _P = string.Join("", _P[5..].SkipWhile(c => c == ' '));
                        First = string.Join("", _P.TakeWhile(c => c != ' '));
                        _P = string.Join("", _P[First.Length..].SkipWhile(c => c == ' '));
                        break;
                    case char c when c == '$':
                        _P = _P[1..];
                        Group = [.. Group.Append(new())];
                        Group[^1].Name = string.Join("", _P.TakeWhile(c => c != ' '));
                        _P = string.Join("", _P.Skip(Group[^1].Name.Length).SkipWhile(c => c == ' '));
                        if (!_P.StartsWith("In")) throw new Exception("'In' is missing.");
                        _P = string.Join("", _P[2..].SkipWhile(c => c == ' '));

                        while (invalid || !_P.StartsWith("Is"))
                        {
                            Group[^1].Pattern += _P[0];
                            if (_P[0] is '"') invalid = !invalid;
                            _P = _P[1..];
                        }
                        Group[^1].Pattern = string.Join("", Group[^1].Pattern.Reverse().SkipWhile(c => c == ' ').Reverse());
                        if (!_P.StartsWith("Is")) throw new Exception("'Is' is missing.");
                        _P = string.Join("", _P[2..].SkipWhile(c => c == ' '));

                        while (invalid || !_P.StartsWith("End"))
                        {
                            Group[^1].Result += _P[0];
                            if (_P[0] is '"') invalid = !invalid;
                            _P = _P[1..];
                        }
                        Group[^1].Result = string.Join("", Group[^1].Result.Reverse().SkipWhile(c => c == ' ').Reverse());
                        break;
                    default: throw new Exception("Unable to read program.");
                }
                if (!_P.StartsWith("End")) throw new Exception("'End' is missing.");
                _P = string.Join("", _P.Skip(3).SkipWhile(c => c is '\n' or '\r' or ' '));
                if (_P == "") break;
            }
            Array.ForEach(Group.Zip(Group.Select((_, i) => i)).ToArray(), t =>
            {
                string pat = "";
                bool ok = false;
                foreach (char c in t.First.Pattern)
                {
                    if (c == '"') ok = !ok;
                    pat += c == '[' && !ok ? "(" : (c == ']' && !ok ? "|)" : c);
                }
                Group[t.Second].Pattern = pat;
            });
        }

        public string Run(string Str)
        {
            pStr = Str;
            List<ushort> Temporary = [0];
            List<sbyte> Situation = [];
            List<ushort> deep = [0];

            string Invoke(string pName)
            {
                //try
                //{
                List<string> Pat = [Group.First(element => element.Name == pName).Pattern.Trim()];
                Memory.Add(new());
                int delete = 0;
                bool finish = false;
                Situation.Add(0);

                int deepCount = 0;

                string InRes = "";

                /// Pattern

                while (Pat[^1].Length != 0)
                {
                    if (finish) break;
                    switch (Pat[^1][0])
                    {
                        case char _ when Pat[^1].StartsWith("<("):
                            Situation.Add(1);
                            Temporary[^1]++;
                            Memory[^1].Ref.Add("");
                            delete = 2;
                            deep[^1]++;
                            deepCount++;
                            break;
                        case char _ when Pat[^1].StartsWith(")>"):
                            if (!Memory[^1].Success)
                            {
                                Situation.RemoveAt(Situation.Count - 1);
                                Memory[^1].Ref.RemoveAt(Memory[^1].Ref.Count - 1);
                            }
                            delete = 2;
                            deepCount--;
                            break;
                        case char _ when Pat[^1].StartsWith("<{"):
                            Situation.Add(2);
                            Temporary[^1]++;
                            if (Memory[^1].Ref.Count == deep[^1]) Memory[^1].Ref.Add(item);
                            else Memory[^1].Ref[deep[^1]] = ((string[])Memory[^1].Ref[deep[^1]]).Append("").ToArray();
                            delete = 2;
                            deep[^1]++;
                            deepCount++;
                            break;
                        case char _ when Pat[^1].StartsWith("}>"):
                            if (!Memory[^1].Success)
                            {
                                string[] array = (string[])Memory[^1].Ref[deep[^1] - 1];
                                Memory[^1].Ref.Insert(deep[^1], array[..^1]);
                                Situation.RemoveAt(Situation.Count - 1);
                            }
                            delete = 2;
                            deepCount--;
                            break;
                        case char _ when Pat[^1].StartsWith("<o"):
                            DeletePat(3 + Pat[^1][2..].TakeWhile(x => x == ' ').Count());
                            string pat = Regex.Match(Pat[^1], @"(\\.|.)[^""]+").Value.EscapeEscape();
                            if (pStr == "" || !pat.Any(x => x == pStr[0]))
                            {
                                finish = true;
                                continue;
                            }
                            else DeletePat(2 + pat.Length + Pat[^1][(1 + pat.Length)..].TakeWhile(x => x == ' ').Count());
                            break;
                        case char _ when Pat[^1].StartsWith("<x"):
                            DeletePat(3 + Pat[^1][2..].TakeWhile(x => x == ' ').Count());
                            pat = Regex.Match(Pat[^1], @"(\\.|[^""])+").Value.EscapeEscape();
                            if (pStr == "" || pat.Any(x => x == pStr[0]))
                            {
                                finish = true;
                                continue;
                            }
                            else DeletePat(2 + pat.Length + Pat[^1][(1 + pat.Length)..].TakeWhile(x => x == ' ').Count());
                            break;
                        case char _ when Pat[^1].StartsWith("<s"):
                            DeletePat(3 + Pat[^1][2..].TakeWhile(x => x == ' ').Count());
                            pat = Regex.Match(Pat[^1], @"((\\.|.)+)""").Groups[1].Value.EscapeEscape();
                            DeletePat(pat.Length + 2 + Pat[^1][pat.Length..].TakeWhile(x => x == ' ').Count());
                            string s = string.Join("", pStr.TakeWhile(x => pat.Contains(x)));
                            if (s != "")
                            {
                                Delete_pStr(s.Length);
                                if (deepCount > 0)
                                    if (Situation[^1] == 1)
                                        Memory[^1].Ref[deep[^1] - 1] += s;
                                    else Memory[^1].Ref[deep[^1] - 1] = ((string[])Memory[^1].Ref[deep[^1] - 1])[..^1].Append(((string[])Memory[^1].Ref[deep[^1] - 1])[^1] + s).ToArray();
                            }
                            break;
                        case char _ when Pat[^1].StartsWith("<ns"):
                            DeletePat(3 + Pat[^1][2..].TakeWhile(x => x == ' ').Count());
                            pat = Regex.Match(Pat[^1], @"((\\.|.)+)""").Groups[1].Value.EscapeEscape();
                            DeletePat(pat.Length + 2 + Pat[^1][(pat.Length + 1)..].TakeWhile(x => x == ' ').Count());
                            s = string.Join("", pStr.TakeWhile(x => !pat.Contains(x)));
                            if (s != "")
                            {
                                Delete_pStr(s.Length);
                                if (deepCount > 0)
                                    if (Situation[^1] == 1)
                                        Memory[^1].Ref[deep[^1] - 1] += s;
                                    else Memory[^1].Ref[deep[^1] - 1] = ((string[])Memory[^1].Ref[deep[^1] - 1])[..^1].Append(((string[])Memory[^1].Ref[deep[^1] - 1])[^1] + s).ToArray();
                            }
                            break;
                        case char _ when Pat[^1].StartsWith("<\""):
                            DeletePat(2);
                            Delete_pStr(pStr.TakeWhile(x => x == Pat[^1][0]).Count());
                            DeletePat(2);
                            break;
                        case '<':
                            DeletePat(1 + Pat[^1][1..].TakeWhile(x => x == ' ').Count());
                            string patName = Regex.Match(Pat[^1], @"([A-Za-z0-9_]+)\s*>").Groups[1].Value;
                            DeletePat(1 + patName.Length + Pat[^1][patName.Length..].TakeWhile(x => x == ' ').Count());
                            Memory.Add(new());
                            deep.Add(0);
                            string res = Invoke(patName);
                            Memory.RemoveAt(Memory.Count - 1);
                            deep.RemoveAt(deep.Count - 1);
                            if (deepCount > 0)
                                if (Situation[^1] == 1) Memory[^1].Ref[deep[^1] - 1] += res;
                                else Memory[^1].Ref[deep[^1] - 1] = ((string[])Memory[^1].Ref[deep[^1] - 1])[..^1].Append(((string[])Memory[^1].Ref[deep[^1] - 1])[^1] + res).ToArray();
                            break;
                        case '(':
                            DeletePat(1);
                            Temporary.Add(0);
                            Memory.Add(Memory[^1]);
                            Pat.Add(Pat[^1]);
                            deep.Add(deep[^1]);
                            break;
                        case '|':
                            DeletePat(1);
                            if (Memory[^1].Success)
                            {
                                int count = 1;
                                bool isdq = false;
                                while (count != 0 || isdq)
                                {
                                    if (Pat[^1][0] == '"') isdq = !isdq;
                                    if (Pat[^1][0] == '(') count++;
                                    else if (Pat[^1][0] == ')') count--;
                                    DeletePat(1);
                                }
                                Temporary.RemoveAt(Temporary.Count - 1);
                                Memory[^2] = new() { Success = Memory[^1].Success, Ref = [.. Memory[^2].Ref.Concat(Memory[^1].Ref)] };
                                Memory.RemoveAt(Memory.Count - 1);
                                Pat[^2] = Pat[^1];
                                Pat.RemoveAt(Pat.Count - 1);
                                deep.RemoveAt(deep.Count - 1);
                            }
                            else
                            {
                                if (Temporary[^1] != 0) Memory[^1].Ref.RemoveRange(Memory[^1].Ref.Count - Temporary[^1], Temporary[^1]);
                                Memory.RemoveAt(Memory.Count - 1);
                                Memory.Add(Memory[^1]);
                                Temporary.Add(0);
                                int index = Pat[^1].Length;
                                Pat.RemoveAt(Pat.Count - 1);
                                DeletePat(Pat[^1].Length - index);
                                Pat.Add(Pat[^1]);
                                deep.RemoveAt(deep.Count - 1);
                                deep.Add(deep[^1]);
                            }
                            break;
                        case ')':
                            DeletePat(1);
                            break;
                        case '{':
                            DeletePat(1);
                            Pat.Add(Pat[^1]);
                            deep.Add(deep[^1]);
                            break;
                        case '}':
                            if (Memory[^1].Success)
                            {
                                deep[^1] -= Temporary[^1];
                                Pat.RemoveAt(Pat.Count - 1);
                                Pat.Add(Pat[^1]);
                                deep.RemoveAt(deep.Count - 1);
                                deep.Add(deep[^1]);
                            }
                            else
                            {
                                DeletePat(1);
                                int index = Pat[^1].Length;
                                Pat.RemoveAt(Pat.Count - 1);
                                DeletePat(Pat[^1].Length - index);
                                Memory[^1] = new() { Ref = Memory[^1].Ref, Success = true };
                            }
                            _ = deep;
                            break;
                        case '"':
                            DeletePat(1);
                            string str = Regex.Match(Pat[^1], @"((\\.|[^""""])*)").Groups[1].Value;
                            int len = str.Length;
                            str = str.EscapeEscape();
                            if (!pStr.StartsWith(str) || !Memory[^1].Success)
                                Memory[^1] = new() { Success = false, Ref = Memory[^1].Ref };
                            else
                            {
                                Delete_pStr(str.Length);
                                if (deepCount > 0)
                                    if (Situation[^1] == 1)
                                        Memory[^1].Ref[deep[^1] - 1] += str;
                                    else Memory[^1].Ref[deep[^1] - 1] = ((string[])Memory[^1].Ref[deep[^1] - 1])[..^1].Append(((string[])Memory[^1].Ref[deep[^1] - 1])[^1] + str).ToArray();
                            }
                            DeletePat(len + 1);
                            break;
                        case char _ when Pat[^1].StartsWith("Number"):
                            s = "";
                            DeletePat(6);
                            if (Memory[^1].Success && (new Func<Match, int>(m => new dynamic[] { s = m.Value, m.Index }[1]))(Regex.Match(pStr, @"[0-9]+")) == 0)
                            {
                                Delete_pStr(s.Length);
                                if (deepCount > 0)
                                    if (Situation[^1] == 1) Memory[^1].Ref[deep[^1] - 1] += s;
                                    else Memory[^1].Ref[deep[^1] - 1] = ((string[])Memory[^1].Ref[deep[^1] - 1])[..^1].Append(((string[])Memory[^1].Ref[deep[^1] - 1])[^1] + s).ToArray();
                            }
                            else Memory[^1] = new() { Success = false, Ref = Memory[^1].Ref };
                            break;
                        case char _ when Pat[^1].StartsWith("Char"):
                            DeletePat(4);
                            if (!Memory[^1].Success) Memory[^1] = new() { Ref = Memory[^1].Ref, Success = false };
                            else if (pStr != "")
                            {
                                char c = pStr[0];

                                if (deepCount > 0)
                                    if (Situation[^1] == 1) Memory[^1].Ref[deep[^1] - 1] += c;
                                    else Memory[^1].Ref[deep[^1] - 1] = ((string[])Memory[^1].Ref[deep[^1] - 1])[..^1].Append(((string[])Memory[^1].Ref[deep[^1] - 1])[^1] + c).ToArray();
                                Delete_pStr(1);
                            }
                            break;
                        case char _ when Pat[^1].StartsWith("String"):
                            DeletePat(6);
                            if (!Memory[^1].Success) Memory[^1] = new() { Success = false, Ref = Memory[^1].Ref };
                            else
                            {
                                s = "";
                                if ((new Func<Match, int>(m => new dynamic[] { s = m.Value, m.Index }[1]))(Regex.Match(pStr, @"[^""'\s]+")) == 0)
                                {
                                    Delete_pStr(s.Length);

                                    if (deepCount > 0)
                                        if (Situation[^1] == 1) Memory[^1].Ref[deep[^1] - 1] += s;
                                        else Memory[^1].Ref[deep[^1] - 1] = ((string[])Memory[^1].Ref[deep[^1] - 1])[..^1].Append(((string[])Memory[^1].Ref[deep[^1] - 1])[^1] + s).ToArray();
                                }
                                else Memory[^1] = new() { Success = false };
                            }
                            break;
                    }
                    DeletePat(delete);
                    delete = 0;
                    if (Pat[^1].StartsWith(' ')) DeletePat(Pat[^1].TakeWhile(x => x == ' ').Count());
                    if (trim is char ch && pStr.StartsWith(ch)) Delete_pStr(pStr.TakeWhile(x => x == ' ').Count());
                    if (Pat[^1] == "") break;
                }
                void DeletePat(int index) => Pat[^1] = Pat[^1][index..];
                finish = false;

                /// Result

                string ResP = Group.First(element => element.Name == pName).Result.Trim();

                if (Memory[^1].Success)
                    InRes = string.Join("", Calculation(ResP));

                string ret = Memory[^1].Success ? InRes : "";
                return ret;
                //}
                //catch
                //{
                //    return "";
                //}
            }
            void Delete_pStr(int index) => pStr = pStr[index..];
            return Invoke(First);
        }

        private static readonly Stack<double> VVar = new();

        private static dynamic[] Calculation(string _formula)
        {
            List<string> formula = [_formula];
            Stack<dynamic> stack = new();
            Stack<bool> Conditions = new();
            Stack<sbyte> ControlType = new(); // 1 = If, 2 = For, 3 = Switch
            Stack<sbyte> ProcessType = new();

            OrderedDictionary<string, double> LVar = [];
            string LastVName = "";
            
            // @0[] => ] = 1, @1[] => ] = 2, $0[] => ] = 3, $1[] => ] = 4, Condition=']'=> 5, Skip='[' => 6, LoopBase=']' => 7, LoopCheck='[' => 8, LoopUpdate=']' => 9
            // now -> 19


            Stack<(double Base, double Last, double Inc)> Loop = new();
            Stack<int> SaveElement = new();

            int depth = 0;

            int Index() => formula[^1].Select((_, i) => i).First(x => Regex.Replace(formula[^1][..(x + 1)], @"""(\\.|.)*""", "").Count(x => x == ']') - Regex.Replace(formula[^1][..(x + 1)], @"""(\\.|.)*""", "").Count(x => x == '[') == 1);

            while (formula[^1] != "")
            {
                switch (formula[^1][0])
                {
                    case '@' when formula[^1].StartsWith("@0"):
                        formula[^1] = formula[^1][2..];
                        ProcessType.Push(1);
                        break;
                    case '@' when formula[^1].StartsWith("@1"):
                        formula[^1] = formula[^1][2..];
                        ProcessType.Push(2);
                        break;
                    case '$' when formula[^1].StartsWith("$0+"):
                        formula[^1] = formula[^1][3..];
                        ProcessType.Push(14);
                        break;
                    case '$' when formula[^1].StartsWith("$0"):
                        formula[^1] = formula[^1][2..];
                        ProcessType.Push(3);
                        break;
                    case '$' when formula[^1].StartsWith("$1"):
                        formula[^1] = formula[^1][2..];
                        ProcessType.Push(4);
                        break;
                    case '?':
                        formula[^1] = formula[^1][1..];
                        ProcessType.Push(15);
                        break;
                    case '(':
                        depth++;
                        formula[^1] = formula[^1][1..];
                        ControlType.Push(0);
                        break;
                    case ')':
                        depth--;
                        formula[^1] = formula[^1][1..];
                        _ = ControlType.Pop();
                        break;
                    case char _ when formula[^1].StartsWith("If") && depth > 0 && ControlType.Peek() == 0:
                        _ = ControlType.Pop();
                        ControlType.Push(1);
                        formula[^1] = formula[^1][2..];
                        ProcessType.Push(5);
                        break;
                    case char _ when formula[^1].StartsWith("ElseIf") && depth > 0 && ControlType.Peek() == 1:
                        formula[^1] = formula[^1][6..];
                        ProcessType.Push((sbyte)(Conditions.Peek() ? 0 : 5));
                        if (!Conditions.Peek()) _ = Conditions.Pop();
                        else
                        {
                            ProcessType.Push(6);
                            ProcessType.Push(6);
                        }
                        break;
                    case char _ when formula[^1].StartsWith("Else") && depth > 0 && ControlType.Peek() == 1:
                        formula[^1] = formula[^1][4..];
                        ProcessType.Push((sbyte)(Conditions.Peek() ? 6 : 0));
                        _ = Conditions.Pop();
                        break;
                    case char _ when formula[^1].StartsWith("ForB") && depth > 0 && ControlType.Peek() == 0:
                        _ = ControlType.Pop();
                        ControlType.Push(3);
                        formula[^1] = formula[^1][4..];
                        ProcessType.Push(10);
                        break;
                    case char _ when formula[^1].StartsWith("For") && depth > 0 && ControlType.Peek() == 0:
                        _ = ControlType.Pop();
                        ControlType.Push(2);
                        formula[^1] = formula[^1][3..];
                        ProcessType.Push(7);
                        break;
                    case char _ when formula[^1].StartsWith("While") && depth > 0 && ControlType.Peek() == 0:
                        _ = ControlType.Pop();
                        ControlType.Push(4);
                        formula[^1] = formula[^1][5..];
                        ProcessType.Push(16);
                        break;
                    case char _ when formula[^1].StartsWith("Load") && depth > 0 && ControlType.Peek() == 0:
                        _ = ControlType.Pop();
                        ControlType.Push(5);
                        formula[^1] = formula[^1][(4 + formula[^1][4..].TakeWhile(c => c == ' ').Count())..];
                        LastVName = Regex.Match(formula[^1], @"[^\s[]+").Value;
                        formula[^1] = formula[^1][LastVName.Length..];
                        ProcessType.Push(19);
                        break;
                    case ']':
                        formula[^1] = formula[^1][1..];
                        if (!ProcessType.TryPeek(out _)) break;
                        switch (ProcessType.Peek())
                        {
                            case 0:
                                _ = ProcessType.Pop();
                                break;
                            case 1:
                                double _d = stack.Pop();
                                ushort index = unchecked((ushort)_d);
                                stack.Push((string)Memory[^1].Ref[index]);
                                _ = ProcessType.Pop();
                                break;
                            case 2:
                                _d = stack.Pop();
                                double _d2 = stack.Pop();
                                index = unchecked((ushort)_d);
                                ushort index2 = unchecked((ushort)_d2);
                                stack.Push(((string[])Memory[^1].Ref[index2])[index]);
                                _ = ProcessType.Pop();
                                break;
                            case 3:
                                _d = stack.Pop();
                                index = unchecked((ushort)_d);
                                stack.Push(((string)Memory[^1].Ref[index]).Length);
                                _ = ProcessType.Pop();
                                break;
                            case 4:
                                _d = stack.Pop();
                                _d2 = stack.Pop();
                                index = unchecked((ushort)_d);
                                index2 = unchecked((ushort)_d2);
                                stack.Push(((string[])Memory[^1].Ref[index2])[index].Length);
                                _ = ProcessType.Pop();
                                break;
                            case 5:
                                bool condition = stack.Pop();
                                Conditions.Push(condition);
                                _ = ProcessType.Pop();
                                if (condition) ProcessType.Push(0);
                                else ProcessType.Push(6);
                                break;
                            case 7:
                                double _inc = stack.Pop();
                                double _last = stack.Pop();
                                double _base = stack.Pop();
                                Loop.Push((_base, _last, _inc));
                                VVar.Push(_base);
                                _ = ProcessType.Pop();
                                ProcessType.Push(8);
                                formula.Add(formula[^1]);
                                break;
                            case 9:
                                VVar.Push(VVar.Pop() + Loop.Peek().Inc);
                                if (Loop.Peek().Inc >= 0 ? Loop.Peek().Last >= VVar.Peek() : VVar.Peek() > Loop.Peek().Last)
                                {
                                    formula.RemoveAt(formula.Count - 1);
                                    formula.Add(formula[^1]);
                                }
                                else _ = ProcessType.Pop();
                                break;
                            case 10:
                                _inc = stack.Pop();
                                _base = stack.Pop();
                                Loop.Push((_base, _inc >= 0 ? double.MaxValue : double.MinValue, _inc));
                                VVar.Push(_base);
                                _ = ProcessType.Pop();
                                ProcessType.Push(11);
                                break;
                            case 13:
                                VVar.Push(VVar.Pop() + Loop.Peek().Inc);
                                formula.RemoveAt(formula.Count - 1);
                                formula.Add(formula[^1]);
                                _ = ProcessType.Pop();
                                ProcessType.Push(12);
                                break;
                            case 14:
                                _d = stack.Pop();
                                index = unchecked((ushort)_d);
                                stack.Push(((string[])Memory[^1].Ref[index]).Length);
                                _ = ProcessType.Pop();
                                break;
                            case 15:
                                _d = stack.Pop();
                                index = unchecked((ushort)_d);
                                if (Memory[^1].Ref.Count > index) stack.Push(true);
                                else stack.Push(false);
                                _ = ProcessType.Pop();
                                break;
                            case 18:
                                formula.RemoveAt(formula.Count - 1);
                                formula.Add(formula[^1]);
                                _ = ProcessType.Pop();
                                ProcessType.Push(17);
                                break;
                            case 19:
                                if (LVar.ContainsKey(LastVName)) LVar[LastVName] = stack.Pop();
                                else LVar.Add(LastVName, stack.Pop());
                                _ = ProcessType.Pop();
                                break;
                        }
                        break;
                    case '[':
                        formula[^1] = formula[^1][1..];
                        if (!ProcessType.TryPeek(out _)) break;
                        switch (ProcessType.Peek())
                        {
                            case 0:
                                _ = ProcessType.Pop();
                                break;
                            case 6:
                                int index = Index();
                                formula[^1] = formula[^1][(index + 1)..];
                                _ = ProcessType.Pop();
                                break;
                            case 8:
                                _ = ProcessType.Pop();
                                if (Loop.Peek().Inc >= 0 ? Loop.Peek().Last - Loop.Peek().Base < 0 : Loop.Peek().Base - Loop.Peek().Last > 0)
                                {
                                    index = Index();
                                    formula[^1] = formula[^1][(index + 1)..];
                                }
                                else ProcessType.Push(9);
                                break;
                            case 11:
                                formula.Add(formula[^1]);
                                _ = ProcessType.Pop();
                                ProcessType.Push(12);
                                break;
                            case 12:
                                _ = ProcessType.Pop();
                                if (!stack.Pop())
                                {
                                    index = Index();
                                    formula[^1] = formula[^1][(index + 1)..];
                                }
                                else ProcessType.Push(13);
                                break;
                            case 16:
                                formula.Add(formula[^1]);
                                _ = ProcessType.Pop();
                                ProcessType.Push(17);
                                break;
                            case 17:
                                _ = ProcessType.Pop();
                                bool condition = stack.Pop();
                                if (!condition)
                                {
                                    index = Index();
                                    formula[^1] = formula[^1][(index + 1)..];
                                }
                                else ProcessType.Push(18);
                                break;
                        }
                        break;
                    case '`' when VVar.Count > 0:
                        stack.Push(VVar.Peek());
                        formula[^1] = formula[^1][1..];
                        break;
                    case '"':
                        formula[^1] = formula[^1][1..];
                        string str = Regex.Match(formula[^1], @"(\\.|[^""])*").Value;
                        stack.Push(str.EscapeEscape());
                        formula[^1] = formula[^1][(str.Length + 1)..];
                        break;
                    case 'l':
                        stack.Push($"{stack.Pop()}".Length);
                        formula[^1] = formula[^1][1..];
                        break;
                    case '#':
                        formula[^1] = formula[^1][1..];
                        switch (stack.Pop())
                        {
                            case string:
                                stack.Push("@str");
                                break;
                            case double:
                                stack.Push("@num");
                                break;
                            case bool:
                                stack.Push("@bool");
                                break;
                        }
                        break;
                    case char _ when formula[^1].StartsWith("Num"):
                        formula[^1] = formula[^1][3..];
                        stack.Push("@num");
                        break;
                    case char _ when formula[^1].StartsWith("Str"):
                        formula[^1] = formula[^1][3..];
                        stack.Push("@str");
                        break;
                    case char _ when formula[^1].StartsWith("Bool"):
                        formula[^1] = formula[^1][4..];
                        stack.Push("@bool");
                        break;
                    case char _ when formula[^1].StartsWith("True"):
                        formula[^1] = formula[^1][4..];
                        stack.Push(true);
                        break;
                    case char _ when formula[^1].StartsWith("False"):
                        formula[^1] = formula[^1][5..];
                        stack.Push(false);
                        break;
                    case char c when Regex.IsMatch(c.ToString(), @"[0-9]"):
                        double d = double.Parse(Regex.Match(formula[^1], @"[0-9]+(\.[0-9]+)?").Value);
                        stack.Push(d);
                        formula[^1] = formula[^1][d.ToString().Length..];
                        break;
                    case '+':
                        dynamic dynamic = stack.Pop();
                        stack.Push(stack.Pop() + dynamic);
                        formula[^1] = formula[^1][1..];
                        break;
                    case '-':
                        dynamic = stack.Pop();
                        stack.Push(stack.Pop() - dynamic);
                        formula[^1] = formula[^1][1..];
                        break;
                    case '*':
                        dynamic = stack.Pop();
                        stack.Push(stack.Pop() * dynamic);
                        formula[^1] = formula[^1][1..];
                        break;
                    case '/':
                        dynamic = stack.Pop();
                        stack.Push(stack.Pop() / dynamic);
                        formula[^1] = formula[^1][1..];
                        break;
                    case '%':
                        dynamic = stack.Pop();
                        stack.Push(stack.Pop() % dynamic);
                        formula[^1] = formula[^1][1..];
                        break;
                    case '^':
                        dynamic = stack.Pop();
                        stack.Push(Math.Pow(stack.Pop(), dynamic));
                        formula[^1] = formula[^1][1..];
                        break;
                    case '=':
                        dynamic = stack.Pop();
                        stack.Push(stack.Pop() == dynamic);
                        formula[^1] = formula[^1][1..];
                        break;
                    case '!':
                        stack.Push(!stack.Pop());
                        formula[^1] = formula[^1][1..];
                        break;
                    case '<':
                        dynamic = stack.Pop();
                        stack.Push(stack.Pop() < dynamic);
                        formula[^1] = formula[^1][1..];
                        break;
                    case '>':
                        dynamic = stack.Pop();
                        stack.Push(stack.Pop() > dynamic);
                        formula[^1] = formula[^1][1..];
                        break;
                    case '&':
                        dynamic = stack.Pop();
                        stack.Push(stack.Pop() && dynamic);
                        formula[^1] = formula[^1][1..];
                        break;
                    case '|':
                        dynamic = stack.Pop();
                        stack.Push(stack.Pop() || dynamic);
                        formula[^1] = formula[^1][1..];
                        break;
                    case '~':
                        formula[^1] = formula[^1][1..];
                        string type = stack.Pop();
                        switch (type)
                        {
                            case "@num":
                                stack.Push(double.Parse($"{stack.Pop()}"));
                                break;
                            case "@str":
                                stack.Push($"{stack.Pop()}");
                                break;
                            case "@bool":
                                stack.Push(bool.Parse($"{stack.Pop()}"));
                                break;
                        }
                        break;
                    case '\\':
                        formula[^1] = formula[^1][1..];
                        string vName = Regex.Match(formula[^1], @"[^\s]+").Value;
                        formula[^1] = formula[^1][vName.Length..];
                        stack.Push(LVar[vName]);
                        break;
                    case ':':
                        formula[^1] = formula[^1][1..];
                        stack.Push(stack.Peek());
                        break;
                    default:
                        formula[^1] = formula[^1][1..];
                        break;
                }
                if (formula[^1].StartsWith(' ')) formula[^1] = formula[^1][formula[^1].TakeWhile(x => x == ' ').Count()..];
            }

            return stack.Reverse().ToArray();
        }
    }
    public struct UCCScriptRef()
    {
        public List<dynamic> Ref = [];
        public bool Success { get; set; } = true;
    }
}