using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

var input = args[0];
var output = args[1];

using var image = Image.Load(input);
using var frame = image.Frames.CloneFrame(0);
frame.SaveAsPng(output);
Console.WriteLine($"Saved first frame PNG {frame.Width}x{frame.Height}");
