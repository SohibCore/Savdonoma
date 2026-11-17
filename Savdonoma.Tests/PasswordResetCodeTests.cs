using Savdonoma.Core.Licensing;

namespace Savdonoma.Tests;

public class PasswordResetCodeTests
{
    private static readonly (string Private, string Public) Keys = LicenseCrypto.GenerateKeyPair();
    private const string Machine = "AB12-CD34-EF56-7890";
    private static readonly DateOnly Today = new(2026, 11, 17);

    [Fact]
    public void Request_code_has_expected_format()
    {
        var code = PasswordResetCode.NewRequestCode();
        Assert.Matches("^[2-9A-Z]{4}-[2-9A-Z]{4}$", code);
        Assert.NotEqual(code, PasswordResetCode.NewRequestCode());
    }

    [Fact]
    public void Code_signed_today_is_valid()
    {
        var code = PasswordResetCode.Create(Machine, "ABCD-2345", Today, Keys.Private);
        Assert.True(PasswordResetCode.Verify(code, Machine, "ABCD-2345", Today, Keys.Public));
    }

    [Fact]
    public void Code_is_tolerant_to_case_spaces_and_dashes()
    {
        var code = PasswordResetCode.Create(Machine, "ABCD-2345", Today, Keys.Private);
        Assert.True(PasswordResetCode.Verify(" " + code + "\n", Machine.ToLowerInvariant(), "abcd2345", Today, Keys.Public));
    }

    [Fact]
    public void Code_from_yesterday_is_valid_but_older_is_not()
    {
        var yesterday = PasswordResetCode.Create(Machine, "ABCD-2345", Today.AddDays(-1), Keys.Private);
        var older = PasswordResetCode.Create(Machine, "ABCD-2345", Today.AddDays(-2), Keys.Private);

        Assert.True(PasswordResetCode.Verify(yesterday, Machine, "ABCD-2345", Today, Keys.Public));
        Assert.False(PasswordResetCode.Verify(older, Machine, "ABCD-2345", Today, Keys.Public));
    }

    [Fact]
    public void Code_for_other_machine_or_request_is_rejected()
    {
        var code = PasswordResetCode.Create(Machine, "ABCD-2345", Today, Keys.Private);

        Assert.False(PasswordResetCode.Verify(code, "0000-0000-0000-0000", "ABCD-2345", Today, Keys.Public));
        Assert.False(PasswordResetCode.Verify(code, Machine, "ABCD-2346", Today, Keys.Public));
    }

    [Fact]
    public void Code_signed_with_other_key_is_rejected()
    {
        var other = LicenseCrypto.GenerateKeyPair();
        var code = PasswordResetCode.Create(Machine, "ABCD-2345", Today, other.PrivateKey);
        Assert.False(PasswordResetCode.Verify(code, Machine, "ABCD-2345", Today, Keys.Public));
    }

    [Theory]
    [InlineData("")]
    [InlineData("salom")]
    [InlineData("!!!***")]
    [InlineData("A")]
    public void Garbage_is_rejected(string code)
        => Assert.False(PasswordResetCode.Verify(code, Machine, "ABCD-2345", Today, Keys.Public));
}
