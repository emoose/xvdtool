using System;
using Xunit;
using LibXboxOne.Keys;

namespace LibXboxOne.Tests
{
    public class DurangoKeysTests
    {
        [Fact]
        public void TestOdkIndexEnumConversion()
        {
            var redSuccess0 = DurangoKeys.GetOdkIndexFromString("RedOdk", out OdkIndex redODK);
            var redSuccess1 = DurangoKeys.GetOdkIndexFromString("redodk", out OdkIndex redODKlower);
            var redSuccess2 = DurangoKeys.GetOdkIndexFromString("2", out OdkIndex redODKnumber);
            var standardSuccess = DurangoKeys.GetOdkIndexFromString("0", out OdkIndex standardODKnumber);
            var unknownNumberSuccess = DurangoKeys.GetOdkIndexFromString("42", out OdkIndex unknownOdkNumber);

            var invalidNameFail = DurangoKeys.GetOdkIndexFromString("redodkblabla", out OdkIndex nameFail);

            Assert.True(redSuccess0);
            Assert.True(redSuccess1);
            Assert.True(redSuccess2);
            Assert.True(standardSuccess);
            Assert.True(unknownNumberSuccess);

            Assert.False(invalidNameFail);

            Assert.Equal(OdkIndex.RedOdk, redODK);
            Assert.Equal(OdkIndex.RedOdk, redODKlower);
            Assert.Equal(OdkIndex.RedOdk, redODKnumber);
            Assert.Equal((OdkIndex)42, unknownOdkNumber);

            Assert.Equal(OdkIndex.Invalid, nameFail);

        }
    }
}