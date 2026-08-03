using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Tests;

public class FilenameBuilderTests
{
    [Fact]
    public void BuildFileName_With_All_Fields()
    {
        var name = FilenameBuilder.BuildFileName("007", "中三", "广场舞教学", "mp4");
        Assert.Equal("007 中三 广场舞教学.mp4", name);
    }

    [Fact]
    public void BuildFileName_Skips_Empty_Type()
    {
        var name = FilenameBuilder.BuildFileName("007", "", "广场舞教学", "MP4");
        Assert.Equal("007 广场舞教学.mp4", name);
    }

    [Fact]
    public void BuildFileName_Empty_Everything_Falls_Back()
    {
        Assert.Equal("未命名.mp3", FilenameBuilder.BuildFileName("", "", "", "mp3"));
    }

    [Fact]
    public void Sanitize_Replaces_Illegal_Chars()
    {
        Assert.Equal("a b c", FilenameBuilder.Sanitize("a/b\\c"));
        Assert.Equal("正常 标题", FilenameBuilder.Sanitize("  正常  标题  "));
    }

    [Fact]
    public void Truncate_Long_Title_Adds_Ellipsis()
    {
        var title = new string('长', 40);
        var result = FilenameBuilder.Truncate(title);
        Assert.Equal(31, result.Length);
        Assert.EndsWith("…", result);
    }

    [Fact]
    public void MakeUnique_Adds_Number_When_Exists()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dyui-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var first = FilenameBuilder.MakeUnique(dir, "007 中三 舞", "mp4");
            Assert.Equal("007 中三 舞.mp4", first);
            File.WriteAllText(Path.Combine(dir, first), "x");
            var second = FilenameBuilder.MakeUnique(dir, "007 中三 舞", "mp4");
            Assert.Equal("007 中三 舞（2）.mp4", second);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
