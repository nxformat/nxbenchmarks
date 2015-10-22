# NX Benchmark Specification

The following 5 benchmarks are the official benchmarks for NX libraries. All libraries that wish to be listed on the NX specification must implement these benchmarks.

Benchmarks are to be conducted on a Data.nx converted from the v40 Data.wz. The Data.nx **must** have a SHA256 hash of `3E64F1DB671D210ADAC3FE7B72B4508FA66F7DD495BD526071B44E32512F6F9C`.

###Load (Ld)
This benchmark measures the time taken to reach a state where all nodes can be **accessed**. Nodes need only be accessible, that is, they do not need to be parsed or loaded yet, but they must be accessible without any extra method calls.

This benchmark should be conducted for a maximum of 0x1000 trials.

The benchmark for a library must be equivalent to the code below. The load time is recorded as the time taken to complete the entire sequence as shown below.

```csharp
NXFile file = new NXFile("/path/to/file");
// ... methods to load file
```

After the end of the load, any node must be accessible and its value retrieved without any further method calls. For example, the following expression, or its equivalent, must result in `true`.

```csharp
file.BaseNode["String"]["Map.img"]["victoria"]["100000000"]["mapName"] == "Henesys"
```

###Recurse (Re)
This benchmark measures the time taken to recurse every node in a loaded and parsed file.

This benchmark should be conducted for a maximum of 0x100 trials.

The benchmark for a library must be equivalent to the code below. The recurse time is recorded as the time taken to complete the entire sequence as shown below.

```csharp
RecurseHelper(file.BaseNode);

static void RecurseHelper(NXNode n) {
    foreach (NXNode m in n) RecurseHelper(m);
}
```

...where `file` is a loaded and parsed NX file.

###Load and Recurse (LR)
This benchmark measures the time taken to recurse through every node in the file from scratch. It effectively tests the time taken to construct the entire node tree. For lazy-loaded libraries, it is equivalent to a parse benchmark.

This benchmark should be conducted for a maximum of 0x100 trials.

The benchmark for a library must be equivalent to the code below. The load and recurse time is recorded as the time taken to complete the entire sequence as shown below.

```csharp
NXFile file = new NXFile("/path/to/file");
// ... methods to load file
RecurseHelper(file.BaseNode);
```

...where `RecurseHelper` is the same method from the _Re_ benchmark above.

###Search All (SA)
This benchmark measures the time to lookup every node by name. All loading and parsing can be completed before this benchmark is run.

This benchmark should be conducted for a maximum of 0x100 trials.

The benchmark for a library must be equivalent to the code below. The search all time is recorded as the time taken to complete the entire sequence as shown below.

```csharp
StringRecurseHelper(file.BaseNode);

static void StringRecurseHelper(NXNode n) {
    foreach (NXNode m in n) 
        if (n[m.Name] == m) StringRecurseHelper(m);
        else throw new Exception("Fail!");
}
```

###Decompress All (De)
This benchmark measures the time to decompress every bitmap in the file. All loading and parsing can be completed before this benchmark is run, but no bitmaps should be decompressed before this benchmark is run.

This benchmark should be conducted for a maximum of 0x100 trials.

The benchmark for a library must be equivalent to the code below. The decompress all time is recorded as the time taken to complete the entire sequence as shown below.

```csharp
DecompressHelper(file.BaseNode);

static void DecompressHelper(NXNode n)
{
    NXBitmapNode b = n as NXBitmapNode;
    if (b != null) { Bitmap x = b.Value; }
    foreach (NXNode m in n) ReNXDecompressHelper(m);   
}
```


*Note that this code will result in memory leaks in reNX! The bitmaps must be disposed later on.*

##Reporting of Results
Benchmark results must be recorded in microseconds, using the appropriate method for each OS ([`CLOCK_MONOTONIC`][cm] in POSIX systems, and the [performance counter][hpc] in Windows).

[cm]: http://linux.die.net/man/3/clock_gettime
[hpc]: http://msdn.microsoft.com/en-us/library/windows/desktop/ms644904.aspx

The official benchmark tables will require library benchmarks to report the following 3 values for each benchmark. The descriptions assume that the array has been sorted in ascending order; `/` indicates integer division.

 * 75th percentile: element at index `length / 4`.
 * 50% mean: the mean of the slice from `length / 4` to `length * 3 / 4`, inclusive.
 * Best: element at index `0`.

Benchmarks must output the benchmark results in the following format to `stdout`.

```
Name\t75%t\tM50%\tBest
Ld\t123\t456\t789
Re\t123\t456\t789
LR\t123\t456\t789
SA\t123\t456\t789
De\t123\t456\t789
```

...where `\t` represents the `TAB` character.

`stdout` may not contain any other output. However, benchmarks are free to output any information to `stderr`.
