using Xunit;
using LibXboxOne.Keys;

namespace LibXboxOne.Tests
{
    public class DurangoKeysTests
    {
        [Fact]
        public void TestOdkIndexEnumConversion()
        {
            var redSuccess0 = DurangoKeys.GetOdkIndexFromString("RedOdk", out OdkIndex redOdk);
            var redSuccess1 = DurangoKeys.GetOdkIndexFromString("redodk", out OdkIndex redOdkLower);
            var redSuccess2 = DurangoKeys.GetOdkIndexFromString("2", out OdkIndex redOdkNumber);
            var standardSuccess = DurangoKeys.GetOdkIndexFromString("0", out OdkIndex standardOdkNumber);
            var unknownNumberSuccess = DurangoKeys.GetOdkIndexFromString("42", out OdkIndex unknownOdkNumber);

            var invalidNameFail = DurangoKeys.GetOdkIndexFromString("redodkblabla", out OdkIndex nameFail);

            Assert.True(redSuccess0);
            Assert.True(redSuccess1);
            Assert.True(redSuccess2);
            Assert.True(standardSuccess);
            Assert.True(unknownNumberSuccess);

            Assert.False(invalidNameFail);

            Assert.Equal(OdkIndex.RedOdk, redOdk);
            Assert.Equal(OdkIndex.RedOdk, redOdkLower);
            Assert.Equal(OdkIndex.RedOdk, redOdkNumber);
            Assert.Equal((OdkIndex)42, unknownOdkNumber);

            Assert.Equal(OdkIndex.Invalid, nameFail);

        }
    }
}