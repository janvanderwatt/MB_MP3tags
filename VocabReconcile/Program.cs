using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;

var materialPath = args.Length > 0 ? args[0] : @"D:\LightCat Dropbox\Documents\Language\Chinese\mandarin blueprint\personal material\Chinese Material.xlsx";
var phrasesPath = args.Length > 1 ? args[1] : @"D:\LightCat Dropbox\Documents\Language\Chinese\Chinese Phrases.xlsm";
var outputPath = args.Length > 2 ? args[2] : @"D:\LightCat Dropbox\Documents\Language\Chinese\Chinese Phrases Updated.xlsm";
var outputIsSeparate = !SamePath(phrasesPath, outputPath);
var hasStagedOutput = outputIsSeparate && FileRetry.Run(() => File.Exists(outputPath));
var targetPath = hasStagedOutput ? outputPath : phrasesPath;

if (!FileRetry.Run(() => File.Exists(materialPath)))
{
    Console.Error.WriteLine($"Cannot find material workbook: {materialPath}");
    return 1;
}

if (!FileRetry.Run(() => File.Exists(phrasesPath)))
{
    Console.Error.WriteLine($"Cannot find phrases workbook: {phrasesPath}");
    return 1;
}

using var materialWorkbook = new SimpleExcelWorkbook(materialPath);

const int MaxEnglishColumns = 6;

List<List<string>> targetRows;
using (var targetWorkbook = new SimpleExcelWorkbook(targetPath))
{
    targetRows = targetWorkbook.ReadSheet("vocabulary");
}

IReadOnlyList<string> firstTargetRow = targetRows.Count > 0 ? targetRows[0] : [];
var targetHeaders = HeaderMap(firstTargetRow);
var targetChineseColumn = RequiredColumn(targetHeaders, "Chinese", "Chinese Phrases vocabulary");

var existingChinese = targetRows
    .Skip(1)
    .Select(row => Clean(GetCell(row, targetChineseColumn)))
    .Where(value => value.Length > 0)
    .ToHashSet(StringComparer.Ordinal);

var missing = ReadMaterialRows(materialWorkbook)
    .Where(row => row.Chinese.Length > 0)
    .Where(row => !existingChinese.Contains(row.Chinese))
    .DistinctBy(row => row.Chinese)
    .ToList();

if (missing.Count > 0)
{
    if (!SamePath(targetPath, outputPath))
    {
        FileRetry.Run(() => File.Copy(targetPath, outputPath, overwrite: true));
    }

    WorkbookUpdater.AppendVocabularyRows(outputPath, "vocabulary", missing, MaxEnglishColumns);
}

Console.WriteLine($"Updated rows: {missing.Count}");

var shouldPublishStagedWorkbook = outputIsSeparate && (hasStagedOutput || missing.Count > 0);
if (shouldPublishStagedWorkbook)
{
    var archivePath = PublishUpdatedWorkbook(phrasesPath, outputPath);
    Console.WriteLine($"Archived previous workbook: {Path.GetFileName(archivePath)}");
    Console.WriteLine($"Published workbook: {Path.GetFileName(phrasesPath)}");
}

return 0;

static IEnumerable<MaterialRow> ReadMaterialRows(SimpleExcelWorkbook workbook)
{
    foreach (var row in ReadMaterialSheet(workbook, "Traverse", "Traverse English", splitEnglishOnCommas: true))
    {
        yield return row;
    }
}

static IEnumerable<MaterialRow> ReadMaterialSheet(
    SimpleExcelWorkbook workbook,
    string sheetName,
    string englishHeader,
    bool splitEnglishOnCommas)
{
    var rows = workbook.ReadSheet(sheetName);
    if (rows.Count == 0)
    {
        yield break;
    }

    var headers = HeaderMap(rows[0]);
    var fromColumn = RequiredColumn(headers, "From", sheetName);
    var chineseColumn = RequiredColumn(headers, "Chinese", sheetName);
    var pinyinColumn = RequiredColumn(headers, "Pinyin", sheetName);
    var englishColumn = RequiredColumn(headers, englishHeader, sheetName);

    foreach (var row in rows.Skip(1))
    {
        var chinese = Clean(GetCell(row, chineseColumn));
        if (chinese.Length == 0)
        {
            continue;
        }

        var pinyin = Clean(GetCell(row, pinyinColumn));
        var english = Clean(GetCell(row, englishColumn));
        var introducedCode = ToIntroducedCode(Clean(GetCell(row, fromColumn)));
        IReadOnlyList<string> englishColumns = splitEnglishOnCommas
            ? SplitTraverseEnglish(english)
            : english.Length == 0 ? [] : [english];

        yield return new MaterialRow(sheetName, chinese, pinyin, englishColumns, introducedCode);
    }
}

static Dictionary<string, int> HeaderMap(IReadOnlyList<string> headers)
{
    var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < headers.Count; i++)
    {
        var header = Clean(headers[i]);
        if (header.Length > 0 && !map.ContainsKey(header))
        {
            map.Add(header, i);
        }
    }

    return map;
}

static int RequiredColumn(Dictionary<string, int> headers, string name, string sheetName)
{
    if (headers.TryGetValue(name, out var index))
    {
        return index;
    }

    throw new InvalidOperationException($"Sheet '{sheetName}' is missing required column '{name}'.");
}

static string GetCell(IReadOnlyList<string> row, int index) => index < row.Count ? row[index] : string.Empty;

static string Clean(string? value) => (value ?? string.Empty).Trim();

static bool SamePath(string left, string right) =>
    string.Equals(
        Path.GetFullPath(left),
        Path.GetFullPath(right),
        StringComparison.OrdinalIgnoreCase);

static string PublishUpdatedWorkbook(string currentPath, string updatedPath)
{
    return FileRetry.Run(
        () =>
        {
            var archivePath = NextRevisionPath(currentPath);
            File.Replace(updatedPath, currentPath, archivePath, ignoreMetadataErrors: true);
            return archivePath;
        });
}

static string NextRevisionPath(string currentPath)
{
    var directory = Path.GetDirectoryName(Path.GetFullPath(currentPath))
        ?? throw new InvalidOperationException($"Cannot determine directory for '{currentPath}'.");
    var baseName = Path.GetFileNameWithoutExtension(currentPath);
    var extension = Path.GetExtension(currentPath);
    var revisionPattern = new Regex(
        $"^{Regex.Escape(baseName)} v(?<revision>\\d+){Regex.Escape(extension)}$",
        RegexOptions.IgnoreCase);

    var existingRevisionFiles = FileRetry.Run(() => Directory
        .EnumerateFiles(directory, $"{baseName} v*{extension}")
        .ToList());

    var nextRevision = existingRevisionFiles
        .Select(path => revisionPattern.Match(Path.GetFileName(path)))
        .Where(match => match.Success)
        .Select(match => int.Parse(match.Groups["revision"].Value, CultureInfo.InvariantCulture))
        .DefaultIfEmpty(0)
        .Max() + 1;

    return Path.Combine(directory, $"{baseName} v{nextRevision:000}{extension}");
}

static IReadOnlyList<string> SplitTraverseEnglish(string value)
{
    if (value.Length == 0)
    {
        return [];
    }

    return value
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Select(ToDestinationEnglish)
        .ToList();
}

static string ToDestinationEnglish(string value)
{
    return Regex.Replace(
        value.Trim(),
        @"\s+\((adj|v|n|av|adv|prep|conj|pron|mw|num|part|aux|int|interj)\)(?=\s|$)",
        ":$1",
        RegexOptions.IgnoreCase);
}

static string ToIntroducedCode(string fromValue)
{
    var match = Regex.Match(fromValue, @"^\s*(\d+):.+:(\d+)\s*$");
    if (!match.Success)
    {
        return string.Empty;
    }

    var lesson = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    var part = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
    return $"mbLI{lesson:00}P{part}";
}

sealed record MaterialRow(
    string SourceSheet,
    string Chinese,
    string Pinyin,
    IReadOnlyList<string> EnglishColumns,
    string IntroducedCode);

static class FileRetry
{
    public static void Run(Action operation) =>
        Run(
            () =>
            {
                operation();
                return true;
            });

    public static T Run<T>(Func<T> operation)
    {
        const int maxAttempts = 12;
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return operation();
            }
            catch (Exception ex) when (IsRetriableFileException(ex) && attempt < maxAttempts)
            {
                Console.Error.WriteLine($"File is temporarily unavailable ({ex.GetType().Name}). Retrying in {delay.TotalSeconds:0}s...");
                Thread.Sleep(delay);
            }
        }
    }

    private static bool IsRetriableFileException(Exception ex) =>
        ex is IOException or UnauthorizedAccessException;
}

static class WorkbookUpdater
{
    private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static void AppendVocabularyRows(
        string workbookPath,
        string sheetName,
        IReadOnlyList<MaterialRow> rowsToAppend,
        int maxEnglishColumns)
    {
        using var archive = FileRetry.Run(() => ZipFile.Open(workbookPath, ZipArchiveMode.Update));
        var sheetPath = GetSheetPath(archive, sheetName);
        var sheetEntry = archive.GetEntry(sheetPath)
            ?? throw new InvalidOperationException($"Workbook is missing expected sheet part '{sheetPath}'.");

        XDocument worksheet;
        using (var stream = sheetEntry.Open())
        {
            worksheet = XDocument.Load(stream);
        }

        var sheetData = worksheet.Root!.Element(SpreadsheetNs + "sheetData")
            ?? throw new InvalidOperationException($"Sheet '{sheetName}' is missing sheetData.");

        var existingRows = sheetData.Elements(SpreadsheetNs + "row").ToList();
        var lastRowNumber = existingRows
            .Select(RowNumber)
            .DefaultIfEmpty(0)
            .Max();
        var styleByColumn = BuildStyleMap(existingRows);

        var nextRowNumber = lastRowNumber + 1;
        foreach (var materialRow in rowsToAppend.Where(row => row.Chinese.Length > 0))
        {
            sheetData.Add(CreateVocabularyRow(materialRow, nextRowNumber, maxEnglishColumns, styleByColumn));
            nextRowNumber++;
        }

        var finalRowNumber = nextRowNumber - 1;
        UpdateWorksheetRanges(worksheet, finalRowNumber);
        SaveEntry(sheetEntry, worksheet);
        RemoveCalcChain(archive);
    }

    private static XElement CreateVocabularyRow(
        MaterialRow materialRow,
        int rowNumber,
        int maxEnglishColumns,
        IReadOnlyDictionary<string, string> styleByColumn)
    {
        var row = new XElement(SpreadsheetNs + "row", new XAttribute("r", rowNumber));
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["A"] = materialRow.Chinese,
            ["B"] = materialRow.Pinyin,
            ["C"] = ToPinyinColour(materialRow.Pinyin),
            ["D"] = $"=LEN(C{rowNumber})",
            ["K"] = $"=COUNTIF($A:$A,A{rowNumber})",
            ["N"] = materialRow.IntroducedCode,
            ["S"] = $"=IFERROR(INDEX(props!$B:$B,MATCH(vocabulary!A{rowNumber},props!$A:$A,0)),\"-\")"
        };

        var englishValues = materialRow.EnglishColumns.Take(maxEnglishColumns).ToList();
        for (var i = 0; i < englishValues.Count; i++)
        {
            values[ColumnName(5 + i)] = englishValues[i];
        }

        for (var columnIndex = 1; columnIndex <= 19; columnIndex++)
        {
            var columnName = ColumnName(columnIndex);
            values.TryGetValue(columnName, out var value);
            row.Add(CreateCell(columnName, rowNumber, value, styleByColumn));
        }

        return row;
    }

    private static XElement CreateCell(
        string columnName,
        int rowNumber,
        string? value,
        IReadOnlyDictionary<string, string> styleByColumn)
    {
        var cell = new XElement(
            SpreadsheetNs + "c",
            new XAttribute("r", $"{columnName}{rowNumber}"));

        if (styleByColumn.TryGetValue(columnName, out var style))
        {
            cell.SetAttributeValue("s", style);
        }

        if (string.IsNullOrEmpty(value))
        {
            return cell;
        }

        if (value.StartsWith('='))
        {
            cell.Add(new XElement(SpreadsheetNs + "f", value[1..]));
            return cell;
        }

        cell.SetAttributeValue("t", "inlineStr");
        cell.Add(new XElement(
            SpreadsheetNs + "is",
            TextElement(value)));
        return cell;
    }

    private static XElement TextElement(string value)
    {
        var text = new XElement(SpreadsheetNs + "t", value);
        if (value.Length != value.Trim().Length)
        {
            XNamespace xml = XNamespace.Xml;
            text.SetAttributeValue(xml + "space", "preserve");
        }

        return text;
    }

    private static Dictionary<string, string> BuildStyleMap(IReadOnlyList<XElement> existingRows)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in existingRows.AsEnumerable().Reverse())
        {
            foreach (var cell in row.Elements(SpreadsheetNs + "c"))
            {
                var reference = (string?)cell.Attribute("r") ?? string.Empty;
                var columnName = Regex.Match(reference, "^[A-Z]+", RegexOptions.IgnoreCase).Value.ToUpperInvariant();
                var style = (string?)cell.Attribute("s");
                if (columnName.Length > 0 && !string.IsNullOrEmpty(style) && !result.ContainsKey(columnName))
                {
                    result[columnName] = style;
                }
            }
        }

        return result;
    }

    private static void UpdateWorksheetRanges(XDocument worksheet, int finalRowNumber)
    {
        var dimension = worksheet.Root!.Element(SpreadsheetNs + "dimension");
        dimension?.SetAttributeValue("ref", $"A1:S{finalRowNumber}");

        var autoFilter = worksheet.Root!.Element(SpreadsheetNs + "autoFilter");
        if (autoFilter is not null)
        {
            var currentRef = (string?)autoFilter.Attribute("ref") ?? "A1:S1";
            var firstCell = currentRef.Split(':', StringSplitOptions.TrimEntries)[0];
            autoFilter.SetAttributeValue("ref", $"{firstCell}:S{finalRowNumber}");
        }
    }

    private static void SaveEntry(ZipArchiveEntry entry, XDocument document)
    {
        using (var stream = entry.Open())
        {
            stream.SetLength(0);
            document.Save(stream);
        }
    }

    private static void RemoveCalcChain(ZipArchive archive)
    {
        archive.GetEntry("xl/calcChain.xml")?.Delete();
        var workbookRels = archive.GetEntry("xl/_rels/workbook.xml.rels");
        if (workbookRels is null)
        {
            return;
        }

        XDocument relationships;
        using (var stream = workbookRels.Open())
        {
            relationships = XDocument.Load(stream);
        }

        var calcChainRels = relationships.Root!
            .Elements(PackageRelationshipNs + "Relationship")
            .Where(rel => ((string?)rel.Attribute("Type") ?? string.Empty).EndsWith("/calcChain", StringComparison.Ordinal))
            .ToList();

        if (calcChainRels.Count == 0)
        {
            return;
        }

        foreach (var rel in calcChainRels)
        {
            rel.Remove();
        }

        SaveEntry(workbookRels, relationships);
    }

    private static string GetSheetPath(ZipArchive archive, string sheetName)
    {
        var workbookEntry = archive.GetEntry("xl/workbook.xml")
            ?? throw new InvalidOperationException("Workbook is missing xl/workbook.xml.");
        var relsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels")
            ?? throw new InvalidOperationException("Workbook is missing xl/_rels/workbook.xml.rels.");

        XDocument workbook;
        XDocument relationships;
        using (var workbookStream = workbookEntry.Open())
        {
            workbook = XDocument.Load(workbookStream);
        }

        using (var relsStream = relsEntry.Open())
        {
            relationships = XDocument.Load(relsStream);
        }

        var relationshipTargets = relationships
            .Root!
            .Elements(PackageRelationshipNs + "Relationship")
            .ToDictionary(
                rel => (string)rel.Attribute("Id")!,
                rel => NormalizeWorkbookTarget((string)rel.Attribute("Target")!),
                StringComparer.Ordinal);

        var sheet = workbook.Descendants(SpreadsheetNs + "sheet")
            .FirstOrDefault(item => string.Equals((string?)item.Attribute("name"), sheetName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Workbook does not contain a sheet named '{sheetName}'.");
        var relationshipId = (string?)sheet.Attribute(RelationshipNs + "id")
            ?? throw new InvalidOperationException($"Sheet '{sheetName}' is missing a relationship id.");

        return relationshipTargets.TryGetValue(relationshipId, out var path)
            ? path
            : throw new InvalidOperationException($"Sheet '{sheetName}' relationship target was not found.");
    }

    private static int RowNumber(XElement row)
    {
        var rowText = (string?)row.Attribute("r");
        return int.TryParse(rowText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rowNumber)
            ? rowNumber
            : 0;
    }

    private static string ColumnName(int oneBasedIndex)
    {
        var value = oneBasedIndex;
        var name = string.Empty;
        while (value > 0)
        {
            value--;
            name = (char)('A' + value % 26) + name;
            value /= 26;
        }

        return name;
    }

    private static string ToPinyinColour(string pinyin) => pinyin.Trim().Replace("-", "|", StringComparison.Ordinal);

    private static string NormalizeWorkbookTarget(string target)
    {
        var cleaned = target.Replace('\\', '/').TrimStart('/');
        return cleaned.StartsWith("xl/", StringComparison.Ordinal)
            ? cleaned
            : $"xl/{cleaned}";
    }
}

sealed class SimpleExcelWorkbook : IDisposable
{
    private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

    private readonly ZipArchive archive;
    private readonly List<string> sharedStrings;
    private readonly Dictionary<string, string> sheetPathsByName;

    public SimpleExcelWorkbook(string path)
    {
        archive = FileRetry.Run(() => ZipFile.OpenRead(path));
        sharedStrings = LoadSharedStrings();
        sheetPathsByName = LoadSheetPaths();
    }

    public List<List<string>> ReadSheet(string sheetName)
    {
        if (!sheetPathsByName.TryGetValue(sheetName, out var sheetPath))
        {
            throw new InvalidOperationException($"Workbook does not contain a sheet named '{sheetName}'.");
        }

        var entry = archive.GetEntry(sheetPath)
            ?? throw new InvalidOperationException($"Workbook is missing expected sheet part '{sheetPath}'.");

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        var rows = new List<List<string>>();

        foreach (var rowElement in document.Descendants(SpreadsheetNs + "row"))
        {
            var valuesByColumn = new Dictionary<int, string>();
            var lastColumn = -1;

            foreach (var cell in rowElement.Elements(SpreadsheetNs + "c"))
            {
                var reference = (string?)cell.Attribute("r") ?? string.Empty;
                var columnIndex = ColumnIndexFromReference(reference);
                if (columnIndex < 0)
                {
                    continue;
                }

                valuesByColumn[columnIndex] = ReadCell(cell);
                lastColumn = Math.Max(lastColumn, columnIndex);
            }

            var row = new List<string>();
            for (var column = 0; column <= lastColumn; column++)
            {
                row.Add(valuesByColumn.TryGetValue(column, out var value) ? value : string.Empty);
            }

            rows.Add(row);
        }

        return rows;
    }

    public void Dispose() => archive.Dispose();

    private List<string> LoadSharedStrings()
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        return document
            .Descendants(SpreadsheetNs + "si")
            .Select(ReadSharedString)
            .ToList();
    }

    private Dictionary<string, string> LoadSheetPaths()
    {
        var workbookEntry = archive.GetEntry("xl/workbook.xml")
            ?? throw new InvalidOperationException("Workbook is missing xl/workbook.xml.");
        var relsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels")
            ?? throw new InvalidOperationException("Workbook is missing xl/_rels/workbook.xml.rels.");

        using var workbookStream = workbookEntry.Open();
        using var relsStream = relsEntry.Open();
        var workbook = XDocument.Load(workbookStream);
        var relationships = XDocument.Load(relsStream)
            .Root!
            .Elements(PackageRelationshipNs + "Relationship")
            .ToDictionary(
                rel => (string)rel.Attribute("Id")!,
                rel => NormalizeWorkbookTarget((string)rel.Attribute("Target")!),
                StringComparer.Ordinal);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sheet in workbook.Descendants(SpreadsheetNs + "sheet"))
        {
            var name = (string?)sheet.Attribute("name");
            var relationshipId = (string?)sheet.Attribute(RelationshipNs + "id");
            if (name is null || relationshipId is null || !relationships.TryGetValue(relationshipId, out var target))
            {
                continue;
            }

            result[name] = target;
        }

        return result;
    }

    private static string NormalizeWorkbookTarget(string target)
    {
        var cleaned = target.Replace('\\', '/').TrimStart('/');
        return cleaned.StartsWith("xl/", StringComparison.Ordinal)
            ? cleaned
            : $"xl/{cleaned}";
    }

    private string ReadCell(XElement cell)
    {
        var type = (string?)cell.Attribute("t");

        if (string.Equals(type, "s", StringComparison.Ordinal))
        {
            var indexText = (string?)cell.Element(SpreadsheetNs + "v");
            return int.TryParse(indexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                && index >= 0
                && index < sharedStrings.Count
                ? sharedStrings[index]
                : string.Empty;
        }

        if (string.Equals(type, "inlineStr", StringComparison.Ordinal))
        {
            return string.Concat(cell.Descendants(SpreadsheetNs + "t").Select(text => text.Value));
        }

        return (string?)cell.Element(SpreadsheetNs + "v") ?? string.Empty;
    }

    private static string ReadSharedString(XElement sharedString)
    {
        var directText = sharedString.Element(SpreadsheetNs + "t");
        if (directText is not null)
        {
            return directText.Value;
        }

        return string.Concat(sharedString.Descendants(SpreadsheetNs + "t").Select(text => text.Value));
    }

    private static int ColumnIndexFromReference(string reference)
    {
        var letters = Regex.Match(reference, "^[A-Z]+", RegexOptions.IgnoreCase).Value.ToUpperInvariant();
        if (letters.Length == 0)
        {
            return -1;
        }

        var value = 0;
        foreach (var letter in letters)
        {
            value = value * 26 + letter - 'A' + 1;
        }

        return value - 1;
    }
}
