using System.Collections.Concurrent;
using System.Drawing;
using System.Text.Json;

var baseCrystalDirectory = @"";

var concurrentBag = new ConcurrentBag<ColourMetadata>();

// Should just filter here but cbf atm
var files = Directory.GetFiles(baseCrystalDirectory, "*", SearchOption.AllDirectories);

Parallel.ForEach(files, filepath =>
{
    ProcessFile(filepath);
});

//foreach (var file in files)
//{
//    ProcessFile(file);
//}

Console.WriteLine(concurrentBag.Count);

var colourDictionary = new Dictionary<Colour, HashSet<string>>();

foreach (var colourMetadata in concurrentBag)
{
    if (colourDictionary.ContainsKey(colourMetadata.Colour))
    {
        colourDictionary[colourMetadata.Colour].Add(colourMetadata.Filepath);
    }
    else
    {
        colourDictionary.Add(colourMetadata.Colour, new HashSet<string> { colourMetadata.Filepath });
    }
}

var colourFilesList = colourDictionary.Select(kvp => new ColourFiles
{
    Colour = kvp.Key,
    Files = kvp.Value.ToList()
}).ToList();

var options = new JsonSerializerOptions
{
    WriteIndented = true
};

var json = JsonSerializer.Serialize(colourFilesList, options);

Console.WriteLine($"Total colours: {colourDictionary.Count}");
Console.WriteLine($"Total files: {colourDictionary.Values.Sum(x => x.Count)}");


void ProcessFile(string filepath)
{
    if (filepath.Contains(@"\docs\"))
    {
        return;
    }

    if (Path.GetExtension(filepath) == ".pal" || Path.GetExtension(filepath) == ".asm")
    {
        GetColoursFromFile(filepath);
    }
    else if (Path.GetExtension(filepath) == ".png")
    {
        // These are the greyscale tilesets pngs that get the palettes applied to them
        if (filepath.Contains(@"\gfx\tilesets")
            || filepath.Contains(@"\gfx\sprites")
            || filepath.Contains(@"\gfx\player")
            || filepath.Contains(@"\gfx\intro")
            || filepath.Contains(@"\gfx\title")
            || filepath.Contains(@"\gfx\mobile")
            || filepath.Contains(@"\gfx\battle_anims")
            || filepath.Contains(@"\gfx\unown_puzzle")
            || filepath.Contains(@"\gfx\trainer_card")
            || filepath.Contains(@"\gfx\footprints")
            || filepath.Contains(@"\gfx\overworld")
            || filepath.Contains(@"\gfx\trade")
            || filepath.Contains(@"\gfx\diploma")
            || filepath.Contains(@"\gfx\evo")
            || filepath.Contains(@"\gfx\pokegear")
            || filepath.Contains(@"\gfx\pokedex")
            || filepath.Contains(@"\gfx\pack")
            || filepath.Contains(@"\gfx\credits")
            || filepath.Contains(@"\gfx\battle")
            || filepath.Contains(@"\gfx\stats")
            || filepath.Contains(@"\gfx\slots")
            || filepath.Contains(@"\gfx\emotes")
            || filepath.Contains(@"\gfx\font")
            || filepath.Contains(@"\gfx\pc")
            || filepath.Contains(@"\gfx\egg")
            || filepath.Contains(@"\gfx\printer")
            || filepath.Contains(@"\gfx\card_flip")
            || filepath.Contains(@"\gfx\memory_game")
            || filepath.Contains(@"\gfx\mystery_gift")
            || filepath.Contains(@"\gfx\debug")
            || filepath.Contains(@"\gfx\splash")
            || filepath.Contains(@"\gfx\sgb")
            || filepath.Contains(@"\gfx\mail")
            || filepath.Contains(@"\gfx\frames")
            || filepath.Contains(@"\gfx\new_game")
            || filepath.Contains(@"\gfx\naming_screen")
            || filepath.Contains(@"\gfx\icons"))
        {
            return;
        }

        var image = Image.FromFile(filepath);
        var bitmap = new Bitmap(image);
        for (var x = 0; x < bitmap.Width; x++)
        {
            for (var y = 0; y < bitmap.Height; y++)
            {
                var pixel = bitmap.GetPixel(x, y);
                var colour = new Colour(pixel.R, pixel.G, pixel.B);
                concurrentBag.Add(new ColourMetadata(colour, filepath.Replace(baseCrystalDirectory, "")));
            }
        }
    }
}

// Could clean up, but it does the job
void GetColoursFromFile(string filepath)
{
    var lines = File.ReadAllLines(filepath);

    Span<Range> rawColours = stackalloc Range[3];

    foreach (var line in lines)
    {
        var lineSpan = line.AsSpan().Trim();

        if (lineSpan.StartsWith("RGB", StringComparison.CurrentCulture))
        {
            var trimmedSpan = lineSpan.Trim()[4..];

            // pokecrystal/engine/movie/intro.asm does programmatic fades
            if (trimmedSpan.IndexOf("hue") != -1)
            {
                continue;
            }
            else if (filepath.EndsWith(@"\credits\credits.pal"))
            {
                // The credits use a different colour format
                var colours = trimmedSpan.Split(' ');

                foreach (var colour in colours)
                {
                    trimmedSpan[colour].Split(rawColours, ',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    var red = (int.Parse(trimmedSpan[rawColours[0]]) * 255) / 31;
                    var green = (int.Parse(trimmedSpan[rawColours[1]]) * 255) / 31;
                    var blueString = trimmedSpan[rawColours[2]].EndsWith(',')
                        ? trimmedSpan[rawColours[2]][..^1]
                        : trimmedSpan[rawColours[2]];
                    var blue = (int.Parse(blueString) * 255) / 31;
                    concurrentBag.Add(new ColourMetadata(new Colour(red, green, blue), filepath.Replace(baseCrystalDirectory, "")));
                }
            }
            else if (trimmedSpan.Contains(';'))
            {
                var commentRemovedSpan = trimmedSpan[..trimmedSpan.IndexOf(';')];

                var colours = commentRemovedSpan.Split(' ');

                foreach (var colour in colours)
                {
                    commentRemovedSpan[colour].Split(rawColours, ',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    var red = (int.Parse(commentRemovedSpan[rawColours[0]]) * 255) / 31; ;
                    var green = (int.Parse(commentRemovedSpan[rawColours[1]]) * 255) / 31; ;
                    var blueString = commentRemovedSpan[rawColours[2]].EndsWith(',')
                        ? commentRemovedSpan[rawColours[2]][..^1]
                        : commentRemovedSpan[rawColours[2]];
                    var blue = (int.Parse(blueString) * 255) / 31; ;
                    concurrentBag.Add(new ColourMetadata(new Colour(red, green, blue), filepath.Replace(baseCrystalDirectory, "")));
                }
            }
            else
            {
                trimmedSpan.Split(rawColours, ',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                // Extra trims due to stray tabs
                var red = (int.Parse(trimmedSpan[rawColours[0]]) * 255) / 31; ;
                var green = (int.Parse(trimmedSpan[rawColours[1]]) * 255) / 31; ;
                var blue = (int.Parse(trimmedSpan[rawColours[2]]) * 255) / 31; ;
                concurrentBag.Add(new ColourMetadata(new Colour(red, green, blue), filepath.Replace(baseCrystalDirectory, "")));
            }
        }
    }
}

public record Colour(int Red, int Green, int Blue);
public record ColourMetadata(Colour Colour, string Filepath);

// ended up making a new class anyway for serialisaton
public class ColourFiles
{
    public Colour Colour { get; set; }
    public List<string> Files { get; set; }
}