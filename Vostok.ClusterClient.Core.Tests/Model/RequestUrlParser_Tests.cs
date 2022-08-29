using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Tests.Model;

[TestFixture]
internal class RequestUrlParser_Tests
{
    [Test]
    public void Should_work_with_parameters()
    {
        var builder = new RequestUrlBuilder
        {
            "foo/", "bar/", "baz",
            {"a", 1},
            {"b", "2+2=5?"},
            {"=?=&=", "==!!=="}
        };

        var parser = new RequestUrlParser(builder.Build().OriginalString);

        parser.TryGetQueryParameter("a", out var value).Should().BeTrue();
        value.Should().Be("1");
        
        parser.TryGetQueryParameter("b", out value).Should().BeTrue();
        value.Should().Be("2+2=5?");
        
        parser.TryGetQueryParameter("=?=&=", out value).Should().BeTrue();
        value.Should().Be("==!!==");

        parser.TryGetQueryParameter("=+=", out value).Should().BeFalse();
        
        parser.TryGetQueryParameter(null, out value).Should().BeFalse();
    }
    
    [Test]
    public void Should_work_with_empty_parameters()
    {
        var parser = new RequestUrlParser("xxx?a=&b=x&c=&d=");

        parser.TryGetQueryParameter("a", out var value).Should().BeTrue();
        value.Should().Be("");
        
        parser.TryGetQueryParameter("b", out value).Should().BeTrue();
        value.Should().Be("x");
        
        parser.TryGetQueryParameter("c", out value).Should().BeTrue();
        value.Should().Be("");
        
        parser.TryGetQueryParameter("d", out value).Should().BeTrue();
        value.Should().Be("");
    }

    [Test]
    public void Should_work_with_parameters_without_value()
    {
        var parser = new RequestUrlParser("http://example.com/foo?bar");

        parser.TryGetQueryParameter("bar", out var value).Should().BeTrue();
        value.Should().Be("");
    }
    
    [Test]
    public void Should_work_without_parameters()
    {
        var builder = new RequestUrlBuilder
        {
            "foo/", "bar/", "baz"
        };

        var parser = new RequestUrlParser(builder.Build().OriginalString);

        parser.TryGetQueryParameter("x", out _).Should().BeFalse();
    }
    
    [Test]
    public void Should_work_without_url()
    {
        new RequestUrlParser(null).TryGetQueryParameter("x", out _).Should().BeFalse();
        new RequestUrlParser("").TryGetQueryParameter("x", out _).Should().BeFalse();
    }
    
    [Test]
    public void Should_not_throw_on_null_or_empty_keys()
    {
        new RequestUrlParser("http://example.com/foo?bar=xyz").TryGetQueryParameter("bar", out _).Should().BeTrue();
        new RequestUrlParser("http://example.com/foo?bar=xyz").TryGetQueryParameter(null, out _).Should().BeFalse();
        new RequestUrlParser("http://example.com/foo?bar=xyz").TryGetQueryParameter("", out _).Should().BeFalse();
    }
    
    [Test]
    public void Should_not_throw_on_incorrect_url()
    {
        new RequestUrlParser("??==??").TryGetQueryParameter("x", out _).Should().BeFalse();
        new RequestUrlParser("http://example.com/foo?").TryGetQueryParameter("x", out _).Should().BeFalse();
        new RequestUrlParser("http://example.com/foo?&").TryGetQueryParameter("x", out _).Should().BeFalse();
        new RequestUrlParser("http://example.com/foo&").TryGetQueryParameter("x", out _).Should().BeFalse();
    }
}