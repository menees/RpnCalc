# ![Icon](http://www.menees.com/Images/RpnCalc3.png) RPN Calc
This is an [RPN](https://en.wikipedia.org/wiki/Reverse_Polish_notation) calculator for Windows desktop similar to an old [HP48](https://en.wikipedia.org/wiki/HP_48_series) calculator. It supports binary/octal/decimal/hexadecimal values, large integers, fractions, doubles, complex numbers, DateTimes, and TimeSpans.

RPN Calc is a WPF app, and it requires either the .NET Framework or .NET Core. There is no installer. Just download the zip, extract it to a folder, and run RpnCalc.exe.

![RPN Calc](http://www.menees.com/Images/RpnCalc3Screen.png)

## Data Entry
| Data Type | Entry Value | Display Value | Entry Format |
| --- | --- | --- | --- |
| Binary | #10101b | # 10101b | HP-style using # prefix and b suffix |
| Hexadecimal | #ABCDh | # ABCDh | HP-style using # prefix and h suffix |
| Hexadecimal | 0xABCD | # ABCDh | C-style using 0x prefix |
| Fraction | 1_2 | 1/2 | Underscore-separated numerator and denominator |
| Fraction | 1_3_4 | 1 3/4 | Underscore-separated whole part, numerator, and denominator |
| Complex | (1,3) | (1,3) | Rectangular |
| Complex | (2,@45) | (2,@45) | Polar using the current angle mode (e.g., degrees or radians) |
| DateTime | "9/11/10 4:51pm" | 9/11/2010 4:51:00 PM | Double quoted date/time using the current culture's formatting rules |
| TimeSpan | 19:01 | 00:19:01 | -d.hh:mm:ss.fff where the sign, days, hours, and milliseconds are optional |