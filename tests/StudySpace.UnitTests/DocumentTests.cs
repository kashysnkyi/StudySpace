using System.ComponentModel.DataAnnotations;
using StudySpace.Core.Entities;

namespace StudySpace.UnitTests;

public sealed class DocumentTests
{
    [Fact]
    public void NewDocumentStartsAsDraft()
    {
        var document = new Document();

        Assert.NotEqual(Guid.Empty, document.Id);
        Assert.Equal(DocumentStatus.Draft, document.Status);
        Assert.Empty(document.Blocks);
    }

    [Fact]
    public void ShortTitleFailsValidation()
    {
        var document = new Document { Title = "No" };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            document,
            new ValidationContext(document),
            results,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Document.Title)));
    }
}

