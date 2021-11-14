Extremely WIP and barely of the description, but it's an improved UWP port of KrasiF's sorting algos, a WPF program.

# Guide for adding your own algorithms
- Taking odd-even as a reference, add the lines `selectedArr = new int[] { i };` and `AddHistorySnap();` at the bottom of either each or the needed loops. "i" in the first one could be whichever first variable is declared in the loop if it's not a regular "for" loop.
- Add a new ComboBoxItem at the bottom of the combo box for the algorithms in the XAML, and then go to the code-behind (the .xaml.cs file) and into `Visualize_Click(...)`, and add a new "case" with the number after the above. For example, if your new algorithm is the 6th, then add below the last `break;`:
```csharp
case 6:
  OddEvenSort(arr, arr.Length);
  DrawHistory();
  break;
```
- ...The function that calls the algorithm should have `arr` as the argument that refers to the array to be sorted, and, if there's any, `arr.Length` as the argument that refers to the length of that list, and just `0` as the argument that refers to the start of the list.
- It doesn't matter if the algorithm is declared as a void or int or int[], or public or private. What matters is that it shouldn't be static or async, or else errors would occur and you'll need to do some complicated workarounds in order to get the program to accept static and/or async algorithms.
