using System;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
namespace gazegallery;
public static class FileNames {
 public static string Next(string path){string dir=Path.GetDirectoryName(path)!,ext=Path.GetExtension(path),stem=Path.GetFileNameWithoutExtension(path);var m=Regex.Match(stem,@"^(.*)_([0-9a-fA-F]+)$");BigInteger number=0;int digits=2;if(m.Success){stem=m.Groups[1].Value;number=BigInteger.Parse("0"+m.Groups[2].Value,System.Globalization.NumberStyles.HexNumber);digits=Math.Max(2,((m.Groups[2].Length+1)/2)*2);}string candidate;do{number++;string hex=number.ToString("X").TrimStart('0');if(hex.Length==0)hex="0";digits=Math.Max(digits,((hex.Length+1)/2)*2);candidate=Path.Combine(dir,stem+"_"+hex.PadLeft(digits,'0')+ext);}while(File.Exists(candidate));return candidate;}
 public static string Available(string path)=>File.Exists(path)?Next(path):path;
}
