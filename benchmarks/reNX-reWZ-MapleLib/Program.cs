// This program is copyright angelsl, 2011 to 2013 inclusive.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using reNX;
using reNX.NXProperties;
using reWZ;
using reWZ.WZProperties;

namespace NETNXBench {
    internal static class Program {
        private const string NXFILE = @"D:\Misc\Development\NX\PKG4.nx";
        private const string WZFILE = @"D:\Misc\Development\NX\PKG1.wz";
        private static readonly TextWriter _outf;

        static Program() {
            _outf = Console.Out;
            Console.SetOut(Console.Error);
        }

        private static void Main(string[] args) {
            _outf.WriteLine("Name\t75%t\tM50%\tBest");
            switch (args.Length > 0 ? args[0].ToLower() : null) {
                case "renx":
                    ReNX.Benchmark();
                    return;
                case "rewz":
                    ReWZ.Benchmark();
                    return;
                case "ml":
                    //MLBenchmark();
                    return;
                default:
                    Console.WriteLine("Usage: NETNXBench.exe <lib>; lib is one of reNX, reWZ, or ML.");
                    return;
            }
        }

        private static void Test(Benchmark t, Func<long> c, uint trials, Action prepare = null, Action postpare = null) {
            var tickres = new long[trials];
            long best = long.MaxValue;
            PinPosition();
            if (prepare != null) prepare();
            for (uint i = 0; i < trials; ++i) {
                long cur = 0;
                cur = (tickres[i] = c());
                if (best > cur) best = cur;
                SnapWrite("{0,2:G}: {1,4}/{2,-4}; C{3,8} B{4,8}", t, i + 1, trials, TTMS(cur), TTMS(best));
            }
            if (postpare != null) postpare();
            Array.Sort(tickres);
            _outf.WriteLine("{0:G}\t{1}\t{2}\t{3}", t, TTMS(tickres[trials*3/4]),
                            TTMS(tickres.Slice(trials*1/4, trials*3/4).Average()), TTMS(tickres[0]));
        }

        private static void StepTest(Benchmark t, Func<long> c, uint trials, uint step, Action prepare = null,
                                     Action postpare = null) {
            var tickres = new long[trials];
            PinPosition();
            if (prepare != null) prepare();
            for (uint i = 0; i < trials;) {
                for (uint j = 0; j < step && i < trials; ++j, ++i) tickres[i] = c();
                SnapWrite("{0,2:G}: {1,4}/{2,-4}; C{3,8} B{4,8}", t, i, trials, "N/A", "N/A");
            }
            if (postpare != null) postpare();
            Array.Sort(tickres);
            SnapWrite("{0,2:G}: {1,4}/{1,-4}; C{2,8} B{3,8}", t, trials, "N/A", TTMS(tickres[0]));
            _outf.WriteLine("{0:G}\t{1}\t{2}\t{3}", t, TTMS(tickres[trials*3/4]),
                            TTMS(tickres.Slice(trials*1/4, trials*3/4).Average()), TTMS(tickres[0]));
        }

        private static class ReNX {
            private static NXFile _f;

            public static void Benchmark() {
                StepTest(Program.Benchmark.Ld, Load, 0x1000, 0x200);
                Test(Program.Benchmark.Re, Recurse, 0x80, prepare: () => _f = new NXFile(NXFILE),
                     postpare: () => _f.Dispose());
                Test(Program.Benchmark.LR, LoadRecurse, 0x10);
                Test(Program.Benchmark.SA, SearchAll, 0x40, prepare: () => _f = new NXFile(NXFILE),
                     postpare: () => _f.Dispose());
                Test(Program.Benchmark.De, Decompress, 0x5, prepare: () => { _f = new NXFile(NXFILE); },
                     postpare: () => _f.Dispose());
            }

            private static long Load() {
                long dur = Stopwatch.GetTimestamp();
                _f = new NXFile(NXFILE);
                dur = Stopwatch.GetTimestamp() - dur;
                _f.Dispose();
                return dur;
            }

            private static long LoadRecurse() {
                long dur = Stopwatch.GetTimestamp();
                _f = new NXFile(NXFILE);
                RecurseHelper(_f.BaseNode);
                dur = Stopwatch.GetTimestamp() - dur;
                _f.Dispose();
                return dur;
            }

            private static void RecurseHelper(NXNode n) {
                foreach (NXNode m in n) RecurseHelper(m);
            }

            private static long Recurse() {
                long dur = Stopwatch.GetTimestamp();
                RecurseHelper(_f.BaseNode);
                return Stopwatch.GetTimestamp() - dur;
            }

            private static long SearchAll() {
                long dur = Stopwatch.GetTimestamp();
                SearchHelper(_f.BaseNode);
                return Stopwatch.GetTimestamp() - dur;
            }

            private static void SearchHelper(NXNode n) {
                foreach (NXNode m in n) {
                    if (n[m.Name] == m) SearchHelper(m);
                    else throw new InvalidOperationException("Dictionary index failed.");
                }
            }

            private static long Decompress() {
                long dur = Stopwatch.GetTimestamp();
                DecompressHelper(_f.BaseNode);
                dur = Stopwatch.GetTimestamp() - dur;
                DisposeHelper(_f.BaseNode);
                return dur;
            }

            private static void DecompressHelper(NXNode n) {
                var b = n as NXBitmapNode;
                if (b != null) {
                    Bitmap x = b.Value;
                }
                foreach (NXNode m in n) DecompressHelper(m);
            }

            private static void DisposeHelper(NXNode n) {
                var b = n as NXBitmapNode;
                if (b != null) b.Dispose();
                foreach (NXNode m in n) DisposeHelper(m);
            }
        }

        private static class ReWZ {
            private static WZFile _f;

            public static void Benchmark() {
                StepTest(Program.Benchmark.Ld, Load, 0x400, 0x40);
                Test(Program.Benchmark.Re, Recurse, 0x80,
                     prepare: () => _f = new WZFile(WZFILE, WZVariant.Classic, false), postpare: () => _f.Dispose());
                Test(Program.Benchmark.LR, LoadRecurse, 0x4);
                Test(Program.Benchmark.SA, SearchAll, 0x40,
                     prepare: () => _f = new WZFile(WZFILE, WZVariant.Classic, false), postpare: () => _f.Dispose());
                Test(Program.Benchmark.De, Decompress, 0x4, prepare: () =>
                {
                    _f = new WZFile(WZFILE, WZVariant.Classic, false);
                    RecurseHelper(_f.MainDirectory);
                }, postpare: () => _f.Dispose());
            }

            private static long Load() {
                long dur = Stopwatch.GetTimestamp();
                _f = new WZFile(WZFILE, WZVariant.Classic, false);
                dur = Stopwatch.GetTimestamp() - dur;
                _f.Dispose();
                return dur;
            }

            private static long LoadRecurse() {
                long dur = Stopwatch.GetTimestamp();
                _f = new WZFile(WZFILE, WZVariant.Classic, false, WZReadSelection.EagerParseImage);
                RecurseHelper(_f.MainDirectory);
                dur = Stopwatch.GetTimestamp() - dur;
                _f.Dispose();
                return dur;
            }

            private static void RecurseHelper(WZObject wzo) {
                foreach (WZObject m in wzo) RecurseHelper(m);
            }

            private static long Recurse() {
                long dur = Stopwatch.GetTimestamp();
                RecurseHelper(_f.MainDirectory);
                return Stopwatch.GetTimestamp() - dur;
            }

            private static long SearchAll() {
                long dur = Stopwatch.GetTimestamp();
                SearchHelper(_f.MainDirectory);
                return Stopwatch.GetTimestamp() - dur;
            }

            private static void SearchHelper(WZObject n) {
                foreach (WZObject m in n) {
                    if (n[m.Name] == m) SearchHelper(m);
                    else throw new InvalidOperationException("Dictionary index failed.");
                }
            }

            private static long Decompress() {
                long dur = Stopwatch.GetTimestamp();
                DecompressHelper(_f.MainDirectory);
                dur = Stopwatch.GetTimestamp() - dur;
                DisposeHelper(_f.MainDirectory);
                return dur;
            }

            private static void DecompressHelper(WZObject n) {
                var b = n as WZCanvasProperty;
                if (b != null) {
                    Bitmap x = b.Value;
                }
                foreach (WZObject m in n) DecompressHelper(m);
            }

            private static void DisposeHelper(WZObject n) {
                var b = n as WZCanvasProperty;
                if (b != null) b.Dispose();
                foreach (WZObject m in n) DisposeHelper(m);
            }
        }

        #region MapleLib

        private static void MLLoad(Stopwatch sw) {}
        private static void MLLoadRecurse(Stopwatch sw) {}
        private static void MLRecurse(Stopwatch sw) {}
        private static void MLSearchAll(Stopwatch sw) {}

        #endregion

        #region P/Invoke Windows

        private static readonly IntPtr _stderr = GetStdHandle(-12);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCursorPosition(IntPtr hConsoleOutput, COORD cursorPosition);

        internal struct COORD {
            internal short X;
            internal short Y;
        }

        #endregion

        #region Console and other Miscellaneous Methods

        private static COORD _curPos;

        private static long TTMS(long t) {
            return t*1000000/Stopwatch.Frequency;
        }

        private static double TTMS(double t) {
            return t*1000000/Stopwatch.Frequency;
        }

        private static void PinPosition() {
            _curPos = new COORD {X = (short)Console.CursorLeft, Y = (short)Console.CursorTop};
        }

        private static void SnapWrite(string fstr, params object[] args) {
            SetConsoleCursorPosition(_stderr, _curPos);
            Console.WriteLine(fstr, args);
        }

        private static T[] Slice<T>(this T[] arr, uint indexFrom, uint indexTo) {
            uint length = 1 + indexTo - indexFrom;
            var result = new T[length];
            Array.Copy(arr, indexFrom, result, 0, length);

            return result;
        }

        [Flags]
        internal enum Benchmark {
            Ld = 0x1,
            LR = 0x2,
            Re = 0x4,
            SA = 0x8,
            De = 0x10
        }

        #endregion
    }
}
